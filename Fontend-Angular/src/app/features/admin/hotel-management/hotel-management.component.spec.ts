import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HotelManagementComponent } from './hotel-management.component';
import { HotelService } from '../../../core/services/hotel.service';
import { DashboardService, RoomTypeService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { AdminDashboardDto, HotelDetailsDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

const MOCK_DASHBOARD: AdminDashboardDto = {
  hotelId: 'hotel-001', hotelName: 'Grand Palace', isActive: true,
  isBlockedBySuperAdmin: false, totalRooms: 20, activeRooms: 18, totalRoomTypes: 3,
  totalReservations: 120, pendingReservations: 5, activeReservations: 10,
  completedReservations: 100, cancelledReservations: 5, totalRevenue: 600000,
  totalReviews: 45, averageRating: 4.3
};

const MOCK_HOTEL: HotelDetailsDto = {
  hotelId: 'hotel-001', name: 'Grand Palace', address: '1 MG Road',
  city: 'Chennai', state: 'TN', description: 'Luxury hotel.',
  imageUrl: 'https://example.com/img.jpg', contactNumber: '9840650390',
  upiId: 'hotel@upi', averageRating: 4.3, reviewCount: 45, gstPercent: 18,
  amenities: ['WiFi'], reviews: [], roomTypes: []
};

describe('HotelManagementComponent', () => {
  let component: HotelManagementComponent;
  let fixture: ComponentFixture<HotelManagementComponent>;
  let hotelSpy: jasmine.SpyObj<HotelService>;
  let dashboardSpy: jasmine.SpyObj<DashboardService>;
  let roomTypeSpy: jasmine.SpyObj<RoomTypeService>;
  let toastSpy: jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    hotelSpy     = jasmine.createSpyObj('HotelService',     ['getHotelDetails', 'updateHotel']);
    dashboardSpy = jasmine.createSpyObj('DashboardService', ['getAdminDashboard']);
    roomTypeSpy  = jasmine.createSpyObj('RoomTypeService',  ['updateHotelGst']);
    toastSpy     = jasmine.createSpyObj('ToastService',     ['success', 'error']);

    dashboardSpy.getAdminDashboard.and.returnValue(of(MOCK_DASHBOARD));
    hotelSpy.getHotelDetails.and.returnValue(of(MOCK_HOTEL));

    await TestBed.configureTestingModule({
      imports: [HotelManagementComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        { provide: HotelService,     useValue: hotelSpy },
        { provide: DashboardService, useValue: dashboardSpy },
        { provide: RoomTypeService,  useValue: roomTypeSpy },
        { provide: ToastService,     useValue: toastSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(HotelManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── Creation ──────────────────────────────────────────────────────────────

  it('should create', () => expect(component).toBeTruthy());

  // ── Initial state ─────────────────────────────────────────────────────────

  it('isSaving — should start as false', () => expect(component.isSaving()).toBeFalse());
  it('isSavingGst — should start as false', () => expect(component.isSavingGst()).toBeFalse());

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should call getAdminDashboard', () => {
    expect(dashboardSpy.getAdminDashboard).toHaveBeenCalledOnceWith();
  });

  it('ngOnInit — should call getHotelDetails with hotelId', () => {
    expect(hotelSpy.getHotelDetails).toHaveBeenCalledOnceWith('hotel-001');
  });

  it('ngOnInit — should set isLoading to false after load', () => {
    expect(component.isLoading()).toBeFalse();
  });

  it('ngOnInit — should store dashboard in signal', () => {
    expect(component.dashboard()?.hotelName).toBe('Grand Palace');
  });

  // ── Form pre-fill ─────────────────────────────────────────────────────────

  it('should pre-fill form name', () => {
    expect(component.form.get('name')?.value).toBe('Grand Palace');
  });

  it('should pre-fill form address', () => {
    expect(component.form.get('address')?.value).toBe('1 MG Road');
  });

  it('should pre-fill form contactNumber', () => {
    expect(component.form.get('contactNumber')?.value).toBe('9840650390');
  });

  it('should pre-fill cityControl', () => {
    expect(component.cityControl.value).toBe('Chennai');
  });

  it('should pre-fill stateControl', () => {
    expect(component.stateControl.value).toBe('TN');
  });

  it('should pre-fill gstForm gstPercent', () => {
    expect(component.gstForm.get('gstPercent')?.value).toBe(18);
  });

  // ── Form validation ───────────────────────────────────────────────────────

  it('form — should be invalid when name is cleared', () => {
    component.form.get('name')?.setValue('');
    expect(component.form.invalid).toBeTrue();
  });

  it('form — should be invalid when contactNumber is cleared', () => {
    component.form.get('contactNumber')?.setValue('');
    expect(component.form.invalid).toBeTrue();
  });

  it('form — should be invalid when contactNumber is not 10 digits', () => {
    component.form.get('contactNumber')?.setValue('12345');
    expect(component.form.get('contactNumber')?.invalid).toBeTrue();
  });

  it('gstForm — should be invalid when gstPercent > 28', () => {
    component.gstForm.get('gstPercent')?.setValue(30);
    expect(component.gstForm.invalid).toBeTrue();
  });

  it('gstForm — should be invalid when gstPercent < 0', () => {
    component.gstForm.get('gstPercent')?.setValue(-1);
    expect(component.gstForm.invalid).toBeTrue();
  });

  // ── save() ────────────────────────────────────────────────────────────────

  it('save — should call updateHotel with form values', () => {
    hotelSpy.updateHotel.and.returnValue(of(undefined));
    component.save();
    expect(hotelSpy.updateHotel).toHaveBeenCalledOnceWith(
      jasmine.objectContaining({ name: 'Grand Palace', contactNumber: '9840650390' })
    );
  });

  it('save — should show success toast', () => {
    hotelSpy.updateHotel.and.returnValue(of(undefined));
    component.save();
    expect(toastSpy.success).toHaveBeenCalledOnceWith('Hotel updated successfully.');
  });

  it('save — should reset isSaving to false on success', () => {
    hotelSpy.updateHotel.and.returnValue(of(undefined));
    component.save();
    expect(component.isSaving()).toBeFalse();
  });

  it('save — should NOT call updateHotel when form is invalid', () => {
    component.form.get('name')?.setValue('');
    component.save();
    expect(hotelSpy.updateHotel).not.toHaveBeenCalled();
  });

  it('save — should mark all fields touched when form is invalid', () => {
    component.form.get('name')?.setValue('');
    component.save();
    expect(component.form.get('name')?.touched).toBeTrue();
  });

  it('save — should reset isSaving to false on error', () => {
    hotelSpy.updateHotel.and.returnValue(throwError(() => new Error('fail')));
    component.save();
    expect(component.isSaving()).toBeFalse();
  });

  // ── saveGst() ─────────────────────────────────────────────────────────────

  it('saveGst — should call updateHotelGst with gstPercent', () => {
    roomTypeSpy.updateHotelGst.and.returnValue(of(undefined));
    component.gstForm.patchValue({ gstPercent: 12 });
    component.saveGst();
    expect(roomTypeSpy.updateHotelGst).toHaveBeenCalledOnceWith(12);
  });

  it('saveGst — should show success toast', () => {
    roomTypeSpy.updateHotelGst.and.returnValue(of(undefined));
    component.saveGst();
    expect(toastSpy.success).toHaveBeenCalledOnceWith('GST updated!');
  });

  it('saveGst — should NOT call updateHotelGst when gstForm is invalid', () => {
    component.gstForm.get('gstPercent')?.setValue(30);
    component.saveGst();
    expect(roomTypeSpy.updateHotelGst).not.toHaveBeenCalled();
  });

  it('saveGst — should reset isSavingGst to false on success', () => {
    roomTypeSpy.updateHotelGst.and.returnValue(of(undefined));
    component.saveGst();
    expect(component.isSavingGst()).toBeFalse();
  });

  it('saveGst — should reset isSavingGst to false on error', () => {
    roomTypeSpy.updateHotelGst.and.returnValue(throwError(() => new Error('fail')));
    component.saveGst();
    expect(component.isSavingGst()).toBeFalse();
  });
});
