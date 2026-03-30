import { Component, inject, signal, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { DatePipe } from '@angular/common';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { AmenityRequestService } from '../../../core/services/amenity-request.service';
import { ToastService } from '../../../core/services/toast.service';
import { AmenityRequestResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-amenity-requests',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink, DatePipe,
    MatTableModule, MatButtonModule, MatFormFieldModule, MatInputModule,
    MatIconModule, MatChipsModule, MatProgressSpinnerModule, MatPaginatorModule,
  ],
  templateUrl: './amenity-requests.component.html',
  styleUrl: './amenity-requests.component.scss',
})
export class AmenityRequestsComponent implements OnInit {
  private service = inject(AmenityRequestService);
  private toast   = inject(ToastService);
  private fb      = inject(FormBuilder);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  loading    = signal(false);
  submitting = signal(false);
  requests   = signal<AmenityRequestResponseDto[]>([]);
  totalCount = signal(0);
  pageSize   = 10;
  currentPage = 1;
  searchTerm  = '';
  displayedColumns = ['amenityName', 'category', 'status', 'note', 'date'];

  private searchSubject = new Subject<string>();

  form = this.fb.group({
    amenityName: ['', [Validators.required, Validators.maxLength(200)]],
    category:    ['', [Validators.required, Validators.maxLength(100)]],
    iconName:    ['']
  });

  ngOnInit() {
    this.load();
    this.searchSubject.pipe(debounceTime(400), distinctUntilChanged())
      .subscribe(s => { this.searchTerm = s; this.resetPage(); this.load(); });
  }

  private resetPage() {
    this.currentPage = 1;
    this.paginator?.firstPage();
  }

  load() {
    this.loading.set(true);
    this.service.getMine(this.currentPage, this.pageSize, this.searchTerm || undefined).subscribe({
      next: data => {
        this.requests.set(data.requests ?? []);
        this.totalCount.set(data.totalCount ?? 0);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onSearch(e: Event) {
    this.searchSubject.next((e.target as HTMLInputElement).value);
  }

  onPage(e: PageEvent) { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); window.scrollTo({ top: 0, behavior: 'smooth' }); }

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.submitting.set(true);
    this.service.create(this.form.value as any).subscribe({
      next: () => {
        this.toast.success('Request submitted!');
        this.form.reset();
        this.resetPage();
        this.load();
        this.submitting.set(false);
      },
      error: () => this.submitting.set(false)
    });
  }

  statusClass(status: string): string {
    const m: Record<string, string> = { Approved: 'badge-success', Pending: 'badge-warning', Rejected: 'badge-error' };
    return m[status] ?? 'badge-muted';
  }
}
