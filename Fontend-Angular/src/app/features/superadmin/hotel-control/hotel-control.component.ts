import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DatePipe, DecimalPipe } from '@angular/common';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { HotelService } from '../../../core/services/hotel.service';
import { ToastService } from '../../../core/services/toast.service';
import { SuperAdminHotelListDto } from '../../../core/models/models';

@Component({
  selector: 'app-hotel-control',
  standalone: true,
  imports: [
    CommonModule, RouterLink, DatePipe, DecimalPipe,
    MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule,
    MatTableModule, MatPaginatorModule, MatChipsModule,
    MatProgressSpinnerModule, MatTooltipModule,
  ],
  templateUrl: './hotel-control.component.html',
  styleUrl: './hotel-control.component.scss'
})
export class HotelControlComponent implements OnInit {
  private hotelService = inject(HotelService);
  private toast        = inject(ToastService);

  hotels       = signal<SuperAdminHotelListDto[]>([]);
  totalCount   = signal(0);
  loading      = signal(false);
  pageSize     = 10;
  currentPage  = 1;
  displayedColumns = ['name', 'city', 'status', 'reservations', 'revenue', 'contact', 'joined', 'actions'];

  private searchSubject = new Subject<string>();

  ngOnInit() {
    this.load();
    this.searchSubject.pipe(debounceTime(400), distinctUntilChanged())
      .subscribe(() => { this.currentPage = 1; this.load(); });
  }

  load() {
    this.loading.set(true);
    this.hotelService.getAllHotelsForSuperAdmin(this.currentPage, this.pageSize).subscribe({
      next: res => {
        this.hotels.set(res.hotels ?? []);
        this.totalCount.set(res.totalCount ?? 0);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onPage(e: PageEvent) { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); }

  block(hotel: SuperAdminHotelListDto) {
    if (!confirm(`Block ${hotel.name}? This will prevent the admin from activating it.`)) return;
    this.hotelService.blockHotel(hotel.hotelId).subscribe(() => {
      this.toast.success(`${hotel.name} blocked.`);
      this.load();
    });
  }

  unblock(hotel: SuperAdminHotelListDto) {
    this.hotelService.unblockHotel(hotel.hotelId).subscribe(() => {
      this.toast.success(`${hotel.name} unblocked.`);
      this.load();
    });
  }

  statusClass(h: SuperAdminHotelListDto): string {
    if (h.isBlockedBySuperAdmin) return 'badge-error';
    if (h.isActive) return 'badge-success';
    return 'badge-warning';
  }

  statusLabel(h: SuperAdminHotelListDto): string {
    if (h.isBlockedBySuperAdmin) return '🚫 Blocked';
    if (h.isActive) return '✅ Active';
    return '⏸️ Inactive';
  }
}
