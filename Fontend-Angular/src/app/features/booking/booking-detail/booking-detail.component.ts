import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { DatePipe, DecimalPipe } from '@angular/common';
import { BookingService } from '../../../core/services/booking.service';
import { TransactionService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { ReservationDetailsDto } from '../../../core/models/models';

declare var Razorpay: any;

@Component({
  selector: 'app-booking-detail',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule, DatePipe, DecimalPipe,
    MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatCardModule, MatProgressSpinnerModule, MatChipsModule
  ],
  templateUrl: './booking-detail.component.html',
  styleUrl: './booking-detail.component.scss'
})
export class BookingDetailComponent implements OnInit, OnDestroy {
  private route              = inject(ActivatedRoute);
  private bookingService     = inject(BookingService);
  private transactionService = inject(TransactionService);
  private toast              = inject(ToastService);
  private fb                 = inject(FormBuilder);

  reservation    = signal<ReservationDetailsDto | null>(null);
  showCancelForm = signal(false);
  isCancelling   = signal(false);
  isDownloading  = signal(false);
  isPaying       = signal(false);

  // Countdown timer for pending reservations
  timeLeft       = signal<string>('');
  isExpired      = signal(false);
  private timer: any;

  cancelForm = this.fb.group({
    reason: ['', [Validators.required, Validators.minLength(5)]],
  });

