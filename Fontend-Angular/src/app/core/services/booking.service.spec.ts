import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { BookingService } from './booking.service';
import {
  CreateReservationDto,
  CancelReservationDto,
  ReservationDetailsDto,
  ReservationResponseDto
} from '../models/models';

const BASE = environment.apiUrl;

// ── Reusable mock data ────────────────────────────────────────────────────────

const MOCK_RESERVATION_RESPONSE: ReservationResponseDto = {
  reservationCode: 'RES-ABCD1234',
  reservationId: 'res-001',
  totalAmount: 10000,
  gstPercent: 18,
  gstAmount: 1800,
  discountPercent: 0,
  discountAmount: 0,
  walletAmountUsed: 0,
  finalAmount: 10000,
  status: 'Pending',
  totalRooms: 1,
  rooms: [{ roomId: 'r-001', roomNumber: '101', floor: 1 }]
};

const MOCK_RESERVATION_DETAIL: ReservationDetailsDto = {
  reservationCode: 'RES-ABCD1234',
  reservationId: 'res-001',
  hotelId: 'hotel-001',
  hotelName: 'Grand Palace',
  roomTypeId: 'rt-001',
  roomTypeName: 'Deluxe',
  checkInDate: '2025-06-01',
  checkOutDate: '2025-06-03',
  numberOfRooms: 1,
  totalAmount: 10000,
  gstPercent: 18,
  gstAmount: 1800,
  discountPercent: 0,
  discountAmount: 0,
  walletAmountUsed: 0,
  finalAmount: 10000,
  status: 'Confirmed',
  isCheckedIn: false,
  createdDate: '2025-05-15T10:00:00Z',
  rooms: [{ roomId: 'r-001', roomNumber: '101', floor: 1 }],
  cancellationFeePaid: false,
  cancellationFeeAmount: 0,
  cancellationPolicyText: ''
};

// ─────────────────────────────────────────────────────────────────────────────

