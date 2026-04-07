import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { BookingDetailComponent } from './booking-detail.component';
import { BookingService } from '../../../core/services/booking.service';
import { ToastService } from '../../../core/services/toast.service';
import { ReservationDetailsDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ── Mock data ──────────────────────────────────────────────────────────────────

function makeReservation(status: string, overrides?: Partial<ReservationDetailsDto>): ReservationDetailsDto {
  return {
    reservationCode: 'RES-ABCD1234',
    reservationId:   'res-001',
    hotelId:         'hotel-001',
    hotelName:       'Grand Palace',
    roomTypeId:      'rt-001',
    roomTypeName:    'Deluxe',
    checkInDate:     '2025-06-01',
    checkOutDate:    '2025-06-03',
    numberOfRooms:   1,
    totalAmount:     7000,
    gstPercent:      18,
    gstAmount:       1260,
    discountPercent: 0,
    discountAmount:  0,
    walletAmountUsed: 0,
    finalAmount:     7000,
    status,
    isCheckedIn:     false,
    createdDate:     '2025-05-01T10:00:00Z',
    rooms:           [{ roomId: 'r-001', roomNumber: '101', floor: 1 }],
    cancellationFeePaid: false,
    cancellationFeeAmount: 0,
    cancellationPolicyText: '',
    ...overrides
  };
}

const CONFIRMED_RES  = makeReservation('Confirmed');
const PENDING_RES    = makeReservation('Pending');
const CANCELLED_RES  = makeReservation('Cancelled');
const COMPLETED_RES  = makeReservation('Completed', { isCheckedIn: true });

// ─────────────────────────────────────────────────────────────────────────────

describe('BookingDetailComponent', () => {
  let component: BookingDetailComponent;
  let fixture:   ComponentFixture<BookingDetailComponent>;

  let bookingSpy: jasmine.SpyObj<BookingService>;
  let toastSpy:   jasmine.SpyObj<ToastService>;

  async function setup(reservationCode = 'RES-ABCD1234', reservation = CONFIRMED_RES) {
    bookingSpy = jasmine.createSpyObj('BookingService', [
      'getReservationByCode', 'cancelReservation'
    ]);
    toastSpy = jasmine.createSpyObj('ToastService', ['success', 'error']);

    bookingSpy.getReservationByCode.and.returnValue(of(reservation));
    bookingSpy.cancelReservation.and.returnValue(of(undefined));

    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [BookingDetailComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: BookingService, useValue: bookingSpy },
        { provide: ToastService,   useValue: toastSpy   },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => reservationCode } } }
        },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(BookingDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  beforeEach(async () => await setup());

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── INITIAL SIGNAL STATE ───────────────────────────────────────────────────

  it('showCancelForm — should start as false', () => {
    expect(component.showCancelForm()).toBeFalse();
  });

  it('isCancelling — should start as false', () => {
    expect(component.isCancelling()).toBeFalse();
  });

  // ── ngOnInit ───────────────────────────────────────────────────────────────

  it('ngOnInit — should call getReservationByCode with route param', () => {
    expect(bookingSpy.getReservationByCode)
      .toHaveBeenCalledOnceWith('RES-ABCD1234');
  });

  it('ngOnInit — should populate reservation signal with API response', () => {
    expect(component.reservation()).not.toBeNull();
    expect(component.reservation()?.hotelName).toBe('Grand Palace');
    expect(component.reservation()?.status).toBe('Confirmed');
    expect(component.reservation()?.totalAmount).toBe(7000);
  });

  it('ngOnInit — should use empty string when no code param is present', async () => {
    bookingSpy.getReservationByCode.and.returnValue(of(CONFIRMED_RES));

    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [BookingDetailComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: BookingService, useValue: bookingSpy },
        { provide: ToastService,   useValue: toastSpy   },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => null } } }
        },
      ]
    }).compileComponents();

    const f = TestBed.createComponent(BookingDetailComponent);
    f.detectChanges();

    expect(bookingSpy.getReservationByCode).toHaveBeenCalledWith('');
  });

  // ── FORM VALIDATION ────────────────────────────────────────────────────────

  it('cancelForm — should be invalid initially', () => {
    expect(component.cancelForm.invalid).toBeTrue();
  });

  it('cancelForm — should be invalid when reason is empty', () => {
    component.cancelForm.get('reason')?.setValue('');
    expect(component.cancelForm.invalid).toBeTrue();
  });

  it('cancelForm — should be invalid when reason is less than 5 characters', () => {
    component.cancelForm.get('reason')?.setValue('bad');
    expect(component.cancelForm.get('reason')?.invalid).toBeTrue();
  });

  it('cancelForm — should be valid when reason is exactly 5 characters', () => {
    component.cancelForm.get('reason')?.setValue('tired');
    expect(component.cancelForm.valid).toBeTrue();
  });

  it('cancelForm — should be valid with a proper reason', () => {
    component.cancelForm.get('reason')?.setValue('Change of travel plans');
    expect(component.cancelForm.valid).toBeTrue();
  });

  // ── statusClass() ──────────────────────────────────────────────────────────

  it('statusClass() — Pending → badge-warning', () => {
    expect(component.statusClass('Pending')).toBe('badge-warning');
  });

  it('statusClass() — Confirmed → badge-success', () => {
    expect(component.statusClass('Confirmed')).toBe('badge-success');
  });

  it('statusClass() — Completed → badge-primary', () => {
    expect(component.statusClass('Completed')).toBe('badge-primary');
  });

  it('statusClass() — Cancelled → badge-error', () => {
    expect(component.statusClass('Cancelled')).toBe('badge-error');
  });

  it('statusClass() — NoShow → badge-muted', () => {
    expect(component.statusClass('NoShow')).toBe('badge-muted');
  });

  it('statusClass() — unknown status → badge-muted', () => {
    expect(component.statusClass('Whatever')).toBe('badge-muted');
  });

  // ── canCancel() ────────────────────────────────────────────────────────────

  it('canCancel() — should return true for Pending reservation', () => {
    expect(component.canCancel(PENDING_RES)).toBeTrue();
  });

  it('canCancel() — should return true for Confirmed reservation', () => {
    expect(component.canCancel(CONFIRMED_RES)).toBeTrue();
  });

  it('canCancel() — should return false for Cancelled reservation', () => {
    expect(component.canCancel(CANCELLED_RES)).toBeFalse();
  });

  it('canCancel() — should return false for Completed reservation', () => {
    expect(component.canCancel(COMPLETED_RES)).toBeFalse();
  });

  it('canCancel() — should return false for NoShow reservation', () => {
    expect(component.canCancel(makeReservation('NoShow'))).toBeFalse();
  });

  // ── cancel() — HAPPY PATH ──────────────────────────────────────────────────

  it('cancel() — should call cancelReservation with code and reason', () => {
    component.cancelForm.get('reason')?.setValue('Change of travel plans');

    component.cancel();

    expect(bookingSpy.cancelReservation).toHaveBeenCalledOnceWith(
      'RES-ABCD1234',
      { reason: 'Change of travel plans' }
    );
  });

  it('cancel() — should show success toast on success', () => {
    component.cancelForm.get('reason')?.setValue('Change of travel plans');

    component.cancel();

    expect(toastSpy.success).toHaveBeenCalledOnceWith(
      'Reservation cancelled. Refund will be credited to your wallet if applicable.'
    );
  });

  it('cancel() — should update reservation status to Cancelled in signal', () => {
    component.cancelForm.get('reason')?.setValue('Change of travel plans');

    component.cancel();

    expect(component.reservation()?.status).toBe('Cancelled');
  });

  it('cancel() — should hide cancel form after success', () => {
    component.showCancelForm.set(true);
    component.cancelForm.get('reason')?.setValue('Change of travel plans');

    component.cancel();

    expect(component.showCancelForm()).toBeFalse();
  });

  it('cancel() — should reset isCancelling to false on success', () => {
    component.cancelForm.get('reason')?.setValue('Change of travel plans');

    component.cancel();

    expect(component.isCancelling()).toBeFalse();
  });

  it('cancel() — should set isCancelling to true during in-flight request', () => {
    const subject = new Subject<void>();
    bookingSpy.cancelReservation.and.returnValue(subject.asObservable());
    component.cancelForm.get('reason')?.setValue('Change of travel plans');

    component.cancel();

    expect(component.isCancelling()).toBeTrue();

    subject.next();
    subject.complete();
  });

  it('cancel() — should preserve other reservation fields after status update', () => {
    component.cancelForm.get('reason')?.setValue('Change of travel plans');

    component.cancel();

    const res = component.reservation();
    expect(res?.hotelName).toBe('Grand Palace');
    expect(res?.totalAmount).toBe(7000);
    expect(res?.reservationCode).toBe('RES-ABCD1234');
  });

  // ── cancel() — INVALID FORM ────────────────────────────────────────────────

  it('cancel() — should NOT call service when cancelForm is invalid', () => {
    // cancelForm is empty by default
    component.cancel();
    expect(bookingSpy.cancelReservation).not.toHaveBeenCalled();
  });

  it('cancel() — should mark reason as touched when form is invalid', () => {
    component.cancel();
    expect(component.cancelForm.get('reason')?.touched).toBeTrue();
  });

  it('cancel() — should NOT show toast when form is invalid', () => {
    component.cancel();
    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  it('cancel() — should NOT update reservation status when form is invalid', () => {
    component.cancel();
    expect(component.reservation()?.status).toBe('Confirmed');
  });

  // ── cancel() — ERROR ───────────────────────────────────────────────────────

  it('cancel() — should reset isCancelling to false on API error', () => {
    bookingSpy.cancelReservation.and.returnValue(
      throwError(() => new Error('Server error'))
    );
    component.cancelForm.get('reason')?.setValue('Change of travel plans');

    component.cancel();

    expect(component.isCancelling()).toBeFalse();
  });

  it('cancel() — should NOT show success toast on API error', () => {
    bookingSpy.cancelReservation.and.returnValue(
      throwError(() => new Error('Server error'))
    );
    component.cancelForm.get('reason')?.setValue('Change of travel plans');

    component.cancel();

    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  it('cancel() — should NOT update reservation status on API error', () => {
    bookingSpy.cancelReservation.and.returnValue(
      throwError(() => new Error('Server error'))
    );
    component.cancelForm.get('reason')?.setValue('Change of travel plans');

    component.cancel();

    expect(component.reservation()?.status).toBe('Confirmed');
  });

  it('cancel() — should NOT close cancel form on API error', () => {
    bookingSpy.cancelReservation.and.returnValue(
      throwError(() => new Error('Server error'))
    );
    component.showCancelForm.set(true);
    component.cancelForm.get('reason')?.setValue('Change of travel plans');

    component.cancel();

    expect(component.showCancelForm()).toBeTrue();
  });
});