import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { DatePipe, DecimalPipe } from '@angular/common';
import { BookingService } from '../../../core/services/booking.service';
import { ToastService } from '../../../core/services/toast.service';
import { ReservationDetailsDto } from '../../../core/models/models';

@Component({
  selector: 'app-reservation-management',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, DatePipe, DecimalPipe],
  templateUrl: './reservation-management.component.html',
  styleUrl: './reservation-management.component.scss'
})
export class ReservationManagementComponent implements OnInit {
  private bookingService = inject(BookingService);
  private toast = inject(ToastService);

  reservations = signal<ReservationDetailsDto[]>([]);
  total = signal(0);
  page = signal(1);
  readonly pageSize = 15;
  filter = signal('all');

  readonly filters = ['all', 'Pending', 'Confirmed', 'Completed', 'Cancelled', 'NoShow'];

  ngOnInit() { this.load(); }

  load() {
    this.bookingService.getHotelReservations(this.page(), this.pageSize).subscribe(res => {
      this.reservations.set(res.reservations as ReservationDetailsDto[]);
      this.total.set(res.totalCount);
    });
  }

  complete(code: string) {
    if (!confirm(`Mark reservation ${code} as Completed?`)) return;
    this.bookingService.completeReservation(code).subscribe(() => {
      this.toast.success('Reservation marked as completed.');
      this.reservations.update(r => r.map(x => x.reservationCode === code
        ? { ...x, status: 'Completed', isCheckedIn: true } : x));
    });
  }

  get filtered(): ReservationDetailsDto[] {
    const f = this.filter();
    if (f === 'all') return this.reservations();
    return this.reservations().filter(r => r.status === f);
  }

  statusClass(s: string): string {
    const m: Record<string, string> = {
      Pending: 'badge-warning', Confirmed: 'badge-success',
      Completed: 'badge-primary', Cancelled: 'badge-error', NoShow: 'badge-muted',
    };
    return m[s] ?? 'badge-muted';
  }

  nextPage() { this.page.update(p => p + 1); this.load(); }
  prevPage() { if (this.page() > 1) { this.page.update(p => p - 1); this.load(); } }

  get totalPages() { return Math.ceil(this.total() / this.pageSize); }
}
