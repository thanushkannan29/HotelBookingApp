import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { DatePipe, DecimalPipe } from '@angular/common';
import { BookingService } from '../../../core/services/booking.service';
import { ReservationDetailsDto } from '../../../core/models/models';

@Component({
  selector: 'app-booking-list',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, DatePipe, DecimalPipe],
  templateUrl: './booking-list.component.html',
  styleUrl: './booking-list.component.scss'
})
export class BookingListComponent implements OnInit {
  private bookingService = inject(BookingService);

  reservations = signal<ReservationDetailsDto[]>([]);
  filter = signal<string>('all');

  readonly filters = ['all', 'Pending', 'Confirmed', 'Completed', 'Cancelled', 'NoShow'];

  ngOnInit() {
    this.bookingService.getMyReservations().subscribe(r => this.reservations.set(r));
  }

  get filtered(): ReservationDetailsDto[] {
    const f = this.filter();
    if (f === 'all') return this.reservations();
    return this.reservations().filter(r => r.status === f);
  }

  statusClass(status: string): string {
    const map: Record<string, string> = {
      Pending: 'badge-warning',
      Confirmed: 'badge-success',
      Completed: 'badge-primary',
      Cancelled: 'badge-error',
      NoShow: 'badge-muted',
    };
    return map[status] ?? 'badge-muted';
  }
}
