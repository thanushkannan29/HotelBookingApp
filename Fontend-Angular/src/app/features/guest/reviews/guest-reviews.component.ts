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
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { DatePipe } from '@angular/common';
import { ReviewService } from '../../../core/services/api.services';
import { BookingService } from '../../../core/services/booking.service';
import { ToastService } from '../../../core/services/toast.service';
import { MyReviewsResponseDto, ReservationDetailsDto } from '../../../core/models/models';
<<<<<<< Updated upstream
import { SlicePipe } from '@angular/common';
=======
import { environment } from '../../../../environments/environment';
>>>>>>> Stashed changes

@Component({
  selector: 'app-guest-reviews',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink, DatePipe,
    MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatTooltipModule, MatDividerModule,
    MatTableModule, MatSortModule, MatPaginatorModule,SlicePipe,
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

  // F5: MatTable for reviews list
  dataSource = new MatTableDataSource<MyReviewsResponseDto>([]);
  displayedColumns = ['hotel', 'stay', 'rating', 'comment', 'date', 'actions'];
<<<<<<< Updated upstream
=======
  stars = [1, 2, 3, 4, 5];
  readonly reviewRewardPoints = environment.reviewRewardPoints;
>>>>>>> Stashed changes

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  // F6A: per-reservation form — uses reservationId instead of hotelId
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

  stars = [1, 2, 3, 4, 5];

  ngOnInit() {
    this.reviewService.getMyReviews().subscribe(r => {
      this.reviews.set(r);
      this.dataSource.data = r;
    });
    this.bookingService.getMyReservations().subscribe((res: ReservationDetailsDto[]) => {
      this.completedStays.set(res.filter((r: ReservationDetailsDto) => r.status === 'Completed'));
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

  // F6A: per-reservation logic — show ALL completed reservations without a review
  get reviewableStays(): ReservationDetailsDto[] {
    const reviewedResIds = new Set(this.reviews().map(r => r.reservationId));
    return this.completedStays().filter(s => !reviewedResIds.has(s.reservationId));
  }

  // Label shown in the reservation picker
  stayLabel(stay: ReservationDetailsDto): string {
    return `${stay.hotelName} — ${stay.reservationCode}`;
  }

  // F6A + F6B: pass reservationId + derive hotelId from selected stay
  addReview() {
    if (this.addForm.invalid) { this.addForm.markAllAsTouched(); return; }
    this.isSaving.set(true);
    const v = this.addForm.value;
    const stay = this.completedStays().find(s => s.reservationId === v.reservationId);
    this.reviewService.addReview({
      hotelId:      stay?.hotelId ?? '',
      reservationId: v.reservationId!,
      rating:       v.rating!,
      comment:      v.comment!,
      imageUrl:     v.imageUrl || undefined,
    }).subscribe({
      next: () => {
        this.toast.success('Review posted!');
        this.addForm.reset({ rating: 5 });
        this.showAddForm.set(false);
        this.refreshReviews();
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
        this.refreshReviews();
        this.isSaving.set(false);
      },
      error: () => this.isSaving.set(false),
    });
  }

  deleteReview(reviewId: string) {
    if (!confirm('Delete this review?')) return;
    this.reviewService.deleteReview(reviewId).subscribe(() => {
      this.toast.success('Review deleted.');
      this.refreshReviews();
    });
  }

  private refreshReviews() {
    this.reviewService.getMyReviews().subscribe(r => {
      this.reviews.set(r);
      this.dataSource.data = r;
    });
  }
}