import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';
import { environment } from '../../../environments/environment';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router     = inject(Router);
  const toast      = inject(ToastService);

  // Only attach JWT to requests going to our own API; skip external APIs (Groq, etc.)
  if (!req.url.startsWith(environment.apiUrl)) {
    return next(req);
  }

  const token  = authService.token();
  const cloned = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(cloned).pipe(
    catchError((error: HttpErrorResponse) => {
      let message = 'An unexpected error occurred.';

      if (error.error?.message) {
        message = error.error.message;
      } else if (error.status === 0) {
        message = 'Cannot connect to server. Make sure the API is running.';
      } else if (error.status === 401) {
        authService.logout();
        return throwError(() => error); // logout navigates, skip toast
      } else if (error.status === 403) {
        message = 'You do not have permission for this action.';
        router.navigate(['/unauthorized']);
      } else if (error.status === 404) {
        message = error.error?.message ?? 'Resource not found.';
      } else if (error.status === 409) {
        message = error.error?.message ?? 'Conflict — resource already exists.';
      } else if (error.status === 429) {
        message = 'Too many requests. Please wait a moment.';
      } else if (error.status >= 500) {
        message = 'Server error. Please try again later.';
      }

      toast.error(message);
      return throwError(() => error);
    })
  );
};
