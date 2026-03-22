import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { HotelManagementComponent } from './hotel-management.component';
import { HotelService } from '../../../core/services/hotel.service';
import { DashboardService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { AdminDashboardDto, HotelDetailsDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ── Mock data ──────────────────────────────────────────────────────────────────

const MOCK_DASHBOARD: AdminDashboardDto = {
  hotelId:               'hotel-001',
  hotelName:             'Grand Palace',
  isActive:              true,
  isBlockedBySuperAdmin: false,
  totalRooms:            20,
  activeRooms:           18,
  totalRoomTypes:        3,
  totalReservations:     120,
  pendingReservations:   5,
  activeReservations:    10,
  completedReservations: 100,
  cancelledReservations: 5,
  totalRevenue:          600000,
  totalReviews:          45,
  averageRating:         4.3,
  pendingRefundRequests: 2
};

const MOCK_HOTEL_DETAILS: HotelDetailsDto = {
  hotelId:       'hotel-001',
  name:          'Grand Palace',
  address:       '1 MG Road',
  city:          'Chennai',
  description:   'A luxury hotel in the heart of the city.',
  imageUrl:      'https://example.com/img.jpg',
  contactNumber: '9840650390',
  averageRating: 4.3,
  reviewCount:   45,
  amenities:     ['WiFi', 'Pool', 'Gym'],
  reviews:       [],
  roomTypes:     []
};

// ─────────────────────────────────────────────────────────────────────────────

describe('HotelManagementComponent', () => {
  let component: HotelManagementComponent;
  let fixture:   ComponentFixture<HotelManagementComponent>;

  let hotelSpy:     jasmine.SpyObj<HotelService>;
  let dashboardSpy: jasmine.SpyObj<DashboardService>;
  let toastSpy:     jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    hotelSpy     = jasmine.createSpyObj('HotelService',     ['getHotelDetails', 'updateHotel']);
    dashboardSpy = jasmine.createSpyObj('DashboardService', ['getAdminDashboard']);
    toastSpy     = jasmine.createSpyObj('ToastService',     ['success', 'error']);

    // Default happy-path responses
    dashboardSpy.getAdminDashboard.and.returnValue(of(MOCK_DASHBOARD));
    hotelSpy.getHotelDetails.and.returnValue(of(MOCK_HOTEL_DETAILS));

    await TestBed.configureTestingModule({
      imports: [HotelManagementComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: HotelService,     useValue: hotelSpy     },
        { provide: DashboardService, useValue: dashboardSpy },
        { provide: ToastService,     useValue: toastSpy     },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(HotelManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── INITIAL SIGNAL STATE ───────────────────────────────────────────────────

  it('isSaving — should start as false', () => {
    expect(component.isSaving()).toBeFalse();
  });

  // ── ngOnInit — API CALLS ───────────────────────────────────────────────────

  it('ngOnInit — should call getAdminDashboard on startup', () => {
    expect(dashboardSpy.getAdminDashboard).toHaveBeenCalledOnceWith();
  });

  it('ngOnInit — should call getHotelDetails with hotelId from dashboard', () => {
    expect(hotelSpy.getHotelDetails).toHaveBeenCalledOnceWith('hotel-001');
  });

  it('ngOnInit — should set isLoading to false after hotel details load', () => {
    expect(component.isLoading()).toBeFalse();
  });

  it('ngOnInit — should store dashboard data in dashboard signal', () => {
    expect(component.dashboard()).not.toBeNull();
    expect(component.dashboard()?.hotelName).toBe('Grand Palace');
  });

  // ── ngOnInit — FORM PRE-FILL ───────────────────────────────────────────────

  it('should pre-fill form name from hotel details', () => {
    expect(component.form.get('name')?.value).toBe('Grand Palace');
  });

  it('should pre-fill form address from hotel details', () => {
    expect(component.form.get('address')?.value).toBe('1 MG Road');
  });

  it('should pre-fill form city from hotel details', () => {
    expect(component.form.get('city')?.value).toBe('Chennai');
  });

  it('should pre-fill form description from hotel details', () => {
    expect(component.form.get('description')?.value)
      .toBe('A luxury hotel in the heart of the city.');
  });

  it('should pre-fill form contactNumber from hotel details', () => {
    expect(component.form.get('contactNumber')?.value).toBe('9840650390');
  });

  it('should pre-fill form imageUrl from hotel details', () => {
    expect(component.form.get('imageUrl')?.value)
      .toBe('https://example.com/img.jpg');
  });

  it('form — should be valid after pre-fill (all required fields populated)', () => {
    expect(component.form.valid).toBeTrue();
  });

  // ── FORM VALIDATION ────────────────────────────────────────────────────────

  it('form — should be invalid when name is cleared', () => {
    component.form.get('name')?.setValue('');
    expect(component.form.invalid).toBeTrue();
  });

  it('form — should be invalid when address is cleared', () => {
    component.form.get('address')?.setValue('');
    expect(component.form.invalid).toBeTrue();
  });

  it('form — should be invalid when city is cleared', () => {
    component.form.get('city')?.setValue('');
    expect(component.form.invalid).toBeTrue();
  });

  it('form — should be invalid when contactNumber is cleared', () => {
    component.form.get('contactNumber')?.setValue('');
    expect(component.form.invalid).toBeTrue();
  });

  it('form — description and imageUrl are optional (form still valid without them)', () => {
    component.form.patchValue({ description: '', imageUrl: '' });
    expect(component.form.valid).toBeTrue();
  });

  // ── save() — HAPPY PATH ────────────────────────────────────────────────────

  it('save() — should call updateHotel with form values', () => {
    hotelSpy.updateHotel.and.returnValue(of(undefined));

    component.save();

    expect(hotelSpy.updateHotel).toHaveBeenCalledOnceWith(
      jasmine.objectContaining({
        name:          'Grand Palace',
        address:       '1 MG Road',
        city:          'Chennai',
        contactNumber: '9840650390',
      })
    );
  });

  it('save() — should show success toast on successful update', () => {
    hotelSpy.updateHotel.and.returnValue(of(undefined));

    component.save();

    expect(toastSpy.success)
      .toHaveBeenCalledOnceWith('Hotel updated successfully.');
  });

it('save() — should set isSaving to true during request', () => {
  const subject = new Subject<void>();  // ← now fully typed, no require()
  hotelSpy.updateHotel.and.returnValue(subject.asObservable());

  component.save();

  expect(component.isSaving()).toBeTrue();

  subject.next();
  subject.complete();
});

  it('save() — should reset isSaving to false on success', () => {
    hotelSpy.updateHotel.and.returnValue(of(undefined));

    component.save();

    expect(component.isSaving()).toBeFalse();
  });

  // ── save() — INVALID FORM ──────────────────────────────────────────────────

  it('save() — should NOT call updateHotel when form is invalid', () => {
    component.form.get('name')?.setValue('');

    component.save();

    expect(hotelSpy.updateHotel).not.toHaveBeenCalled();
  });

  it('save() — should mark all fields as touched when form is invalid', () => {
    component.form.get('name')?.setValue('');

    component.save();

    expect(component.form.get('name')?.touched).toBeTrue();
    expect(component.form.get('address')?.touched).toBeTrue();
    expect(component.form.get('city')?.touched).toBeTrue();
    expect(component.form.get('contactNumber')?.touched).toBeTrue();
  });

  it('save() — should NOT show success toast when form is invalid', () => {
    component.form.get('name')?.setValue('');

    component.save();

    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  // ── save() — ERROR ─────────────────────────────────────────────────────────

  it('save() — should reset isSaving to false on API error', () => {
    hotelSpy.updateHotel.and.returnValue(
      throwError(() => new Error('Server error'))
    );

    component.save();

    expect(component.isSaving()).toBeFalse();
  });

  it('save() — should NOT show success toast on API error', () => {
    hotelSpy.updateHotel.and.returnValue(
      throwError(() => new Error('Server error'))
    );

    component.save();

    expect(toastSpy.success).not.toHaveBeenCalled();
  });
});