  ngOnInit() {
    const code = this.route.snapshot.paramMap.get('code') ?? '';
    this.bookingService.getReservationByCode(code).subscribe(r => {
      this.reservation.set(r);
      if (r.status === 'Pending' && r.expiryTime) {
        this.startCountdown(new Date(r.expiryTime));
      }
    });
    this.loadRazorpay();
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

  private startCountdown(expiry: Date) {
    const tick = () => {
      const now = new Date().getTime();
      const diff = expiry.getTime() - now;
      if (diff <= 0) {
        this.timeLeft.set('Expired');
        this.isExpired.set(true);
        clearInterval(this.timer);
        // Update local status to Cancelled
        this.reservation.update(r => r ? { ...r, status: 'Cancelled' } : r);
        return;
      }
      const mins = Math.floor(diff / 60000);
      const secs = Math.floor((diff % 60000) / 1000);
      this.timeLeft.set(`${mins}m ${secs}s`);
    };
    tick();
    this.timer = setInterval(tick, 1000);
  }

  canPayNow(res: ReservationDetailsDto): boolean {
    if (res.status !== 'Pending') return false;
    if (!res.expiryTime) return false;
    return new Date(res.expiryTime) > new Date();
  }

  payWithRazorpay() {
    const res = this.reservation();
    if (!res) return;

    const amountPaise = Math.round((res.finalAmount > 0 ? res.finalAmount : res.totalAmount) * 100);
    const upiId = res.upiId ?? '';

    const options = {
      key: 'rzp_test_SVtcM9b8whLPCh',
      amount: amountPaise,
      currency: 'INR',
      name: '🏨 StayHub',
      description: `Booking: ${res.reservationCode} — ${res.hotelName}`,
      prefill: { method: 'upi', vpa: upiId || undefined },
      notes: { reservationCode: res.reservationCode },
      theme: { color: '#2d3a8c' },
      handler: (response: any) => {
        this.isPaying.set(true);
        this.transactionService.createPayment({
          reservationId: res.reservationId,
          paymentMethod: 3,
        }).subscribe({
          next: () => {
            this.isPaying.set(false);
            this.toast.success('Payment successful! Booking confirmed.');
            this.reservation.update(r => r ? { ...r, status: 'Confirmed' } : r);
            if (this.timer) clearInterval(this.timer);
          },
          error: () => {
            this.isPaying.set(false);
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
        this.toast.error(`Payment failed: ${response.error?.description ?? 'Unknown error'}`);
      });
      rzp.open();
    } catch {
      this.toast.error('Razorpay failed to load. Please try again.');
    }
  }

  cancel() {
    if (this.cancelForm.invalid) { this.cancelForm.markAllAsTouched(); return; }
    this.isCancelling.set(true);
    const res = this.reservation()!;
    this.bookingService.cancelReservation(res.reservationCode, {
      reason: this.cancelForm.get('reason')!.value!,
    }).subscribe({
      next: () => {
        this.toast.success('Reservation cancelled. Refund request created if applicable.');
        this.reservation.update(r => r ? { ...r, status: 'Cancelled' } : r);
        this.showCancelForm.set(false);
        this.isCancelling.set(false);
        if (this.timer) clearInterval(this.timer);
      },
      error: () => this.isCancelling.set(false),
    });
  }

  async downloadPdf() {
    const res = this.reservation();
    if (!res) return;
    this.isDownloading.set(true);
    try {
      const { default: jsPDF } = await import('jspdf');
      const doc = new jsPDF();
      let y = 20;
      doc.setFontSize(18); doc.text('StayHub - Booking Confirmation', 20, y); y += 12;
      doc.setFontSize(12);
      doc.text(`Reservation Code: ${res.reservationCode}`, 20, y); y += 8;
      doc.text(`Hotel: ${res.hotelName}`, 20, y); y += 8;
      doc.text(`Room Type: ${res.roomTypeName}`, 20, y); y += 8;
      doc.text(`Check-in: ${res.checkInDate}`, 20, y); y += 8;
      doc.text(`Check-out: ${res.checkOutDate}`, 20, y); y += 8;
      doc.text(`Rooms: ${res.numberOfRooms}`, 20, y); y += 8;
      doc.text(`Status: ${res.status}`, 20, y); y += 12;
      doc.setFontSize(13); doc.text('Price Breakdown', 20, y); y += 8;
      doc.setFontSize(11);
      doc.text(`Base Amount: Rs.${res.totalAmount.toFixed(2)}`, 20, y); y += 7;
      if (res.gstAmount > 0) { doc.text(`GST (${res.gstPercent}%): Rs.${res.gstAmount.toFixed(2)}`, 20, y); y += 7; }
      if (res.discountAmount > 0) { doc.text(`Discount: -Rs.${res.discountAmount.toFixed(2)}`, 20, y); y += 7; }
      if (res.walletAmountUsed > 0) { doc.text(`Wallet Used: -Rs.${res.walletAmountUsed.toFixed(2)}`, 20, y); y += 7; }
      doc.setFontSize(13); doc.text(`Final Amount: Rs.${res.finalAmount.toFixed(2)}`, 20, y); y += 10;
      doc.setFontSize(10); doc.text(`Booked on: ${new Date(res.createdDate).toLocaleDateString()}`, 20, y);
      doc.save(`booking-${res.reservationCode}.pdf`);
      this.toast.success('PDF downloaded!');
    } catch { this.toast.error('PDF generation failed. Run: npm install jspdf'); }
    this.isDownloading.set(false);
  }

  statusClass(status: string): string {
    const map: Record<string, string> = {
      Pending: 'badge-warning', Confirmed: 'badge-success',
      Completed: 'badge-primary', Cancelled: 'badge-error', NoShow: 'badge-muted',
    };
    return map[status] ?? 'badge-muted';
  }

  canCancel(res: ReservationDetailsDto): boolean {
    return res.status === 'Pending' || res.status === 'Confirmed';
  }

  getRefundPreview(res: ReservationDetailsDto): string {
    const checkIn = new Date(res.checkInDate);
    const today = new Date(); today.setHours(0,0,0,0); checkIn.setHours(0,0,0,0);
    const days = Math.round((checkIn.getTime() - today.getTime()) / 86400000);
    if (days >= 5) return `You will receive ₹${(res.totalAmount * 0.5).toFixed(2)} refund (50% — 5+ days before check-in)`;
    if (days >= 3) return `You will receive ₹${(res.totalAmount * 0.25).toFixed(2)} refund (25% — 3–4 days before check-in)`;
    return 'No refund applicable — within 2 days of check-in';
  }
}
