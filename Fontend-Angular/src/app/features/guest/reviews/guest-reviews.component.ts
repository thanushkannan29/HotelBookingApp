import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { DatePipe } from '@angular/common';
import { ReviewService } from '../../../core/services/api.services';
import { BookingService } from '../../../core/services/booking.service';
import { ToastService } from '../../../core/services/toast.service';
import { MyReviewsResponseDto, ReservationDetailsDto } from '../../../core/models/models';

@Component({
  selector: 'app-guest-reviews',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink, DatePipe,
    MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatTooltipModule, MatDividerModule
  ],
  templateUrl: './guest-reviews.component.html',
  styleUrl: './guest-reviews.component.scss'
})
export class GuestReviewsComponent implements OnInit {
  private reviewService = inject(ReviewService);
  private bookingService= inject(BookingService);  // not in BookingService — use directly
  private toast         = inject(ToastService);
  private fb            = inject(FormBuilder);

  reviews        = signal<MyReviewsResponseDto[]>([]);
  completedStays = signal<ReservationDetailsDto[]>([]);
  editingId      = signal<string | null>(null);
  showAddForm    = signal(false);
  isSaving       = signal(false);

  addForm = this.fb.group({
    hotelId:  ['', Validators.required],
    rating:   [5, [Validators.required, Validators.min(1), Validators.max(5)]],
    comment:  ['', [Validators.required, Validators.minLength(10)]],
    imageUrl: [''],
  });

  editForm = this.fb.group({
    rating:   [5, [Validators.required, Validators.min(1), Validators.max(5)]],
    comment:  ['', [Validators.required]],
    imageUrl: [''],
  });

  stars = [1, 2, 3, 4, 5];

  ngOnInit() {
    this.reviewService.getMyReviews().subscribe(r => this.reviews.set(r));
    // Load completed reservations so user can pick which hotel to review
    this.bookingService.getMyReservations().subscribe((res: ReservationDetailsDto[]) => {
      this.completedStays.set(res.filter((r: ReservationDetailsDto) => r.status === 'Completed'));
    });
  }

  // Unique hotels from completed stays that haven't been reviewed yet
  get reviewableHotels() {
    const reviewed = new Set(this.reviews().map(r => r.hotelId));
    const seen     = new Set<string>();
    return this.completedStays().filter(s => {
      if (reviewed.has(s.hotelId) || seen.has(s.hotelId)) return false;
      seen.add(s.hotelId);
      return true;
    });
  }

  addReview() {
    if (this.addForm.invalid) { this.addForm.markAllAsTouched(); return; }
    this.isSaving.set(true);
    const v = this.addForm.value;
    this.reviewService.addReview({
      hotelId:  v.hotelId!,
      rating:   v.rating!,
      comment:  v.comment!,
      imageUrl: v.imageUrl || undefined,
    }).subscribe({
      next: () => {
        this.toast.success('Review posted!');
        this.addForm.reset({ rating: 5 });
        this.showAddForm.set(false);
        this.reviewService.getMyReviews().subscribe(r => this.reviews.set(r));
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
        this.reviewService.getMyReviews().subscribe(r => this.reviews.set(r));
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
    });
  }
}
