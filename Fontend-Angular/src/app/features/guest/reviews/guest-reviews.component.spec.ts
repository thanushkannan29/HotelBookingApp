import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { GuestReviewsComponent } from './guest-reviews.component';
import { ReviewService } from '../../../core/services/api.services';
import { BookingService } from '../../../core/services/booking.service';
import { ToastService } from '../../../core/services/toast.service';
import { MyReviewsResponseDto, ReservationDetailsDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

const MOCK_REVIEWS: MyReviewsResponseDto[] = [
  { reviewId: 'rev-001', hotelId: 'hotel-001', hotelName: 'Grand Palace', reservationId: 'id-RES-0001', reservationCode: 'RES-0001', rating: 5, comment: 'Absolutely wonderful stay!', createdDate: '2025-01-10T10:00:00Z', contributionPoints: 100 },
  { reviewId: 'rev-002', hotelId: 'hotel-002', hotelName: 'Sea View Inn',  reservationId: 'id-RES-0002', reservationCode: 'RES-0002', rating: 4, comment: 'Great location, good service.', imageUrl: 'https://example.com/photo.jpg', createdDate: '2025-01-05T10:00:00Z', contributionPoints: 80 },
];

function makeReservation(code: string, hotelId: string, status: string): ReservationDetailsDto {
  return {
    reservationCode: code, reservationId: `id-${code}`,
    hotelId, hotelName: `Hotel ${hotelId}`,
    roomTypeId: 'rt-001', roomTypeName: 'Deluxe',
    checkInDate: '2025-01-01', checkOutDate: '2025-01-03',
    numberOfRooms: 1, totalAmount: 7000,
    gstPercent: 18, gstAmount: 1260, discountPercent: 0, discountAmount: 0,
    walletAmountUsed: 0, finalAmount: 7000,
    status, isCheckedIn: status === 'Completed', createdDate: '2024-12-01T10:00:00Z',
    rooms: [], cancellationFeePaid: false, cancellationFeeAmount: 0, cancellationPolicyText: ''
  };
}

const MOCK_RESERVATIONS: ReservationDetailsDto[] = [
  makeReservation('RES-0001', 'hotel-001', 'Completed'),
  makeReservation('RES-0002', 'hotel-002', 'Completed'),
  makeReservation('RES-0003', 'hotel-003', 'Completed'), // not yet reviewed
  makeReservation('RES-0004', 'hotel-004', 'Confirmed'), // not completed
];

const MOCK_REVIEW_RESPONSE = {
  reviewId: 'rev-003', hotelId: 'hotel-003', userId: 'usr-001', userName: 'Alice',
  reservationId: 'id-RES-0003', reservationCode: 'RES-0003',
  rating: 5, comment: 'Fantastic!', createdDate: '2025-02-01T10:00:00Z', contributionPoints: 100
};

describe('GuestReviewsComponent', () => {
  let component: GuestReviewsComponent;
  let fixture: ComponentFixture<GuestReviewsComponent>;
  let reviewSpy: jasmine.SpyObj<ReviewService>;
  let bookingSpy: jasmine.SpyObj<BookingService>;
  let toastSpy: jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    reviewSpy  = jasmine.createSpyObj('ReviewService', ['getMyReviewsPaged', 'addReview', 'updateReview', 'deleteReview']);
    bookingSpy = jasmine.createSpyObj('BookingService', ['getMyReservations']);
    toastSpy   = jasmine.createSpyObj('ToastService', ['success', 'error']);

    reviewSpy.getMyReviewsPaged.and.returnValue(of({ totalCount: 2, reviews: MOCK_REVIEWS }));
    reviewSpy.addReview.and.returnValue(of(MOCK_REVIEW_RESPONSE as any));
    reviewSpy.updateReview.and.returnValue(of({ ...MOCK_REVIEW_RESPONSE, reviewId: 'rev-001' } as any));
    reviewSpy.deleteReview.and.returnValue(of(undefined));
    bookingSpy.getMyReservations.and.returnValue(of(MOCK_RESERVATIONS as any));

    await TestBed.configureTestingModule({
      imports: [GuestReviewsComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]),
        { provide: ReviewService,  useValue: reviewSpy },
        { provide: BookingService, useValue: bookingSpy },
        { provide: ToastService,   useValue: toastSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(GuestReviewsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── Initial state ─────────────────────────────────────────────────────────

  it('stars — should be [1, 2, 3, 4, 5]', () => expect(component.stars).toEqual([1, 2, 3, 4, 5]));
  it('editingId — should start as null', () => expect(component.editingId()).toBeNull());
  it('showAddForm — should start as false', () => expect(component.showAddForm()).toBeFalse());
  it('isSaving — should start as false', () => expect(component.isSaving()).toBeFalse());

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should call getMyReviewsPaged', () => {
    expect(reviewSpy.getMyReviewsPaged).toHaveBeenCalled();
  });

  it('ngOnInit — should call getMyReservations', () => {
    expect(bookingSpy.getMyReservations).toHaveBeenCalled();
  });

  it('ngOnInit — should populate reviews signal', () => {
    expect(component.reviews().length).toBe(2);
  });

  it('ngOnInit — should populate completedStays with only Completed reservations', () => {
    expect(component.completedStays().length).toBe(3);
    expect(component.completedStays().every(r => r.status === 'Completed')).toBeTrue();
  });

  it('load — should set loading to false on error', () => {
    reviewSpy.getMyReviewsPaged.and.returnValue(throwError(() => new Error('fail')));
    component.load();
    expect(component.loading()).toBeFalse();
  });

  // ── reviewableStays getter ────────────────────────────────────────────────

  it('reviewableStays — should exclude reservations already reviewed', () => {
    const ids = component.reviewableStays.map(s => s.reservationId);
    expect(ids).not.toContain('id-RES-0001');
    expect(ids).not.toContain('id-RES-0002');
  });

  it('reviewableStays — should include RES-0003 (completed, not reviewed)', () => {
    const ids = component.reviewableStays.map(s => s.reservationId);
    expect(ids).toContain('id-RES-0003');
  });

  it('reviewableStays — should return empty when all completed stays are reviewed', () => {
    component.completedStays.set([
      makeReservation('RES-0001', 'hotel-001', 'Completed'),
      makeReservation('RES-0002', 'hotel-002', 'Completed'),
    ]);
    expect(component.reviewableStays.length).toBe(0);
  });

  // ── addForm ───────────────────────────────────────────────────────────────

  it('addForm — should be invalid initially', () => expect(component.addForm.invalid).toBeTrue());
  it('addForm — rating should default to 5', () => expect(component.addForm.get('rating')?.value).toBe(5));

  it('addForm — should be valid when all required fields are filled', () => {
    component.addForm.patchValue({ reservationId: 'id-RES-0003', rating: 5, comment: 'Amazing experience overall!' });
    expect(component.addForm.valid).toBeTrue();
  });

  it('addForm — should be invalid when comment is shorter than 10 characters', () => {
    component.addForm.patchValue({ reservationId: 'id-RES-0003', rating: 5, comment: 'Short' });
    expect(component.addForm.get('comment')?.invalid).toBeTrue();
  });

  // ── addReview ─────────────────────────────────────────────────────────────

  it('addReview — should call addReview service when form is valid', () => {
    component.addForm.patchValue({ reservationId: 'id-RES-0003', rating: 5, comment: 'Fantastic place to stay!' });
    component.addReview();
    expect(reviewSpy.addReview).toHaveBeenCalled();
  });

  it('addReview — should show success toast', () => {
    component.addForm.patchValue({ reservationId: 'id-RES-0003', rating: 5, comment: 'Fantastic place to stay!' });
    component.addReview();
    expect(toastSpy.success).toHaveBeenCalledWith('Review posted!');
  });

  it('addReview — should hide add form after success', () => {
    component.showAddForm.set(true);
    component.addForm.patchValue({ reservationId: 'id-RES-0003', rating: 5, comment: 'Fantastic place to stay!' });
    component.addReview();
    expect(component.showAddForm()).toBeFalse();
  });

  it('addReview — should NOT call service when form is invalid', () => {
    component.addReview();
    expect(reviewSpy.addReview).not.toHaveBeenCalled();
  });

  it('addReview — should mark all touched when form is invalid', () => {
    component.addReview();
    expect(component.addForm.get('reservationId')?.touched).toBeTrue();
  });

  it('addReview — should reset isSaving to false on error', () => {
    reviewSpy.addReview.and.returnValue(throwError(() => new Error('fail')));
    component.addForm.patchValue({ reservationId: 'id-RES-0003', rating: 5, comment: 'Fantastic place to stay!' });
    component.addReview();
    expect(component.isSaving()).toBeFalse();
  });

  // ── startEdit / saveEdit ──────────────────────────────────────────────────

  it('startEdit — should set editingId', () => {
    component.startEdit(MOCK_REVIEWS[0]);
    expect(component.editingId()).toBe('rev-001');
  });

  it('startEdit — should patch editForm with review values', () => {
    component.startEdit(MOCK_REVIEWS[0]);
    expect(component.editForm.get('rating')?.value).toBe(5);
    expect(component.editForm.get('comment')?.value).toBe('Absolutely wonderful stay!');
  });

  it('saveEdit — should call updateReview', () => {
    component.startEdit(MOCK_REVIEWS[0]);
    component.saveEdit('rev-001');
    expect(reviewSpy.updateReview).toHaveBeenCalledWith('rev-001', jasmine.any(Object));
  });

  it('saveEdit — should show success toast', () => {
    component.startEdit(MOCK_REVIEWS[0]);
    component.saveEdit('rev-001');
    expect(toastSpy.success).toHaveBeenCalledWith('Review updated.');
  });

  it('saveEdit — should clear editingId on success', () => {
    component.startEdit(MOCK_REVIEWS[0]);
    component.saveEdit('rev-001');
    expect(component.editingId()).toBeNull();
  });

  it('saveEdit — should NOT call service when editForm is invalid', () => {
    component.editForm.get('comment')?.setValue('');
    component.saveEdit('rev-001');
    expect(reviewSpy.updateReview).not.toHaveBeenCalled();
  });

  it('saveEdit — should reset isSaving to false on error', () => {
    reviewSpy.updateReview.and.returnValue(throwError(() => new Error('fail')));
    component.startEdit(MOCK_REVIEWS[0]);
    component.saveEdit('rev-001');
    expect(component.isSaving()).toBeFalse();
  });

  // ── deleteReview ──────────────────────────────────────────────────────────

  it('deleteReview — should call deleteReview service when confirmed', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    component.deleteReview('rev-001');
    expect(reviewSpy.deleteReview).toHaveBeenCalledWith('rev-001');
  });

  it('deleteReview — should show success toast', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    component.deleteReview('rev-001');
    expect(toastSpy.success).toHaveBeenCalledWith('Review deleted.');
  });

  it('deleteReview — should NOT call service when confirm is cancelled', () => {
    spyOn(window, 'confirm').and.returnValue(false);
    component.deleteReview('rev-001');
    expect(reviewSpy.deleteReview).not.toHaveBeenCalled();
  });

  // ── onPage ────────────────────────────────────────────────────────────────

  it('onPage — should update currentPage and reload', () => {
    reviewSpy.getMyReviewsPaged.calls.reset();
    component.onPage({ pageIndex: 1, pageSize: 10, length: 20 } as any);
    expect(component.currentPage).toBe(2);
    expect(reviewSpy.getMyReviewsPaged).toHaveBeenCalled();
  });
});
