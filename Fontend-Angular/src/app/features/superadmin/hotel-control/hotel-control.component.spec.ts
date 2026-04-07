import { ComponentFixture, TestBed, fakeAsync, tick, flushMicrotasks } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HotelControlComponent } from './hotel-control.component';
import { HotelService } from '../../../core/services/hotel.service';
import { ToastService } from '../../../core/services/toast.service';
import { MatDialog } from '@angular/material/dialog';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

function makeHotel(id: string, isActive: boolean, isBlocked: boolean) {
  return {
    hotelId: id, name: `Hotel ${id}`, city: 'Chennai', state: 'TN',
    isActive, isBlockedBySuperAdmin: isBlocked,
    totalReservations: 10, totalRevenue: 50000,
    contactNumber: '9840650390', createdAt: '2025-01-01T00:00:00Z'
  };
}

const MOCK_HOTELS = [
  makeHotel('h-001', true,  false),
  makeHotel('h-002', true,  false),
  makeHotel('h-003', false, false),
  makeHotel('h-004', false, true),
  makeHotel('h-005', true,  true),
];
const MOCK_PAGED = { totalCount: 5, hotels: MOCK_HOTELS };

describe('HotelControlComponent', () => {
  let component: HotelControlComponent;
  let fixture: ComponentFixture<HotelControlComponent>;
  let hotelSpy: jasmine.SpyObj<HotelService>;
  let toastSpy: jasmine.SpyObj<ToastService>;
  let dialog: MatDialog;

  beforeEach(async () => {
    hotelSpy  = jasmine.createSpyObj('HotelService', ['getAllHotelsForSuperAdmin', 'blockHotel', 'unblockHotel']);
    toastSpy  = jasmine.createSpyObj('ToastService', ['success', 'error']);

    hotelSpy.getAllHotelsForSuperAdmin.and.returnValue(of(MOCK_PAGED as any));
    hotelSpy.blockHotel.and.returnValue(of(undefined));
    hotelSpy.unblockHotel.and.returnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [HotelControlComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]),
        { provide: HotelService,  useValue: hotelSpy },
        { provide: ToastService,  useValue: toastSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(HotelControlComponent);
    component = fixture.componentInstance;
    dialog = TestBed.inject(MatDialog);
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── Initial state ─────────────────────────────────────────────────────────

  it('pageSize — should be 10', () => expect(component.pageSize).toBe(10));
  it('currentPage — should start at 1', () => expect(component.currentPage).toBe(1));
  it('selectedStatus — should start as "All"', () => expect(component.selectedStatus).toBe('All'));
  it('statusTabs — should contain All, Active, Inactive, Blocked', () => {
    expect(component.statusTabs).toEqual(['All', 'Active', 'Inactive', 'Blocked']);
  });

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should call getAllHotelsForSuperAdmin', () => {
    expect(hotelSpy.getAllHotelsForSuperAdmin).toHaveBeenCalledWith(1, 10, undefined, 'All');
  });

  it('ngOnInit — should populate hotels signal', () => {
    expect(component.hotels().length).toBe(5);
  });

  it('ngOnInit — should set totalCount', () => {
    expect(component.totalCount()).toBe(5);
  });

  it('loading — should be false after load', () => {
    expect(component.loading()).toBeFalse();
  });

  it('load — should set loading to false on error', () => {
    hotelSpy.getAllHotelsForSuperAdmin.and.returnValue(throwError(() => new Error('fail')));
    component.load();
    expect(component.loading()).toBeFalse();
  });

  // ── onTabChange ───────────────────────────────────────────────────────────

  it('onTabChange — should set selectedStatus', () => {
    component.onTabChange(1); // 'Active'
    expect(component.selectedStatus).toBe('Active');
  });

  it('onTabChange — should reset currentPage to 1', () => {
    component.currentPage = 3;
    component.onTabChange(2);
    expect(component.currentPage).toBe(1);
  });

  it('onTabChange — should reload', () => {
    hotelSpy.getAllHotelsForSuperAdmin.calls.reset();
    component.onTabChange(3); // 'Blocked'
    expect(hotelSpy.getAllHotelsForSuperAdmin).toHaveBeenCalled();
  });

  // ── onPage ────────────────────────────────────────────────────────────────

  it('onPage — should update currentPage and reload', () => {
    hotelSpy.getAllHotelsForSuperAdmin.calls.reset();
    component.onPage({ pageIndex: 1, pageSize: 10, length: 20 } as any);
    expect(component.currentPage).toBe(2);
    expect(hotelSpy.getAllHotelsForSuperAdmin).toHaveBeenCalled();
  });

  // ── statusClass ───────────────────────────────────────────────────────────

  it('statusClass — blocked → badge-error', () => {
    expect(component.statusClass(MOCK_HOTELS[3] as any)).toBe('badge-error');
  });

  it('statusClass — active → badge-success', () => {
    expect(component.statusClass(MOCK_HOTELS[0] as any)).toBe('badge-success');
  });

  it('statusClass — inactive → badge-warning', () => {
    expect(component.statusClass(MOCK_HOTELS[2] as any)).toBe('badge-warning');
  });

  // ── statusLabel ───────────────────────────────────────────────────────────

  it('statusLabel — blocked → Blocked',   () => expect(component.statusLabel(MOCK_HOTELS[3] as any)).toBe('Blocked'));
  it('statusLabel — active → Active',     () => expect(component.statusLabel(MOCK_HOTELS[0] as any)).toBe('Active'));
  it('statusLabel — inactive → Inactive', () => expect(component.statusLabel(MOCK_HOTELS[2] as any)).toBe('Inactive'));

  // ── block ─────────────────────────────────────────────────────────────────

  it('block — should open confirm dialog', fakeAsync(async () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of(true) } as any);
    await component.block(MOCK_HOTELS[0] as any);
    flushMicrotasks();
    expect(MatDialog.prototype.open).toHaveBeenCalled();
  }));

  it('block — should call blockHotel when confirmed', fakeAsync(async () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of(true) } as any);
    await component.block(MOCK_HOTELS[0] as any);
    flushMicrotasks();
    expect(hotelSpy.blockHotel).toHaveBeenCalledWith('h-001');
  }));

  it('block — should NOT call blockHotel when cancelled', fakeAsync(async () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of(false) } as any);
    await component.block(MOCK_HOTELS[0] as any);
    flushMicrotasks();
    expect(hotelSpy.blockHotel).not.toHaveBeenCalled();
  }));

  // ── unblock ───────────────────────────────────────────────────────────────

  it('unblock — should call unblockHotel when confirmed', fakeAsync(async () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of(true) } as any);
    await component.unblock(MOCK_HOTELS[3] as any);
    flushMicrotasks();
    expect(hotelSpy.unblockHotel).toHaveBeenCalledWith('h-004');
  }));

  it('unblock — should NOT call unblockHotel when cancelled', fakeAsync(async () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of(false) } as any);
    await component.unblock(MOCK_HOTELS[3] as any);
    flushMicrotasks();
    expect(hotelSpy.unblockHotel).not.toHaveBeenCalled();
  }));

  // ── onSearch debounce ─────────────────────────────────────────────────────

  it('onSearch — should update searchTerm after debounce', fakeAsync(() => {
    hotelSpy.getAllHotelsForSuperAdmin.calls.reset();
    component.onSearch({ target: { value: 'Grand' } } as any);
    tick(400);
    expect(component.searchTerm).toBe('Grand');
    expect(hotelSpy.getAllHotelsForSuperAdmin).toHaveBeenCalled();
  }));
});
