import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { LoginDto, RegisterUserDto, RegisterHotelAdminDto } from '../models/models';

// ── Minimal valid JWT for testing (role: Guest, exp: far future) ──────────────
// Header: {"alg":"HS256","typ":"JWT"}
// Payload: {"nameid":"usr-001","unique_name":"Thanush","role":"Guest","exp":9999999999}
const MOCK_GUEST_TOKEN =
  'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.' +
  'eyJuYW1laWQiOiJ1c3ItMDAxIiwidW5pcXVlX25hbWUiOiJUaGFudXNoIiwicm9sZSI6Ikd1ZXN0IiwiZXhwIjo5OTk5OTk5OTk5fQ.' +
  'signature';

const MOCK_ADMIN_TOKEN =
  'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.' +
  'eyJuYW1laWQiOiJ1c3ItMDAyIiwidW5pcXVlX25hbWUiOiJBZG1pblVzZXIiLCJyb2xlIjoiQWRtaW4iLCJIb3RlbElkIjoiaG90ZWwtMDAxIiwiZXhwIjo5OTk5OTk5OTk5fQ.' +
  'signature';

const MOCK_SA_TOKEN =
  'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.' +
  'eyJuYW1laWQiOiJ1c3ItMDAzIiwidW5pcXVlX25hbWUiOiJTdXBlckFkbWluIiwicm9sZSI6IlN1cGVyQWRtaW4iLCJleHAiOjk5OTk5OTk5OTl9.' +
  'signature';

