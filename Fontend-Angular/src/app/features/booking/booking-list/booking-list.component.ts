import { Component, inject, signal, OnInit } from '@angular/core';
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
import { DatePipe, DecimalPipe } from '@angular/common';
import { BookingService } from '../../../core/services/booking.service';
import { ReservationDetailsDto } from '../../../core/models/models';

@Component({
  selector: 'app-booking-list',
  standalone: true,
  imports: [
    CommonModule, RouterLink, DatePipe, DecimalPipe,
    MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule,
    MatTableModule, MatPaginatorModule, MatTabsModule,
    MatProgressSpinnerModule, MatChipsModule,
  ],
  templateUrl: './booking-list.component.html',
  styleUrl: './booking-list.component.scss'
})
export class BookingListComponent implements OnInit {
  private bookingService = inject(BookingService);

  reservations = signal<ReservationDetailsDto[]>([]);
  totalCount   = signal(0);
  loading      = signal(false);
  pageSize     = 10;
  currentPage  = 1;

  displayedColumns = ['reservationCode', 'hotelName', 'checkIn', 'checkOut', 'amount', 'status'];
  readonly statusTabs = ['All', 'Pending', 'Confirmed', 'Completed', 'Cancelled', 'NoShow'];
  selectedStatus = 'All';

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.bookingService.getMyReservationsHistory(this.currentPage, this.pageSize).subscribe({
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
      Pending: '⏳', Confirmed: '✅', Completed: '🏆', Cancelled: '❌', NoShow: '👻'
    };
    return m[s] ?? '';
  }
}
