import { Routes } from '@angular/router';

export const GUEST_ROUTES: Routes = [
  {
    path: 'dashboard',
    loadComponent: () => import('./dashboard/guest-dashboard.component').then(m => m.GuestDashboardComponent),
  },
  {
    path: 'bookings',
    loadComponent: () => import('../booking/booking-list/booking-list.component').then(m => m.BookingListComponent),
  },
  {
    path: 'profile',
    loadComponent: () => import('./profile/guest-profile.component').then(m => m.GuestProfileComponent),
  },
  {
    path: 'reviews',
    loadComponent: () => import('./reviews/guest-reviews.component').then(m => m.GuestReviewsComponent),
  },
  {
    path: 'transactions',
    loadComponent: () => import('./transactions/guest-transactions.component').then(m => m.GuestTransactionsComponent),
  },
  {
    path: 'wallet',
    loadComponent: () => import('./wallet/guest-wallet.component').then(m => m.GuestWalletComponent),
  },
  {
    path: 'promo-codes',
    loadComponent: () => import('./promo-codes/guest-promo-codes.component').then(m => m.GuestPromoCodesComponent),
  },
  {
    path: 'support',
    loadComponent: () => import('./support-requests/guest-support-requests.component').then(m => m.GuestSupportRequestsComponent),
  },
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
];
