import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { HotelControlComponent } from './hotel-control.component';
import { HotelService } from '../../../core/services/hotel.service';
import { ToastService } from '../../../core/services/toast.service';
import { SuperAdminHotelListDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ── Mock data ──────────────────────────────────────────────────────────────────

function makeHotel(id: string, name: string, isActive: boolean, isBlocked: boolean): SuperAdminHotelListDto {
  return {
    hotelId:               id,
    name,
    city:                  'Chennai',
    contactNumber:         '9840650390',
    isActive,
    isBlockedBySuperAdmin: isBlocked,
    createdAt:             '2024-01-01T00:00:00Z',
    totalReservations:     100,
    totalRevenue:          500000,
  };
}

const HOTEL_ACTIVE_1  = makeHotel('hotel-001', 'Grand Palace',   true,  false);
const HOTEL_ACTIVE_2  = makeHotel('hotel-002', 'Sea View Inn',   true,  false);
const HOTEL_INACTIVE  = makeHotel('hotel-003', 'City Lights',    false, false);
const HOTEL_BLOCKED   = makeHotel('hotel-004', 'Blocked Hotel',  false, true);
const HOTEL_BLOCKED_2 = makeHotel('hotel-005', 'Blocked Hotel 2',false, true);

const ALL_HOTELS = [HOTEL_ACTIVE_1, HOTEL_ACTIVE_2, HOTEL_INACTIVE, HOTEL_BLOCKED, HOTEL_BLOCKED_2];

// ─────────────────────────────────────────────────────────────────────────────

describe('HotelControlComponent', () => {
  let component: HotelControlComponent;
  let fixture:   ComponentFixture<HotelControlComponent>;

  let hotelSpy: jasmine.SpyObj<HotelService>;
  let toastSpy: jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    hotelSpy = jasmine.createSpyObj('HotelService', [
      'getAllHotelsForSuperAdmin', 'blockHotel', 'unblockHotel'
    ]);
    toastSpy = jasmine.createSpyObj('ToastService', ['success', 'error']);

    hotelSpy.getAllHotelsForSuperAdmin.and.returnValue(of(ALL_HOTELS));
    hotelSpy.blockHotel.and.returnValue(of(undefined));
    hotelSpy.unblockHotel.and.returnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [HotelControlComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: HotelService, useValue: hotelSpy },
        { provide: ToastService, useValue: toastSpy },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(HotelControlComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── INITIAL SIGNAL STATE ───────────────────────────────────────────────────

  it('filterMode — should start as "all"', () => {
    expect(component.filterMode()).toBe('all');
  });

  // ── ngOnInit ───────────────────────────────────────────────────────────────

  it('ngOnInit — should call getAllHotelsForSuperAdmin on startup', () => {
    expect(hotelSpy.getAllHotelsForSuperAdmin).toHaveBeenCalledOnceWith();
  });

  it('ngOnInit — should populate hotels signal with all returned hotels', () => {
    expect(component.hotels().length).toBe(5);
  });

  it('ngOnInit — should store correct hotel names', () => {
    const names = component.hotels().map(h => h.name);
    expect(names).toContain('Grand Palace');
    expect(names).toContain('Blocked Hotel');
  });

  // ── filtered GETTER ────────────────────────────────────────────────────────

  it('filtered — should return all 5 hotels when filterMode is "all"', () => {
    component.filterMode.set('all');
    expect(component.filtered.length).toBe(5);
  });

  it('filtered — should return only active non-blocked hotels when filterMode is "active"', () => {
    component.filterMode.set('active');
    expect(component.filtered.length).toBe(2);
    expect(component.filtered.every(h => h.isActive && !h.isBlockedBySuperAdmin)).toBeTrue();
  });

  it('filtered — active filter should NOT include inactive hotels', () => {
    component.filterMode.set('active');
    const names = component.filtered.map(h => h.name);
    expect(names).not.toContain('City Lights');   // inactive, not blocked
    expect(names).not.toContain('Blocked Hotel'); // blocked
  });

  it('filtered — should return only blocked hotels when filterMode is "blocked"', () => {
    component.filterMode.set('blocked');
    expect(component.filtered.length).toBe(2);
    expect(component.filtered.every(h => h.isBlockedBySuperAdmin)).toBeTrue();
  });

  it('filtered — blocked filter should contain correct hotel names', () => {
    component.filterMode.set('blocked');
    const names = component.filtered.map(h => h.name);
    expect(names).toContain('Blocked Hotel');
    expect(names).toContain('Blocked Hotel 2');
  });

  it('filtered — should react to filterMode signal changes', () => {
    component.filterMode.set('active');
    expect(component.filtered.length).toBe(2);

    component.filterMode.set('blocked');
    expect(component.filtered.length).toBe(2);

    component.filterMode.set('all');
    expect(component.filtered.length).toBe(5);
  });

  it('filtered — should return empty when no hotels match filter', () => {
    component.hotels.set([HOTEL_ACTIVE_1, HOTEL_ACTIVE_2]); // no blocked hotels
    component.filterMode.set('blocked');
    expect(component.filtered.length).toBe(0);
  });

  // ── block() — HAPPY PATH ───────────────────────────────────────────────────

  it('block() — should call blockHotel with the hotel ID', () => {
    spyOn(window, 'confirm').and.returnValue(true);

    component.block(HOTEL_ACTIVE_1);

    expect(hotelSpy.blockHotel).toHaveBeenCalledOnceWith('hotel-001');
  });

  it('block() — should show success toast with hotel name', () => {
    spyOn(window, 'confirm').and.returnValue(true);

    component.block(HOTEL_ACTIVE_1);

    expect(toastSpy.success).toHaveBeenCalledOnceWith('Grand Palace blocked.');
  });

  it('block() — should set isBlockedBySuperAdmin to true in hotels signal', () => {
    spyOn(window, 'confirm').and.returnValue(true);

    component.block(HOTEL_ACTIVE_1);

    const updated = component.hotels().find(h => h.hotelId === 'hotel-001');
    expect(updated?.isBlockedBySuperAdmin).toBeTrue();
  });

  it('block() — should set isActive to false in hotels signal', () => {
    spyOn(window, 'confirm').and.returnValue(true);

    component.block(HOTEL_ACTIVE_1);

    const updated = component.hotels().find(h => h.hotelId === 'hotel-001');
    expect(updated?.isActive).toBeFalse();
  });

  it('block() — should NOT change other hotels in the signal', () => {
    spyOn(window, 'confirm').and.returnValue(true);

    component.block(HOTEL_ACTIVE_1);

    const other = component.hotels().find(h => h.hotelId === 'hotel-002');
    expect(other?.isBlockedBySuperAdmin).toBe(HOTEL_ACTIVE_2.isBlockedBySuperAdmin);
    expect(other?.isActive).toBe(HOTEL_ACTIVE_2.isActive);
  });

  // ── block() — CONFIRM CANCELLED ────────────────────────────────────────────

  it('block() — should NOT call blockHotel when confirm is cancelled', () => {
    spyOn(window, 'confirm').and.returnValue(false);

    component.block(HOTEL_ACTIVE_1);

    expect(hotelSpy.blockHotel).not.toHaveBeenCalled();
  });

  it('block() — should NOT show toast when confirm is cancelled', () => {
    spyOn(window, 'confirm').and.returnValue(false);

    component.block(HOTEL_ACTIVE_1);

    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  it('block() — should NOT update hotels signal when confirm is cancelled', () => {
    spyOn(window, 'confirm').and.returnValue(false);
    const originalBlocked = component.hotels()
      .find(h => h.hotelId === 'hotel-001')?.isBlockedBySuperAdmin;

    component.block(HOTEL_ACTIVE_1);

    const afterBlocked = component.hotels()
      .find(h => h.hotelId === 'hotel-001')?.isBlockedBySuperAdmin;
    expect(afterBlocked).toBe(originalBlocked);
  });

  // ── unblock() — HAPPY PATH ─────────────────────────────────────────────────

  it('unblock() — should call unblockHotel with the hotel ID', () => {
    component.unblock(HOTEL_BLOCKED);

    expect(hotelSpy.unblockHotel).toHaveBeenCalledOnceWith('hotel-004');
  });

  it('unblock() — should show success toast with hotel name', () => {
    component.unblock(HOTEL_BLOCKED);

    expect(toastSpy.success).toHaveBeenCalledOnceWith('Blocked Hotel unblocked.');
  });

  it('unblock() — should set isBlockedBySuperAdmin to false in hotels signal', () => {
    component.unblock(HOTEL_BLOCKED);

    const updated = component.hotels().find(h => h.hotelId === 'hotel-004');
    expect(updated?.isBlockedBySuperAdmin).toBeFalse();
  });

  it('unblock() — should NOT change isActive when unblocking', () => {
    // Unblocking only removes the block — admin must re-activate separately
    component.unblock(HOTEL_BLOCKED);

    const updated = component.hotels().find(h => h.hotelId === 'hotel-004');
    expect(updated?.isActive).toBe(HOTEL_BLOCKED.isActive); // unchanged
  });

  it('unblock() — should NOT require confirm dialog', () => {
    const confirmSpy = spyOn(window, 'confirm');

    component.unblock(HOTEL_BLOCKED);

    expect(confirmSpy).not.toHaveBeenCalled();
  });

  it('unblock() — should NOT change other hotels in the signal', () => {
    component.unblock(HOTEL_BLOCKED);

    const other = component.hotels().find(h => h.hotelId === 'hotel-005');
    expect(other?.isBlockedBySuperAdmin).toBe(HOTEL_BLOCKED_2.isBlockedBySuperAdmin);
  });

  // ── FILTER + SIGNAL INTERACTION ────────────────────────────────────────────

  it('blocked filter count should decrease after unblocking a hotel', () => {
    component.filterMode.set('blocked');
    expect(component.filtered.length).toBe(2);

    component.unblock(HOTEL_BLOCKED);

    expect(component.filtered.length).toBe(1);
  });

  it('blocked filter count should increase after blocking an active hotel', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    component.filterMode.set('blocked');
    expect(component.filtered.length).toBe(2);

    component.block(HOTEL_ACTIVE_1);

    expect(component.filtered.length).toBe(3);
  });
});