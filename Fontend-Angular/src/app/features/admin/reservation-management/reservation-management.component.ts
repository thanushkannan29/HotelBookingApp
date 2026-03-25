import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule } from '@angular/material/sort';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { DatePipe, DecimalPipe } from '@angular/common';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { BookingService } from '../../../core/services/booking.service';
import { ToastService } from '../../../core/services/toast.service';
import { ReservationDetailsDto } from '../../../core/models/models';

@Component({
  selector: 'app-reservation-management',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule, DatePipe, DecimalPipe,
    MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule,
    MatTableModule, MatSortModule, MatPaginatorModule,
    MatTabsModule, MatProgressSpinnerModule, MatChipsModule,
  ],
  templateUrl: './reservation-management.component.html',
  styleUrl: './reservation-management.component.scss'
})
export class ReservationManagementComponent implements OnInit {
  private bookingService = inject(BookingService);
  private toast          = inject(ToastService);

  reservations   = signal<ReservationDetailsDto[]>([]);
  totalCount     = signal(0);
  loading        = signal(false);
  pageSize       = 10;
  currentPage    = 1;
  selectedStatus = 'All';
  searchTerm     = '';
  displayedColumns = ['reservationCode', 'guestName', 'checkIn', 'checkOut', 'rooms', 'amount', 'status', 'actions'];

  readonly statusTabs = ['All', 'Pending', 'Confirmed', 'Completed', 'Cancelled', 'NoShow'];
  private searchSubject = new Subject<string>();

  ngOnInit() {
    this.load();
    this.searchSubject.pipe(debounceTime(400), distinctUntilChanged())
      .subscribe(s => { this.searchTerm = s; this.currentPage = 1; this.load(); });
  }

  load() {
    this.loading.set(true);
    this.bookingService.getHotelReservations(
      this.currentPage, this.pageSize, this.selectedStatus, this.searchTerm
    ).subscribe({
      next: res => {
        this.reservations.set(res.reservations as ReservationDetailsDto[]);
        this.totalCount.set(res.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onTabChange(index: number) {
    this.selectedStatus = this.statusTabs[index];
    this.currentPage = 1;
    this.load();
  }

  onSearch(e: Event) {
    this.searchSubject.next((e.target as HTMLInputElement).value);
  }

  onPage(e: PageEvent) {
    this.currentPage = e.pageIndex + 1;
    this.pageSize = e.pageSize;
    this.load();
  }

  complete(code: string) {
    if (!confirm(`Mark reservation ${code} as Completed?`)) return;
    this.bookingService.completeReservation(code).subscribe(() => {
      this.toast.success('Reservation marked as completed.');
      this.load();
    });
  }

  confirm(code: string) {
    if (!confirm(`Confirm reservation ${code}?`)) return;
    this.bookingService.confirmReservation(code).subscribe(() => {
      this.toast.success('Reservation confirmed.');
      this.load();
    });
  }

  statusClass(s: string): string {
    const m: Record<string, string> = {
      Pending: 'badge-warning', Confirmed: 'badge-success',
      Completed: 'badge-primary', Cancelled: 'badge-error', NoShow: 'badge-muted',
    };
    return m[s] ?? 'badge-muted';
  }

  statusEmoji(s: string): string {
    const m: Record<string, string> = {
      Pending: '⏳', Confirmed: '✅', Completed: '🏆', Cancelled: '❌', NoShow: '👻'
    };
    return m[s] ?? '';
  }
}
