import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { GuestReviewsComponent } from './guest-reviews.component';
import { ReviewService } from '../../../core/services/api.services';
import { BookingService } from '../../../core/services/booking.service';
import { ToastService } from '../../../core/services/toast.service';
import { MyReviewsResponseDto, ReservationDetailsDto, ReviewResponseDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ── Mock data ──────────────────────────────────────────────────────────────────

const MOCK_REVIEWS: MyReviewsResponseDto[] = [
  { reviewId: 'rev-001', hotelId: 'hotel-001', hotelName: 'Grand Palace', reservationId: 'id-RES-0001', reservationCode: 'RES-0001', rating: 5, comment: 'Absolutely wonderful stay!', createdDate: '2025-01-10T10:00:00Z', contributionPoints: 10 },
  { reviewId: 'rev-002', hotelId: 'hotel-002', hotelName: 'Sea View Inn',  reservationId: 'id-RES-0002', reservationCode: 'RES-0002', rating: 4, comment: 'Great location, good service.', imageUrl: 'https://example.com/photo.jpg', createdDate: '2025-01-05T10:00:00Z', contributionPoints: 10 },
];

function makeReservation(code: string, hotelId: string, status: string): ReservationDetailsDto {
  return {
    reservationCode: code,
    reservationId:   `id-${code}`,
    hotelId,
    hotelName:       `Hotel ${hotelId}`,
    roomTypeId:      'rt-001',
    roomTypeName:    'Deluxe',
    checkInDate:     '2025-01-01',
    checkOutDate:    '2025-01-03',
    numberOfRooms:   1,
    totalAmount:     7000,
    gstPercent:      0,
    gstAmount:       0,
    discountPercent: 0,
    discountAmount:  0,
    walletAmountUsed: 0,
    finalAmount:     7000,
    status,
    isCheckedIn:     status === 'Completed',
    createdDate:     '2024-12-01T10:00:00Z',
    rooms:           [],
    cancellationFeePaid:    false,
    cancellationFeeAmount:  0,
    cancellationPolicyText: '',
  };
}

const MOCK_RESERVATIONS: ReservationDetailsDto[] = [
  makeReservation('RES-0001', 'hotel-001', 'Completed'),
  makeReservation('RES-0002', 'hotel-002', 'Completed'),
  makeReservation('RES-0003', 'hotel-003', 'Completed'), // not yet reviewed
  makeReservation('RES-0004', 'hotel-004', 'Confirmed'), // not completed
  makeReservation('RES-0005', 'hotel-005', 'Pending'),   // not completed
];

const MOCK_REVIEW_RESPONSE: ReviewResponseDto = {
  reviewId: 'rev-003', hotelId: 'hotel-003', userId: 'usr-001', userName: 'Alice',
  reservationId: 'id-RES-0003', reservationCode: 'RES-0003',
  rating: 5, comment: 'Fantastic!', createdDate: '2025-02-01T10:00:00Z', contributionPoints: 10
};

const MOCK_UPDATE_RESPONSE: ReviewResponseDto = {
  reviewId: 'rev-001', hotelId: 'hotel-001', userId: 'usr-001', userName: 'Alice',
  reservationId: 'id-RES-0001', reservationCode: 'RES-0001',
  rating: 4, comment: 'Updated comment.', createdDate: '2025-01-10T10:00:00Z', contributionPoints: 10
};

describe('GuestReviewsComponent', () => {
  let component: GuestReviewsComponent;
  let fixture:   ComponentFixture<GuestReviewsComponent>;

  let reviewSpy:  jasmine.SpyObj<ReviewService>;
  let bookingSpy: jasmine.SpyObj<BookingService>;
  let toastSpy:   jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    reviewSpy  = jasmine.createSpyObj('ReviewService', [
      'getMyReviewsPaged', 'addReview', 'updateReview', 'deleteReview'
    ]);
    bookingSpy = jasmine.createSpyObj('BookingService', ['getMyReservations']);
    toastSpy   = jasmine.createSpyObj('ToastService', ['success', 'error']);

    // Default happy-path responses
    reviewSpy.getMyReviewsPaged.and.returnValue(of({ totalCount: 2, reviews: MOCK_REVIEWS }));
    reviewSpy.addReview.and.returnValue(of(MOCK_REVIEW_RESPONSE));
    reviewSpy.updateReview.and.returnValue(of(MOCK_UPDATE_RESPONSE));
    reviewSpy.deleteReview.and.returnValue(of(undefined));
    bookingSpy.getMyReservations.and.returnValue(of(MOCK_RESERVATIONS));

    await TestBed.configureTestingModule({
      imports: [GuestReviewsComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ReviewService,  useValue: reviewSpy  },
        { provide: BookingService, useValue: bookingSpy },
        { provide: ToastService,   useValue: toastSpy   },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(GuestReviewsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── CONSTANTS ──────────────────────────────────────────────────────────────

  it('stars — should be [1, 2, 3, 4, 5]', () => {
    expect(component.stars).toEqual([1, 2, 3, 4, 5]);
  });

  // ── INITIAL SIGNAL STATE ───────────────────────────────────────────────────

  it('editingId — should start as null', () => {
    expect(component.editingId()).toBeNull();
  });

  it('showAddForm — should start as false', () => {
    expect(component.showAddForm()).toBeFalse();
  });

  it('isSaving — should start as false', () => {
    expect(component.isSaving()).toBeFalse();
  });

  // ── ngOnInit ───────────────────────────────────────────────────────────────

  it('ngOnInit — should call getMyReviewsPaged on startup', () => {
    expect(reviewSpy.getMyReviewsPaged).toHaveBeenCalled();
  });

  it('ngOnInit — should call getMyReservations on startup', () => {
    expect(bookingSpy.getMyReservations).toHaveBeenCalledOnceWith();
  });

  it('ngOnInit — should populate reviews signal', () => {
    expect(component.reviews().length).toBe(2);
    expect(component.reviews()[0].hotelName).toBe('Grand Palace');
  });

  it('ngOnInit — should populate completedStays with only Completed reservations', () => {
    expect(component.completedStays().length).toBe(3);
    expect(component.completedStays().every(r => r.status === 'Completed')).toBeTrue();
  });

  it('ngOnInit — should NOT include Confirmed or Pending in completedStays', () => {
    const statuses = component.completedStays().map(r => r.status);
    expect(statuses).not.toContain('Confirmed');
    expect(statuses).not.toContain('Pending');
  });

  // ── reviewableHotels GETTER ────────────────────────────────────────────────

  it('reviewableHotels — should exclude hotels already reviewed', () => {
    const ids = component.reviewableHotels.map(s => s.hotelId);
    expect(ids).not.toContain('hotel-001');
    expect(ids).not.toContain('hotel-002');
  });

  it('reviewableHotels — should include hotel-003 (completed, not reviewed)', () => {
    const ids = component.reviewableHotels.map(s => s.hotelId);
    expect(ids).toContain('hotel-003');
  });

  it('reviewableHotels — should deduplicate hotels (same hotel booked twice)', () => {
    component.completedStays.update(s => [
      ...s,
      makeReservation('RES-0099', 'hotel-003', 'Completed')
    ]);
    const ids = component.reviewableHotels.map(s => s.hotelId);
    const hotel003Count = ids.filter(id => id === 'hotel-003').length;
    expect(hotel003Count).toBe(1);
  });

  it('reviewableHotels — should return empty when all completed hotels are reviewed', () => {
    component.completedStays.set([
      makeReservation('RES-X', 'hotel-001', 'Completed'),
      makeReservation('RES-Y', 'hotel-002', 'Completed'),
    ]);
    expect(component.reviewableHotels.length).toBe(0);
  });

  // ── FORM VALIDATION — addForm ──────────────────────────────────────────────

  it('addForm — should be invalid initially', () => {
    expect(component.addForm.invalid).toBeTrue();
  });

  it('addForm — rating should default to 5', () => {
    expect(component.addForm.get('rating')?.value).toBe(5);
  });

  it('addForm — should be valid when all required fields are filled', () => {
    component.addForm.patchValue({
      reservationId: 'id-RES-0003',
      rating:  5,
      comment: 'Amazing experience overall!',
    });
    expect(component.addForm.valid).toBeTrue();
  });

  it('addForm — should be invalid when reservationId is empty', () => {
    component.addForm.patchValue({ reservationId: '', rating: 5, comment: 'Great stay!' });
    expect(component.addForm.invalid).toBeTrue();
  });

  it('addForm — should be invalid when comment is shorter than 10 characters', () => {
    component.addForm.patchValue({ reservationId: 'id-RES-0003', rating: 5, comment: 'Short' });
    expect(component.addForm.get('comment')?.invalid).toBeTrue();
  });

  it('addForm — should be invalid when rating is 0', () => {
    component.addForm.patchValue({ reservationId: 'id-RES-0003', rating: 0, comment: 'Great stay here!' });
    expect(component.addForm.get('rating')?.invalid).toBeTrue();
  });

  it('addForm — should be invalid when rating exceeds 5', () => {
    component.addForm.patchValue({ reservationId: 'id-RES-0003', rating: 6, comment: 'Great stay here!' });
    expect(component.addForm.get('rating')?.invalid).toBeTrue();
  });

  it('addForm — imageUrl is optional', () => {
    component.addForm.patchValue({
      reservationId: 'id-RES-0003', rating: 4,
      comment: 'Lovely place to stay.', imageUrl: ''
    });
    expect(component.addForm.valid).toBeTrue();
  });

  // ── FORM VALIDATION — editForm ─────────────────────────────────────────────

  it('editForm — rating should default to 5', () => {
    expect(component.editForm.get('rating')?.value).toBe(5);
  });

  it('editForm — should be invalid when comment is empty', () => {
    component.editForm.patchValue({ rating: 4, comment: '' });
    expect(component.editForm.get('comment')?.invalid).toBeTrue();
  });

  it('editForm — should be valid when rating and comment are set', () => {
    component.editForm.patchValue({ rating: 4, comment: 'Good stay' });
    expect(component.editForm.valid).toBeTrue();
  });

  // ── addReview() — HAPPY PATH ───────────────────────────────────────────────

  it('addReview() — should call reviewService.addReview with form values', () => {
    component.completedStays.set(MOCK_RESERVATIONS.filter(r => r.status === 'Completed'));
    component.addForm.patchValue({
      reservationId: 'id-RES-0003', rating: 5,
      comment: 'Fantastic place to stay!', imageUrl: ''
    });

    component.addReview();

    expect(reviewSpy.addReview).toHaveBeenCalledOnceWith(
      jasmine.objectContaining({ rating: 5, comment: 'Fantastic place to stay!' })
    );
  });

  it('addReview() — should NOT include imageUrl when empty', () => {
    component.completedStays.set(MOCK_RESERVATIONS.filter(r => r.status === 'Completed'));
    component.addForm.patchValue({
      reservationId: 'id-RES-0003', rating: 5,
      comment: 'Amazing experience here!', imageUrl: ''
    });

    component.addReview();

    const payload = reviewSpy.addReview.calls.mostRecent().args[0];
    expect(payload.imageUrl).toBeUndefined();
  });

  it('addReview() — should include imageUrl when provided', () => {
    component.completedStays.set(MOCK_RESERVATIONS.filter(r => r.status === 'Completed'));
    component.addForm.patchValue({
      reservationId: 'id-RES-0003', rating: 5,
      comment: 'Amazing experience here!', imageUrl: 'https://example.com/img.jpg'
    });

    component.addReview();

    const payload = reviewSpy.addReview.calls.mostRecent().args[0];
    expect(payload.imageUrl).toBe('https://example.com/img.jpg');
  });

  it('addReview() — should show success toast on success', () => {
    component.completedStays.set(MOCK_RESERVATIONS.filter(r => r.status === 'Completed'));
    component.addForm.patchValue({
      reservationId: 'id-RES-0003', rating: 5, comment: 'Amazing experience here!'
    });

    component.addReview();

    expect(toastSpy.success).toHaveBeenCalledWith('Review posted!');
  });

  it('addReview() — should hide add form after success', () => {
    component.completedStays.set(MOCK_RESERVATIONS.filter(r => r.status === 'Completed'));
    component.showAddForm.set(true);
    component.addForm.patchValue({
      reservationId: 'id-RES-0003', rating: 5, comment: 'Amazing experience here!'
    });

    component.addReview();

    expect(component.showAddForm()).toBeFalse();
  });

  it('addReview() — should reset addForm with rating defaulting to 5', () => {
    component.completedStays.set(MOCK_RESERVATIONS.filter(r => r.status === 'Completed'));
    component.addForm.patchValue({
      reservationId: 'id-RES-0003', rating: 3, comment: 'Amazing experience here!'
    });

    component.addReview();

    expect(component.addForm.get('reservationId')?.value).toBeFalsy();
    expect(component.addForm.get('rating')?.value).toBe(5);
  });

  it('addReview() — should reload reviews after success', () => {
    component.completedStays.set(MOCK_RESERVATIONS.filter(r => r.status === 'Completed'));
    component.addForm.patchValue({
      reservationId: 'id-RES-0003', rating: 5, comment: 'Amazing experience here!'
    });
    reviewSpy.getMyReviewsPaged.calls.reset();

    component.addReview();

    expect(reviewSpy.getMyReviewsPaged).toHaveBeenCalled();
  });

  it('addReview() — should reset isSaving to false on success', () => {
    component.completedStays.set(MOCK_RESERVATIONS.filter(r => r.status === 'Completed'));
    component.addForm.patchValue({
      reservationId: 'id-RES-0003', rating: 5, comment: 'Amazing experience here!'
    });

    component.addReview();

    expect(component.isSaving()).toBeFalse();
  });

  it('addReview() — should set isSaving to true during in-flight request', () => {
    const subject = new Subject<ReviewResponseDto>();
    reviewSpy.addReview.and.returnValue(subject.asObservable());
    component.completedStays.set(MOCK_RESERVATIONS.filter(r => r.status === 'Completed'));
    component.addForm.patchValue({
      reservationId: 'id-RES-0003', rating: 5, comment: 'Amazing experience here!'
    });

    component.addReview();

    expect(component.isSaving()).toBeTrue();

    subject.next(MOCK_REVIEW_RESPONSE);
    subject.complete();
  });

  // ── addReview() — INVALID FORM ─────────────────────────────────────────────

  it('addReview() — should NOT call service when addForm is invalid', () => {
    component.addReview();
    expect(reviewSpy.addReview).not.toHaveBeenCalled();
  });

  it('addReview() — should mark all fields touched when form is invalid', () => {
    component.addReview();
    expect(component.addForm.get('reservationId')?.touched).toBeTrue();
    expect(component.addForm.get('comment')?.touched).toBeTrue();
  });

  // ── addReview() — ERROR ────────────────────────────────────────────────────

  it('addReview() — should reset isSaving to false on API error', () => {
    reviewSpy.addReview.and.returnValue(throwError(() => new Error('fail')));
    component.completedStays.set(MOCK_RESERVATIONS.filter(r => r.status === 'Completed'));
    component.addForm.patchValue({
      reservationId: 'id-RES-0003', rating: 5, comment: 'Amazing experience here!'
    });

    component.addReview();

    expect(component.isSaving()).toBeFalse();
  });

  it('addReview() — should NOT show success toast on API error', () => {
    reviewSpy.addReview.and.returnValue(throwError(() => new Error('fail')));
    component.completedStays.set(MOCK_RESERVATIONS.filter(r => r.status === 'Completed'));
    component.addForm.patchValue({
      reservationId: 'id-RES-0003', rating: 5, comment: 'Amazing experience here!'
    });

    component.addReview();

    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  // ── startEdit() ────────────────────────────────────────────────────────────

  it('startEdit() — should set editingId to the review ID', () => {
    component.startEdit(MOCK_REVIEWS[0]);
    expect(component.editingId()).toBe('rev-001');
  });

  it('startEdit() — should patch editForm with review values', () => {
    component.startEdit(MOCK_REVIEWS[0]);
    expect(component.editForm.get('rating')?.value).toBe(5);
    expect(component.editForm.get('comment')?.value).toBe('Absolutely wonderful stay!');
  });

  it('startEdit() — should set imageUrl to empty string when review has none', () => {
    component.startEdit(MOCK_REVIEWS[0]);
    expect(component.editForm.get('imageUrl')?.value).toBe('');
  });

  it('startEdit() — should set imageUrl when review has one', () => {
    component.startEdit(MOCK_REVIEWS[1]);
    expect(component.editForm.get('imageUrl')?.value).toBe('https://example.com/photo.jpg');
  });

  it('startEdit() — switching reviews updates editingId and form', () => {
    component.startEdit(MOCK_REVIEWS[0]);
    component.startEdit(MOCK_REVIEWS[1]);
    expect(component.editingId()).toBe('rev-002');
    expect(component.editForm.get('rating')?.value).toBe(4);
  });

  // ── saveEdit() — HAPPY PATH ────────────────────────────────────────────────

  it('saveEdit() — should call updateReview with correct reviewId and form values', () => {
    component.startEdit(MOCK_REVIEWS[0]);
    component.editForm.patchValue({ rating: 4, comment: 'Updated: still great!' });

    component.saveEdit('rev-001');

    expect(reviewSpy.updateReview).toHaveBeenCalledOnceWith(
      'rev-001',
      jasmine.objectContaining({ rating: 4, comment: 'Updated: still great!' })
    );
  });

  it('saveEdit() — should show success toast on success', () => {
    component.startEdit(MOCK_REVIEWS[0]);
    component.saveEdit('rev-001');
    expect(toastSpy.success).toHaveBeenCalledOnceWith('Review updated.');
  });

  it('saveEdit() — should clear editingId after success', () => {
    component.startEdit(MOCK_REVIEWS[0]);
    component.saveEdit('rev-001');
    expect(component.editingId()).toBeNull();
  });

  it('saveEdit() — should reload reviews after success', () => {
    component.startEdit(MOCK_REVIEWS[0]);
    reviewSpy.getMyReviewsPaged.calls.reset();
    component.saveEdit('rev-001');
    expect(reviewSpy.getMyReviewsPaged).toHaveBeenCalled();
  });

  it('saveEdit() — should reset isSaving to false on success', () => {
    component.startEdit(MOCK_REVIEWS[0]);
    component.saveEdit('rev-001');
    expect(component.isSaving()).toBeFalse();
  });

  it('saveEdit() — should set isSaving to true during in-flight request', () => {
    const subject = new Subject<ReviewResponseDto>();
    reviewSpy.updateReview.and.returnValue(subject.asObservable());
    component.startEdit(MOCK_REVIEWS[0]);

    component.saveEdit('rev-001');

    expect(component.isSaving()).toBeTrue();

    subject.next(MOCK_UPDATE_RESPONSE);
    subject.complete();
  });

  // ── saveEdit() — INVALID / ERROR ───────────────────────────────────────────

  it('saveEdit() — should NOT call service when editForm is invalid', () => {
    component.editForm.get('comment')?.setValue('');
    component.saveEdit('rev-001');
    expect(reviewSpy.updateReview).not.toHaveBeenCalled();
  });

  it('saveEdit() — should reset isSaving to false on API error', () => {
    reviewSpy.updateReview.and.returnValue(throwError(() => new Error('fail')));
    component.startEdit(MOCK_REVIEWS[0]);
    component.saveEdit('rev-001');
    expect(component.isSaving()).toBeFalse();
  });

  it('saveEdit() — should NOT show success toast on API error', () => {
    reviewSpy.updateReview.and.returnValue(throwError(() => new Error('fail')));
    component.startEdit(MOCK_REVIEWS[0]);
    component.saveEdit('rev-001');
    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  // ── deleteReview() — HAPPY PATH ────────────────────────────────────────────

  it('deleteReview() — should call deleteReview service with the review ID', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    component.deleteReview('rev-001');
    expect(reviewSpy.deleteReview).toHaveBeenCalledOnceWith('rev-001');
  });

  it('deleteReview() — should show success toast on deletion', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    component.deleteReview('rev-001');
    expect(toastSpy.success).toHaveBeenCalledOnceWith('Review deleted.');
  });

  it('deleteReview() — should remove the review from the reviews signal', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    component.deleteReview('rev-001');
    const ids = component.reviews().map(r => r.reviewId);
    expect(ids).not.toContain('rev-001');
    expect(ids).toContain('rev-002');
  });

  it('deleteReview() — should keep remaining reviews intact', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    component.deleteReview('rev-001');
    expect(component.reviews().length).toBe(1);
    expect(component.reviews()[0].reviewId).toBe('rev-002');
  });

  // ── deleteReview() — CONFIRM CANCELLED ────────────────────────────────────

  it('deleteReview() — should NOT call service when confirm is cancelled', () => {
    spyOn(window, 'confirm').and.returnValue(false);
    component.deleteReview('rev-001');
    expect(reviewSpy.deleteReview).not.toHaveBeenCalled();
  });

  it('deleteReview() — should NOT show toast when confirm is cancelled', () => {
    spyOn(window, 'confirm').and.returnValue(false);
    component.deleteReview('rev-001');
    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  it('deleteReview() — should NOT remove review from signal when confirm is cancelled', () => {
    spyOn(window, 'confirm').and.returnValue(false);
    component.deleteReview('rev-001');
    expect(component.reviews().length).toBe(2);
  });
});
