import { Routes } from '@angular/router';

export const ADMIN_ROUTES: Routes = [
  {
    path: 'dashboard',
    loadComponent: () => import('./dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent),
  },
  {
    path: 'hotel',
    loadComponent: () => import('./hotel-management/hotel-management.component').then(m => m.HotelManagementComponent),
  },
  {
    path: 'rooms',
    loadComponent: () => import('./room-management/room-management.component').then(m => m.RoomManagementComponent),
  },
  {
    path: 'roomtypes',
    loadComponent: () => import('./room-management/roomtype-management.component').then(m => m.RoomTypeManagementComponent),
  },
  {
    path: 'inventory',
    loadComponent: () => import('./inventory-management/inventory-management.component').then(m => m.InventoryManagementComponent),
  },
  {
    path: 'reservations',
    loadComponent: () => import('./reservation-management/reservation-management.component').then(m => m.ReservationManagementComponent),
  },
  {
    path: 'audit-logs',
    loadComponent: () => import('./audit-logs/audit-logs.component').then(m => m.AuditLogsComponent),
  },
  // F7A: Admin Reviews page
  {
    path: 'reviews',
    loadComponent: () => import('./reviews/admin-reviews.component').then(m => m.AdminReviewsComponent),
  },
  // F7B: Admin Transactions page
  {
    path: 'transactions',
    loadComponent: () => import('./transactions/admin-transactions.component').then(m => m.AdminTransactionsComponent),
  },
  {
    path: 'amenity-requests',
    loadComponent: () => import('./amenity-requests/amenity-requests.component').then(m => m.AmenityRequestsComponent),
  },
  {
    path: 'support',
    loadComponent: () => import('./support-requests/admin-support-requests.component').then(m => m.AdminSupportRequestsComponent),
  },
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
];
