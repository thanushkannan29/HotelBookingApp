import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { AmenityService } from '../../../core/services/amenity.service';
import { ToastService } from '../../../core/services/toast.service';
import { AmenityResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-superadmin-amenity-management',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatCardModule, MatTableModule, MatButtonModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatPaginatorModule, MatIconModule,
    MatChipsModule, MatProgressSpinnerModule, MatSlideToggleModule,
  ],
  template: `
    <div class="container py-4">
      <h2 class="mb-3">🏷️ Amenity Management</h2>

      <!-- Add / Edit Form -->
      <mat-card class="mb-4">
        <mat-card-header>
          <mat-card-title>{{ editingId() ? 'Edit Amenity' : 'Add New Amenity' }}</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <form [formGroup]="form" (ngSubmit)="save()" class="d-flex flex-wrap gap-3 align-items-start pt-3">
            <mat-form-field appearance="outline">
              <mat-label>Name</mat-label>
              <input matInput formControlName="name" />
              @if (form.get('name')?.hasError('required') && form.get('name')?.touched) {
                <mat-error>Required</mat-error>
              }
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Category</mat-label>
              <mat-select formControlName="category">
                @for (c of categories; track c) {
                  <mat-option [value]="c">{{ c }}</mat-option>
                }
              </mat-select>
              @if (form.get('category')?.hasError('required') && form.get('category')?.touched) {
                <mat-error>Required</mat-error>
              }
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Icon Name (optional)</mat-label>
              <input matInput formControlName="iconName" placeholder="e.g. wifi" />
            </mat-form-field>
            <div class="d-flex gap-2 align-items-center">
              <button mat-flat-button color="primary" type="submit" [disabled]="saving()">
                {{ saving() ? 'Saving…' : (editingId() ? 'Update' : 'Add Amenity') }}
              </button>
              @if (editingId()) {
                <button mat-stroked-button type="button" (click)="cancelEdit()">Cancel</button>
              }
            </div>
          </form>
        </mat-card-content>
      </mat-card>

      <!-- Filters -->
      <div class="d-flex gap-3 mb-3 flex-wrap">
        <mat-form-field appearance="outline">
          <mat-label>Search</mat-label>
          <mat-icon matPrefix>search</mat-icon>
          <input matInput (input)="onSearch($any($event.target).value)" placeholder="Name or category…" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Category</mat-label>
          <mat-select [(value)]="selectedCategory" (selectionChange)="onCategoryChange()">
            <mat-option value="All">All</mat-option>
            @for (c of categories; track c) {
              <mat-option [value]="c">{{ c }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
      </div>

      @if (loading()) {
        <div class="text-center py-5"><mat-spinner diameter="48" /></div>
      } @else {
        <mat-card>
          <mat-card-content>
            <table mat-table [dataSource]="amenities()" class="w-100">
              <ng-container matColumnDef="name">
                <th mat-header-cell *matHeaderCellDef>Name</th>
                <td mat-cell *matCellDef="let a">{{ a.name }}</td>
              </ng-container>
              <ng-container matColumnDef="category">
                <th mat-header-cell *matHeaderCellDef>Category</th>
                <td mat-cell *matCellDef="let a">{{ a.category }}</td>
              </ng-container>
              <ng-container matColumnDef="iconName">
                <th mat-header-cell *matHeaderCellDef>Icon</th>
                <td mat-cell *matCellDef="let a">{{ a.iconName || '—' }}</td>
              </ng-container>
              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef>Status</th>
                <td mat-cell *matCellDef="let a">
                  <mat-chip [color]="a.isActive ? 'primary' : undefined" highlighted>
                    {{ a.isActive ? 'Active' : 'Inactive' }}
                  </mat-chip>
                </td>
              </ng-container>
              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef>Actions</th>
                <td mat-cell *matCellDef="let a">
                  <button mat-icon-button (click)="startEdit(a)" matTooltip="Edit">
                    <mat-icon>edit</mat-icon>
                  </button>
                  <mat-slide-toggle
                    [checked]="a.isActive"
                    (change)="toggle(a)"
                    [matTooltip]="a.isActive ? 'Deactivate' : 'Activate'"
                    class="mx-2">
                  </mat-slide-toggle>
                  <button mat-icon-button color="warn" (click)="delete(a)" matTooltip="Delete">
                    <mat-icon>delete</mat-icon>
                  </button>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
            </table>
            <mat-paginator
              [length]="totalCount()"
              [pageSize]="pageSize"
              [pageSizeOptions]="[10, 20, 50]"
              showFirstLastButtons
              (page)="onPage($event)"
            />
          </mat-card-content>
        </mat-card>
      }
    </div>
  `
})
export class SuperadminAmenityManagementComponent implements OnInit {
  private service = inject(AmenityService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  loading = signal(true);
  saving = signal(false);
  amenities = signal<AmenityResponseDto[]>([]);
  totalCount = signal(0);
  editingId = signal<string | null>(null);
  pageSize = 10;
  currentPage = 1;
  searchQuery = '';
  selectedCategory = 'All';
  displayedColumns = ['name', 'category', 'iconName', 'status', 'actions'];
  categories = ['Room', 'Bathroom', 'Tech', 'Services', 'Food'];

  private searchSubject = new Subject<string>();

  form = this.fb.group({
    name: ['', Validators.required],
    category: ['', Validators.required],
    iconName: [''],
  });

  ngOnInit() {
    this.searchSubject.pipe(debounceTime(400), distinctUntilChanged())
      .subscribe(() => { this.currentPage = 1; this.load(); });
    this.load();
  }

  load() {
    this.loading.set(true);
    this.service.getAllPaged(this.currentPage, this.pageSize, this.searchQuery || undefined, this.selectedCategory).subscribe({
      next: data => { this.amenities.set(data.amenities); this.totalCount.set(data.totalCount); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  onSearch(value: string) { this.searchQuery = value; this.searchSubject.next(value); }
  onCategoryChange() { this.currentPage = 1; this.load(); }
  onPage(e: PageEvent) { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); }

  startEdit(a: AmenityResponseDto) {
    this.editingId.set(a.amenityId);
    this.form.patchValue({ name: a.name, category: a.category, iconName: a.iconName ?? '' });
  }

  cancelEdit() { this.editingId.set(null); this.form.reset(); }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    const v = this.form.value;
    const id = this.editingId();
    const obs = id
      ? this.service.update({ amenityId: id, name: v.name!, category: v.category!, iconName: v.iconName || undefined, isActive: true })
      : this.service.create({ name: v.name!, category: v.category!, iconName: v.iconName || undefined });

    obs.subscribe({
      next: () => {
        this.toast.success(id ? 'Amenity updated.' : 'Amenity created.');
        this.form.reset();
        this.editingId.set(null);
        this.saving.set(false);
        this.load();
      },
      error: () => this.saving.set(false)
    });
  }

  toggle(a: AmenityResponseDto) {
    this.service.toggleStatus(a.amenityId).subscribe({
      next: res => {
        this.toast.success(`Amenity ${res.isActive ? 'activated' : 'deactivated'}.`);
        this.load();
      }
    });
  }

  delete(a: AmenityResponseDto) {
    if (!confirm(`Delete "${a.name}"? This cannot be undone.`)) return;
    this.service.delete(a.amenityId).subscribe({
      next: () => { this.toast.success('Amenity deleted.'); this.load(); },
      error: () => this.toast.error('Cannot delete — amenity is in use by room types.')
    });
  }
}