describe('BookingService', () => {
  let service: BookingService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(BookingService);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify()); // fails if any unexpected HTTP call was made

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  // ── GUEST: createReservation ───────────────────────────────────────────────

  it('createReservation() — should POST to /guest/reservations with dto', () => {
    const dto: CreateReservationDto = {
      hotelId:       'hotel-001',
      roomTypeId:    'rt-001',
      checkInDate:   '2025-06-01',
      checkOutDate:  '2025-06-03',
      numberOfRooms: 1
    };

    service.createReservation(dto).subscribe(result => {
      expect(result.reservationCode).toBe('RES-ABCD1234');
      expect(result.status).toBe('Pending');
      expect(result.totalAmount).toBe(10000);
      expect(result.rooms.length).toBe(1);
    });

    const req = http.expectOne(`${BASE}/guest/reservations`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.hotelId).toBe('hotel-001');
    expect(req.request.body.roomTypeId).toBe('rt-001');
    expect(req.request.body.checkInDate).toBe('2025-06-01');
    expect(req.request.body.numberOfRooms).toBe(1);
    req.flush({ success: true, data: MOCK_RESERVATION_RESPONSE });
  });

  it('createReservation() — should include selectedRoomIds when provided', () => {
    const dto: CreateReservationDto = {
      hotelId:         'hotel-001',
      roomTypeId:      'rt-001',
      checkInDate:     '2025-06-01',
      checkOutDate:    '2025-06-03',
      numberOfRooms:   2,
      selectedRoomIds: ['r-001', 'r-002']
    };

    service.createReservation(dto).subscribe();

    const req = http.expectOne(`${BASE}/guest/reservations`);
    expect(req.request.body.selectedRoomIds).toEqual(['r-001', 'r-002']);
    expect(req.request.body.numberOfRooms).toBe(2);
    req.flush({ success: true, data: MOCK_RESERVATION_RESPONSE });
  });

  // ── GUEST: getMyReservations ───────────────────────────────────────────────

  it('getMyReservations() — should GET /guest/reservations and return array', () => {
    service.getMyReservations().subscribe(result => {
      expect(result.length).toBe(2);
      expect(result[0].hotelName).toBe('Grand Palace');
      expect(result[1].status).toBe('Cancelled');
    });

    const req = http.expectOne(`${BASE}/guest/reservations`);
    expect(req.request.method).toBe('GET');
    req.flush({
      success: true,
      data: [
        MOCK_RESERVATION_DETAIL,
        { ...MOCK_RESERVATION_DETAIL, reservationId: 'res-002', reservationCode: 'RES-WXYZ5678', status: 'Cancelled' }
      ]
    });
  });

  it('getMyReservations() — should return empty array when no reservations', () => {
    service.getMyReservations().subscribe(result => {
      expect(result.length).toBe(0);
    });

    http.expectOne(`${BASE}/guest/reservations`)
        .flush({ success: true, data: [] });
  });

  // ── GUEST: getMyReservationsHistory ───────────────────────────────────────

  it('getMyReservationsHistory() — should POST to /guest/reservations/history with body', () => {
    service.getMyReservationsHistory(1, 10).subscribe(result => {
      expect(result.totalCount).toBe(5);
      expect(result.reservations.length).toBe(1);
    });

    const req = http.expectOne(`${BASE}/guest/reservations/history`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.page).toBe(1);
    expect(req.request.body.pageSize).toBe(10);
    req.flush({
      success: true,
      data: { totalCount: 5, reservations: [MOCK_RESERVATION_DETAIL] }
    });
  });

  it('getMyReservationsHistory() — page 2 should send correct page in body', () => {
    service.getMyReservationsHistory(2, 5).subscribe();

    const req = http.expectOne(`${BASE}/guest/reservations/history`);
    expect(req.request.body.page).toBe(2);
    expect(req.request.body.pageSize).toBe(5);
    req.flush({ success: true, data: { totalCount: 10, reservations: [] } });
  });

  // ── GUEST: getReservationByCode ────────────────────────────────────────────

  it('getReservationByCode() — should GET /guest/reservations/{code}', () => {
    service.getReservationByCode('RES-ABCD1234').subscribe(result => {
      expect(result.reservationCode).toBe('RES-ABCD1234');
      expect(result.hotelName).toBe('Grand Palace');
      expect(result.totalAmount).toBe(10000);
      expect(result.isCheckedIn).toBeFalse();
    });

    const req = http.expectOne(`${BASE}/guest/reservations/RES-ABCD1234`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: MOCK_RESERVATION_DETAIL });
  });

  it('getReservationByCode() — should embed the code in the URL correctly', () => {
    service.getReservationByCode('RES-ZZZZ9999').subscribe();

    const req = http.expectOne(`${BASE}/guest/reservations/RES-ZZZZ9999`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: { ...MOCK_RESERVATION_DETAIL, reservationCode: 'RES-ZZZZ9999' } });
  });

  // ── GUEST: cancelReservation ───────────────────────────────────────────────

  it('cancelReservation() — should PATCH /guest/reservations/{code}/cancel with reason', () => {
    const dto: CancelReservationDto = { reason: 'Change of travel plans' };

    service.cancelReservation('RES-ABCD1234', dto).subscribe(result => {
      expect(result).toBeUndefined();
    });

    const req = http.expectOne(`${BASE}/guest/reservations/RES-ABCD1234/cancel`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body.reason).toBe('Change of travel plans');
    req.flush({ success: true, message: 'Reservation cancelled successfully.' });
  });

  it('cancelReservation() — should embed the code correctly in the URL', () => {
    service.cancelReservation('RES-WXYZ5678', { reason: 'Hotel quality issues' }).subscribe();

    const req = http.expectOne(`${BASE}/guest/reservations/RES-WXYZ5678/cancel`);
    expect(req.request.body.reason).toBe('Hotel quality issues');
    req.flush({ success: true, message: 'Reservation cancelled successfully.' });
  });

  // ── GUEST: getAvailableRooms ───────────────────────────────────────────────

  it('getAvailableRooms() — should GET /guest/reservations/available-rooms with all 4 params', () => {
    service.getAvailableRooms('hotel-001', 'rt-001', '2025-06-01', '2025-06-03').subscribe(result => {
      expect(result.length).toBe(3);
      expect(result[0].roomNumber).toBe('101');
      expect(result[1].floor).toBe(1);
    });

    const req = http.expectOne(r => r.url === `${BASE}/guest/reservations/available-rooms`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('hotelId')).toBe('hotel-001');
    expect(req.request.params.get('roomTypeId')).toBe('rt-001');
    expect(req.request.params.get('checkIn')).toBe('2025-06-01');
    expect(req.request.params.get('checkOut')).toBe('2025-06-03');
    req.flush({
      success: true,
      data: [
        { roomId: 'r-001', roomNumber: '101', floor: 1, roomTypeName: 'Deluxe' },
        { roomId: 'r-002', roomNumber: '102', floor: 1, roomTypeName: 'Deluxe' },
        { roomId: 'r-003', roomNumber: '201', floor: 2, roomTypeName: 'Deluxe' }
      ]
    });
  });

  it('getAvailableRooms() — should return empty array when all rooms are booked', () => {
    service.getAvailableRooms('hotel-001', 'rt-001', '2025-12-24', '2025-12-26').subscribe(result => {
      expect(result.length).toBe(0);
    });

    http.expectOne(r => r.url === `${BASE}/guest/reservations/available-rooms`)
        .flush({ success: true, data: [] });
  });

  // ── ADMIN: getHotelReservations ────────────────────────────────────────────

  it('getHotelReservations() — should POST to /admin/reservations/list with body', () => {
    service.getHotelReservations(1, 15).subscribe(result => {
      expect(result.totalCount).toBe(42);
      expect(result.reservations.length).toBe(1);
    });

    const req = http.expectOne(`${BASE}/admin/reservations/list`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.page).toBe(1);
    expect(req.request.body.pageSize).toBe(15);
    req.flush({
      success: true,
      data: { totalCount: 42, reservations: [MOCK_RESERVATION_DETAIL] }
    });
  });

  it('getHotelReservations() — page 3 should send correct page in body', () => {
    service.getHotelReservations(3, 10).subscribe();

    const req = http.expectOne(`${BASE}/admin/reservations/list`);
    expect(req.request.body.page).toBe(3);
    expect(req.request.body.pageSize).toBe(10);
    req.flush({ success: true, data: { totalCount: 100, reservations: [] } });
  });

  // ── ADMIN: completeReservation ─────────────────────────────────────────────

  it('completeReservation() — should PATCH /admin/reservations/{code}/complete with empty body', () => {
    service.completeReservation('RES-ABCD1234').subscribe(result => {
      expect(result).toBeUndefined();
    });

    const req = http.expectOne(`${BASE}/admin/reservations/RES-ABCD1234/complete`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({});
    req.flush({ success: true, message: 'Reservation marked as completed.' });
  });

  it('completeReservation() — should embed the reservation code in the URL', () => {
    service.completeReservation('RES-WXYZ5678').subscribe();

    const req = http.expectOne(`${BASE}/admin/reservations/RES-WXYZ5678/complete`);
    expect(req.request.method).toBe('PATCH');
    req.flush({ success: true, message: 'Reservation marked as completed.' });
  });
});