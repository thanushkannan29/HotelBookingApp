import { Component, inject, signal, OnInit, AfterViewInit, ViewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { DatePipe, DecimalPipe } from '@angular/common';
import { BookingService } from '../../../core/services/booking.service';
import { ToastService } from '../../../core/services/toast.service';
import { ReservationDetailsDto } from '../../../core/models/models';

@Component({
  selector: 'app-reservation-management',
  standalone: true,
  imports: [
    RouterLink, MatButtonModule, MatIconModule, DatePipe, DecimalPipe,
    MatFormFieldModule, MatInputModule,
    MatTableModule, MatSortModule, MatPaginatorModule,
  ],
  templateUrl: './reservation-management.component.html',
  styleUrl: './reservation-management.component.scss'
})
export class ReservationManagementComponent implements OnInit, AfterViewInit {
  private bookingService = inject(BookingService);
  private toast = inject(ToastService);

  dataSource = new MatTableDataSource<ReservationDetailsDto>([]);
  displayedColumns = ['reservationCode', 'guestName', 'checkIn', 'checkOut', 'rooms', 'amount', 'status', 'actions'];

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  filter = signal('all');
  readonly filters = ['all', 'Pending', 'Confirmed', 'Completed', 'Cancelled', 'NoShow'];

  ngOnInit() { this.load(); }

  ngAfterViewInit() {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
  }

  load() {
    this.bookingService.getHotelReservations(1, 200).subscribe(res => {
      this.dataSource.data = res.reservations as ReservationDetailsDto[];
      this.applyStatusFilter();
    });
  }

  applyFilter(event: Event) {
    const val = (event.target as HTMLInputElement).value;
    this.dataSource.filter = val.trim().toLowerCase();
    if (this.dataSource.paginator) this.dataSource.paginator.firstPage();
  }

  setStatusFilter(f: string) {
    this.filter.set(f);
    this.applyStatusFilter();
  }

  private applyStatusFilter() {
    const f = this.filter();
    if (f === 'all') {
      this.dataSource.filterPredicate = () => true;
    } else {
      this.dataSource.filterPredicate = (row: ReservationDetailsDto) => row.status === f;
    }
    this.dataSource.filter = this.dataSource.filter || ' '; // trigger re-filter
    this.dataSource.filter = this.dataSource.filter.trim();
  }

  complete(code: string) {
    if (!confirm(`Mark reservation ${code} as Completed?`)) return;
    this.bookingService.completeReservation(code).subscribe(() => {
      this.toast.success('Reservation marked as completed.');
      this.dataSource.data = this.dataSource.data.map(x =>
        x.reservationCode === code ? { ...x, status: 'Completed', isCheckedIn: true } : x
      );
    });
  }

  statusClass(s: string): string {
    const m: Record<string, string> = {
      Pending: 'badge-warning', Confirmed: 'badge-success',
      Completed: 'badge-primary', Cancelled: 'badge-error', NoShow: 'badge-muted',
    };
    return m[s] ?? 'badge-muted';
  }
}