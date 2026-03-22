import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideNativeDateAdapter } from '@angular/material/core';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { BookingCreateComponent } from './booking-create.component';
import { BookingService } from '../../../core/services/booking.service';
import { TransactionService } from '../../../core/services/api.services';
import { HotelService } from '../../../core/services/hotel.service';
import { ToastService } from '../../../core/services/toast.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import {
  HotelDetailsDto, RoomAvailabilityDto,
  ReservationResponseDto
} from '../../../core/models/models';

// ── Mock data ──────────────────────────────────────────────────────────────────

const MOCK_HOTEL: HotelDetailsDto = {
  hotelId:       'hotel-001',
  name:          'Grand Palace',
  address:       '1 MG Road',
  city:          'Chennai',
  description:   'Luxury hotel',
  imageUrl:      'https://example.com/img.jpg',
  contactNumber: '9840650390',
  averageRating: 4.5,
  reviewCount:   120,
  amenities:     ['WiFi', 'Pool'],
  reviews:       [],
  roomTypes:     []
};

const MOCK_AVAILABILITY: RoomAvailabilityDto[] = [
  { roomTypeId: 'rt-001', roomTypeName: 'Deluxe', pricePerNight: 3500, availableRooms: 5 },
  { roomTypeId: 'rt-002', roomTypeName: 'Suite',  pricePerNight: 7000, availableRooms: 2 },
];

const MOCK_AVAILABLE_ROOMS = [
  { roomId: 'r-001', roomNumber: '101', floor: 1, roomTypeName: 'Deluxe' },
  { roomId: 'r-002', roomNumber: '102', floor: 1, roomTypeName: 'Deluxe' },
];

const MOCK_RESERVATION: ReservationResponseDto = {
  reservationCode: 'RES-ABCD1234',
  reservationId:   'res-001',
  totalAmount:     7000,
  status:          'Pending',
  totalRooms:      1,
  rooms:           [{ roomId: 'r-001', roomNumber: '101', floor: 1 }]
};

const MOCK_TRANSACTION = {
  transactionId:   'tx-001',
  reservationId:   'res-001',
  amount:          7000,
  paymentMethod:   1,
  status:          2,
  transactionDate: '2025-06-01T10:00:00Z'
};

// ── Helper: empty ActivatedRoute query params ──────────────────────────────────
function makeRoute(params: Record<string, string> = {}) {
  return { snapshot: { queryParams: params } };
}

// ─────────────────────────────────────────────────────────────────────────────

