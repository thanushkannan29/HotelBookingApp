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
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { CityService } from '../../../core/services/city.service';
import { ToastService } from '../../../core/services/toast.service';
import { CityDto } from '../../../core/models/models';

@Component({
  selector: 'app-city-management',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatCardModule, MatTableModule, MatButtonModule, MatFormFieldModule,
    MatInputModule, MatPaginatorModule, MatIconModule, MatDialogModule,
    MatChipsModule, MatProgressSpinnerModule, MatSlideToggleModule, MatTooltipModule
  ],
  template: `
    <div class="container py-4">
      <div class="d-flex justify-content-between align-items-center mb-4">
        <h2>🏙️ City Management</h2>
        <button mat-raised-button color="primary" (click)="openForm()">
          <mat-icon>add</mat-icon> Add City
        </button>
      </div>

      <!-- Add/Edit Form -->
      @if (showForm()) {
        <mat-card class="mb-4">
          <mat-card-header>
            <mat-card-title>{{ editingId() ? 'Edit City' : 'Add New City' }}</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <form [formGroup]="form" (ngSubmit)="save()" class="row g-3 mt-1">
              <div class="col-md-4">
                <mat-form-field appearance="outline" class="w-100">
                  <mat-label>City Name</mat-label>
                  <input matInput formControlName="cityName" />
                  <mat-error>Required</mat-error>
                </mat-form-field>
              </div>
              <div class="col-md-4">
                <mat-form-field appearance="outline" class="w-100">
                  <mat-label>State Name</mat-label>
                  <input matInput formControlName="stateName" />
                  <mat-error>Required</mat-error>
                </mat-form-field>
              </div>
              <div class="col-md-2">
                <mat-form-field appearance="outline" class="w-100">
                  <mat-label>Pin Code</mat-label>
                  <input matInput formControlName="pinCode" />
                </mat-form-field>
              </div>
              <div class="col-md-2 d-flex gap-2 align-items-center">
                <button mat-raised-button color="primary" type="submit" [disabled]="form.invalid || saving()">
                  {{ saving() ? 'Saving...' : 'Save' }}
                </button>
                <button mat-button type="button" (click)="cancelForm()">Cancel</button>
              </div>
            </form>
          </mat-card-content>
        </mat-card>
      }

      <!-- Search -->
      <mat-form-field appearance="outline" class="w-100 mb-3">
        <mat-label>🔍 Search cities</mat-label>
        <input matInput (input)="onSearch($event)" placeholder="Search by name or state..." />
        <mat-icon matSuffix>search</mat-icon>
      </mat-form-field>

      @if (loading()) {
        <div class="text-center py-5"><mat-spinner diameter="48" /></div>
      } @else {
        <mat-card>
          <mat-card-content>
            <table mat-table [dataSource]="cities()" class="w-100">
              <ng-container matColumnDef="cityName">
                <th mat-header-cell *matHeaderCellDef>City</th>
                <td mat-cell *matCellDef="let c">{{ c.cityName }}</td>
              </ng-container>
              <ng-container matColumnDef="stateName">
                <th mat-header-cell *matHeaderCellDef>State</th>
                <td mat-cell *matCellDef="let c">{{ c.stateName }}</td>
              </ng-container>
              <ng-container matColumnDef="pinCode">
                <th mat-header-cell *matHeaderCellDef>Pin Code</th>
                <td mat-cell *matCellDef="let c">{{ c.pinCode }}</td>
              </ng-container>
              <ng-container matColumnDef="isActive">
                <th mat-header-cell *matHeaderCellDef>Active</th>
                <td mat-cell *matCellDef="let c">
                  <mat-chip [color]="c.isActive ? 'primary' : 'warn'" highlighted>
                    {{ c.isActive ? 'Active' : 'Inactive' }}
                  </mat-chip>
                </td>
              </ng-container>
              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef>Actions</th>
                <td mat-cell *matCellDef="let c">
                  <button mat-icon-button (click)="edit(c)" matTooltip="Edit"><mat-icon>edit</mat-icon></button>
                  <button mat-icon-button (click)="toggle(c)" [matTooltip]="c.isActive ? 'Deactivate' : 'Activate'">
                    <mat-icon>{{ c.isActive ? 'toggle_on' : 'toggle_off' }}</mat-icon>
                  </button>
                  <button mat-icon-button color="warn" (click)="delete(c.cityId)" matTooltip="Delete">
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
export class CityManagementComponent implements OnInit {
  private cityService = inject(CityService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  loading = signal(true);
  saving = signal(false);
  showForm = signal(false);
  editingId = signal<string | null>(null);
  cities = signal<CityDto[]>([]);
  totalCount = signal(0);
  pageSize = 10;
  currentPage = 1;
  searchTerm = '';
  displayedColumns = ['cityName', 'stateName', 'pinCode', 'isActive', 'actions'];
  private searchSubject = new Subject<string>();

  form = this.fb.group({
    cityName: ['', [Validators.required, Validators.maxLength(100)]],
    stateName: ['', [Validators.required, Validators.maxLength(100)]],
    pinCode: ['', Validators.maxLength(10)]
  });

  ngOnInit() {
    this.load();
    this.searchSubject.pipe(debounceTime(400), distinctUntilChanged())
      .subscribe(s => { this.searchTerm = s; this.currentPage = 1; this.load(); });
  }

  load() {
    this.loading.set(true);
    this.cityService.getAllPaged(this.currentPage, this.pageSize, this.searchTerm).subscribe({
      next: data => { this.cities.set(data.cities); this.totalCount.set(data.totalCount); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  openForm() { this.showForm.set(true); this.editingId.set(null); this.form.reset(); }
  cancelForm() { this.showForm.set(false); this.editingId.set(null); this.form.reset(); }

  edit(city: CityDto) {
    this.editingId.set(city.cityId);
    this.form.patchValue({ cityName: city.cityName, stateName: city.stateName, pinCode: city.pinCode });
    this.showForm.set(true);
  }

  save() {
    if (this.form.invalid) return;
    this.saving.set(true);
    const dto = this.form.value as any;
    const req = this.editingId()
      ? this.cityService.update(this.editingId()!, dto)
      : this.cityService.add(dto);
    req.subscribe({
      next: () => { this.toast.success('City saved!'); this.cancelForm(); this.load(); this.saving.set(false); },
      error: () => this.saving.set(false)
    });
  }

  toggle(city: CityDto) {
    this.cityService.toggleStatus(city.cityId).subscribe({
      next: () => { this.toast.success('Status updated'); this.load(); }
    });
  }

  delete(id: string) {
    if (!confirm('Delete this city?')) return;
    this.cityService.delete(id).subscribe({
      next: () => { this.toast.success('City deleted'); this.load(); }
    });
  }

  onSearch(e: Event) { this.searchSubject.next((e.target as HTMLInputElement).value); }
  onPage(e: PageEvent) { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); }
}
