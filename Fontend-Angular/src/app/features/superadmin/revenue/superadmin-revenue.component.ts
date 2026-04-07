import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RevenueService } from '../../../core/services/revenue.service';
import { SuperAdminRevenueDto, RevenueSummaryDto } from '../../../core/models/models';

@Component({
  selector: 'app-superadmin-revenue',
  standalone: true,
  imports: [
    CommonModule, MatCardModule, MatTableModule, MatButtonModule,
    MatPaginatorModule, MatIconModule, MatProgressSpinnerModule
  ],
  template: `
    <div class="container py-4">
      <h2 class="mb-4">💰 Revenue & Commission</h2>

      <!-- Summary Cards -->
      @if (summary()) {
        <div class="row mb-4">
          <div class="col-sm-6 col-lg-4">
            <mat-card class="summary-card total">
              <mat-card-content class="text-center py-3">
                <div class="summary-label">Total Earned</div>
                <div class="summary-value">₹{{ summary()!.totalCommissionEarned | number:'1.2-2' }}</div>
              </mat-card-content>
            </mat-card>
          </div>
        </div>
      }

      @if (loading()) {
        <div class="text-center py-5"><mat-spinner diameter="48" /></div>
      }
      <mat-card>
        <mat-card-content>
          <table mat-table [dataSource]="items()" class="w-100" [style.display]="loading() ? 'none' : ''">
              <ng-container matColumnDef="reservationCode">
                <th mat-header-cell *matHeaderCellDef>Reservation</th>
                <td mat-cell *matCellDef="let r">{{ r.reservationCode }}</td>
              </ng-container>
              <ng-container matColumnDef="hotelName">
                <th mat-header-cell *matHeaderCellDef>Hotel</th>
                <td mat-cell *matCellDef="let r">{{ r.hotelName }}</td>
              </ng-container>
              <ng-container matColumnDef="reservationAmount">
                <th mat-header-cell *matHeaderCellDef>Reservation Amt</th>
                <td mat-cell *matCellDef="let r">₹{{ r.reservationAmount | number:'1.2-2' }}</td>
              </ng-container>
              <ng-container matColumnDef="commissionAmount">
                <th mat-header-cell *matHeaderCellDef>Commission (2%)</th>
                <td mat-cell *matCellDef="let r"><strong>₹{{ r.commissionAmount | number:'1.2-2' }}</strong></td>
              </ng-container>
              <ng-container matColumnDef="date">
                <th mat-header-cell *matHeaderCellDef>Date</th>
                <td mat-cell *matCellDef="let r">{{ r.createdAt | date:'mediumDate' }}</td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
              <tr class="mat-row" *matNoDataRow>
                <td class="mat-cell" colspan="5" style="text-align:center;padding:32px;">No revenue records yet</td>
              </tr>
            </table>
            <!-- Always in DOM -->
            <mat-paginator
              [length]="totalCount()"
              [pageSize]="pageSize"
              [pageSizeOptions]="[10, 20, 50]"
              showFirstLastButtons
              (page)="onPage($event)"
            />
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .summary-card.total { background: linear-gradient(135deg, #1565c0, #1976d2); color: white; }
    .summary-card.pending { background: linear-gradient(135deg, #e65100, #f57c00); color: white; }
    .summary-card.sent { background: linear-gradient(135deg, #2e7d32, #43a047); color: white; }
    .summary-label { font-size: 13px; opacity: 0.9; }
    .summary-value { font-size: 28px; font-weight: 700; margin-top: 4px; }
  `]
})
export class SuperadminRevenueComponent implements OnInit {
  private service = inject(RevenueService);

  loading = signal(true);
  items = signal<SuperAdminRevenueDto[]>([]);
  totalCount = signal(0);
  summary = signal<RevenueSummaryDto | null>(null);
  pageSize = 20;
  currentPage = 1;
  displayedColumns = ['reservationCode', 'hotelName', 'reservationAmount', 'commissionAmount', 'date'];

  ngOnInit() {
    this.loadSummary();
    this.load();
  }

  loadSummary() {
    this.service.getSummary().subscribe({ next: s => this.summary.set(s) });
  }

  load() {
    this.loading.set(true);
    this.service.getAll(this.currentPage, this.pageSize).subscribe({
      next: data => { this.items.set(data.items); this.totalCount.set(data.totalCount); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  onPage(e: PageEvent) { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); window.scrollTo({ top: 0, behavior: 'smooth' }); }
}
