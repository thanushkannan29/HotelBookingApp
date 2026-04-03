import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { BookingListComponent } from './booking-list.component';
import { BookingService } from '../../../core/services/booking.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

function makeRes(code: string, status: string) {
  return {
    reservationCode: code, reservationId: `id-${code}`,
    hotelId: 'hotel-001', hotelName: 'Grand Palace',
    roomTypeId: 'rt-001', roomTypeName: 'Deluxe',
    checkInDate: '2025-06-01', checkOutDate: '2025-06-03',
    numberOfRooms: 1, totalAmount: 7000,
    gstPercent: 18, gstAmount: 1260, discountPercent: 0, discountAmount: 0,
    walletAmountUsed: 0, finalAmount: 7000,
    status, isCheckedIn: status === 'Completed', createdDate: '2025-05-01T10:00:00Z',
    rooms: [{ roomId: 'r-001', roomNumber: '101', floor: 1 }],
    cancellationFeePaid: false, cancellationFeeAmount: 0, cancellationPolicyText: ''
  };
}

const MOCK_RESERVATIONS = [
  makeRes('RES-0001', 'Confirmed'), makeRes('RES-0002', 'Pending'),
  makeRes('RES-0003', 'Completed'), makeRes('RES-0004', 'Cancelled'),
  makeRes('RES-0005', 'NoShow'),   makeRes('RES-0006', 'Confirmed'),
];
const MOCK_PAGED = { totalCount: 6, reservations: MOCK_RESERVATIONS };

describe('BookingListComponent', () => {
  let component: BookingListComponent;
  let fixture: ComponentFixture<BookingListComponent>;
  let bookingSpy: jasmine.SpyObj<BookingService>;

  beforeEach(async () => {
    bookingSpy = jasmine.createSpyObj('BookingService', ['getMyReservationsHistory']);
    bookingSpy.getMyReservationsHistory.and.returnValue(of(MOCK_PAGED as any));

    await TestBed.configureTestingModule({
      imports: [BookingListComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]),
        { provide: BookingService, useValue: bookingSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(BookingListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── Initial state ─────────────────────────────────────────────────────────

  it('pageSize — should be 10', () => expect(component.pageSize).toBe(10));
  it('currentPage — should start at 1', () => expect(component.currentPage).toBe(1));
  it('selectedStatus — should start as "All"', () => expect(component.selectedStatus).toBe('All'));
  it('statusTabs — should contain all 6 tabs', () => {
    expect(component.statusTabs).toEqual(['All', 'Pending', 'Confirmed', 'Completed', 'Cancelled', 'NoShow']);
  });

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should call getMyReservationsHistory', () => {
    expect(bookingSpy.getMyReservationsHistory).toHaveBeenCalledWith(1, 10, 'All', '');
  });

  it('ngOnInit — should populate reservations signal', () => {
    expect(component.reservations().length).toBe(6);
  });

  it('ngOnInit — should set totalCount', () => {
    expect(component.totalCount()).toBe(6);
  });

  it('loading — should be false after load', () => {
    expect(component.loading()).toBeFalse();
  });

  it('load — should set loading to false on error', () => {
    bookingSpy.getMyReservationsHistory.and.returnValue(throwError(() => new Error('fail')));
    component.load();
    expect(component.loading()).toBeFalse();
  });

  // ── onTabChange ───────────────────────────────────────────────────────────

  it('onTabChange — should set selectedStatus', () => {
    component.onTabChange(2); // 'Confirmed'
    expect(component.selectedStatus).toBe('Confirmed');
  });

  it('onTabChange — should reset currentPage to 1', () => {
    component.currentPage = 3;
    component.onTabChange(1);
    expect(component.currentPage).toBe(1);
  });

  it('onTabChange — should reload', () => {
    bookingSpy.getMyReservationsHistory.calls.reset();
    component.onTabChange(1);
    expect(bookingSpy.getMyReservationsHistory).toHaveBeenCalled();
  });

  // ── onPage ────────────────────────────────────────────────────────────────

  it('onPage — should update currentPage and reload', () => {
    bookingSpy.getMyReservationsHistory.calls.reset();
    component.onPage({ pageIndex: 1, pageSize: 10, length: 20 } as any);
    expect(component.currentPage).toBe(2);
    expect(bookingSpy.getMyReservationsHistory).toHaveBeenCalled();
  });

  // ── statusClass ───────────────────────────────────────────────────────────

  it('statusClass — Pending → badge-warning',   () => expect(component.statusClass('Pending')).toBe('badge-warning'));
  it('statusClass — Confirmed → badge-success', () => expect(component.statusClass('Confirmed')).toBe('badge-success'));
  it('statusClass — Completed → badge-primary', () => expect(component.statusClass('Completed')).toBe('badge-primary'));
  it('statusClass — Cancelled → badge-error',   () => expect(component.statusClass('Cancelled')).toBe('badge-error'));
  it('statusClass — NoShow → badge-muted',      () => expect(component.statusClass('NoShow')).toBe('badge-muted'));
  it('statusClass — unknown → badge-muted',     () => expect(component.statusClass('Unknown')).toBe('badge-muted'));

  // ── canPayNow ─────────────────────────────────────────────────────────────

  it('canPayNow — should return true for Pending with future expiryTime', () => {
    const future = new Date(Date.now() + 600000).toISOString();
    const res = { ...makeRes('RES-X', 'Pending'), expiryTime: future } as any;
    expect(component.canPayNow(res)).toBeTrue();
  });

  it('canPayNow — should return false for Confirmed', () => {
    expect(component.canPayNow(makeRes('RES-X', 'Confirmed') as any)).toBeFalse();
  });

  it('canPayNow — should return false for Pending without expiryTime', () => {
    expect(component.canPayNow(makeRes('RES-X', 'Pending') as any)).toBeFalse();
  });

  // ── onSearch debounce ─────────────────────────────────────────────────────

  it('onSearch — should update searchTerm after debounce', fakeAsync(() => {
    bookingSpy.getMyReservationsHistory.calls.reset();
    component.onSearch('Grand');
    tick(400);
    expect(component.searchTerm).toBe('Grand');
    expect(bookingSpy.getMyReservationsHistory).toHaveBeenCalled();
  }));
});
