import { Component, inject, signal, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
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
    MatTableModule, MatPaginatorModule,
    MatTabsModule, MatProgressSpinnerModule, MatChipsModule, MatDialogModule,
  ],
  templateUrl: './reservation-management.component.html',
  styleUrl: './reservation-management.component.scss'
})
export class ReservationManagementComponent implements OnInit {
  private bookingService = inject(BookingService);
  private toast          = inject(ToastService);
  private dialog         = inject(MatDialog);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  reservations   = signal<ReservationDetailsDto[]>([]);
  totalCount     = signal(0);
  loading        = signal(false);
  pageSize       = 10;
  currentPage    = 1;
  selectedStatus = 'All';
  searchTerm     = '';
  sortField      = '';
  sortDir        = '';

  displayedColumns = ['reservationCode', 'guestName', 'checkIn', 'checkOut', 'rooms', 'amount', 'status', 'actions'];
  readonly statusTabs = ['All', 'Pending', 'Confirmed', 'Completed', 'Cancelled', 'NoShow'];
  private searchSubject = new Subject<string>();

  ngOnInit() {
    this.load();
    this.searchSubject.pipe(debounceTime(400), distinctUntilChanged())
      .subscribe(s => { this.searchTerm = s; this.resetPage(); this.load(); });
  }

  private resetPage() {
    this.currentPage = 1;
    // firstPage() resets the paginator UI without emitting a page event
    this.paginator?.firstPage();
  }

  load() {
    this.loading.set(true);
    this.bookingService.getHotelReservations(
      this.currentPage, this.pageSize, this.selectedStatus, this.searchTerm,
      this.sortField, this.sortDir
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
    this.resetPage();
    this.load();
  }

  onSearch(e: Event) {
    this.searchSubject.next((e.target as HTMLInputElement).value);
  }

  onPage(e: PageEvent) {
    this.currentPage = e.pageIndex + 1;
    this.pageSize    = e.pageSize;
    this.load();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  onSort(field: string) {
    if (this.sortField === field) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortField = field;
      this.sortDir   = 'asc';
    }
    this.resetPage();
    this.load();
  }

  async complete(code: string) {
    const { ConfirmDialogComponent } = await import('../../../shared/components/confirm-dialog/confirm-dialog.component');
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Complete Reservation', message: `Mark reservation ${code} as Completed?`, confirmLabel: 'Complete', confirmColor: 'primary', icon: 'check_circle' }
    });
    ref.afterClosed().subscribe(ok => {
      if (!ok) return;
      this.bookingService.completeReservation(code).subscribe(() => {
        this.toast.success('Reservation marked as completed.');
        this.load();
      });
    });
  }

  async confirm(code: string) {
    const { ConfirmDialogComponent } = await import('../../../shared/components/confirm-dialog/confirm-dialog.component');
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Confirm Reservation', message: `Confirm reservation ${code}?`, confirmLabel: 'Confirm', confirmColor: 'primary', icon: 'check' }
    });
    ref.afterClosed().subscribe(ok => {
      if (!ok) return;
      this.bookingService.confirmReservation(code).subscribe(() => {
        this.toast.success('Reservation confirmed.');
        this.load();
      });
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
