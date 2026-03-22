import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ReservationManagementComponent } from './reservation-management.component';
import { BookingService } from '../../../core/services/booking.service';
import { ToastService } from '../../../core/services/toast.service';
import { ReservationDetailsDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ── Mock data ──────────────────────────────────────────────────────────────────

// ✅ Fixed: no generic type on overrides parameter — avoids parser confusion
function makeRes(code: string, status: string, overrides?: Partial<ReservationDetailsDto>): ReservationDetailsDto {
  return {
    reservationCode: code,
    reservationId:   `id-${code}`,
    hotelId:         'hotel-001',
    hotelName:       'Grand Palace',
    roomTypeId:      'rt-001',
    roomTypeName:    'Deluxe',
    checkInDate:     '2025-06-01',
    checkOutDate:    '2025-06-03',
    numberOfRooms:   1,
    totalAmount:     10000,
    status,
    isCheckedIn:     false,
    createdDate:     '2025-05-01T10:00:00Z',
    rooms:           [{ roomId: 'r-001', roomNumber: '101', floor: 1 }],
    ...overrides
  };
}

const MOCK_RESERVATIONS: ReservationDetailsDto[] = [
  makeRes('RES-0001', 'Confirmed'),
  makeRes('RES-0002', 'Pending'),
  makeRes('RES-0003', 'Completed', { isCheckedIn: true }),
  makeRes('RES-0004', 'Cancelled'),
  makeRes('RES-0005', 'NoShow'),
  makeRes('RES-0006', 'Confirmed'),
];

const MOCK_PAGED = { totalCount: 6, reservations: MOCK_RESERVATIONS };

// ─────────────────────────────────────────────────────────────────────────────

describe('ReservationManagementComponent', () => {
  let component: ReservationManagementComponent;
  let fixture:   ComponentFixture<ReservationManagementComponent>;

  let bookingSpy: jasmine.SpyObj<BookingService>;
  let toastSpy:   jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    bookingSpy = jasmine.createSpyObj('BookingService', [
      'getHotelReservations', 'completeReservation'
    ]);
    toastSpy = jasmine.createSpyObj('ToastService', ['success', 'error']);

    bookingSpy.getHotelReservations.and.returnValue(of(MOCK_PAGED));
    bookingSpy.completeReservation.and.returnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [ReservationManagementComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: BookingService, useValue: bookingSpy },
        { provide: ToastService,   useValue: toastSpy   },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(ReservationManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── CONSTANTS ──────────────────────────────────────────────────────────────

  it('pageSize — should be 15', () => {
    expect(component.pageSize).toBe(15);
  });

  it('filters — should contain all expected status values', () => {
    expect(component.filters).toEqual([
      'all', 'Pending', 'Confirmed', 'Completed', 'Cancelled', 'NoShow'
    ]);
  });

  // ── INITIAL SIGNAL STATE ───────────────────────────────────────────────────

  it('page — should start at 1', () => {
    expect(component.page()).toBe(1);
  });

  it('filter — should start as "all"', () => {
    expect(component.filter()).toBe('all');
  });

  // ── ngOnInit / load() ──────────────────────────────────────────────────────

  it('ngOnInit — should call getHotelReservations with page 1 and pageSize 15', () => {
    expect(bookingSpy.getHotelReservations).toHaveBeenCalledOnceWith(1, 15);
  });

  it('load() — should populate reservations signal', () => {
    expect(component.reservations().length).toBe(6);
    expect(component.reservations()[0].reservationCode).toBe('RES-0001');
  });

  it('load() — should set total signal to totalCount', () => {
    expect(component.total()).toBe(6);
  });

  // ── filtered GETTER ────────────────────────────────────────────────────────

  it('filtered — should return all reservations when filter is "all"', () => {
    component.filter.set('all');
    expect(component.filtered.length).toBe(6);
  });

  it('filtered — should return only Confirmed when filter is "Confirmed"', () => {
    component.filter.set('Confirmed');
    expect(component.filtered.length).toBe(2);
    expect(component.filtered.every(r => r.status === 'Confirmed')).toBeTrue();
  });

  it('filtered — should return only Pending when filter is "Pending"', () => {
    component.filter.set('Pending');
    expect(component.filtered.length).toBe(1);
    expect(component.filtered[0].reservationCode).toBe('RES-0002');
  });

  it('filtered — should return only Completed when filter is "Completed"', () => {
    component.filter.set('Completed');
    expect(component.filtered.length).toBe(1);
    expect(component.filtered[0].isCheckedIn).toBeTrue();
  });

  it('filtered — should return only Cancelled when filter is "Cancelled"', () => {
    component.filter.set('Cancelled');
    expect(component.filtered.length).toBe(1);
    expect(component.filtered[0].reservationCode).toBe('RES-0004');
  });

  it('filtered — should return only NoShow when filter is "NoShow"', () => {
    component.filter.set('NoShow');
    expect(component.filtered.length).toBe(1);
    expect(component.filtered[0].reservationCode).toBe('RES-0005');
  });

  it('filtered — should return empty array when filter matches nothing', () => {
    component.filter.set('Confirmed');
    component.reservations.set([makeRes('RES-X', 'Pending')]);
    expect(component.filtered.length).toBe(0);
  });

  // ── totalPages GETTER ──────────────────────────────────────────────────────

  it('totalPages — should be 1 when total is 6 and pageSize is 15', () => {
    expect(component.totalPages).toBe(1);
  });

  it('totalPages — should be 2 when total is 16 and pageSize is 15', () => {
    component.total.set(16);
    expect(component.totalPages).toBe(2);
  });

  it('totalPages — should be 3 when total is 31', () => {
    component.total.set(31);
    expect(component.totalPages).toBe(3);
  });

  it('totalPages — should be 0 when total is 0', () => {
    component.total.set(0);
    expect(component.totalPages).toBe(0);
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
    expect(component.statusClass('Unknown')).toBe('badge-muted');
    expect(component.statusClass('')).toBe('badge-muted');
  });

  // ── nextPage() / prevPage() ────────────────────────────────────────────────

  it('nextPage() — should increment page and reload', () => {
    bookingSpy.getHotelReservations.calls.reset();

    component.nextPage();

    expect(component.page()).toBe(2);
    expect(bookingSpy.getHotelReservations).toHaveBeenCalledWith(2, 15);
  });

  it('nextPage() — calling twice should reach page 3', () => {
    component.nextPage();
    component.nextPage();
    expect(component.page()).toBe(3);
  });

  it('prevPage() — should decrement page and reload when on page 2', () => {
    component.nextPage();
    bookingSpy.getHotelReservations.calls.reset();

    component.prevPage();

    expect(component.page()).toBe(1);
    expect(bookingSpy.getHotelReservations).toHaveBeenCalledWith(1, 15);
  });

  it('prevPage() — should NOT go below page 1', () => {
    bookingSpy.getHotelReservations.calls.reset();

    component.prevPage();

    expect(component.page()).toBe(1);
    expect(bookingSpy.getHotelReservations).not.toHaveBeenCalled();
  });

  // ── complete() — HAPPY PATH ────────────────────────────────────────────────

  it('complete() — should call completeReservation with the reservation code', () => {
    spyOn(window, 'confirm').and.returnValue(true);

    component.complete('RES-0001');

    expect(bookingSpy.completeReservation).toHaveBeenCalledOnceWith('RES-0001');
  });

  it('complete() — should show success toast on completion', () => {
    spyOn(window, 'confirm').and.returnValue(true);

    component.complete('RES-0001');

    expect(toastSpy.success)
      .toHaveBeenCalledOnceWith('Reservation marked as completed.');
  });

  it('complete() — should update status to Completed in reservations signal', () => {
    spyOn(window, 'confirm').and.returnValue(true);

    component.complete('RES-0001');

    const updated = component.reservations()
      .find(r => r.reservationCode === 'RES-0001');
    expect(updated?.status).toBe('Completed');
  });

  it('complete() — should set isCheckedIn to true in reservations signal', () => {
    spyOn(window, 'confirm').and.returnValue(true);

    component.complete('RES-0001');

    const updated = component.reservations()
      .find(r => r.reservationCode === 'RES-0001');
    expect(updated?.isCheckedIn).toBeTrue();
  });

  it('complete() — should leave all other reservations unchanged', () => {
    spyOn(window, 'confirm').and.returnValue(true);

    component.complete('RES-0001');

    const others = component.reservations()
      .filter(r => r.reservationCode !== 'RES-0001');
    expect(others.every(r => r.status !== 'Completed' || r.reservationCode === 'RES-0003'))
      .toBeTrue();
  });

  // ── complete() — CONFIRM CANCELLED ────────────────────────────────────────

  it('complete() — should NOT call service when confirm is cancelled', () => {
    spyOn(window, 'confirm').and.returnValue(false);

    component.complete('RES-0001');

    expect(bookingSpy.completeReservation).not.toHaveBeenCalled();
  });

  it('complete() — should NOT show toast when confirm is cancelled', () => {
    spyOn(window, 'confirm').and.returnValue(false);

    component.complete('RES-0001');

    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  it('complete() — should NOT update reservations signal when confirm is cancelled', () => {
    spyOn(window, 'confirm').and.returnValue(false);
    const originalStatus = component.reservations()
      .find(r => r.reservationCode === 'RES-0001')?.status;

    component.complete('RES-0001');

    const afterStatus = component.reservations()
      .find(r => r.reservationCode === 'RES-0001')?.status;
    expect(afterStatus).toBe(originalStatus);
  });
});