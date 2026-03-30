import { Component, inject, signal, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { AmenityService } from '../../../core/services/amenity.service';
import { ToastService } from '../../../core/services/toast.service';
import { AmenityResponseDto } from '../../../core/models/models';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-superadmin-amenity-management',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatTableModule, MatButtonModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatPaginator, MatPaginatorModule, MatIconModule,
    MatChipsModule, MatProgressSpinnerModule, MatSlideToggleModule,
    MatTooltipModule, MatDialogModule,
  ],
  templateUrl: './superadmin-amenity-management.component.html',
  styleUrl: './superadmin-amenity-management.component.scss',
})
export class SuperadminAmenityManagementComponent implements OnInit {
  private service = inject(AmenityService);
  private toast   = inject(ToastService);
  private fb      = inject(FormBuilder);
  private dialog  = inject(MatDialog);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  loading    = signal(false);
  saving     = signal(false);
  amenities  = signal<AmenityResponseDto[]>([]);
  totalCount = signal(0);
  editingId  = signal<string | null>(null);
  pageSize   = 10;
  currentPage = 1;
  searchQuery = '';
  selectedCategory = 'All';
  displayedColumns = ['name', 'category', 'iconName', 'status', 'actions'];
  categories = ['Room', 'Bathroom', 'Tech', 'Services', 'Food'];

  private searchSubject = new Subject<string>();

  form = this.fb.group({
    name:     ['', Validators.required],
    category: ['', Validators.required],
    iconName: [''],
  });

  ngOnInit() {
    this.searchSubject.pipe(debounceTime(400), distinctUntilChanged())
      .subscribe(() => { this.currentPage = 1; this.paginator?.firstPage(); this.load(); });
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
  onCategoryChange() { this.currentPage = 1; this.paginator?.firstPage(); this.load(); }
  onPage(e: PageEvent) { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); window.scrollTo({ top: 0, behavior: 'smooth' }); }

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
        this.form.reset(); this.editingId.set(null); this.saving.set(false); this.load();
      },
      error: () => this.saving.set(false)
    });
  }

  toggle(a: AmenityResponseDto) {
    this.service.toggleStatus(a.amenityId).subscribe({
      next: res => { this.toast.success(`Amenity ${res.isActive ? 'activated' : 'deactivated'}.`); this.load(); }
    });
  }

  delete(a: AmenityResponseDto) {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Delete Amenity',
        message: `Delete "${a.name}"? This cannot be undone.`,
        confirmLabel: 'Delete',
        confirmColor: 'warn',
        icon: 'delete_forever'
      }
    });
    ref.afterClosed().subscribe(ok => {
      if (!ok) return;
      this.service.delete(a.amenityId).subscribe({
        next: () => { this.toast.success('Amenity deleted.'); this.load(); },
        error: () => this.toast.error('Cannot delete — amenity is in use by room types.')
      });
    });
  }
}
