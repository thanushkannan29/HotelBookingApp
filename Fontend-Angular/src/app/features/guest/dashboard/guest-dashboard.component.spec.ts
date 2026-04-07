import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { GuestDashboardComponent } from './guest-dashboard.component';
import { DashboardService, UserService } from '../../../core/services/api.services';
import { AuthService } from '../../../core/services/auth.service';
import { GuestDashboardDto, UserProfileResponseDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ── Mock data ──────────────────────────────────────────────────────────────────

const MOCK_DASHBOARD: GuestDashboardDto = {
  totalBookings:    8,
  activeBookings:   2,
  completedBookings: 5,
  cancelledBookings: 1,
  totalSpent:       40000,
};

const MOCK_PROFILE: UserProfileResponseDto = {
  userId:      'usr-001',
  email:       'thanush@test.com',
  role:        'Guest',
  name:        'Thanush K',
  phoneNumber: '9840650390',
  address:     '1 Anna Nagar',
  state:       'Tamil Nadu',
  city:        'Chennai',
  pincode:     '600040',
  createdAt:   '2024-01-01T00:00:00Z',
  totalReviewPoints: 100,
};

// ─────────────────────────────────────────────────────────────────────────────

describe('GuestDashboardComponent', () => {
  let component: GuestDashboardComponent;
  let fixture:   ComponentFixture<GuestDashboardComponent>;

  let dashboardSpy: jasmine.SpyObj<DashboardService>;
  let userSpy:      jasmine.SpyObj<UserService>;
  let authSpy:      jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    dashboardSpy = jasmine.createSpyObj('DashboardService', ['getGuestDashboard']);
    userSpy      = jasmine.createSpyObj('UserService',      ['getProfile']);
    authSpy      = jasmine.createSpyObj('AuthService',      ['isAuthenticated', 'updateUserName'], {
      currentUser: () => ({ userId: 'usr-001', userName: 'Thanush K', role: 'Guest' })
    });

    dashboardSpy.getGuestDashboard.and.returnValue(of(MOCK_DASHBOARD));
    userSpy.getProfile.and.returnValue(of(MOCK_PROFILE));

    await TestBed.configureTestingModule({
      imports: [GuestDashboardComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: DashboardService, useValue: dashboardSpy },
        { provide: UserService,      useValue: userSpy      },
        { provide: AuthService,      useValue: authSpy      },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(GuestDashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── ngOnInit ───────────────────────────────────────────────────────────────

  it('ngOnInit — should call getGuestDashboard on startup', () => {
    expect(dashboardSpy.getGuestDashboard).toHaveBeenCalledOnceWith();
  });

  it('ngOnInit — should call getProfile on startup', () => {
    expect(userSpy.getProfile).toHaveBeenCalledOnceWith();
  });

  it('ngOnInit — should populate data signal with dashboard response', () => {
    expect(component.data()).not.toBeNull();
    expect(component.data()?.totalBookings).toBe(8);
    expect(component.data()?.totalSpent).toBe(40000);
  });

  it('ngOnInit — should populate profile signal with user profile', () => {
    expect(component.profile()).not.toBeNull();
    expect(component.profile()?.name).toBe('Thanush K');
    expect(component.profile()?.email).toBe('thanush@test.com');
    expect(component.profile()?.city).toBe('Chennai');
  });

  // ── SIGNAL STATE ───────────────────────────────────────────────────────────

  it('data — should start as null before ngOnInit when not yet subscribed', () => {
    // Create a fresh component without detectChanges (ngOnInit not run)
    const freshFixture = TestBed.createComponent(GuestDashboardComponent);
    const freshCmp     = freshFixture.componentInstance;
    // data signal is null before detectChanges triggers ngOnInit
    expect(freshCmp.data()).toBeNull();
    expect(freshCmp.profile()).toBeNull();
  });

  it('data — activeBookings should be correct', () => {
    expect(component.data()?.activeBookings).toBe(2);
  });

  it('data — completedBookings should be correct', () => {
    expect(component.data()?.completedBookings).toBe(5);
  });

  it('data — cancelledBookings should be correct', () => {
    expect(component.data()?.cancelledBookings).toBe(1);
  });

  // ── PROFILE SIGNAL ─────────────────────────────────────────────────────────

  it('profile — should have correct role', () => {
    expect(component.profile()?.role).toBe('Guest');
  });

  it('profile — profileImageUrl should be undefined when not set', () => {
    expect(component.profile()?.profileImageUrl).toBeUndefined();
  });

  it('profile — should update when set directly', () => {
    const updated: UserProfileResponseDto = {
      ...MOCK_PROFILE,
      name: 'Thanush Kumar',
      city: 'Coimbatore',
    };
    component.profile.set(updated);
    expect(component.profile()?.name).toBe('Thanush Kumar');
    expect(component.profile()?.city).toBe('Coimbatore');
  });

  // ── TEMPLATE RENDERS ───────────────────────────────────────────────────────

  it('should render total bookings in the template', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('8');
  });

  it('should render total spent in the template', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    // DecimalPipe formats 40000 as "40,000"
    expect(el.textContent).toContain('40,000');
  });

  // ── AUTH SERVICE INTEGRATION ───────────────────────────────────────────────

  it('auth — should expose the injected AuthService', () => {
    expect(component.auth).toBeTruthy();
  });

  it('auth.currentUser — should return the mocked user', () => {
    expect(component.auth.currentUser()?.userName).toBe('Thanush K');
    expect(component.auth.currentUser()?.role).toBe('Guest');
  });
});