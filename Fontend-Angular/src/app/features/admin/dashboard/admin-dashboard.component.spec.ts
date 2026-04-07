import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AdminDashboardComponent } from './admin-dashboard.component';
import { DashboardService } from '../../../core/services/api.services';
import { HotelService } from '../../../core/services/hotel.service';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/services/auth.service';
import { ReportService } from './report.service';
import { AdminDashboardDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

const MOCK_DASHBOARD: AdminDashboardDto = {
  hotelId: 'hotel-001', hotelName: 'Grand Palace', hotelImageUrl: undefined,
  isActive: true, isBlockedBySuperAdmin: false,
  totalRooms: 20, activeRooms: 18, totalRoomTypes: 3,
  totalReservations: 120, pendingReservations: 5, activeReservations: 10,
  completedReservations: 100, cancelledReservations: 5,
  totalRevenue: 600000, totalReviews: 45, averageRating: 4.3
};

describe('AdminDashboardComponent', () => {
  let component: AdminDashboardComponent;
  let fixture: ComponentFixture<AdminDashboardComponent>;
  let dashboardSpy: jasmine.SpyObj<DashboardService>;
  let hotelSpy: jasmine.SpyObj<HotelService>;
  let toastSpy: jasmine.SpyObj<ToastService>;
  let authSpy: jasmine.SpyObj<AuthService>;
  let reportSpy: jasmine.SpyObj<ReportService>;

  beforeEach(async () => {
    dashboardSpy = jasmine.createSpyObj('DashboardService', ['getAdminDashboard']);
    hotelSpy     = jasmine.createSpyObj('HotelService', ['toggleHotelStatus']);
    toastSpy     = jasmine.createSpyObj('ToastService', ['success', 'error']);
    reportSpy    = jasmine.createSpyObj('ReportService', ['downloadReport']);
    authSpy      = jasmine.createSpyObj('AuthService', ['updateHotelImage'], {
      currentUser: () => ({ userName: 'Admin', role: 'Admin' })
    });

    dashboardSpy.getAdminDashboard.and.returnValue(of(MOCK_DASHBOARD));

    await TestBed.configureTestingModule({
      imports: [AdminDashboardComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        { provide: DashboardService, useValue: dashboardSpy },
        { provide: HotelService,     useValue: hotelSpy },
        { provide: ToastService,     useValue: toastSpy },
        { provide: AuthService,      useValue: authSpy },
        { provide: ReportService,    useValue: reportSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AdminDashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── Creation ──────────────────────────────────────────────────────────────

  it('should create', () => expect(component).toBeTruthy());

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should call getAdminDashboard', () => {
    expect(dashboardSpy.getAdminDashboard).toHaveBeenCalledOnceWith();
  });

  it('ngOnInit — should populate data signal', () => {
    expect(component.data()?.hotelName).toBe('Grand Palace');
    expect(component.data()?.totalRevenue).toBe(600000);
  });

  it('ngOnInit — should call updateHotelImage', () => {
    expect(authSpy.updateHotelImage).toHaveBeenCalled();
  });

  // ── Initial signal state ──────────────────────────────────────────────────

  it('isTogglingStatus — should start as false', () => {
    expect(component.isTogglingStatus()).toBeFalse();
  });

  // ── toggleHotelStatus — deactivate ────────────────────────────────────────

  it('toggleHotelStatus — should call toggleHotelStatus(false) when hotel is active', () => {
    hotelSpy.toggleHotelStatus.and.returnValue(of(undefined));
    component.toggleHotelStatus();
    expect(hotelSpy.toggleHotelStatus).toHaveBeenCalledOnceWith(false);
  });

  it('toggleHotelStatus — should update data.isActive to false after deactivation', () => {
    hotelSpy.toggleHotelStatus.and.returnValue(of(undefined));
    component.toggleHotelStatus();
    expect(component.data()?.isActive).toBeFalse();
  });

  it('toggleHotelStatus — should show "Hotel deactivated." toast when deactivating', () => {
    hotelSpy.toggleHotelStatus.and.returnValue(of(undefined));
    component.toggleHotelStatus();
    expect(toastSpy.success).toHaveBeenCalledOnceWith('Hotel deactivated.');
  });

  // ── toggleHotelStatus — activate ─────────────────────────────────────────

  it('toggleHotelStatus — should call toggleHotelStatus(true) when hotel is inactive', () => {
    hotelSpy.toggleHotelStatus.and.returnValue(of(undefined));
    component.data.set({ ...MOCK_DASHBOARD, isActive: false });
    component.toggleHotelStatus();
    expect(hotelSpy.toggleHotelStatus).toHaveBeenCalledOnceWith(true);
  });

  it('toggleHotelStatus — should show "Hotel is now live." toast when activating', () => {
    hotelSpy.toggleHotelStatus.and.returnValue(of(undefined));
    component.data.set({ ...MOCK_DASHBOARD, isActive: false });
    component.toggleHotelStatus();
    expect(toastSpy.success).toHaveBeenCalledOnceWith('Hotel is now live.');
  });

  it('toggleHotelStatus — should reset isTogglingStatus to false on success', () => {
    hotelSpy.toggleHotelStatus.and.returnValue(of(undefined));
    component.toggleHotelStatus();
    expect(component.isTogglingStatus()).toBeFalse();
  });

  // ── toggleHotelStatus — error ─────────────────────────────────────────────

  it('toggleHotelStatus — should reset isTogglingStatus to false on error', () => {
    hotelSpy.toggleHotelStatus.and.returnValue(throwError(() => new Error('fail')));
    component.toggleHotelStatus();
    expect(component.isTogglingStatus()).toBeFalse();
  });

  it('toggleHotelStatus — should NOT update isActive on error', () => {
    hotelSpy.toggleHotelStatus.and.returnValue(throwError(() => new Error('fail')));
    component.toggleHotelStatus();
    expect(component.data()?.isActive).toBeTrue();
  });

  // ── toggleHotelStatus — guard ─────────────────────────────────────────────

  it('toggleHotelStatus — should do nothing when data is null', () => {
    component.data.set(null);
    component.toggleHotelStatus();
    expect(hotelSpy.toggleHotelStatus).not.toHaveBeenCalled();
  });

  // ── downloadReport ────────────────────────────────────────────────────────

  it('downloadReport — should call reportService.downloadReport with data', () => {
    component.downloadReport();
    expect(reportSpy.downloadReport).toHaveBeenCalledOnceWith(MOCK_DASHBOARD);
  });

  it('downloadReport — should NOT call reportService when data is null', () => {
    component.data.set(null);
    component.downloadReport();
    expect(reportSpy.downloadReport).not.toHaveBeenCalled();
  });

  // ── Template ──────────────────────────────────────────────────────────────

  it('should render hotel name in template', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Grand Palace');
  });
});
