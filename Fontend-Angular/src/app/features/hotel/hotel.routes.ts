import { Routes } from '@angular/router';

export const HOTEL_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./hotel-list/hotel-list.component').then(m => m.HotelListComponent),
  },
  {
    path: ':id',
    loadComponent: () => import('./hotel-details/hotel-details.component').then(m => m.HotelDetailsComponent),
  },
];
