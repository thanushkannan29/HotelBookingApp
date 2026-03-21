import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./login/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () => import('./register/register.component').then(m => m.RegisterComponent),
  },
  {
    path: 'register-admin',
    loadComponent: () => import('./register/register-admin.component').then(m => m.RegisterAdminComponent),
  },
  { path: '', redirectTo: 'login', pathMatch: 'full' },
];
