import { inject } from '@angular/core';
import { Router, CanActivateFn, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated()) return true;
  localStorage.setItem('returnUrl', state.url);
  router.navigate(['/auth/login']);
  return false;
};

export const guestGuard: CanActivateFn = (route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated() && auth.isGuest()) return true;
  if (auth.isAuthenticated()) {
    router.navigate([auth.getRedirectUrl()]);
    return false;
  }
  localStorage.setItem('returnUrl', state.url);
  router.navigate(['/auth/login']);
  return false;
};

export const adminGuard: CanActivateFn = (route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated() && auth.isAdmin()) return true;
  if (auth.isAuthenticated()) {
    router.navigate([auth.getRedirectUrl()]);
    return false;
  }
  localStorage.setItem('returnUrl', state.url);
  router.navigate(['/auth/login']);
  return false;
};

export const superAdminGuard: CanActivateFn = (route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated() && auth.isSuperAdmin()) return true;
  if (auth.isAuthenticated()) {
    router.navigate([auth.getRedirectUrl()]);
    return false;
  }
  localStorage.setItem('returnUrl', state.url);
  router.navigate(['/auth/login']);
  return false;
};

export const publicGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isAuthenticated()) return true;
  router.navigate([auth.getRedirectUrl()]);
  return false;
};
