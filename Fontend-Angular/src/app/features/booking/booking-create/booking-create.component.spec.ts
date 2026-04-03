import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideNativeDateAdapter } from '@angular/material/core';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { BookingCreateComponent } from './booking-create.component';
import { BookingService } from '../../../core/services/booking.service';
import { TransactionService } from '../../../core/services/api.services';
import { HotelService } from '../../../core/services/hotel.service';
import { WalletService } from '../../../core/services/wallet.service';
import { ToastService } from '../../../core/services/toast.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

const MOCK_HOTEL = {
  hotelId: 'hotel-001', name: 'Grand Palace', address: '1 MG Road',
  city: 'Chennai', state: 'TN', description: 'Luxury hotel.',
  imageUrl: 'https://example.com/img.jpg', contactNumber: '9840650390',
  averageRating: 4.5, reviewCount: 120, gstPercent: 18,
  amenities: ['WiFi'], reviews: [], roomTypes: []
};

const MOCK_AVAILABILITY = [
  { roomTypeId: 'rt-001', roomTypeName: 'Deluxe', pricePerNight: 3500, availableRooms: 5 },
];

const MOCK_RESERVATION = {
  reservationCode: 'RES-ABCD1234', reservationId: 'res-001',
  totalAmount: 7000, gstPercent: 18, gstAmount: 1260,
  discountPercent: 0, discountAmount: 0, walletAmountUsed: 0, finalAmount: 7000,
  status: 'Pending', totalRooms: 1,
  rooms: [{ roomId: 'r-001', roomNumber: '101', floor: 1 }]
};

const MOCK_WALLET = { walletId: 'w-001', balance: 500 };
const MOCK_WALLET_PAGED = { wallet: MOCK_WALLET, transactions: [], totalCount: 0 };

