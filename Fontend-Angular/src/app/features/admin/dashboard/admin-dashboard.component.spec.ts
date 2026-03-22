import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AdminDashboardComponent } from './admin-dashboard.component';
import { DashboardService, UserService } from '../../../core/services/api.services';
import { HotelService } from '../../../core/services/hotel.service';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/services/auth.service';
import { AdminDashboardDto, UserProfileResponseDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ── Mock data ──────────────────────────────────────────────────────────────────

const MOCK_DASHBOARD: AdminDashboardDto = {
  hotelId: 'hotel-001',
  hotelName: 'Grand Palace',
  isActive: true,
  isBlockedBySuperAdmin: false,
  totalRooms: 20,
  activeRooms: 18,
  totalRoomTypes: 3,
  totalReservations: 120,
  pendingReservations: 5,
  activeReservations: 10,
  completedReservations: 100,
  cancelledReservations: 5,
  totalRevenue: 600000,
  totalReviews: 45,
  averageRating: 4.3,
  pendingRefundRequests: 2
};

const MOCK_PROFILE: UserProfileResponseDto = {
  userId: 'usr-001',
  email: 'admin@grandpalace.com',
  role: 'Admin',
  name: 'Thanush K',
  phoneNumber: '9840650390',
  address: '1 MG Road',
  state: 'Tamil Nadu',
  city: 'Chennai',
  pincode: '600001',
  // profileImageUrl omitted — field is optional (string | undefined), not nullable
  createdAt: '2024-01-01T00:00:00Z'
};

// ─────────────────────────────────────────────────────────────────────────────

describe('AdminDashboardComponent', () => {
  let component: AdminDashboardComponent;
  let fixture: ComponentFixture<AdminDashboardComponent>;

  let dashboardSpy: jasmine.SpyObj<DashboardService>;
  let hotelSpy:     jasmine.SpyObj<HotelService>;
  let userSpy:      jasmine.SpyObj<UserService>;
  let toastSpy:     jasmine.SpyObj<ToastService>;
  let authSpy:      jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    dashboardSpy = jasmine.createSpyObj('DashboardService', ['getAdminDashboard']);
    hotelSpy     = jasmine.createSpyObj('HotelService',     ['toggleHotelStatus']);
    userSpy      = jasmine.createSpyObj('UserService',      ['getProfile']);
    toastSpy     = jasmine.createSpyObj('ToastService',     ['success', 'error']);
    authSpy      = jasmine.createSpyObj('AuthService',      ['isAuthenticated'], {
      currentUser: () => ({ userName: 'Thanush K', role: 'Admin' })
    });

    // Default responses
    dashboardSpy.getAdminDashboard.and.returnValue(of(MOCK_DASHBOARD));
    userSpy.getProfile.and.returnValue(of(MOCK_PROFILE));

    await TestBed.configureTestingModule({
      imports: [AdminDashboardComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: DashboardService, useValue: dashboardSpy },
        { provide: HotelService,     useValue: hotelSpy     },
        { provide: UserService,      useValue: userSpy      },
        { provide: ToastService,     useValue: toastSpy     },
        { provide: AuthService,      useValue: authSpy      },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(AdminDashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── ngOnInit ───────────────────────────────────────────────────────────────

  it('ngOnInit — should call getAdminDashboard on startup', () => {
    expect(dashboardSpy.getAdminDashboard).toHaveBeenCalledOnceWith();
  });

  it('ngOnInit — should call getProfile on startup', () => {
    expect(userSpy.getProfile).toHaveBeenCalledOnceWith();
  });

  it('ngOnInit — should populate data signal with dashboard response', () => {
    expect(component.data()).not.toBeNull();
    expect(component.data()?.hotelName).toBe('Grand Palace');
    expect(component.data()?.totalRevenue).toBe(600000);
    expect(component.data()?.averageRating).toBe(4.3);
  });

  it('ngOnInit — should populate profile signal with user profile', () => {
    expect(component.profile()).not.toBeNull();
    expect(component.profile()?.name).toBe('Thanush K');
    expect(component.profile()?.role).toBe('Admin');
  });

  // ── INITIAL SIGNAL STATE ───────────────────────────────────────────────────

  it('isTogglingStatus — should start as false', () => {
    expect(component.isTogglingStatus()).toBeFalse();
  });

  // ── toggleHotelStatus() — ACTIVATE ────────────────────────────────────────

  it('toggleHotelStatus() — should call toggleHotelStatus(false) when hotel is currently active', () => {
    hotelSpy.toggleHotelStatus.and.returnValue(of(undefined));

    // data.isActive = true → toggling should deactivate (pass false)
    component.toggleHotelStatus();

    expect(hotelSpy.toggleHotelStatus).toHaveBeenCalledOnceWith(false);
  });

  it('toggleHotelStatus() — should call toggleHotelStatus(true) when hotel is inactive', () => {
    hotelSpy.toggleHotelStatus.and.returnValue(of(undefined));
    component.data.set({ ...MOCK_DASHBOARD, isActive: false });

    component.toggleHotelStatus();

    expect(hotelSpy.toggleHotelStatus).toHaveBeenCalledOnceWith(true);
  });

  it('toggleHotelStatus() — should update data.isActive to false after deactivation', () => {
    hotelSpy.toggleHotelStatus.and.returnValue(of(undefined));

    component.toggleHotelStatus(); // isActive was true → becomes false

    expect(component.data()?.isActive).toBeFalse();
  });

  it('toggleHotelStatus() — should update data.isActive to true after activation', () => {
    hotelSpy.toggleHotelStatus.and.returnValue(of(undefined));
    component.data.set({ ...MOCK_DASHBOARD, isActive: false });

    component.toggleHotelStatus();

    expect(component.data()?.isActive).toBeTrue();
  });

  it('toggleHotelStatus() — should show success toast "Hotel is now live." when activating', () => {
    hotelSpy.toggleHotelStatus.and.returnValue(of(undefined));
    component.data.set({ ...MOCK_DASHBOARD, isActive: false });

    component.toggleHotelStatus();

    expect(toastSpy.success).toHaveBeenCalledOnceWith('Hotel is now live.');
  });

  it('toggleHotelStatus() — should show success toast "Hotel deactivated." when deactivating', () => {
    hotelSpy.toggleHotelStatus.and.returnValue(of(undefined));

    component.toggleHotelStatus(); // isActive was true

    expect(toastSpy.success).toHaveBeenCalledOnceWith('Hotel deactivated.');
  });

  it('toggleHotelStatus() — should reset isTogglingStatus to false on success', () => {
    hotelSpy.toggleHotelStatus.and.returnValue(of(undefined));

    component.toggleHotelStatus();

    expect(component.isTogglingStatus()).toBeFalse();
  });

  // ── toggleHotelStatus() — ERROR ────────────────────────────────────────────

  it('toggleHotelStatus() — should reset isTogglingStatus to false on error', () => {
    hotelSpy.toggleHotelStatus.and.returnValue(throwError(() => new Error('Server error')));

    component.toggleHotelStatus();

    expect(component.isTogglingStatus()).toBeFalse();
  });

  it('toggleHotelStatus() — should NOT update data.isActive on error', () => {
    hotelSpy.toggleHotelStatus.and.returnValue(throwError(() => new Error('Server error')));

    component.toggleHotelStatus(); // started as isActive: true

    // isActive should remain true since request failed
    expect(component.data()?.isActive).toBeTrue();
  });

  it('toggleHotelStatus() — should NOT show success toast on error', () => {
    hotelSpy.toggleHotelStatus.and.returnValue(throwError(() => new Error('Server error')));

    component.toggleHotelStatus();

    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  // ── toggleHotelStatus() — GUARD (no data) ─────────────────────────────────

  it('toggleHotelStatus() — should do nothing when data signal is null', () => {
    component.data.set(null);

    component.toggleHotelStatus();

    expect(hotelSpy.toggleHotelStatus).not.toHaveBeenCalled();
  });

  // ── TEMPLATE RENDERS ───────────────────────────────────────────────────────

  it('should render hotel name in the template', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Grand Palace');
  });

  it('should render total revenue in the template', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    // DecimalPipe formats 600000 as "600,000"
    expect(el.textContent).toContain('600,000');
  });

  it('should show loading state before data arrives', async () => {
    // Reset data to null to simulate loading
    component.data.set(null);
    fixture.detectChanges();
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    // Table/stat cards should not be visible
    expect(el.querySelector('.stat-card') ?? el.querySelector('.stats-grid')).toBeFalsy();
  });
});