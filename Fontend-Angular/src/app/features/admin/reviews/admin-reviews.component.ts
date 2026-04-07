import { Component, inject, signal, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DatePipe } from '@angular/common';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { ReviewService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { ReviewResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-admin-reviews',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, DatePipe,
    MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatPaginatorModule, MatProgressSpinnerModule, MatTooltipModule,
  ],
  templateUrl: './admin-reviews.component.html',
  styleUrl: './admin-reviews.component.scss',
})
export class AdminReviewsComponent implements OnInit {
  private reviewService = inject(ReviewService);
  private toast         = inject(ToastService);
  private fb            = inject(FormBuilder);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  loading      = signal(false);
  isSaving     = signal(false);
  reviews      = signal<ReviewResponseDto[]>([]);
  totalCount   = signal(0);
  pageSize     = 10;
  currentPage  = 1;
  replyingId   = signal<string | null>(null);
  stars        = [1, 2, 3, 4, 5];

  // Sort/filter state
  ratingFilter = 0;   // 0 = all
  sortDir      = '';  // '' | 'asc' | 'desc'

  replyForm = this.fb.group({
    adminReply: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(500)]]
  });

  ngOnInit() { this.load(); }

  private resetPage() {
    this.currentPage = 1;
    this.paginator?.firstPage();
  }

  load() {
    this.loading.set(true);
    const min = this.ratingFilter > 0 ? this.ratingFilter : undefined;
    const max = this.ratingFilter > 0 ? this.ratingFilter : undefined;
    const sort = this.sortDir || undefined;
    this.reviewService.getHotelReviewsAdmin(this.currentPage, this.pageSize, min, max, sort).subscribe({
      next: res => {
        this.reviews.set((res as any).reviews ?? []);
        this.totalCount.set((res as any).totalCount ?? 0);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onPage(e: PageEvent) { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); window.scrollTo({ top: 0, behavior: 'smooth' }); }

  onRatingFilter(rating: number) {
    this.ratingFilter = this.ratingFilter === rating ? 0 : rating;
    this.resetPage(); this.load();
  }

  onSort(dir: string) {
    this.sortDir = this.sortDir === dir ? '' : dir;
    this.resetPage(); this.load();
  }

  startReply(reviewId: string, existing?: string) {
    this.replyingId.set(reviewId);
    this.replyForm.patchValue({ adminReply: existing ?? '' });
  }

  cancelReply() { this.replyingId.set(null); this.replyForm.reset(); }

  submitReply() {
    if (this.replyForm.invalid) { this.replyForm.markAllAsTouched(); return; }
    const id = this.replyingId()!;
    const reply = this.replyForm.get('adminReply')!.value!;
    this.isSaving.set(true);
    this.reviewService.replyToReview(id, reply).subscribe({
      next: () => {
        this.toast.success('Reply saved.');
        this.reviews.update(list =>
          list.map(r => r.reviewId === id ? { ...r, adminReply: reply } : r)
        );
        this.cancelReply();
        this.isSaving.set(false);
      },
      error: () => this.isSaving.set(false),
    });
  }
}
