import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DatePipe, SlicePipe } from '@angular/common';
import { ReviewService } from '../../../core/services/api.services';
import { ReviewResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-admin-reviews',
  standalone: true,
  imports: [
    CommonModule, DatePipe, SlicePipe,
    MatFormFieldModule, MatInputModule, MatIconModule,
    MatTableModule, MatPaginatorModule, MatProgressSpinnerModule,
  ],
  template: `
    <div class="page-wrapper">
      <div class="container">
        <h1 class="section-title">⭐ Guest Reviews</h1>
        <p class="section-subtitle">{{ totalCount() }} reviews for your hotel</p>

        @if (loading()) {
          <div class="text-center py-5"><mat-spinner diameter="48" /></div>
        } @else {
          <div class="table-card">
            <mat-table [dataSource]="reviews()">

              <ng-container matColumnDef="guestName">
                <mat-header-cell *matHeaderCellDef>👤 Guest</mat-header-cell>
                <mat-cell *matCellDef="let r">{{ r.userName }}</mat-cell>
              </ng-container>

              <ng-container matColumnDef="rating">
                <mat-header-cell *matHeaderCellDef>Rating</mat-header-cell>
                <mat-cell *matCellDef="let r">
                  <span class="stars">
                    @for (s of stars; track s) {
                      <mat-icon [style.color]="s <= r.rating ? '#f59e0b' : '#ccc'" style="font-size:18px;width:18px;height:18px;">star</mat-icon>
                    }
                  </span>
                </mat-cell>
              </ng-container>

              <ng-container matColumnDef="comment">
                <mat-header-cell *matHeaderCellDef>Comment</mat-header-cell>
                <mat-cell *matCellDef="let r">
                  {{ r.comment?.length > 80 ? (r.comment | slice:0:80) + '…' : r.comment }}
                </mat-cell>
              </ng-container>

              <ng-container matColumnDef="createdDate">
                <mat-header-cell *matHeaderCellDef>📅 Date</mat-header-cell>
                <mat-cell *matCellDef="let r">{{ r.createdDate | date:'mediumDate' }}</mat-cell>
              </ng-container>

              <mat-header-row *matHeaderRowDef="displayedColumns"></mat-header-row>
              <mat-row *matRowDef="let row; columns: displayedColumns;"></mat-row>

              <tr class="mat-row" *matNoDataRow>
                <td class="mat-cell" [attr.colspan]="displayedColumns.length" style="text-align:center;padding:32px;">
                  No reviews yet.
                </td>
              </tr>
            </mat-table>

            <mat-paginator
              [length]="totalCount()"
              [pageSize]="pageSize"
              [pageSizeOptions]="[10, 25, 50]"
              showFirstLastButtons
              (page)="onPage($event)"
            />
          </div>
        }
      </div>
    </div>
  `,
  styles: [`.stars { display:flex; align-items:center; gap:2px; }`]
})
export class AdminReviewsComponent implements OnInit {
  private reviewService = inject(ReviewService);

  loading      = signal(false);
  reviews      = signal<ReviewResponseDto[]>([]);
  totalCount   = signal(0);
  pageSize     = 10;
  currentPage  = 1;
  displayedColumns = ['guestName', 'rating', 'comment', 'createdDate'];
  stars = [1, 2, 3, 4, 5];

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.reviewService.getHotelReviewsAdmin(this.currentPage, this.pageSize).subscribe({
      next: res => {
        this.reviews.set((res as any).reviews ?? []);
        this.totalCount.set((res as any).totalCount ?? 0);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onPage(e: PageEvent) { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); }
}
