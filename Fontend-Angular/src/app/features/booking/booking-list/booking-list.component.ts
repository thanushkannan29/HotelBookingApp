import { Component, inject, signal, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { DatePipe, DecimalPipe } from '@angular/common';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { BookingService } from '../../../core/services/booking.service';
import { ReservationDetailsDto } from '../../../core/models/models';

@Component({
  selector: 'app-booking-list',
  standalone: true,
  imports: [
    CommonModule, RouterLink, DatePipe, DecimalPipe,
    MatButtonModule, MatIconModule,
    MatTableModule, MatPaginatorModule, MatTabsModule,
    MatProgressSpinnerModule, MatTooltipModule,
    MatFormFieldModule, MatInputModule,
  ],
  templateUrl: './booking-list.component.html',
  styleUrl: './booking-list.component.scss'
})
export class BookingListComponent implements OnInit, OnDestroy {
  private bookingService = inject(BookingService);
  private router         = inject(Router);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  reservations = signal<ReservationDetailsDto[]>([]);
  totalCount   = signal(0);
  loading      = signal(false);
  pageSize     = 10;
  currentPage  = 1;
  searchTerm   = '';

  countdowns: Record<string, string> = {};
  private timer: any;
  private searchSubject = new Subject<string>();

  displayedColumns = ['reservationCode', 'hotelName', 'checkIn', 'checkOut', 'amount', 'status', 'actions'];
  readonly statusTabs = ['All', 'Pending', 'Confirmed', 'Completed', 'Cancelled', 'NoShow'];
  selectedStatus = 'All';

  ngOnInit() {
    this.load();
    this.timer = setInterval(() => this.updateCountdowns(), 1000);
    this.searchSubject.pipe(debounceTime(400), distinctUntilChanged())
      .subscribe(s => { this.searchTerm = s; this.currentPage = 1; this.paginator?.firstPage(); this.load(); });
  }

  ngOnDestroy() {
    if (this.timer) clearInterval(this.timer);
    this.searchSubject.complete();
  }

  load() {
    this.loading.set(true);
    this.bookingService.getMyReservationsHistory(
      this.currentPage, this.pageSize, this.selectedStatus, this.searchTerm
    ).subscribe({
      next: res => {
        this.reservations.set(res.reservations as ReservationDetailsDto[]);
        this.totalCount.set(res.totalCount);
        this.loading.set(false);
        this.updateCountdowns();
      },
      error: () => this.loading.set(false)
    });
  }

  onSearch(value: string) { this.searchSubject.next(value); }

  private updateCountdowns() {
    const now = Date.now();
    const updated: Record<string, string> = {};
    for (const r of this.reservations()) {
      if (r.status === 'Pending' && r.expiryTime) {
        const diff = new Date(r.expiryTime).getTime() - now;
        if (diff > 0) {
          const mins = Math.floor(diff / 60000);
          const secs = Math.floor((diff % 60000) / 1000);
          updated[r.reservationId] = `${mins}m ${secs}s`;
        } else {
          updated[r.reservationId] = 'Expired';
        }
      }
    }
    this.countdowns = updated;
  }

  canPayNow(res: ReservationDetailsDto): boolean {
    return res.status === 'Pending' && !!res.expiryTime && new Date(res.expiryTime) > new Date();
  }

  getCountdown(res: ReservationDetailsDto): string {
    return this.countdowns[res.reservationId] ?? '';
  }

  goToPayment(res: ReservationDetailsDto) {
    this.router.navigate(['/booking/create'], { queryParams: { resume: res.reservationCode } });
  }

  onTabChange(index: number) {
    this.selectedStatus = this.statusTabs[index];
    this.currentPage = 1;
    this.paginator?.firstPage();
    this.load();
  }

  onPage(e: PageEvent) {
    this.currentPage = e.pageIndex + 1;
    this.pageSize = e.pageSize;
    this.load();
    window.scrollTo({ top: 0, behavior: 'smooth' });
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
