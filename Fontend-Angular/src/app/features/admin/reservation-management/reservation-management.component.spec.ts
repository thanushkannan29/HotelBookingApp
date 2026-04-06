import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ReservationManagementComponent } from './reservation-management.component';
import { BookingService } from '../../../core/services/booking.service';
import { ToastService } from '../../../core/services/toast.service';
import { MatDialog } from '@angular/material/dialog';
import { ReservationDetailsDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

function makeRes(code: string, status: string): ReservationDetailsDto {
  return {
    reservationCode: code, reservationId: `id-${code}`,
    hotelId: 'hotel-001', hotelName: 'Grand Palace',
    roomTypeId: 'rt-001', roomTypeName: 'Deluxe',
    checkInDate: '2025-06-01', checkOutDate: '2025-06-03',
    numberOfRooms: 1, totalAmount: 10000,
    gstPercent: 18, gstAmount: 1800, discountPercent: 0, discountAmount: 0,
    walletAmountUsed: 0, finalAmount: 10000,
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

describe('ReservationManagementComponent', () => {
  let component: ReservationManagementComponent;
  let fixture: ComponentFixture<ReservationManagementComponent>;
  let bookingSpy: jasmine.SpyObj<BookingService>;
  let toastSpy: jasmine.SpyObj<ToastService>;
  let dialog: MatDialog;

  beforeEach(async () => {
    bookingSpy = jasmine.createSpyObj('BookingService', [
      'getHotelReservations', 'completeReservation', 'confirmReservation'
    ]);
    toastSpy = jasmine.createSpyObj('ToastService', ['success', 'error']);

    bookingSpy.getHotelReservations.and.returnValue(of(MOCK_PAGED));
    bookingSpy.completeReservation.and.returnValue(of(undefined));
    bookingSpy.confirmReservation.and.returnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [ReservationManagementComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]),
        { provide: BookingService, useValue: bookingSpy },
        { provide: ToastService,   useValue: toastSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ReservationManagementComponent);
    component = fixture.componentInstance;
    dialog = TestBed.inject(MatDialog);
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

  // ── ngOnInit / load ───────────────────────────────────────────────────────

  it('ngOnInit — should call getHotelReservations', () => {
    expect(bookingSpy.getHotelReservations).toHaveBeenCalledWith(1, 10, 'All', '', '', '');
  });

  it('load — should populate reservations signal', () => {
    expect(component.reservations().length).toBe(6);
  });

  it('load — should set totalCount signal', () => {
    expect(component.totalCount()).toBe(6);
  });

  it('load — should set loading to false after success', () => {
    expect(component.loading()).toBeFalse();
  });

  it('load — should set loading to false on error', () => {
    bookingSpy.getHotelReservations.and.returnValue(throwError(() => new Error('fail')));
    component.load();
    expect(component.loading()).toBeFalse();
  });

  // ── onTabChange ───────────────────────────────────────────────────────────

  it('onTabChange — should set selectedStatus from statusTabs', () => {
    component.onTabChange(2); // 'Confirmed'
    expect(component.selectedStatus).toBe('Confirmed');
  });

  it('onTabChange — should reset currentPage to 1', () => {
    component.currentPage = 3;
    component.onTabChange(1);
    expect(component.currentPage).toBe(1);
  });

  it('onTabChange — should reload', () => {
    bookingSpy.getHotelReservations.calls.reset();
    component.onTabChange(1);
    expect(bookingSpy.getHotelReservations).toHaveBeenCalled();
  });

  // ── onPage ────────────────────────────────────────────────────────────────

  it('onPage — should update currentPage and reload', () => {
    bookingSpy.getHotelReservations.calls.reset();
    component.onPage({ pageIndex: 1, pageSize: 10, length: 20 } as any);
    expect(component.currentPage).toBe(2);
    expect(bookingSpy.getHotelReservations).toHaveBeenCalled();
  });

  it('onPage — should update pageSize', () => {
    component.onPage({ pageIndex: 0, pageSize: 20, length: 40 } as any);
    expect(component.pageSize).toBe(20);
  });

  // ── onSort ────────────────────────────────────────────────────────────────

  it('onSort — should set sortField and sortDir to asc on first call', () => {
    component.onSort('checkIn');
    expect(component.sortField).toBe('checkIn');
    expect(component.sortDir).toBe('asc');
  });

  it('onSort — should toggle sortDir to desc on second call with same field', () => {
    component.onSort('checkIn');
    component.onSort('checkIn');
    expect(component.sortDir).toBe('desc');
  });

  it('onSort — should reset sortDir to asc when switching fields', () => {
    component.onSort('checkIn');
    component.onSort('amount');
    expect(component.sortField).toBe('amount');
    expect(component.sortDir).toBe('asc');
  });

  // ── complete ──────────────────────────────────────────────────────────────

  it('complete — should open confirm dialog', fakeAsync(async () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of(true) } as any);
    await component.complete('RES-0001');
    expect(MatDialog.prototype.open).toHaveBeenCalled();
  }));

  it('complete — should call completeReservation when confirmed', fakeAsync(async () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of(true) } as any);
    await component.complete('RES-0001');
    expect(bookingSpy.completeReservation).toHaveBeenCalledWith('RES-0001');
  }));

  it('complete — should show success toast', fakeAsync(async () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of(true) } as any);
    await component.complete('RES-0001');
    expect(toastSpy.success).toHaveBeenCalledWith('Reservation marked as completed.');
  }));

  it('complete — should NOT call service when dialog cancelled', fakeAsync(async () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of(false) } as any);
    await component.complete('RES-0001');
    expect(bookingSpy.completeReservation).not.toHaveBeenCalled();
  }));

  // ── confirm ───────────────────────────────────────────────────────────────

  it('confirm — should open confirm dialog', fakeAsync(async () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of(true) } as any);
    await component.confirm('RES-0002');
    expect(MatDialog.prototype.open).toHaveBeenCalled();
  }));

  it('confirm — should call confirmReservation when confirmed', fakeAsync(async () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of(true) } as any);
    await component.confirm('RES-0002');
    expect(bookingSpy.confirmReservation).toHaveBeenCalledWith('RES-0002');
  }));

  it('confirm — should show success toast', fakeAsync(async () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of(true) } as any);
    await component.confirm('RES-0002');
    expect(toastSpy.success).toHaveBeenCalledWith('Reservation confirmed.');
  }));

  it('confirm — should NOT call service when dialog cancelled', fakeAsync(async () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of(false) } as any);
    await component.confirm('RES-0002');
    expect(bookingSpy.confirmReservation).not.toHaveBeenCalled();
  }));

  // ── statusClass ───────────────────────────────────────────────────────────

  it('statusClass — Pending → badge-warning', () => expect(component.statusClass('Pending')).toBe('badge-warning'));
  it('statusClass — Confirmed → badge-success', () => expect(component.statusClass('Confirmed')).toBe('badge-success'));
  it('statusClass — Completed → badge-primary', () => expect(component.statusClass('Completed')).toBe('badge-primary'));
  it('statusClass — Cancelled → badge-error', () => expect(component.statusClass('Cancelled')).toBe('badge-error'));
  it('statusClass — NoShow → badge-muted', () => expect(component.statusClass('NoShow')).toBe('badge-muted'));
  it('statusClass — unknown → badge-muted', () => expect(component.statusClass('Unknown')).toBe('badge-muted'));

  // ── statusEmoji ───────────────────────────────────────────────────────────

  it('statusEmoji — Pending → ⏳', () => expect(component.statusEmoji('Pending')).toBe('⏳'));
  it('statusEmoji — Confirmed → ✅', () => expect(component.statusEmoji('Confirmed')).toBe('✅'));
  it('statusEmoji — Completed → 🏆', () => expect(component.statusEmoji('Completed')).toBe('🏆'));
  it('statusEmoji — Cancelled → ❌', () => expect(component.statusEmoji('Cancelled')).toBe('❌'));
  it('statusEmoji — NoShow → 👻', () => expect(component.statusEmoji('NoShow')).toBe('👻'));
  it('statusEmoji — unknown → empty string', () => expect(component.statusEmoji('Unknown')).toBe(''));

  // ── onSearch debounce ─────────────────────────────────────────────────────

  it('onSearch — should update searchTerm after debounce', fakeAsync(() => {
    bookingSpy.getHotelReservations.calls.reset();
    component.onSearch({ target: { value: 'Grand' } } as any);
    tick(400);
    expect(component.searchTerm).toBe('Grand');
    expect(bookingSpy.getHotelReservations).toHaveBeenCalled();
  }));
});
