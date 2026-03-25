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
import { HotelService } from '../../../core/services/hotel.service';
import { ToastService } from '../../../core/services/toast.service';
import { SuperAdminHotelListDto } from '../../../core/models/models';

@Component({
  selector: 'app-hotel-control',
  standalone: true,
  imports: [
    RouterLink, DatePipe, DecimalPipe,
    MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule,
    MatTableModule, MatSortModule, MatPaginatorModule,
  ],
  templateUrl: './hotel-control.component.html',
  styleUrl: './hotel-control.component.scss'
})
export class HotelControlComponent implements OnInit, AfterViewInit {
  private hotelService = inject(HotelService);
  private toast = inject(ToastService);

  dataSource = new MatTableDataSource<SuperAdminHotelListDto>([]);
  displayedColumns = ['name', 'city', 'status', 'reservations', 'revenue', 'contact', 'joined', 'actions'];
  filterMode = signal<'all' | 'active' | 'blocked'>('all');

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

ngOnInit() {
  this.hotelService.getAllHotelsForSuperAdmin().subscribe(res => {
    this.dataSource.data = res.hotels ?? [];
    this.applyStatusFilter();
  });
}


  ngAfterViewInit() {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
  }

  applyTextFilter(event: Event) {
    const val = (event.target as HTMLInputElement).value;
    this.dataSource.filter = val.trim().toLowerCase();
    if (this.dataSource.paginator) this.dataSource.paginator.firstPage();
  }

  setStatusFilter(mode: 'all' | 'active' | 'blocked') {
    this.filterMode.set(mode);
    this.applyStatusFilter();
  }

  private applyStatusFilter() {
    const mode = this.filterMode();
    if (mode === 'active') {
      this.dataSource.filterPredicate = (row: SuperAdminHotelListDto) =>
        row.isActive && !row.isBlockedBySuperAdmin;
    } else if (mode === 'blocked') {
      this.dataSource.filterPredicate = (row: SuperAdminHotelListDto) =>
        row.isBlockedBySuperAdmin;
    } else {
      this.dataSource.filterPredicate = () => true;
    }
    // trigger re-filter
    this.dataSource.filter = this.dataSource.filter || ' ';
    this.dataSource.filter = this.dataSource.filter.trim();
    if (this.dataSource.paginator) this.dataSource.paginator.firstPage();
  }

  block(hotel: SuperAdminHotelListDto) {
    if (!confirm(`Block ${hotel.name}? This will prevent the admin from activating it.`)) return;
    this.hotelService.blockHotel(hotel.hotelId).subscribe(() => {
      this.toast.success(`${hotel.name} blocked.`);
      this.dataSource.data = this.dataSource.data.map(x =>
        x.hotelId === hotel.hotelId ? { ...x, isBlockedBySuperAdmin: true, isActive: false } : x
      );
    });
  }

  unblock(hotel: SuperAdminHotelListDto) {
    this.hotelService.unblockHotel(hotel.hotelId).subscribe(() => {
      this.toast.success(`${hotel.name} unblocked.`);
      this.dataSource.data = this.dataSource.data.map(x =>
        x.hotelId === hotel.hotelId ? { ...x, isBlockedBySuperAdmin: false } : x
      );
    });
  }

  get allCount()     { return this.dataSource.data.length; }
  get activeCount()  { return this.dataSource.data.filter(h => h.isActive && !h.isBlockedBySuperAdmin).length; }
  get blockedCount() { return this.dataSource.data.filter(h => h.isBlockedBySuperAdmin).length; }
}