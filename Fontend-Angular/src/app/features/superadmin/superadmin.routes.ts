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
    path: 'audit-logs',
    loadComponent: () => import('../admin/audit-logs/audit-logs.component').then(m => m.AuditLogsComponent),
    data: { mode: 'superadmin' },
  },
  {
    path: 'error-logs',
    loadComponent: () => import('./error-logs/error-logs.component').then(m => m.ErrorLogsComponent),
  },
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
];