// Expired token (exp: 1 = 1970, always in the past)
const MOCK_EXPIRED_TOKEN =
  'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.' +
  'eyJuYW1laWQiOiJ1c3ItMDA0IiwidW5pcXVlX25hbWUiOiJPbGRVc2VyIiwicm9sZSI6Ikd1ZXN0IiwiZXhwIjoxfQ.' +
  'signature';

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;
  let router: Router;

  beforeEach(() => {
    // Clear any token left over from a previous test
    localStorage.removeItem('hotel_token');

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ]
    });

    service = TestBed.inject(AuthService);
    http    = TestBed.inject(HttpTestingController);
    router  = TestBed.inject(Router);
  });

  afterEach(() => {
    http.verify();                          // no unexpected HTTP calls
    localStorage.removeItem('hotel_token'); // clean slate after every test
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should start unauthenticated when localStorage is empty', () => {
    expect(service.isAuthenticated()).toBeFalse();
    expect(service.currentUser()).toBeNull();
    expect(service.token()).toBeNull();
  });

  // ── STORAGE RESTORE ON STARTUP ─────────────────────────────────────────────

  it('should restore a valid Guest session from localStorage on startup', () => {
    localStorage.setItem('hotel_token', MOCK_GUEST_TOKEN);

    // Re-create service so constructor runs loadFromStorage() with the token present
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
    const freshService = TestBed.inject(AuthService);

    expect(freshService.isAuthenticated()).toBeTrue();
    expect(freshService.currentUser()?.userName).toBe('Thanush');
    expect(freshService.currentUser()?.role).toBe('Guest');
  });

  it('should NOT restore an expired token from localStorage', () => {
    localStorage.setItem('hotel_token', MOCK_EXPIRED_TOKEN);

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
    const freshService = TestBed.inject(AuthService);

    expect(freshService.isAuthenticated()).toBeFalse();
    expect(freshService.currentUser()).toBeNull();
    // expired token must be removed from storage
    expect(localStorage.getItem('hotel_token')).toBeNull();
  });

  it('should clear storage when localStorage contains a malformed token', () => {
    localStorage.setItem('hotel_token', 'this.is.not.valid.jwt');

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
    const freshService = TestBed.inject(AuthService);

    expect(freshService.isAuthenticated()).toBeFalse();
    expect(localStorage.getItem('hotel_token')).toBeNull();
  });

  // ── LOGIN ──────────────────────────────────────────────────────────────────

  it('login() — should POST to /auth/login with credentials', () => {
    const dto: LoginDto = { email: 'thanush@test.com', password: 'pass123' };

    service.login(dto).subscribe();

    const req = http.expectOne(`${environment.apiUrl}/auth/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.email).toBe('thanush@test.com');
    expect(req.request.body.password).toBe('pass123');
    req.flush({ success: true, data: { token: MOCK_GUEST_TOKEN } });
  });

  it('login() — should set token in localStorage and update signals on success', () => {
    const dto: LoginDto = { email: 'thanush@test.com', password: 'pass123' };

    service.login(dto).subscribe(res => {
      expect(res.token).toBe(MOCK_GUEST_TOKEN);
    });

    http.expectOne(`${environment.apiUrl}/auth/login`)
        .flush({ success: true, data: { token: MOCK_GUEST_TOKEN } });

    expect(localStorage.getItem('hotel_token')).toBe(MOCK_GUEST_TOKEN);
    expect(service.isAuthenticated()).toBeTrue();
    expect(service.currentUser()?.userName).toBe('Thanush');
    expect(service.currentUser()?.role).toBe('Guest');
    expect(service.isGuest()).toBeTrue();
    expect(service.isAdmin()).toBeFalse();
    expect(service.isSuperAdmin()).toBeFalse();
  });

  it('login() — Admin token should set isAdmin() to true', () => {
    service.login({ email: 'admin@test.com', password: 'pass123' }).subscribe();

    http.expectOne(`${environment.apiUrl}/auth/login`)
        .flush({ success: true, data: { token: MOCK_ADMIN_TOKEN } });

    expect(service.isAdmin()).toBeTrue();
    expect(service.isGuest()).toBeFalse();
    expect(service.currentUser()?.hotelId).toBe('hotel-001');
  });

  it('login() — SuperAdmin token should set isSuperAdmin() to true', () => {
    service.login({ email: 'sa@test.com', password: 'pass123' }).subscribe();

    http.expectOne(`${environment.apiUrl}/auth/login`)
        .flush({ success: true, data: { token: MOCK_SA_TOKEN } });

    expect(service.isSuperAdmin()).toBeTrue();
    expect(service.isAdmin()).toBeFalse();
    expect(service.isGuest()).toBeFalse();
  });

  // ── REGISTER GUEST ─────────────────────────────────────────────────────────

  it('registerGuest() — should POST to /auth/register-guest', () => {
    const dto: RegisterUserDto = {
      name: 'Thanush', email: 'thanush@test.com', password: 'pass123'
    };

    service.registerGuest(dto).subscribe();

    const req = http.expectOne(`${environment.apiUrl}/auth/register-guest`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.name).toBe('Thanush');
    expect(req.request.body.email).toBe('thanush@test.com');
    req.flush({ success: true, data: { token: MOCK_GUEST_TOKEN } });
  });

  it('registerGuest() — should authenticate user after successful registration', () => {
    const dto: RegisterUserDto = {
      name: 'Thanush', email: 'thanush@test.com', password: 'pass123'
    };

    service.registerGuest(dto).subscribe();

    http.expectOne(`${environment.apiUrl}/auth/register-guest`)
        .flush({ success: true, data: { token: MOCK_GUEST_TOKEN } });

    expect(service.isAuthenticated()).toBeTrue();
    expect(service.isGuest()).toBeTrue();
    expect(service.currentUser()?.userId).toBe('usr-001');
  });

  // ── REGISTER HOTEL ADMIN ───────────────────────────────────────────────────

  it('registerHotelAdmin() — should POST to /auth/register-hotel-admin', () => {
    const dto: RegisterHotelAdminDto = {
      name: 'Admin User', email: 'admin@hotel.com', password: 'pass123',
      hotelName: 'Grand Palace', address: '1 MG Road',
      city: 'Chennai', state: 'TN', description: 'Luxury hotel',
      contactNumber: '9840650390'
    };

    service.registerHotelAdmin(dto).subscribe();

    const req = http.expectOne(`${environment.apiUrl}/auth/register-hotel-admin`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.hotelName).toBe('Grand Palace');
    expect(req.request.body.city).toBe('Chennai');
    req.flush({ success: true, data: { token: MOCK_ADMIN_TOKEN } });
  });

  it('registerHotelAdmin() — should set isAdmin() after hotel registration', () => {
    service.registerHotelAdmin({
      name: 'Admin', email: 'admin@hotel.com', password: 'pass123',
      hotelName: 'Grand Palace', address: '1 MG Road',
      city: 'Chennai', state: 'TN', description: '', contactNumber: '9840650390'
    }).subscribe();

    http.expectOne(`${environment.apiUrl}/auth/register-hotel-admin`)
        .flush({ success: true, data: { token: MOCK_ADMIN_TOKEN } });

    expect(service.isAdmin()).toBeTrue();
    expect(service.currentUser()?.hotelId).toBe('hotel-001');
  });

  // ── LOGOUT ─────────────────────────────────────────────────────────────────

  it('logout() — should clear signals and remove token from localStorage', () => {
    // First log in
    service.login({ email: 'thanush@test.com', password: 'pass123' }).subscribe();
    http.expectOne(`${environment.apiUrl}/auth/login`)
        .flush({ success: true, data: { token: MOCK_GUEST_TOKEN } });

    expect(service.isAuthenticated()).toBeTrue();

    // Now log out
    service.logout();

    expect(service.isAuthenticated()).toBeFalse();
    expect(service.currentUser()).toBeNull();
    expect(service.token()).toBeNull();
    expect(localStorage.getItem('hotel_token')).toBeNull();
  });

  it('logout() — should navigate to /auth/login', fakeAsync(() => {
    const navigateSpy = spyOn(router, 'navigate');

    service.login({ email: 'thanush@test.com', password: 'pass123' }).subscribe();
    http.expectOne(`${environment.apiUrl}/auth/login`)
        .flush({ success: true, data: { token: MOCK_GUEST_TOKEN } });

    service.logout();
    tick();

    expect(navigateSpy).toHaveBeenCalledWith(['/auth/login']);
  }));

  // ── getRedirectUrl ─────────────────────────────────────────────────────────

  it('getRedirectUrl() — should return /guest/dashboard for Guest role', () => {
    service.login({ email: 'thanush@test.com', password: 'pass123' }).subscribe();
    http.expectOne(`${environment.apiUrl}/auth/login`)
        .flush({ success: true, data: { token: MOCK_GUEST_TOKEN } });

    expect(service.getRedirectUrl()).toBe('/guest/dashboard');
  });

  it('getRedirectUrl() — should return /admin/dashboard for Admin role', () => {
    service.login({ email: 'admin@test.com', password: 'pass123' }).subscribe();
    http.expectOne(`${environment.apiUrl}/auth/login`)
        .flush({ success: true, data: { token: MOCK_ADMIN_TOKEN } });

    expect(service.getRedirectUrl()).toBe('/admin/dashboard');
  });

  it('getRedirectUrl() — should return /superadmin/dashboard for SuperAdmin role', () => {
    service.login({ email: 'sa@test.com', password: 'pass123' }).subscribe();
    http.expectOne(`${environment.apiUrl}/auth/login`)
        .flush({ success: true, data: { token: MOCK_SA_TOKEN } });

    expect(service.getRedirectUrl()).toBe('/superadmin/dashboard');
  });

  it('getRedirectUrl() — should return /guest/dashboard when not logged in', () => {
    expect(service.getRedirectUrl()).toBe('/guest/dashboard');
  });

  // ── COMPUTED SIGNALS ───────────────────────────────────────────────────────

  it('isAuthenticated() — should be false before login and true after', () => {
    expect(service.isAuthenticated()).toBeFalse();

    service.login({ email: 'thanush@test.com', password: 'pass123' }).subscribe();
    http.expectOne(`${environment.apiUrl}/auth/login`)
        .flush({ success: true, data: { token: MOCK_GUEST_TOKEN } });

    expect(service.isAuthenticated()).toBeTrue();
  });

  it('token() — should expose the raw JWT string after login', () => {
    service.login({ email: 'thanush@test.com', password: 'pass123' }).subscribe();
    http.expectOne(`${environment.apiUrl}/auth/login`)
        .flush({ success: true, data: { token: MOCK_GUEST_TOKEN } });

    expect(service.token()).toBe(MOCK_GUEST_TOKEN);
  });
});