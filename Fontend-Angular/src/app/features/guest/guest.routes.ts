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
    path: 'refunds',
    loadComponent: () => import('./refund-requests/guest-refunds.component').then(m => m.GuestRefundsComponent),
  },
  {
    path: 'transactions',
    loadComponent: () => import('./transactions/guest-transactions.component').then(m => m.GuestTransactionsComponent),
  },
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
];