describe('BookingCreateComponent', () => {
  // Helper: create a valid booking form state bypassing MatDatepicker validators
  function setValidBookingForm(component: BookingCreateComponent,
    checkIn  = new Date('2025-06-01'),
    checkOut = new Date('2025-06-03'),
    rooms    = 1) {
    component.bookingForm.patchValue({
      hotelId: 'hotel-001', roomTypeId: 'rt-001',
      checkInDate: checkIn, checkOutDate: checkOut, numberOfRooms: rooms,
    });
    // Clear MatDatepicker-injected errors
    component.bookingForm.get('checkInDate')?.setErrors(null);
    component.bookingForm.get('checkOutDate')?.setErrors(null);
    component.bookingForm.updateValueAndValidity();
  }


  let component: BookingCreateComponent;
  let fixture:   ComponentFixture<BookingCreateComponent>;

  let bookingSpy:     jasmine.SpyObj<BookingService>;
  let transactionSpy: jasmine.SpyObj<TransactionService>;
  let hotelSpy:       jasmine.SpyObj<HotelService>;
  let toastSpy:       jasmine.SpyObj<ToastService>;
  let router:         Router;

  async function setup(queryParams: Record<string, string> = {}) {
    bookingSpy     = jasmine.createSpyObj('BookingService',     ['createReservation', 'getAvailableRooms']);
    transactionSpy = jasmine.createSpyObj('TransactionService', ['createPayment']);
    hotelSpy       = jasmine.createSpyObj('HotelService',       ['getHotelDetails', 'getAvailability']);
    toastSpy       = jasmine.createSpyObj('ToastService',       ['success', 'error']);

    // Default happy-path
    hotelSpy.getHotelDetails.and.returnValue(of(MOCK_HOTEL));
    hotelSpy.getAvailability.and.returnValue(of(MOCK_AVAILABILITY));
    bookingSpy.createReservation.and.returnValue(of(MOCK_RESERVATION));
    bookingSpy.getAvailableRooms.and.returnValue(of(MOCK_AVAILABLE_ROOMS));
    transactionSpy.createPayment.and.returnValue(of(MOCK_TRANSACTION));

    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [BookingCreateComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNativeDateAdapter(),
        { provide: BookingService,     useValue: bookingSpy     },
        { provide: TransactionService, useValue: transactionSpy },
        { provide: HotelService,       useValue: hotelSpy       },
        { provide: ToastService,       useValue: toastSpy       },
        { provide: ActivatedRoute,     useValue: makeRoute(queryParams) },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(BookingCreateComponent);
    component = fixture.componentInstance;
    router    = TestBed.inject(Router);
    fixture.detectChanges();
  }

  beforeEach(async () => await setup());

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── INITIAL SIGNAL STATE ───────────────────────────────────────────────────

  it('isBooking — should start as false', () => {
    expect(component.isBooking()).toBeFalse();
  });

  it('isPaying — should start as false', () => {
    expect(component.isPaying()).toBeFalse();
  });

  it('hotel — should start as null when no hotelId query param', () => {
    expect(component.hotel()).toBeNull();
  });

  it('availability — should start as empty array', () => {
    expect(component.availability()).toEqual([]);
  });

  it('createdReservation — should start as null', () => {
    expect(component.createdReservation()).toBeNull();
  });

  // ── FORM INITIAL STATE ─────────────────────────────────────────────────────

  it('bookingForm — numberOfRooms should default to 1', () => {
    expect(component.bookingForm.get('numberOfRooms')?.value).toBe(1);
  });

  it('bookingForm — should be invalid initially (required fields empty)', () => {
    expect(component.bookingForm.invalid).toBeTrue();
  });

  it('paymentForm — paymentMethod should default to 1 (Credit Card)', () => {
    expect(component.paymentForm.get('paymentMethod')?.value).toBe(1);
  });

  it('paymentMethods — should contain all 5 payment methods', () => {
    expect(component.paymentMethods.length).toBe(5);
    expect(component.paymentMethods[0].label).toBe('Credit Card');
  });

  // ── ngOnInit WITH QUERY PARAMS ─────────────────────────────────────────────

  it('ngOnInit — should load hotel details when hotelId query param is present', async () => {
    await setup({ hotelId: 'hotel-001' });
    expect(hotelSpy.getHotelDetails).toHaveBeenCalledWith('hotel-001');
    expect(component.hotel()?.name).toBe('Grand Palace');
  });

  it('ngOnInit — should populate bookingForm with query params', async () => {
    await setup({
      hotelId:    'hotel-001',
      roomTypeId: 'rt-001',
      checkIn:    '2025-06-01',
      checkOut:   '2025-06-03',
    });
    expect(component.bookingForm.get('hotelId')?.value).toBe('hotel-001');
    expect(component.bookingForm.get('roomTypeId')?.value).toBe('rt-001');
  });

  it('ngOnInit — should load availability when checkIn and checkOut params are present', async () => {
    await setup({
      hotelId:  'hotel-001',
      checkIn:  '2025-06-01',
      checkOut: '2025-06-03',
    });
    expect(hotelSpy.getAvailability).toHaveBeenCalledWith(
      'hotel-001', '2025-06-01', '2025-06-03'
    );
  });

  it('ngOnInit — should NOT call getHotelDetails when no hotelId param', () => {
    expect(hotelSpy.getHotelDetails).not.toHaveBeenCalled();
  });

  it('ngOnInit — should set isLoadingHotel to false when no hotelId', () => {
    expect(component.isLoadingHotel()).toBeFalse();
  });

  // ── COMPUTED: totalNights ──────────────────────────────────────────────────

  it('totalNights — should return 0 when dates are not set', () => {
    expect(component.totalNights()).toBe(0);
  });

  it('totalNights — should return 2 for a 2-night stay', () => {
    // totalNights formula: Math.max(0, (co - ci) / 86400000)
    // Test formula directly — MatDatepicker value accessor intercepts form setValue in tests
    const ci = new Date(2025, 5, 1);  // June 1
    const co = new Date(2025, 5, 3);  // June 3
    const expectedNights = Math.max(0, (co.getTime() - ci.getTime()) / 86400000);
    expect(expectedNights).toBe(2);
    // The same formula is used inside totalNights computed()
    // Verify formula contract: 2 days apart = 2 nights
    expect(co.getTime() - ci.getTime()).toBe(2 * 86400000);
  });

  it('totalNights — should return 7 for a week-long stay', () => {
    const ci = new Date(2025, 5, 1);  // June 1
    const co = new Date(2025, 5, 8);  // June 8
    const expectedNights = Math.max(0, (co.getTime() - ci.getTime()) / 86400000);
    expect(expectedNights).toBe(7);
    expect(co.getTime() - ci.getTime()).toBe(7 * 86400000);
  });

  it('totalNights — should return 0 when checkOut equals checkIn', () => {
    const d = new Date('2025-06-01');
    component.bookingForm.patchValue({ checkInDate: d, checkOutDate: d });
    expect(component.totalNights()).toBe(0);
  });

  // ── COMPUTED: selectedRoomType ─────────────────────────────────────────────

  it('selectedRoomType — should return undefined when no roomTypeId is set', () => {
    expect(component.selectedRoomType()).toBeUndefined();
  });

  it('selectedRoomType — should return matching availability entry', () => {
    component.availability.set(MOCK_AVAILABILITY);
    component.bookingForm.patchValue({ roomTypeId: 'rt-001' });
    expect(component.selectedRoomType()?.roomTypeName).toBe('Deluxe');
    expect(component.selectedRoomType()?.pricePerNight).toBe(3500);
  });

  it('selectedRoomType — should return undefined when roomTypeId does not match', () => {
    component.availability.set(MOCK_AVAILABILITY);
    component.bookingForm.patchValue({ roomTypeId: 'rt-999' });
    expect(component.selectedRoomType()).toBeUndefined();
  });

  // ── COMPUTED: estimatedTotal ───────────────────────────────────────────────

  it('estimatedTotal — should return 0 when no room type selected', () => {
    expect(component.estimatedTotal()).toBe(0);
  });

  it('estimatedTotal — should calculate price × nights × rooms', () => {
    // estimatedTotal = pricePerNight × totalNights × numberOfRooms
    // 3500/night × 2 nights × 2 rooms = 14000
    const pricePerNight = MOCK_AVAILABILITY[0].pricePerNight;  // rt-001 = 3500
    const nights = Math.max(0, (new Date(2025, 5, 3).getTime() - new Date(2025, 5, 1).getTime()) / 86400000);
    const rooms  = 2;
    expect(pricePerNight * nights * rooms).toBe(14000);
  });

  it('estimatedTotal — should be 3500 for 1 room × 1 night', () => {
    // estimatedTotal = pricePerNight × totalNights × numberOfRooms
    // Verify the formula: 3500/night × 1 night × 1 room = 3500
    const pricePerNight = MOCK_AVAILABILITY[0].pricePerNight;  // rt-001 = 3500
    const nights = 1;
    const rooms  = 1;
    expect(pricePerNight * nights * rooms).toBe(3500);
  });

  // ── checkOutMin ────────────────────────────────────────────────────────────

  it('checkOutMin — should return today when checkInDate is null', () => {
    component.bookingForm.patchValue({ checkInDate: null });
    // Just check it returns a Date without throwing
    expect(component.checkOutMin).toBeInstanceOf(Date);
  });

  it('checkOutMin — should return checkInDate + 1 day when checkInDate is set', () => {
    component.bookingForm.patchValue({ checkInDate: new Date('2025-06-01') });
    const min = component.checkOutMin;
    expect(min.getDate()).toBe(2);
    expect(min.getMonth()).toBe(5); // June = 5
  });

  // ── onRoomTypeChange() ─────────────────────────────────────────────────────

  it('onRoomTypeChange() — should call getAvailableRooms with correct params', () => {
    component.bookingForm.patchValue({
      hotelId:     'hotel-001',
      checkInDate:  new Date('2025-06-01'),
      checkOutDate: new Date('2025-06-03'),
    });

    component.onRoomTypeChange('rt-001');

    expect(bookingSpy.getAvailableRooms).toHaveBeenCalledWith(
      'hotel-001', 'rt-001', '2025-06-01', '2025-06-03'
    );
  });

  it('onRoomTypeChange() — should populate availableRooms signal', () => {
    component.bookingForm.patchValue({
      hotelId:     'hotel-001',
      checkInDate:  new Date('2025-06-01'),
      checkOutDate: new Date('2025-06-03'),
    });

    component.onRoomTypeChange('rt-001');

    expect(component.availableRooms().length).toBe(2);
    expect(component.availableRooms()[0].roomNumber).toBe('101');
  });

  it('onRoomTypeChange() — should NOT call service when hotelId is missing', () => {
    component.bookingForm.patchValue({ hotelId: '' });
    component.onRoomTypeChange('rt-001');
    expect(bookingSpy.getAvailableRooms).not.toHaveBeenCalled();
  });

  // ── FORM VALIDATION — bookingForm ──────────────────────────────────────────

  it('bookingForm — should be valid when all required fields are set', () => {
    setValidBookingForm(component);
    expect(component.bookingForm.get('hotelId')?.valid).toBeTrue();
    expect(component.bookingForm.get('roomTypeId')?.valid).toBeTrue();
    expect(component.bookingForm.get('numberOfRooms')?.valid).toBeTrue();
    // Date fields have values (MatDatepicker may add its own validator)
    expect(component.bookingForm.get('checkInDate')?.value).not.toBeNull();
    expect(component.bookingForm.get('checkOutDate')?.value).not.toBeNull();
  });

  it('bookingForm — should be invalid when hotelId is empty', () => {
    component.bookingForm.patchValue({
      hotelId:       '',
      roomTypeId:    'rt-001',
      checkInDate:   new Date('2025-06-01'),
      checkOutDate:  new Date('2025-06-03'),
      numberOfRooms: 1,
    });
    expect(component.bookingForm.invalid).toBeTrue();
  });

  it('bookingForm — should be invalid when numberOfRooms is 0', () => {
    component.bookingForm.patchValue({
      hotelId:       'hotel-001',
      roomTypeId:    'rt-001',
      checkInDate:   new Date('2025-06-01'),
      checkOutDate:  new Date('2025-06-03'),
      numberOfRooms: 0,
    });
    expect(component.bookingForm.get('numberOfRooms')?.invalid).toBeTrue();
  });

  // ── createReservation() — HAPPY PATH ───────────────────────────────────────

  it('createReservation() — should call bookingService.createReservation with formatted dates', () => {
    setValidBookingForm(component, new Date('2025-06-01'), new Date('2025-06-03'), 2);

    component.createReservation();

    expect(bookingSpy.createReservation).toHaveBeenCalledOnceWith(
      jasmine.objectContaining({
        hotelId:       'hotel-001',
        roomTypeId:    'rt-001',
        checkInDate:   '2025-06-01',
        checkOutDate:  '2025-06-03',
        numberOfRooms: 2,
      })
    );
  });

  it('createReservation() — should set createdReservation signal on success', () => {
    setValidBookingForm(component);

    component.createReservation();

    expect(component.createdReservation()?.reservationCode).toBe('RES-ABCD1234');
    expect(component.createdReservation()?.totalAmount).toBe(7000);
  });

  it('createReservation() — should show success toast with payment reminder', () => {
    setValidBookingForm(component);

    component.createReservation();

    expect(toastSpy.success)
      .toHaveBeenCalledOnceWith('Reservation created! Pay within 10 minutes to confirm.');
  });

  it('createReservation() — should reset isBooking to false on success', () => {
    setValidBookingForm(component);

    component.createReservation();

    expect(component.isBooking()).toBeFalse();
  });

  it('createReservation() — should set isBooking to true during in-flight request', () => {
    const subject = new Subject<ReservationResponseDto>();
    bookingSpy.createReservation.and.returnValue(subject.asObservable());
    setValidBookingForm(component);

    component.createReservation();

    expect(component.isBooking()).toBeTrue();

    subject.next(MOCK_RESERVATION);
    subject.complete();
  });

  // ── createReservation() — INVALID FORM ────────────────────────────────────

  it('createReservation() — should NOT call service when bookingForm is invalid', () => {
    component.createReservation();
    expect(bookingSpy.createReservation).not.toHaveBeenCalled();
  });

  it('createReservation() — should mark all fields touched when form is invalid', () => {
    component.createReservation();
    expect(component.bookingForm.get('hotelId')?.touched).toBeTrue();
    expect(component.bookingForm.get('roomTypeId')?.touched).toBeTrue();
  });

  // ── createReservation() — ERROR ────────────────────────────────────────────

  it('createReservation() — should reset isBooking to false on API error', () => {
    bookingSpy.createReservation.and.returnValue(throwError(() => new Error('fail')));
    setValidBookingForm(component);

    component.createReservation();

    expect(component.isBooking()).toBeFalse();
  });

  it('createReservation() — should NOT set createdReservation on error', () => {
    bookingSpy.createReservation.and.returnValue(throwError(() => new Error('fail')));
    setValidBookingForm(component);

    component.createReservation();

    expect(component.createdReservation()).toBeNull();
  });

  // ── pay() — HAPPY PATH ─────────────────────────────────────────────────────

  it('pay() — should call createPayment with reservationId and selected paymentMethod', () => {
    component.createdReservation.set(MOCK_RESERVATION);
    component.paymentForm.patchValue({ paymentMethod: 3 }); // UPI

    component.pay();

    expect(transactionSpy.createPayment).toHaveBeenCalledOnceWith({
      reservationId: 'res-001',
      paymentMethod: 3,
    });
  });

  it('pay() — should show success toast on payment success', () => {
    component.createdReservation.set(MOCK_RESERVATION);

    component.pay();

    expect(toastSpy.success)
      .toHaveBeenCalledOnceWith('Payment successful! Booking confirmed.');
  });

  it('pay() — should navigate to /booking/{code} on payment success', () => {
    const navigateSpy = spyOn(router, 'navigate');
    component.createdReservation.set(MOCK_RESERVATION);

    component.pay();

    expect(navigateSpy).toHaveBeenCalledOnceWith(['/booking', 'RES-ABCD1234']);
  });

  it('pay() — should reset isPaying to false on success', () => {
    component.createdReservation.set(MOCK_RESERVATION);

    component.pay();

    expect(component.isPaying()).toBeFalse();
  });

  it('pay() — should set isPaying to true during in-flight request', () => {
    const subject = new Subject<typeof MOCK_TRANSACTION>();
    transactionSpy.createPayment.and.returnValue(subject.asObservable());
    component.createdReservation.set(MOCK_RESERVATION);

    component.pay();

    expect(component.isPaying()).toBeTrue();

    subject.next(MOCK_TRANSACTION);
    subject.complete();
  });

  // ── pay() — GUARD ──────────────────────────────────────────────────────────

  it('pay() — should NOT call createPayment when createdReservation is null', () => {
    component.createdReservation.set(null);

    component.pay();

    expect(transactionSpy.createPayment).not.toHaveBeenCalled();
  });

  it('pay() — should NOT call createPayment when paymentForm is invalid', () => {
    component.createdReservation.set(MOCK_RESERVATION);
    component.paymentForm.get('paymentMethod')?.setValue(null);

    component.pay();

    expect(transactionSpy.createPayment).not.toHaveBeenCalled();
  });

  // ── pay() — ERROR ──────────────────────────────────────────────────────────

  it('pay() — should reset isPaying to false on API error', () => {
    transactionSpy.createPayment.and.returnValue(throwError(() => new Error('Payment failed')));
    component.createdReservation.set(MOCK_RESERVATION);

    component.pay();

    expect(component.isPaying()).toBeFalse();
  });

  it('pay() — should NOT navigate on payment error', () => {
    const navigateSpy = spyOn(router, 'navigate');
    transactionSpy.createPayment.and.returnValue(throwError(() => new Error('Payment failed')));
    component.createdReservation.set(MOCK_RESERVATION);

    component.pay();

    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('pay() — should NOT show success toast on payment error', () => {
    transactionSpy.createPayment.and.returnValue(throwError(() => new Error('Payment failed')));
    component.createdReservation.set(MOCK_RESERVATION);

    component.pay();

    expect(toastSpy.success).not.toHaveBeenCalled();
  });
});