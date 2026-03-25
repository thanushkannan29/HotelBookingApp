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
import { ReservationDetailsDto } from '../../../core/models/models';

@Component({
  selector: 'app-booking-list',
  standalone: true,
  imports: [
    RouterLink, MatButtonModule, MatIconModule, DatePipe, DecimalPipe,
    MatFormFieldModule, MatInputModule,
    MatTableModule, MatSortModule, MatPaginatorModule,
  ],
  templateUrl: './booking-list.component.html',
  styleUrl: './booking-list.component.scss'
})
export class BookingListComponent implements OnInit, AfterViewInit {
  private bookingService = inject(BookingService);

  dataSource = new MatTableDataSource<ReservationDetailsDto>([]);
  displayedColumns = ['reservationCode', 'hotelName', 'checkIn', 'checkOut', 'amount', 'status'];
  filter = signal<string>('all');
  readonly filters = ['all', 'Pending', 'Confirmed', 'Completed', 'Cancelled', 'NoShow'];

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  ngOnInit() {
    this.bookingService.getMyReservations().subscribe(r => {
      this.dataSource.data = r as ReservationDetailsDto[];
    });
  }

  ngAfterViewInit() {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
  }

  applyFilter(event: Event) {
    const val = (event.target as HTMLInputElement).value;
    this.dataSource.filter = val.trim().toLowerCase();
    if (this.dataSource.paginator) this.dataSource.paginator.firstPage();
  }

  setStatusFilter(f: string) {
    this.filter.set(f);
    if (f === 'all') {
      this.dataSource.filterPredicate = () => true;
    } else {
      this.dataSource.filterPredicate = (row: ReservationDetailsDto) => row.status === f;
    }
    this.dataSource.filter = ' ';
    this.dataSource.filter = '';
    if (this.dataSource.paginator) this.dataSource.paginator.firstPage();
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