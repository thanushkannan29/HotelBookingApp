import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { PromoCodeService } from '../../../core/services/promo-code.service';
import { ToastService } from '../../../core/services/toast.service';
import { PromoCodeResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-guest-promo-codes',
  standalone: true,
  imports: [
    CommonModule, MatCardModule, MatTableModule, MatButtonModule,
    MatIconModule, MatChipsModule, MatProgressSpinnerModule,
    MatTooltipModule, MatPaginatorModule,
  ],
  template: `
    <div class="container py-4">
      <h2 class="mb-4">🎫 My Promo Codes</h2>

      @if (loading()) {
        <div class="text-center py-5"><mat-spinner diameter="48" /></div>
      } @else if (codes().length === 0 && totalCount() === 0) {
        <mat-card class="text-center py-5">
          <mat-icon style="font-size:48px;color:#ccc">local_offer</mat-icon>
          <p class="mt-2 text-muted">No promo codes yet. Complete a stay to earn one!</p>
        </mat-card>
      } @else {
        <mat-card>
          <mat-card-content>
            <table mat-table [dataSource]="codes()" class="w-100">
              <ng-container matColumnDef="code">
                <th mat-header-cell *matHeaderCellDef>Code</th>
                <td mat-cell *matCellDef="let c">
                  <strong>{{ c.code }}</strong>
                  <button mat-icon-button (click)="copy(c.code)" matTooltip="Copy code">
                    <mat-icon>content_copy</mat-icon>
                  </button>
                </td>
              </ng-container>
              <ng-container matColumnDef="hotel">
                <th mat-header-cell *matHeaderCellDef>Hotel</th>
                <td mat-cell *matCellDef="let c">{{ c.hotelName }}</td>
              </ng-container>
              <ng-container matColumnDef="discount">
                <th mat-header-cell *matHeaderCellDef>Discount</th>
                <td mat-cell *matCellDef="let c">{{ c.discountPercent }}%</td>
              </ng-container>
              <ng-container matColumnDef="expiry">
                <th mat-header-cell *matHeaderCellDef>Expiry</th>
                <td mat-cell *matCellDef="let c">{{ c.expiryDate | date:'mediumDate' }}</td>
              </ng-container>
              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef>Status</th>
                <td mat-cell *matCellDef="let c">
                  <mat-chip [color]="getStatusColor(c.status)" highlighted>{{ c.status }}</mat-chip>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
            </table>
            <mat-paginator
              [length]="totalCount()"
              [pageSize]="pageSize"
              [pageSizeOptions]="[5, 10, 20]"
              showFirstLastButtons
              (page)="onPage($event)"
            />
          </mat-card-content>
        </mat-card>
      }
    </div>
  `
})
export class GuestPromoCodesComponent implements OnInit {
  private promoService = inject(PromoCodeService);
  private toast = inject(ToastService);

  loading = signal(true);
  codes = signal<PromoCodeResponseDto[]>([]);
  totalCount = signal(0);
  pageSize = 10;
  currentPage = 1;
  displayedColumns = ['code', 'hotel', 'discount', 'expiry', 'status'];

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.promoService.getMyCodes(this.currentPage, this.pageSize).subscribe({
      next: data => {
        this.codes.set(data.promoCodes ?? []);
        this.totalCount.set(data.totalCount ?? 0);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onPage(e: PageEvent) { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); }

  copy(code: string) {
    navigator.clipboard.writeText(code);
    this.toast.success('Code copied!');
  }

  getStatusColor(status: string): 'primary' | 'accent' | 'warn' {
    if (status === 'Active') return 'primary';
    if (status === 'Used') return 'accent';
    return 'warn';
  }
}
