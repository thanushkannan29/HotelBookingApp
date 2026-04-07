import { Component, inject, signal, OnInit, ViewChild } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatTabsModule } from '@angular/material/tabs';
import { PromoCodeService } from '../../../core/services/promo-code.service';
import { ToastService } from '../../../core/services/toast.service';
import { PromoCodeResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-guest-promo-codes',
  standalone: true,
  imports: [
    CommonModule, DatePipe, MatTableModule, MatButtonModule, MatIconModule,
    MatChipsModule, MatProgressSpinnerModule, MatTooltipModule,
    MatPaginatorModule, MatTabsModule,
  ],
  templateUrl: './guest-promo-codes.component.html',
  styleUrl: './guest-promo-codes.component.scss',
})
export class GuestPromoCodesComponent implements OnInit {
  private promoService = inject(PromoCodeService);
  private toast        = inject(ToastService);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  loading    = signal(false);
  codes      = signal<PromoCodeResponseDto[]>([]);
  totalCount = signal(0);
  pageSize   = 10;
  currentPage = 1;
  selectedStatus = 'All';
  displayedColumns = ['code', 'hotel', 'discount', 'expiry', 'status'];
  readonly statusTabs = ['All', 'Active', 'Used', 'Expired'];

  ngOnInit() { this.load(); }

  private resetPage() {
    this.currentPage = 1;
    this.paginator?.firstPage();
  }

  load() {
    this.loading.set(true);
    this.promoService.getMyCodes(this.currentPage, this.pageSize, this.selectedStatus).subscribe({
      next: data => {
        this.codes.set(data.promoCodes ?? []);
        this.totalCount.set(data.totalCount ?? 0);
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

  onPage(e: PageEvent) {
    this.currentPage = e.pageIndex + 1;
    this.pageSize    = e.pageSize;
    this.load();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  copy(code: string) {
    navigator.clipboard.writeText(code);
    this.toast.success('Code copied!');
  }

  statusClass(status: string): string {
    const m: Record<string, string> = { Active: 'badge-success', Used: 'badge-muted', Expired: 'badge-error' };
    return m[status] ?? 'badge-muted';
  }
}
