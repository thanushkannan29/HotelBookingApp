import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { AmenityRequestService } from '../../../core/services/amenity-request.service';
import { ToastService } from '../../../core/services/toast.service';
import { AmenityRequestResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-superadmin-amenity-requests',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatCardModule, MatTableModule, MatButtonModule, MatFormFieldModule,
    MatInputModule, MatPaginatorModule, MatIconModule, MatChipsModule,
    MatProgressSpinnerModule, MatSelectModule, MatDialogModule
  ],
  template: `
    <div class="container py-4">
      <h2 class="mb-4">🔧 Amenity Requests</h2>

      <!-- Filter -->
      <div class="d-flex gap-3 mb-3">
        <mat-form-field appearance="outline">
          <mat-label>Filter by Status</mat-label>
          <mat-select [(value)]="selectedStatus" (selectionChange)="load()">
            <mat-option value="All">All</mat-option>
            <mat-option value="Pending">⏳ Pending</mat-option>
            <mat-option value="Approved">✅ Approved</mat-option>
            <mat-option value="Rejected">❌ Rejected</mat-option>
          </mat-select>
        </mat-form-field>
      </div>

      @if (loading()) {
        <div class="text-center py-5"><mat-spinner diameter="48" /></div>
      } @else {
        <mat-card>
          <mat-card-content>
            <table mat-table [dataSource]="requests()" class="w-100">
              <ng-container matColumnDef="amenityName">
                <th mat-header-cell *matHeaderCellDef>Amenity</th>
                <td mat-cell *matCellDef="let r">{{ r.amenityName }}</td>
              </ng-container>
              <ng-container matColumnDef="category">
                <th mat-header-cell *matHeaderCellDef>Category</th>
                <td mat-cell *matCellDef="let r">{{ r.category }}</td>
              </ng-container>
              <ng-container matColumnDef="hotel">
                <th mat-header-cell *matHeaderCellDef>Hotel</th>
                <td mat-cell *matCellDef="let r">{{ r.hotelName }}</td>
              </ng-container>
              <ng-container matColumnDef="admin">
                <th mat-header-cell *matHeaderCellDef>Requested By</th>
                <td mat-cell *matCellDef="let r">{{ r.adminName }}</td>
              </ng-container>
              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef>Status</th>
                <td mat-cell *matCellDef="let r">
                  <mat-chip [color]="getStatusColor(r.status)" highlighted>{{ r.status }}</mat-chip>
                </td>
              </ng-container>
              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef>Actions</th>
                <td mat-cell *matCellDef="let r">
                  @if (r.status === 'Pending') {
                    <button mat-raised-button color="primary" (click)="approve(r.amenityRequestId)" class="me-2">
                      ✅ Approve
                    </button>
                    <button mat-raised-button color="warn" (click)="rejectPrompt(r.amenityRequestId)">
                      ❌ Reject
                    </button>
                  }
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
            </table>
            <mat-paginator
              [length]="totalCount()"
              [pageSize]="pageSize"
              [pageSizeOptions]="[10, 20]"
              showFirstLastButtons
              (page)="onPage($event)"
            />
          </mat-card-content>
        </mat-card>
      }
    </div>
  `
})
export class SuperadminAmenityRequestsComponent implements OnInit {
  private service = inject(AmenityRequestService);
  private toast = inject(ToastService);

  loading = signal(true);
  requests = signal<AmenityRequestResponseDto[]>([]);
  totalCount = signal(0);
  pageSize = 10;
  currentPage = 1;
  selectedStatus = 'All';
  displayedColumns = ['amenityName', 'category', 'hotel', 'admin', 'status', 'actions'];

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.service.getAll(this.selectedStatus, this.currentPage, this.pageSize).subscribe({
      next: data => { this.requests.set(data.requests); this.totalCount.set(data.totalCount); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  approve(id: string) {
    this.service.approve(id).subscribe({
      next: () => { this.toast.success('Amenity approved and added!'); this.load(); }
    });
  }

  rejectPrompt(id: string) {
    const note = prompt('Rejection reason:');
    if (!note) return;
    this.service.reject(id, note).subscribe({
      next: () => { this.toast.success('Request rejected.'); this.load(); }
    });
  }

  getStatusColor(status: string): 'primary' | 'accent' | 'warn' {
    if (status === 'Approved') return 'primary';
    if (status === 'Pending') return 'accent';
    return 'warn';
  }

  onPage(e: PageEvent) { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); }
}
