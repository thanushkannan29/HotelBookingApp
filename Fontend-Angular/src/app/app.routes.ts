import { Routes } from '@angular/router';
import { authGuard, guestGuard, adminGuard, superAdminGuard, publicGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/hotels', pathMatch: 'full' },

  // ── PUBLIC ──────────────────────────────────────────────────────────────
  {
    path: 'hotels',
    loadChildren: () => import('./features/hotel/hotel.routes').then(m => m.HOTEL_ROUTES),
  },

  // ── AUTH ─────────────────────────────────────────────────────────────────
  {
    path: 'auth',
    canActivate: [publicGuard],
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES),
  },

  // ── GUEST ─────────────────────────────────────────────────────────────────
  {
    path: 'guest',
    canActivate: [guestGuard],
    loadChildren: () => import('./features/guest/guest.routes').then(m => m.GUEST_ROUTES),
  },

  // ── BOOKING ───────────────────────────────────────────────────────────────
  {
    path: 'booking',
    canActivate: [guestGuard],
    loadChildren: () => import('./features/booking/booking.routes').then(m => m.BOOKING_ROUTES),
  },

  // ── ADMIN ─────────────────────────────────────────────────────────────────
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadChildren: () => import('./features/admin/admin.routes').then(m => m.ADMIN_ROUTES),
  },

  // ── SUPERADMIN ────────────────────────────────────────────────────────────
  {
    path: 'superadmin',
    canActivate: [superAdminGuard],
    loadChildren: () => import('./features/superadmin/superadmin.routes').then(m => m.SUPERADMIN_ROUTES),
  },

  // ── MISC ──────────────────────────────────────────────────────────────────
  {
    path: 'contact',
    loadComponent: () => import('./features/contact/contact.component').then(m => m.ContactComponent),
  },
  {
    path: 'unauthorized',
    loadComponent: () => import('./features/not-found/not-found.component').then(m => m.NotFoundComponent),
  },
  {
    path: '**',
    loadComponent: () => import('./features/not-found/not-found.component').then(m => m.NotFoundComponent),
  },
];
