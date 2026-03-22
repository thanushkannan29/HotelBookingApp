import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { BookingListComponent } from './booking-list.component';
import { BookingService } from '../../../core/services/booking.service';
import { ReservationDetailsDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ── Mock data ──────────────────────────────────────────────────────────────────

function makeReservation(code: string, status: string): ReservationDetailsDto {
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
    totalAmount:     7000,
    status,
    isCheckedIn:     status === 'Completed',
    createdDate:     '2025-05-01T10:00:00Z',
    rooms:           [{ roomId: 'r-001', roomNumber: '101', floor: 1 }]
  };
}

const MOCK_RESERVATIONS: ReservationDetailsDto[] = [
  makeReservation('RES-0001', 'Confirmed'),
  makeReservation('RES-0002', 'Pending'),
  makeReservation('RES-0003', 'Completed'),
  makeReservation('RES-0004', 'Cancelled'),
  makeReservation('RES-0005', 'NoShow'),
  makeReservation('RES-0006', 'Confirmed'),
];

// ─────────────────────────────────────────────────────────────────────────────

describe('BookingListComponent', () => {
  let component: BookingListComponent;
  let fixture:   ComponentFixture<BookingListComponent>;
  let bookingSpy: jasmine.SpyObj<BookingService>;

  beforeEach(async () => {
    bookingSpy = jasmine.createSpyObj('BookingService', ['getMyReservations']);
    bookingSpy.getMyReservations.and.returnValue(of(MOCK_RESERVATIONS));

    await TestBed.configureTestingModule({
      imports: [BookingListComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: BookingService, useValue: bookingSpy },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(BookingListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── CONSTANTS ──────────────────────────────────────────────────────────────

  it('filters — should contain all 6 filter values', () => {
    expect(component.filters).toEqual([
      'all', 'Pending', 'Confirmed', 'Completed', 'Cancelled', 'NoShow'
    ]);
  });

  // ── INITIAL SIGNAL STATE ───────────────────────────────────────────────────

  it('filter — should start as "all"', () => {
    expect(component.filter()).toBe('all');
  });

  // ── ngOnInit ───────────────────────────────────────────────────────────────

  it('ngOnInit — should call getMyReservations on startup', () => {
    expect(bookingSpy.getMyReservations).toHaveBeenCalledOnceWith();
  });

  it('ngOnInit — should populate reservations signal with all returned items', () => {
    expect(component.reservations().length).toBe(6);
  });

  it('ngOnInit — should store correct reservation codes', () => {
    const codes = component.reservations().map(r => r.reservationCode);
    expect(codes).toContain('RES-0001');
    expect(codes).toContain('RES-0005');
  });

  it('ngOnInit — should handle empty reservations list', async () => {
    bookingSpy.getMyReservations.and.returnValue(of([]));

    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [BookingListComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: BookingService, useValue: bookingSpy },
      ]
    }).compileComponents();

    const f   = TestBed.createComponent(BookingListComponent);
    const cmp = f.componentInstance;
    f.detectChanges();

    expect(cmp.reservations().length).toBe(0);
  });

  // ── filtered GETTER ────────────────────────────────────────────────────────

  it('filtered — should return all 6 reservations when filter is "all"', () => {
    component.filter.set('all');
    expect(component.filtered.length).toBe(6);
  });

  it('filtered — should return only Confirmed reservations', () => {
    component.filter.set('Confirmed');
    expect(component.filtered.length).toBe(2);
    expect(component.filtered.every(r => r.status === 'Confirmed')).toBeTrue();
  });

  it('filtered — should return only Pending reservations', () => {
    component.filter.set('Pending');
    expect(component.filtered.length).toBe(1);
    expect(component.filtered[0].reservationCode).toBe('RES-0002');
  });

  it('filtered — should return only Completed reservations', () => {
    component.filter.set('Completed');
    expect(component.filtered.length).toBe(1);
    expect(component.filtered[0].isCheckedIn).toBeTrue();
  });

  it('filtered — should return only Cancelled reservations', () => {
    component.filter.set('Cancelled');
    expect(component.filtered.length).toBe(1);
    expect(component.filtered[0].reservationCode).toBe('RES-0004');
  });

  it('filtered — should return only NoShow reservations', () => {
    component.filter.set('NoShow');
    expect(component.filtered.length).toBe(1);
    expect(component.filtered[0].reservationCode).toBe('RES-0005');
  });

  it('filtered — should return empty array when filter matches nothing', () => {
    component.filter.set('Confirmed');
    component.reservations.set([makeReservation('RES-X', 'Pending')]);
    expect(component.filtered.length).toBe(0);
  });

  it('filtered — should react to filter signal changes', () => {
    component.filter.set('Pending');
    expect(component.filtered.length).toBe(1);

    component.filter.set('all');
    expect(component.filtered.length).toBe(6);

    component.filter.set('Cancelled');
    expect(component.filtered.length).toBe(1);
  });

  it('filtered — should reflect updated reservations signal', () => {
    component.filter.set('Confirmed');
    expect(component.filtered.length).toBe(2);

    // Add another confirmed reservation
    component.reservations.update(r => [
      ...r, makeReservation('RES-0007', 'Confirmed')
    ]);
    expect(component.filtered.length).toBe(3);
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
    expect(component.statusClass('Random')).toBe('badge-muted');
    expect(component.statusClass('')).toBe('badge-muted');
  });

  it('statusClass() — all known statuses return distinct classes', () => {
    const classes = ['Pending', 'Confirmed', 'Completed', 'Cancelled']
      .map(s => component.statusClass(s));
    const unique = new Set(classes);
    expect(unique.size).toBe(4);
  });
});