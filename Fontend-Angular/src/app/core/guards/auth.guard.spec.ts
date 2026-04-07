import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { authGuard, guestGuard, adminGuard, superAdminGuard, publicGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';

const mockSnapshot = (url = '/test') =>
  ({ url } as unknown as RouterStateSnapshot);

const mockRoute = {} as ActivatedRouteSnapshot;

describe('Auth Guards', () => {
  let authSpy: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(() => {
    authSpy = jasmine.createSpyObj('AuthService', [
      'isAuthenticated', 'isGuest', 'isAdmin', 'isSuperAdmin', 'getRedirectUrl', 'logout'
    ]);
    authSpy.isAuthenticated.and.returnValue(false);
    authSpy.isGuest.and.returnValue(false);
    authSpy.isAdmin.and.returnValue(false);
    authSpy.isSuperAdmin.and.returnValue(false);
    authSpy.getRedirectUrl.and.returnValue('/dashboard');

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthService, useValue: authSpy },
      ]
    });

    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
    spyOn(localStorage, 'setItem');
  });

  // ── authGuard ─────────────────────────────────────────────────────────────

  describe('authGuard', () => {
    it('should return true when authenticated', () => {
      authSpy.isAuthenticated.and.returnValue(true);
      const result = TestBed.runInInjectionContext(() =>
        authGuard(mockRoute, mockSnapshot('/protected'))
      );
      expect(result).toBeTrue();
    });

    it('should return false when not authenticated', () => {
      authSpy.isAuthenticated.and.returnValue(false);
      const result = TestBed.runInInjectionContext(() =>
        authGuard(mockRoute, mockSnapshot('/protected'))
      );
      expect(result).toBeFalse();
    });

    it('should navigate to /auth/login when not authenticated', () => {
      authSpy.isAuthenticated.and.returnValue(false);
      TestBed.runInInjectionContext(() =>
        authGuard(mockRoute, mockSnapshot('/protected'))
      );
      expect(router.navigate).toHaveBeenCalledWith(['/auth/login']);
    });

    it('should save returnUrl to localStorage when not authenticated', () => {
      authSpy.isAuthenticated.and.returnValue(false);
      TestBed.runInInjectionContext(() =>
        authGuard(mockRoute, mockSnapshot('/protected'))
      );
      expect(localStorage.setItem).toHaveBeenCalledWith('returnUrl', '/protected');
    });
  });

  // ── guestGuard ────────────────────────────────────────────────────────────

  describe('guestGuard', () => {
    it('should return true when authenticated as Guest', () => {
      authSpy.isAuthenticated.and.returnValue(true);
      authSpy.isGuest.and.returnValue(true);
      const result = TestBed.runInInjectionContext(() =>
        guestGuard(mockRoute, mockSnapshot('/guest'))
      );
      expect(result).toBeTrue();
    });

    it('should redirect to getRedirectUrl when authenticated but not Guest', () => {
      authSpy.isAuthenticated.and.returnValue(true);
      authSpy.isGuest.and.returnValue(false);
      const result = TestBed.runInInjectionContext(() =>
        guestGuard(mockRoute, mockSnapshot('/guest'))
      );
      expect(result).toBeFalse();
      expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
    });

    it('should navigate to /auth/login when not authenticated', () => {
      authSpy.isAuthenticated.and.returnValue(false);
      TestBed.runInInjectionContext(() =>
        guestGuard(mockRoute, mockSnapshot('/guest'))
      );
      expect(router.navigate).toHaveBeenCalledWith(['/auth/login']);
    });

    it('should save returnUrl when not authenticated', () => {
      authSpy.isAuthenticated.and.returnValue(false);
      TestBed.runInInjectionContext(() =>
        guestGuard(mockRoute, mockSnapshot('/guest'))
      );
      expect(localStorage.setItem).toHaveBeenCalledWith('returnUrl', '/guest');
    });
  });

  // ── adminGuard ────────────────────────────────────────────────────────────

  describe('adminGuard', () => {
    it('should return true when authenticated as Admin', () => {
      authSpy.isAuthenticated.and.returnValue(true);
      authSpy.isAdmin.and.returnValue(true);
      const result = TestBed.runInInjectionContext(() =>
        adminGuard(mockRoute, mockSnapshot('/admin'))
      );
      expect(result).toBeTrue();
    });

    it('should redirect to getRedirectUrl when authenticated but not Admin', () => {
      authSpy.isAuthenticated.and.returnValue(true);
      authSpy.isAdmin.and.returnValue(false);
      const result = TestBed.runInInjectionContext(() =>
        adminGuard(mockRoute, mockSnapshot('/admin'))
      );
      expect(result).toBeFalse();
      expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
    });

    it('should navigate to /auth/login when not authenticated', () => {
      authSpy.isAuthenticated.and.returnValue(false);
      TestBed.runInInjectionContext(() =>
        adminGuard(mockRoute, mockSnapshot('/admin'))
      );
      expect(router.navigate).toHaveBeenCalledWith(['/auth/login']);
    });

    it('should save returnUrl when not authenticated', () => {
      authSpy.isAuthenticated.and.returnValue(false);
      TestBed.runInInjectionContext(() =>
        adminGuard(mockRoute, mockSnapshot('/admin'))
      );
      expect(localStorage.setItem).toHaveBeenCalledWith('returnUrl', '/admin');
    });
  });

  // ── superAdminGuard ───────────────────────────────────────────────────────

  describe('superAdminGuard', () => {
    it('should return true when authenticated as SuperAdmin', () => {
      authSpy.isAuthenticated.and.returnValue(true);
      authSpy.isSuperAdmin.and.returnValue(true);
      const result = TestBed.runInInjectionContext(() =>
        superAdminGuard(mockRoute, mockSnapshot('/superadmin'))
      );
      expect(result).toBeTrue();
    });

    it('should redirect to getRedirectUrl when authenticated but not SuperAdmin', () => {
      authSpy.isAuthenticated.and.returnValue(true);
      authSpy.isSuperAdmin.and.returnValue(false);
      const result = TestBed.runInInjectionContext(() =>
        superAdminGuard(mockRoute, mockSnapshot('/superadmin'))
      );
      expect(result).toBeFalse();
      expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
    });

    it('should navigate to /auth/login when not authenticated', () => {
      authSpy.isAuthenticated.and.returnValue(false);
      TestBed.runInInjectionContext(() =>
        superAdminGuard(mockRoute, mockSnapshot('/superadmin'))
      );
      expect(router.navigate).toHaveBeenCalledWith(['/auth/login']);
    });

    it('should save returnUrl when not authenticated', () => {
      authSpy.isAuthenticated.and.returnValue(false);
      TestBed.runInInjectionContext(() =>
        superAdminGuard(mockRoute, mockSnapshot('/superadmin'))
      );
      expect(localStorage.setItem).toHaveBeenCalledWith('returnUrl', '/superadmin');
    });
  });

  // ── publicGuard ───────────────────────────────────────────────────────────

  describe('publicGuard', () => {
    it('should return true when NOT authenticated', () => {
      authSpy.isAuthenticated.and.returnValue(false);
      const result = TestBed.runInInjectionContext(() =>
        publicGuard(mockRoute, mockSnapshot('/auth/login'))
      );
      expect(result).toBeTrue();
    });

    it('should return false when already authenticated', () => {
      authSpy.isAuthenticated.and.returnValue(true);
      const result = TestBed.runInInjectionContext(() =>
        publicGuard(mockRoute, mockSnapshot('/auth/login'))
      );
      expect(result).toBeFalse();
    });

    it('should redirect to getRedirectUrl when already authenticated', () => {
      authSpy.isAuthenticated.and.returnValue(true);
      TestBed.runInInjectionContext(() =>
        publicGuard(mockRoute, mockSnapshot('/auth/login'))
      );
      expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
    });
  });
});
