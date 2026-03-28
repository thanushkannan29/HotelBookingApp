import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSortModule } from '@angular/material/sort';
import { DatePipe, DecimalPipe } from '@angular/common';
import { BookingService } from '../../../core/services/booking.service';
import { TransactionService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { ReservationDetailsDto } from '../../../core/models/models';

declare var Razorpay: any;

@Component({
  selector: 'app-booking-list',
  standalone: true,
  imports: [
    CommonModule, RouterLink, DatePipe, DecimalPipe,
    MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule,
    MatTableModule, MatPaginatorModule, MatTabsModule, MatSortModule,
    MatProgressSpinnerModule, MatChipsModule, MatTooltipModule,
  ],
  templateUrl: './booking-list.component.html',
  styleUrl: './booking-list.component.scss'
})
export class BookingListComponent implements OnInit, OnDestroy {
  private bookingService     = inject(BookingService);
  private transactionService = inject(TransactionService);
  private toast              = inject(ToastService);

  reservations  = signal<ReservationDetailsDto[]>([]);
  totalCount    = signal(0);
  loading       = signal(false);
  payingId      = signal<string | null>(null);
  expandedPayId = signal<string | null>(null);
  pageSize      = 10;
  currentPage   = 1;

  countdowns: Record<string, string> = {};
  private timer: any;

  displayedColumns = ['reservationCode', 'hotelName', 'checkIn', 'checkOut', 'amount', 'status', 'actions'];
  readonly statusTabs = ['All', 'Pending', 'Confirmed', 'Completed', 'Cancelled', 'NoShow'];
  selectedStatus = 'All';

  ngOnInit() {
    this.load();
    this.loadRazorpay();
    this.timer = setInterval(() => this.updateCountdowns(), 1000);
  }

  ngOnDestroy() {
    if (this.timer) clearInterval(this.timer);
  }

  private loadRazorpay() {
    if (typeof Razorpay !== 'undefined') return;
    const script = document.createElement('script');
    script.src = 'https://checkout.razorpay.com/v1/checkout.js';
    script.async = true;
    document.head.appendChild(script);
  }

  load() {
    this.loading.set(true);
    this.bookingService.getMyReservationsHistory(
      this.currentPage, this.pageSize, this.selectedStatus
    ).subscribe({
      next: (res) => {
        this.reservations.set(res.reservations as ReservationDetailsDto[]);
        this.totalCount.set(res.totalCount);
        this.loading.set(false);
        this.updateCountdowns();
      },
      error: () => this.loading.set(false)
    });
  }

  private updateCountdowns() {
    const now = new Date().getTime();
    const updated: Record<string, string> = {};
    for (const r of this.reservations()) {
      if (r.status === 'Pending' && r.expiryTime) {
        const diff = new Date(r.expiryTime).getTime() - now;
        if (diff > 0) {
          const mins = Math.floor(diff / 60000);
          const secs = Math.floor((diff % 60000) / 1000);
          updated[r.reservationId] = mins + 'm ' + secs + 's';
        } else {
          updated[r.reservationId] = 'Expired';
        }
      }
    }
    this.countdowns = updated;
  }

  canPayNow(res: ReservationDetailsDto): boolean {
    if (res.status !== 'Pending') return false;
    if (!res.expiryTime) return false;
    return new Date(res.expiryTime) > new Date();
  }

  getCountdown(res: ReservationDetailsDto): string {
    return this.countdowns[res.reservationId] ?? '';
  }

  togglePayment(res: ReservationDetailsDto) {
    this.expandedPayId.update(id => id === res.reservationId ? null : res.reservationId);
  }

  payWithRazorpay(res: ReservationDetailsDto) {
    const amountPaise = Math.round((res.finalAmount > 0 ? res.finalAmount : res.totalAmount) * 100);
    const upiId = res.upiId ?? '';
    const options = {
      key: 'rzp_test_SVtcM9b8whLPCh',
      amount: amountPaise,
      currency: 'INR',
      name: 'StayHub',
      description: 'Booking: ' + res.reservationCode,
      prefill: { method: 'upi', vpa: upiId || undefined },
      notes: { reservationCode: res.reservationCode },
      theme: { color: '#2d3a8c' },
      handler: (_response: any) => {
        this.payingId.set(res.reservationId);
        this.transactionService.createPayment({
          reservationId: res.reservationId,
          paymentMethod: 3,
        }).subscribe({
          next: () => {
            this.payingId.set(null);
            this.expandedPayId.set(null);
            this.toast.success('Payment successful! ' + res.reservationCode + ' confirmed.');
            this.load();
          },
          error: () => {
            this.payingId.set(null);
            this.toast.error('Payment recorded but confirmation failed. Contact support.');
          }
        });
      },
      modal: {
        ondismiss: () => {
          this.bookingService.recordFailedPayment(res.reservationId).subscribe();
          this.toast.error('Payment cancelled. You can retry before the reservation expires.');
        }
      }
    };
    try {
      const rzp = new Razorpay(options);
      rzp.on('payment.failed', (response: any) => {
        this.bookingService.recordFailedPayment(res.reservationId).subscribe();
        this.toast.error('Payment failed: ' + (response.error?.description ?? 'Unknown error'));
      });
      rzp.open();
    } catch {
      this.toast.error('Razorpay failed to load. Please open the booking to retry.');
    }
  }

  onTabChange(index: number) {
    this.selectedStatus = this.statusTabs[index];
    this.currentPage = 1;
    this.load();
  }

  onPage(e: PageEvent) {
    this.currentPage = e.pageIndex + 1;
    this.pageSize = e.pageSize;
    this.load();
  }

  statusClass(status: string): string {
    const map: Record<string, string> = {
      Pending: 'badge-warning', Confirmed: 'badge-success',
      Completed: 'badge-primary', Cancelled: 'badge-error', NoShow: 'badge-muted',
    };
    return map[status] ?? 'badge-muted';
  }

  statusEmoji(s: string): string {
    const m: Record<string, string> = {
      All: 'list', Pending: 'schedule', Confirmed: 'check_circle',
      Completed: 'emoji_events', Cancelled: 'cancel', NoShow: 'person_off'
    };
    return m[s] ?? 'info';
  }
}