describe('BookingCreateComponent', () => {
  let component: BookingCreateComponent;
  let fixture: ComponentFixture<BookingCreateComponent>;
  let bookingSpy: jasmine.SpyObj<BookingService>;
  let transactionSpy: jasmine.SpyObj<TransactionService>;
  let hotelSpy: jasmine.SpyObj<HotelService>;
  let walletSpy: jasmine.SpyObj<WalletService>;
  let toastSpy: jasmine.SpyObj<ToastService>;

  async function setup(queryParams: Record<string, string> = {}) {
    bookingSpy     = jasmine.createSpyObj('BookingService', ['createReservation', 'getAvailableRooms', 'getReservationByCode', 'getPaymentQr', 'validatePromoCode', 'recordFailedPayment']);
    transactionSpy = jasmine.createSpyObj('TransactionService', ['createPayment']);
    hotelSpy       = jasmine.createSpyObj('HotelService', ['getHotelDetails', 'getAvailability']);
    walletSpy      = jasmine.createSpyObj('WalletService', ['getWallet', 'topUp']);
    toastSpy       = jasmine.createSpyObj('ToastService', ['success', 'error']);

    hotelSpy.getHotelDetails.and.returnValue(of(MOCK_HOTEL as any));
    hotelSpy.getAvailability.and.returnValue(of(MOCK_AVAILABILITY as any));
    bookingSpy.createReservation.and.returnValue(of(MOCK_RESERVATION as any));
    bookingSpy.getAvailableRooms.and.returnValue(of([]));
    bookingSpy.getPaymentQr.and.returnValue(of({ upiId: 'hotel@upi', qrCodeBase64: '' } as any));
    walletSpy.getWallet.and.returnValue(of(MOCK_WALLET_PAGED as any));

    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [BookingCreateComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]), provideNativeDateAdapter(),
        { provide: BookingService,     useValue: bookingSpy },
        { provide: TransactionService, useValue: transactionSpy },
        { provide: HotelService,       useValue: hotelSpy },
        { provide: WalletService,      useValue: walletSpy },
        { provide: ToastService,       useValue: toastSpy },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParams } } },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(BookingCreateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  beforeEach(async () => await setup());

  it('should create', () => expect(component).toBeTruthy());

  // ── Initial state ─────────────────────────────────────────────────────────

  it('isBooking — should start as false', () => expect(component.isBooking()).toBeFalse());
  it('isPaying — should start as false',  () => expect(component.isPaying()).toBeFalse());
  it('hotel — should start as null when no hotelId param', () => expect(component.hotel()).toBeNull());
  it('availability — should start as empty array', () => expect(component.availability()).toEqual([]));
  it('createdReservation — should start as null', () => expect(component.createdReservation()).toBeNull());
  it('useWallet — should start as false', () => expect(component.useWallet()).toBeFalse());

  // ── bookingForm ───────────────────────────────────────────────────────────

  it('bookingForm — numberOfRooms should default to 1', () => {
    expect(component.bookingForm.get('numberOfRooms')?.value).toBe(1);
  });

  it('bookingForm — should be invalid initially', () => {
    expect(component.bookingForm.invalid).toBeTrue();
  });

  // ── ngOnInit WITH QUERY PARAMS ─────────────────────────────────────────────

  it('ngOnInit — should load hotel details when hotelId param is present', async () => {
    await setup({ hotelId: 'hotel-001' });
    expect(hotelSpy.getHotelDetails).toHaveBeenCalledWith('hotel-001');
    expect(component.hotel()?.name).toBe('Grand Palace');
  });

  it('ngOnInit — should populate bookingForm with hotelId param', async () => {
    await setup({ hotelId: 'hotel-001', roomTypeId: 'rt-001' });
    expect(component.bookingForm.get('hotelId')?.value).toBe('hotel-001');
  });

  it('ngOnInit — should NOT call getHotelDetails when no hotelId param', () => {
    expect(hotelSpy.getHotelDetails).not.toHaveBeenCalled();
  });

  it('ngOnInit — should set isLoadingHotel to false when no hotelId', () => {
    expect(component.isLoadingHotel()).toBeFalse();
  });

  // ── computed: totalNights ─────────────────────────────────────────────────

  it('totalNights — should return 0 when dates are not set', () => {
    expect(component.totalNights()).toBe(0);
  });

  it('totalNights — should return 2 for a 2-night stay', () => {
    component.checkInDate.set(new Date(2025, 5, 1));
    component.checkOutDate.set(new Date(2025, 5, 3));
    expect(component.totalNights()).toBe(2);
  });

  it('totalNights — should return 0 when checkOut equals checkIn', () => {
    const d = new Date(2025, 5, 1);
    component.checkInDate.set(d);
    component.checkOutDate.set(d);
    expect(component.totalNights()).toBe(0);
  });

  // ── computed: selectedRoomType ────────────────────────────────────────────

  it('selectedRoomType — should return undefined when no roomTypeId is set', () => {
    expect(component.selectedRoomType()).toBeUndefined();
  });

  it('selectedRoomType — should return matching availability entry', () => {
    component.availability.set(MOCK_AVAILABILITY as any);
    component.selectedRoomTypeId.set('rt-001');
    expect(component.selectedRoomType()?.roomTypeName).toBe('Deluxe');
  });

  // ── computed: baseTotal ───────────────────────────────────────────────────

  it('baseTotal — should return 0 when no room type selected', () => {
    expect(component.baseTotal()).toBe(0);
  });

  it('baseTotal — should calculate price × nights × rooms', () => {
    component.availability.set(MOCK_AVAILABILITY as any);
    component.selectedRoomTypeId.set('rt-001');
    component.checkInDate.set(new Date(2025, 5, 1));
    component.checkOutDate.set(new Date(2025, 5, 3));
    component.numberOfRooms.set(1);
    expect(component.baseTotal()).toBe(7000); // 3500 × 2 nights × 1 room
  });

  // ── checkOutMin ───────────────────────────────────────────────────────────

  it('checkOutMin — should return a Date', () => {
    expect(component.checkOutMin).toBeInstanceOf(Date);
  });

  it('checkOutMin — should return day after checkIn when set', () => {
    // Use a future date so checkIn+1 > tomorrow
    const futureDate = new Date();
    futureDate.setFullYear(futureDate.getFullYear() + 1);
    futureDate.setMonth(5); futureDate.setDate(1); // June 1 next year
    component.checkInDate.set(futureDate);
    const min = component.checkOutMin;
    expect(min.getDate()).toBe(2);   // June 2
    expect(min.getMonth()).toBe(5);  // June
  });

  // ── onRoomTypeChange ──────────────────────────────────────────────────────

  it('onRoomTypeChange — should call getAvailableRooms when dates are set', () => {
    component.bookingForm.patchValue({
      hotelId: 'hotel-001',
      checkInDate: new Date(2025, 5, 1),
      checkOutDate: new Date(2025, 5, 3),
    });
    component.checkInDate.set(new Date(2025, 5, 1));
    component.checkOutDate.set(new Date(2025, 5, 3));
    component.onRoomTypeChange('rt-001');
    expect(bookingSpy.getAvailableRooms).toHaveBeenCalled();
  });

  // ── createReservation ─────────────────────────────────────────────────────

  it('createReservation — should show error toast when dates are missing', () => {
    component.createReservation();
    expect(toastSpy.error).toHaveBeenCalled();
  });

  it('createReservation — should call bookingService.createReservation when valid', () => {
    component.bookingForm.patchValue({
      hotelId: 'hotel-001', roomTypeId: 'rt-001',
      checkInDate: new Date(2025, 5, 1), checkOutDate: new Date(2025, 5, 3),
      numberOfRooms: 1
    });
    component.checkInDate.set(new Date(2025, 5, 1));
    component.checkOutDate.set(new Date(2025, 5, 3));
    component.selectedRoomTypeId.set('rt-001');
    component.createReservation();
    expect(bookingSpy.createReservation).toHaveBeenCalled();
  });

  it('createReservation — should set createdReservation on success', () => {
    component.bookingForm.patchValue({
      hotelId: 'hotel-001', roomTypeId: 'rt-001',
      checkInDate: new Date(2025, 5, 1), checkOutDate: new Date(2025, 5, 3),
      numberOfRooms: 1
    });
    component.checkInDate.set(new Date(2025, 5, 1));
    component.checkOutDate.set(new Date(2025, 5, 3));
    component.selectedRoomTypeId.set('rt-001');
    component.createReservation();
    expect(component.createdReservation()?.reservationCode).toBe('RES-ABCD1234');
  });

  it('createReservation — should reset isBooking to false on error', () => {
    bookingSpy.createReservation.and.returnValue(throwError(() => new Error('fail')));
    component.bookingForm.patchValue({
      hotelId: 'hotel-001', roomTypeId: 'rt-001',
      checkInDate: new Date(2025, 5, 1), checkOutDate: new Date(2025, 5, 3),
      numberOfRooms: 1
    });
    component.checkInDate.set(new Date(2025, 5, 1));
    component.checkOutDate.set(new Date(2025, 5, 3));
    component.selectedRoomTypeId.set('rt-001');
    component.createReservation();
    expect(component.isBooking()).toBeFalse();
  });

  // ── fmtLocal ──────────────────────────────────────────────────────────────

  it('fmtLocal — should format date as YYYY-MM-DD', () => {
    expect(component.fmtLocal(new Date(2025, 5, 1))).toBe('2025-06-01');
  });

  // ── toggleWallet ──────────────────────────────────────────────────────────

  it('toggleWallet — should set useWallet to true', () => {
    component.toggleWallet(true);
    expect(component.useWallet()).toBeTrue();
  });

  it('toggleWallet — should reset walletAmount to 0 when disabled', () => {
    component.toggleWallet(false);
    expect(component.bookingForm.get('walletAmount')?.value).toBe(0);
  });

  // ── clearPromo ────────────────────────────────────────────────────────────

  it('clearPromo — should reset promoDiscount to 0', () => {
    component.promoDiscount.set(500);
    component.clearPromo();
    expect(component.promoDiscount()).toBe(0);
  });

  it('clearPromo — should reset promoValid to null', () => {
    component.promoValid.set(true);
    component.clearPromo();
    expect(component.promoValid()).toBeNull();
  });
});
