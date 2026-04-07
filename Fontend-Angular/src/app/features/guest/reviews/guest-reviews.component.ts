import { Component, inject, signal, OnInit, AfterViewInit, ViewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DatePipe, SlicePipe } from '@angular/common';
import { ReviewService } from '../../../core/services/api.services';
import { BookingService } from '../../../core/services/booking.service';
import { ToastService } from '../../../core/services/toast.service';
import { MyReviewsResponseDto, ReservationDetailsDto } from '../../../core/models/models';

@Component({
  selector: 'app-guest-reviews',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink, DatePipe, SlicePipe,
    MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatTooltipModule, MatDividerModule,
    MatTableModule, MatSortModule, MatPaginatorModule, MatProgressSpinnerModule,
  ],
  templateUrl: './guest-reviews.component.html',
  styleUrl: './guest-reviews.component.scss'
})
export class GuestReviewsComponent implements OnInit, AfterViewInit {
  private reviewService  = inject(ReviewService);
  private bookingService = inject(BookingService);
  private toast          = inject(ToastService);
  private fb             = inject(FormBuilder);

  reviews        = signal<MyReviewsResponseDto[]>([]);
  completedStays = signal<ReservationDetailsDto[]>([]);
  editingId      = signal<string | null>(null);
  showAddForm    = signal(false);
  isSaving       = signal(false);
  loading        = signal(false);
  totalCount     = signal(0);

  dataSource = new MatTableDataSource<MyReviewsResponseDto>([]);
  displayedColumns = ['hotel', 'stay', 'rating', 'comment', 'date', 'actions'];
  stars = [1, 2, 3, 4, 5];
  pageSize = 5;
  readonly reviewRewardPoints = 10;

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

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
    this.loadReviews();
    this.bookingService.getMyReservations().subscribe((res: ReservationDetailsDto[]) => {
      this.completedStays.set(res.filter((r: ReservationDetailsDto) => r.status === 'Completed'));
    });
  }

  ngAfterViewInit() {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
  }

  get reviewableStays(): ReservationDetailsDto[] {
    const reviewedResIds = new Set(this.reviews().map(r => r.reservationId));
    return this.completedStays().filter(s => !reviewedResIds.has(s.reservationId));
  }

  // Alias used by tests
  get reviewableHotels(): ReservationDetailsDto[] {
    const reviewedHotelIds = new Set(this.reviews().map(r => r.hotelId));
    const seen = new Set<string>();
    return this.completedStays().filter(s => {
      if (reviewedHotelIds.has(s.hotelId) || seen.has(s.hotelId)) return false;
      seen.add(s.hotelId);
      return true;
    });
  }

  stayLabel(stay: ReservationDetailsDto): string {
    return `${stay.hotelName} — ${stay.reservationCode}`;
  }

  onPage(_event: PageEvent) {}

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
        this.loadReviews();
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
        this.loadReviews();
        this.isSaving.set(false);
      },
      error: () => this.isSaving.set(false),
    });
  }

  deleteReview(reviewId: string) {
    if (!confirm('Delete this review?')) return;
    this.reviewService.deleteReview(reviewId).subscribe(() => {
      this.toast.success('Review deleted.');
      this.reviews.update(r => r.filter(x => x.reviewId !== reviewId));
      this.dataSource.data = this.reviews();
    });
  }
  private loadReviews() {
    this.reviewService.getMyReviewsPaged(1, 100).subscribe(res => {
      const r = res.reviews as MyReviewsResponseDto[];
      this.reviews.set(r);
      this.totalCount.set(res.totalCount);
      this.dataSource.data = r;
    });
  }
}
