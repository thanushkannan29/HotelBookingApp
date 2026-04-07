import { Component, inject, signal, OnInit, ViewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatTabsModule } from '@angular/material/tabs';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
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
    MatProgressSpinnerModule, MatTooltipModule, MatTabsModule, MatDialogModule,
  ],
  templateUrl: './hotel-control.component.html',
  styleUrl: './hotel-control.component.scss'
})
export class HotelControlComponent implements OnInit {
  private hotelService = inject(HotelService);
  private toast        = inject(ToastService);
  private dialog       = inject(MatDialog);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  hotels       = signal<SuperAdminHotelListDto[]>([]);
  totalCount   = signal(0);
  loading      = signal(false);
  pageSize     = 10;
  currentPage  = 1;
  searchTerm   = '';
  selectedStatus = 'All';
  displayedColumns = ['name', 'city', 'status', 'reservations', 'revenue', 'contact', 'joined', 'actions'];
  readonly statusTabs = ['All', 'Active', 'Inactive', 'Blocked'];

  private searchSubject = new Subject<string>();

  ngOnInit() {
    this.load();
    this.searchSubject.pipe(debounceTime(400), distinctUntilChanged())
      .subscribe(s => { this.searchTerm = s; this.resetPage(); this.load(); });
  }

  private resetPage() {
    this.currentPage = 1;
    this.paginator?.firstPage();
  }

  load() {
    this.loading.set(true);
    this.hotelService.getAllHotelsForSuperAdmin(
      this.currentPage, this.pageSize,
      this.searchTerm || undefined,
      this.selectedStatus
    ).subscribe({
      next: res => {
        this.hotels.set(res.hotels ?? []);
        this.totalCount.set(res.totalCount ?? 0);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onSearch(e: Event) { this.searchSubject.next((e.target as HTMLInputElement).value); }
  onTabChange(i: number) { this.selectedStatus = this.statusTabs[i]; this.resetPage(); this.load(); }
  onPage(e: PageEvent) { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); window.scrollTo({ top: 0, behavior: 'smooth' }); }

  async block(hotel: SuperAdminHotelListDto) {
    const { ConfirmDialogComponent } = await import('../../../shared/components/confirm-dialog/confirm-dialog.component');
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Block Hotel', message: `Block "${hotel.name}"? The admin will not be able to activate it.`, confirmLabel: 'Block', confirmColor: 'warn' }
    });
    ref.afterClosed().subscribe(ok => {
      if (!ok) return;
      this.hotelService.blockHotel(hotel.hotelId).subscribe(() => {
        this.toast.success(`${hotel.name} blocked.`); this.load();
      });
    });
  }

  async unblock(hotel: SuperAdminHotelListDto) {
    const { ConfirmDialogComponent } = await import('../../../shared/components/confirm-dialog/confirm-dialog.component');
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Unblock Hotel', message: `Unblock "${hotel.name}"?`, confirmLabel: 'Unblock', confirmColor: 'primary' }
    });
    ref.afterClosed().subscribe(ok => {
      if (!ok) return;
      this.hotelService.unblockHotel(hotel.hotelId).subscribe(() => {
        this.toast.success(`${hotel.name} unblocked.`); this.load();
      });
    });
  }

  statusClass(h: SuperAdminHotelListDto): string {
    if (h.isBlockedBySuperAdmin) return 'badge-error';
    if (h.isActive) return 'badge-success';
    return 'badge-warning';
  }

  statusLabel(h: SuperAdminHotelListDto): string {
    if (h.isBlockedBySuperAdmin) return 'Blocked';
    if (h.isActive) return 'Active';
    return 'Inactive';
  }
}
