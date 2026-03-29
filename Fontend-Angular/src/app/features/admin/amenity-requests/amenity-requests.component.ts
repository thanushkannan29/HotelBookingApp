import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { DatePipe } from '@angular/common';
import { AmenityRequestService } from '../../../core/services/amenity-request.service';
import { ToastService } from '../../../core/services/toast.service';
import { AmenityRequestResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-amenity-requests',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, DatePipe,
    MatCardModule, MatTableModule, MatButtonModule, MatFormFieldModule,
    MatInputModule, MatIconModule, MatChipsModule,
    MatProgressSpinnerModule, MatPaginatorModule,
  ],
  template: `
    <div class="container py-4">
      <h2 class="mb-4">🔧 Amenity Requests</h2>

      <!-- Submit Form -->
      <mat-card class="mb-4">
        <mat-card-header>
          <mat-card-title>Request New Amenity</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <form [formGroup]="form" (ngSubmit)="submit()" class="row g-3 mt-1">
            <div class="col-md-4">
              <mat-form-field appearance="outline" class="w-100">
                <mat-label>Amenity Name</mat-label>
                <input matInput formControlName="amenityName" />
                <mat-error>Required</mat-error>
              </mat-form-field>
            </div>
            <div class="col-md-3">
              <mat-form-field appearance="outline" class="w-100">
                <mat-label>Category</mat-label>
                <input matInput formControlName="category" placeholder="e.g. Room, Services" />
                <mat-error>Required</mat-error>
              </mat-form-field>
            </div>
            <div class="col-md-3">
              <mat-form-field appearance="outline" class="w-100">
                <mat-label>Icon Name (optional)</mat-label>
                <input matInput formControlName="iconName" placeholder="e.g. wifi" />
              </mat-form-field>
            </div>
            <div class="col-md-2 d-flex align-items-center">
              <button mat-raised-button color="primary" type="submit" [disabled]="form.invalid || submitting()">
                {{ submitting() ? 'Submitting...' : 'Submit Request' }}
              </button>
            </div>
          </form>
        </mat-card-content>
      </mat-card>

      <!-- My Requests -->
      <mat-card>
        <mat-card-header>
          <mat-card-title>My Requests</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          @if (loading()) {
            <div class="text-center py-4"><mat-spinner diameter="40" /></div>
          } @else if (requests().length === 0 && totalCount() === 0) {
            <div class="text-center py-4 text-muted">No requests submitted yet.</div>
          } @else {
            <table mat-table [dataSource]="requests()" class="w-100">
              <ng-container matColumnDef="amenityName">
                <th mat-header-cell *matHeaderCellDef>Amenity</th>
                <td mat-cell *matCellDef="let r">{{ r.amenityName }}</td>
              </ng-container>
              <ng-container matColumnDef="category">
                <th mat-header-cell *matHeaderCellDef>Category</th>
                <td mat-cell *matCellDef="let r">{{ r.category }}</td>
              </ng-container>
              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef>Status</th>
                <td mat-cell *matCellDef="let r">
                  <mat-chip [color]="getStatusColor(r.status)" highlighted>{{ r.status }}</mat-chip>
                </td>
              </ng-container>
              <ng-container matColumnDef="note">
                <th mat-header-cell *matHeaderCellDef>Note</th>
                <td mat-cell *matCellDef="let r">{{ r.superAdminNote || '—' }}</td>
              </ng-container>
              <ng-container matColumnDef="date">
                <th mat-header-cell *matHeaderCellDef>Date</th>
                <td mat-cell *matCellDef="let r">{{ r.createdAt | date:'mediumDate' }}</td>
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
          }
        </mat-card-content>
      </mat-card>
    </div>
  `
})
export class AmenityRequestsComponent implements OnInit {
  private service = inject(AmenityRequestService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  loading = signal(true);
  submitting = signal(false);
  requests = signal<AmenityRequestResponseDto[]>([]);
  totalCount = signal(0);
  pageSize = 10;
  currentPage = 1;
  displayedColumns = ['amenityName', 'category', 'status', 'note', 'date'];

  form = this.fb.group({
    amenityName: ['', [Validators.required, Validators.maxLength(200)]],
    category: ['', [Validators.required, Validators.maxLength(100)]],
    iconName: ['']
  });

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.service.getMine(this.currentPage, this.pageSize).subscribe({
      next: data => {
        this.requests.set(data.requests ?? []);
        this.totalCount.set(data.totalCount ?? 0);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onPage(e: PageEvent) { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); }

  submit() {
    if (this.form.invalid) return;
    this.submitting.set(true);
    this.service.create(this.form.value as any).subscribe({
      next: () => {
        this.toast.success('Request submitted!');
        this.form.reset();
        this.currentPage = 1;
        this.load();
        this.submitting.set(false);
      },
      error: () => this.submitting.set(false)
    });
  }

  getStatusColor(status: string): 'primary' | 'accent' | 'warn' {
    if (status === 'Approved') return 'primary';
    if (status === 'Pending') return 'accent';
    return 'warn';
  }
}
