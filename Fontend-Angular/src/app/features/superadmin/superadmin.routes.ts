import { Routes } from '@angular/router';

export const SUPERADMIN_ROUTES: Routes = [
  {
    path: 'dashboard',
    loadComponent: () => import('./dashboard/superadmin-dashboard.component').then(m => m.SuperAdminDashboardComponent),
  },
  {
    path: 'hotels',
    loadComponent: () => import('./hotel-control/hotel-control.component').then(m => m.HotelControlComponent),
  },
  {
    path: 'amenity-requests',
    loadComponent: () => import('./amenity-requests/superadmin-amenity-requests.component').then(m => m.SuperadminAmenityRequestsComponent),
  },
  {
    path: 'amenities',
    loadComponent: () => import('./amenity-management/superadmin-amenity-management.component').then(m => m.SuperadminAmenityManagementComponent),
  },
  {
    path: 'revenue',
    loadComponent: () => import('./revenue/superadmin-revenue.component').then(m => m.SuperadminRevenueComponent),
  },
  {
    path: 'audit-logs',
    loadComponent: () => import('../admin/audit-logs/audit-logs.component').then(m => m.AuditLogsComponent),
    data: { mode: 'superadmin' },
  },
  {
    path: 'error-logs',
    loadComponent: () => import('./error-logs/error-logs.component').then(m => m.ErrorLogsComponent),
  },
  {
    path: 'profile',
    loadComponent: () => import('./profile/superadmin-profile.component').then(m => m.SuperAdminProfileComponent),
  },
  {
    path: 'support',
    loadComponent: () => import('./support-requests/superadmin-support-requests.component').then(m => m.SuperadminSupportRequestsComponent),
  },
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
];
