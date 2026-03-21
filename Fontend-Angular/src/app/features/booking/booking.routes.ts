import { Routes } from '@angular/router';

export const BOOKING_ROUTES: Routes = [
  {
    path: 'create',
    loadComponent: () => import('./booking-create/booking-create.component').then(m => m.BookingCreateComponent),
  },
  {
    path: 'list',
    loadComponent: () => import('./booking-list/booking-list.component').then(m => m.BookingListComponent),
  },
  {
    path: ':code',
    loadComponent: () => import('./booking-detail/booking-detail.component').then(m => m.BookingDetailComponent),
  },
  { path: '', redirectTo: 'list', pathMatch: 'full' },
];
