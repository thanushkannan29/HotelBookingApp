import { Component, inject, signal, OnInit, ViewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CommonModule, DatePipe, SlicePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { MatTableModule } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ReviewService } from '../../../core/services/api.services';
import { BookingService } from '../../../core/services/booking.service';
import { ToastService } from '../../../core/services/toast.service';
import { MyReviewsResponseDto, ReservationDetailsDto } from '../../../core/models/models';

@Component({
  selector: 'app-guest-reviews',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink, DatePipe, SlicePipe,
    MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatTooltipModule, MatDividerModule,
    MatTableModule, MatPaginatorModule, MatProgressSpinnerModule,
  ],
  templateUrl: './guest-reviews.component.html',
  styleUrl: './guest-reviews.component.scss'
})
export class GuestReviewsComponent implements OnInit {
  private reviewService  = inject(ReviewService);
  private bookingService = inject(BookingService);
  private toast          = inject(ToastService);
  private fb             = inject(FormBuilder);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  reviews        = signal<MyReviewsResponseDto[]>([]);
  totalCount     = signal(0);
  loading        = signal(false);
  completedStays = signal<ReservationDetailsDto[]>([]);
  editingId      = signal<string | null>(null);
  showAddForm    = signal(false);
  isSaving       = signal(false);

  pageSize    = 10;
  currentPage = 1;
  displayedColumns = ['hotel', 'stay', 'rating', 'comment', 'date', 'actions'];
  stars = [1, 2, 3, 4, 5];

  addForm = this.fb.group({
    reservationId: ['', Validators.required],
    rating:        [5, [Validators.required, Validators.min(1), Validators.max(5)]],
    comment:       ['', [Validators.required, Validators.minLength(10)]],
    imageUrl:      [''],
  });

  editForm = this.fb.group({
    rating:   [5, [Validators.required, Validators.min(1), Validators.max(5)]],
    comment:  ['', [Validators.required]],
    imageUrl: [''],
  });

  ngOnInit() {
    this.load();
    this.bookingService.getMyReservations().subscribe((res: ReservationDetailsDto[]) => {
      this.completedStays.set(res.filter((r: ReservationDetailsDto) => r.status === 'Completed'));
    });
  }

  load() {
    this.loading.set(true);
    this.reviewService.getMyReviewsPaged(this.currentPage, this.pageSize).subscribe({
      next: res => {
        this.reviews.set(res.reviews ?? []);
        this.totalCount.set(res.totalCount ?? 0);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onPage(e: PageEvent) {
    this.currentPage = e.pageIndex + 1;
    this.pageSize    = e.pageSize;
    this.load();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  get reviewableStays(): ReservationDetailsDto[] {
    const reviewedResIds = new Set(this.reviews().map(r => r.reservationId));
    return this.completedStays().filter(s => !reviewedResIds.has(s.reservationId));
  }

  stayLabel(stay: ReservationDetailsDto): string {
    return `${stay.hotelName} — ${stay.reservationCode}`;
  }

  addReview() {
    if (this.addForm.invalid) { this.addForm.markAllAsTouched(); return; }
    this.isSaving.set(true);
    const v = this.addForm.value;
    const stay = this.completedStays().find(s => s.reservationId === v.reservationId);
    this.reviewService.addReview({
      hotelId:       stay?.hotelId ?? '',
      reservationId: v.reservationId!,
      rating:        v.rating!,
      comment:       v.comment!,
      imageUrl:      v.imageUrl || undefined,
    }).subscribe({
      next: () => {
        this.toast.success('Review posted!');
        this.addForm.reset({ rating: 5 });
        this.showAddForm.set(false);
        this.currentPage = 1;
        this.paginator?.firstPage();
        this.load();
        this.isSaving.set(false);
      },
      error: () => this.isSaving.set(false),
    });
  }

  startEdit(r: MyReviewsResponseDto) {
    this.editingId.set(r.reviewId);
    this.editForm.patchValue({ rating: r.rating, comment: r.comment, imageUrl: r.imageUrl ?? '' });
  }

  saveEdit(reviewId: string) {
    if (this.editForm.invalid) return;
    this.isSaving.set(true);
    const v = this.editForm.value;
    this.reviewService.updateReview(reviewId, {
      rating:   v.rating!,
      comment:  v.comment!,
      imageUrl: v.imageUrl || undefined,
    }).subscribe({
      next: () => {
        this.toast.success('Review updated.');
        this.editingId.set(null);
        this.load();
        this.isSaving.set(false);
      },
      error: () => this.isSaving.set(false),
    });
  }

  deleteReview(reviewId: string) {
    if (!confirm('Delete this review?')) return;
    this.reviewService.deleteReview(reviewId).subscribe(() => {
      this.toast.success('Review deleted.');
      this.load();
    });
  }
}
