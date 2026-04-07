import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AdminReviewsComponent } from './admin-reviews.component';
import { ReviewService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

const MOCK_REVIEWS = [
  { reviewId: 'rev-001', hotelId: 'h1', userId: 'u1', userName: 'Alice', reservationId: 'res-001', reservationCode: 'RES001', rating: 5, comment: 'Excellent!', createdDate: '2025-01-10T10:00:00Z', contributionPoints: 10 },
  { reviewId: 'rev-002', hotelId: 'h1', userId: 'u2', userName: 'Bob',   reservationId: 'res-002', reservationCode: 'RES002', rating: 3, comment: 'Average.',   createdDate: '2025-01-11T10:00:00Z', contributionPoints: 10 },
];

describe('AdminReviewsComponent', () => {
  let component: AdminReviewsComponent;
  let fixture: ComponentFixture<AdminReviewsComponent>;
  let reviewSpy: jasmine.SpyObj<ReviewService>;
  let toastSpy: jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    reviewSpy = jasmine.createSpyObj('ReviewService', ['getHotelReviewsAdmin', 'replyToReview']);
    toastSpy  = jasmine.createSpyObj('ToastService',  ['success', 'error']);

    reviewSpy.getHotelReviewsAdmin.and.returnValue(of({ totalCount: 2, reviews: MOCK_REVIEWS }));
    reviewSpy.replyToReview.and.returnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [AdminReviewsComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        { provide: ReviewService, useValue: reviewSpy },
        { provide: ToastService,  useValue: toastSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AdminReviewsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should call getHotelReviewsAdmin', () => {
    expect(reviewSpy.getHotelReviewsAdmin).toHaveBeenCalledWith(1, 10, undefined, undefined, undefined);
  });

  it('ngOnInit — should populate reviews signal', () => {
    expect(component.reviews().length).toBe(2);
  });

  it('ngOnInit — should set totalCount', () => {
    expect(component.totalCount()).toBe(2);
  });

  it('loading — should be false after load', () => {
    expect(component.loading()).toBeFalse();
  });

  // ── Initial state ─────────────────────────────────────────────────────────

  it('isSaving — should start as false', () => expect(component.isSaving()).toBeFalse());
  it('replyingId — should start as null', () => expect(component.replyingId()).toBeNull());

  // ── startReply / cancelReply ──────────────────────────────────────────────

  it('startReply — should set replyingId', () => {
    component.startReply('rev-001');
    expect(component.replyingId()).toBe('rev-001');
  });

  it('startReply — should patch replyForm with existing reply', () => {
    component.startReply('rev-001', 'Thank you!');
    expect(component.replyForm.get('adminReply')?.value).toBe('Thank you!');
  });

  it('cancelReply — should clear replyingId', () => {
    component.startReply('rev-001');
    component.cancelReply();
    expect(component.replyingId()).toBeNull();
  });

  // ── submitReply ───────────────────────────────────────────────────────────

  it('submitReply — should call replyToReview when form is valid', () => {
    component.startReply('rev-001');
    component.replyForm.patchValue({ adminReply: 'Thank you for your feedback!' });
    component.submitReply();
    expect(reviewSpy.replyToReview).toHaveBeenCalledWith('rev-001', 'Thank you for your feedback!');
  });

  it('submitReply — should show success toast', () => {
    component.startReply('rev-001');
    component.replyForm.patchValue({ adminReply: 'Thank you for your feedback!' });
    component.submitReply();
    expect(toastSpy.success).toHaveBeenCalledOnceWith('Reply saved.');
  });

  it('submitReply — should clear replyingId on success', () => {
    component.startReply('rev-001');
    component.replyForm.patchValue({ adminReply: 'Thank you for your feedback!' });
    component.submitReply();
    expect(component.replyingId()).toBeNull();
  });

  it('submitReply — should update reviews signal with new reply', () => {
    component.reviews.set(MOCK_REVIEWS as any);
    component.startReply('rev-001');
    component.replyForm.patchValue({ adminReply: 'Great!' });
    component.submitReply();
    const updated = component.reviews().find(r => r.reviewId === 'rev-001');
    expect(updated?.adminReply).toBe('Great!');
  });

  it('submitReply — should NOT call service when form is invalid', () => {
    component.startReply('rev-001');
    component.submitReply();
    expect(reviewSpy.replyToReview).not.toHaveBeenCalled();
  });

  it('submitReply — should mark all touched when form is invalid', () => {
    component.startReply('rev-001');
    component.submitReply();
    expect(component.replyForm.get('adminReply')?.touched).toBeTrue();
  });

  it('submitReply — should reset isSaving to false on error', () => {
    reviewSpy.replyToReview.and.returnValue(throwError(() => new Error('fail')));
    component.startReply('rev-001');
    component.replyForm.patchValue({ adminReply: 'Thank you!' });
    component.submitReply();
    expect(component.isSaving()).toBeFalse();
  });

  // ── onRatingFilter ────────────────────────────────────────────────────────

  it('onRatingFilter — should set ratingFilter and reload', () => {
    reviewSpy.getHotelReviewsAdmin.calls.reset();
    component.onRatingFilter(5);
    expect(component.ratingFilter).toBe(5);
    expect(reviewSpy.getHotelReviewsAdmin).toHaveBeenCalledWith(1, 10, 5, 5, undefined);
  });

  it('onRatingFilter — should toggle off when same rating clicked twice', () => {
    component.onRatingFilter(5);
    component.onRatingFilter(5);
    expect(component.ratingFilter).toBe(0);
  });

  // ── onSort ────────────────────────────────────────────────────────────────

  it('onSort — should set sortDir and reload', () => {
    reviewSpy.getHotelReviewsAdmin.calls.reset();
    component.onSort('asc');
    expect(component.sortDir).toBe('asc');
    expect(reviewSpy.getHotelReviewsAdmin).toHaveBeenCalled();
  });

  it('onSort — should toggle off when same dir clicked twice', () => {
    component.onSort('asc');
    component.onSort('asc');
    expect(component.sortDir).toBe('');
  });

  // ── onPage ────────────────────────────────────────────────────────────────

  it('onPage — should update currentPage and reload', () => {
    reviewSpy.getHotelReviewsAdmin.calls.reset();
    component.onPage({ pageIndex: 1, pageSize: 10, length: 20 } as any);
    expect(component.currentPage).toBe(2);
    expect(reviewSpy.getHotelReviewsAdmin).toHaveBeenCalled();
  });

  // ── load error ────────────────────────────────────────────────────────────

  it('load — should set loading to false on error', () => {
    reviewSpy.getHotelReviewsAdmin.and.returnValue(throwError(() => new Error('fail')));
    component.load();
    expect(component.loading()).toBeFalse();
  });
});
