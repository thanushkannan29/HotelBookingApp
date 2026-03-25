import { Component, inject, signal, OnInit, AfterViewInit, ViewChild } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DatePipe } from '@angular/common';
import { ReviewService } from '../../../core/services/api.services';
import { ReviewResponseDto } from '../../../core/models/models';
import { SlicePipe } from '@angular/common';

@Component({
  selector: 'app-admin-reviews',
  standalone: true,
  imports: [
    DatePipe,
    MatFormFieldModule, MatInputModule, MatIconModule,
    MatTableModule, MatSortModule, MatPaginatorModule,
    MatProgressSpinnerModule,SlicePipe,
  ],
  template: `
    <div class="page-wrapper">
      <div class="container">
        <h1 class="section-title">Guest Reviews</h1>
        <p class="section-subtitle">All reviews left for your hotel</p>

        <mat-form-field appearance="outline" style="width:100%; margin-bottom:12px;">
          <mat-label>Search reviews</mat-label>
          <input matInput (keyup)="applyFilter($event)" placeholder="Type to filter..." />
          <mat-icon matSuffix>search</mat-icon>
        </mat-form-field>

        @if (isLoading()) {
          <div style="display:flex;justify-content:center;padding:40px;">
            <mat-spinner diameter="40" />
          </div>
        } @else {
          <mat-table [dataSource]="dataSource" matSort class="mat-elevation-z2">

            <ng-container matColumnDef="guestName">
              <mat-header-cell *matHeaderCellDef mat-sort-header>Guest</mat-header-cell>
              <mat-cell *matCellDef="let row">{{ row.userName }}</mat-cell>
            </ng-container>

            <ng-container matColumnDef="rating">
              <mat-header-cell *matHeaderCellDef mat-sort-header>Rating</mat-header-cell>
              <mat-cell *matCellDef="let row">
                <span class="stars">
                  @for (s of stars; track s) {
                    <mat-icon [class.filled]="s <= row.rating" class="star-icon">star</mat-icon>
                  }
                </span>
              </mat-cell>
            </ng-container>

            <ng-container matColumnDef="comment">
              <mat-header-cell *matHeaderCellDef>Comment</mat-header-cell>
              <mat-cell *matCellDef="let row" class="comment-cell">
                {{ row.comment?.length > 80 ? (row.comment | slice:0:80) + '…' : row.comment }}
              </mat-cell>
            </ng-container>

            <ng-container matColumnDef="createdDate">
              <mat-header-cell *matHeaderCellDef mat-sort-header>Date</mat-header-cell>
              <mat-cell *matCellDef="let row">{{ row.createdDate | date:'mediumDate' }}</mat-cell>
            </ng-container>

            <mat-header-row *matHeaderRowDef="displayedColumns"></mat-header-row>
            <mat-row *matRowDef="let row; columns: displayedColumns;"></mat-row>

            <tr class="mat-row" *matNoDataRow>
              <td class="mat-cell" [attr.colspan]="displayedColumns.length" style="text-align:center;padding:24px;">
                No reviews found.
              </td>
            </tr>
          </mat-table>

          <mat-paginator [pageSizeOptions]="[5, 10, 25, 50]"
                         showFirstLastButtons
                         aria-label="Select page">
          </mat-paginator>
        }
      </div>
    </div>
  `,
  styles: [`
    .star-icon { font-size: 18px; width: 18px; height: 18px; color: #ccc; }
    .star-icon.filled { color: #f59e0b; }
    .comment-cell { max-width: 400px; white-space: normal; line-height: 1.4; }
    .stars { display: flex; align-items: center; gap: 2px; }
  `]
})
export class AdminReviewsComponent implements OnInit, AfterViewInit {
  private reviewService = inject(ReviewService);

  isLoading = signal(true);
  dataSource = new MatTableDataSource<ReviewResponseDto>([]);
  displayedColumns = ['guestName', 'rating', 'comment', 'createdDate'];
  stars = [1, 2, 3, 4, 5];

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  ngOnInit() {
    this.reviewService.getHotelReviewsAdmin(1, 100).subscribe({
      next: (res) => {
        this.dataSource.data = (res as any).reviews ?? (res as any) ?? [];
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
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
}
