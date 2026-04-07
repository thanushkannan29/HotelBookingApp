import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';
import {
  provideHttpClient,
  withInterceptors,
  HttpClient,
  HttpErrorResponse
} from '@angular/common/http';
import { provideRouter, Router } from '@angular/router';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';

const LOCAL_URL    = 'http://localhost:5000/api/test';
const EXTERNAL_URL = 'https://generativelanguage.googleapis.com/v1/models';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authSpy: jasmine.SpyObj<AuthService>;
  let toastSpy: jasmine.SpyObj<ToastService>;
  let router: Router;

  beforeEach(() => {
    authSpy  = jasmine.createSpyObj('AuthService', ['token', 'logout']);
    toastSpy = jasmine.createSpyObj('ToastService', ['error', 'success', 'info']);
    authSpy.token.and.returnValue(null);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthService, useValue: authSpy },
        { provide: ToastService, useValue: toastSpy },
      ]
    });

    http     = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    router   = TestBed.inject(Router);
    spyOn(router, 'navigate');
  });

  afterEach(() => httpMock.verify());

  // ── Token attachment ──────────────────────────────────────────────────────

  it('should attach Authorization header when token is present', () => {
    authSpy.token.and.returnValue('my-jwt-token');
    http.get(LOCAL_URL).subscribe();
    const req = httpMock.expectOne(LOCAL_URL);
    expect(req.request.headers.get('Authorization')).toBe('Bearer my-jwt-token');
    req.flush({});
  });

  it('should NOT attach Authorization header when token is null', () => {
    authSpy.token.and.returnValue(null);
    http.get(LOCAL_URL).subscribe();
    const req = httpMock.expectOne(LOCAL_URL);
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });

  // ── External URL bypass ───────────────────────────────────────────────────

  it('should skip interceptor for external URLs', () => {
    authSpy.token.and.returnValue('my-jwt-token');
    http.get(EXTERNAL_URL).subscribe();
    const req = httpMock.expectOne(EXTERNAL_URL);
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });

  // ── Error handling ────────────────────────────────────────────────────────

  it('should call toast.error on status 0 (no connection)', () => {
    http.get(LOCAL_URL).subscribe({ error: () => {} });
    const req = httpMock.expectOne(LOCAL_URL);
    req.flush(null, { status: 0, statusText: 'Unknown Error' });
    expect(toastSpy.error).toHaveBeenCalledWith('Cannot connect to server. Make sure the API is running.');
  });

  it('should call authService.logout on 401', () => {
    http.get(LOCAL_URL).subscribe({ error: () => {} });
    const req = httpMock.expectOne(LOCAL_URL);
    req.flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(authSpy.logout).toHaveBeenCalled();
  });

  it('should NOT call toast.error on 401 (logout handles navigation)', () => {
    http.get(LOCAL_URL).subscribe({ error: () => {} });
    const req = httpMock.expectOne(LOCAL_URL);
    req.flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(toastSpy.error).not.toHaveBeenCalled();
  });

  it('should navigate to /unauthorized on 403', () => {
    http.get(LOCAL_URL).subscribe({ error: () => {} });
    const req = httpMock.expectOne(LOCAL_URL);
    req.flush(null, { status: 403, statusText: 'Forbidden' });
    expect(router.navigate).toHaveBeenCalledWith(['/unauthorized']);
  });

  it('should call toast.error with permission message on 403', () => {
    http.get(LOCAL_URL).subscribe({ error: () => {} });
    const req = httpMock.expectOne(LOCAL_URL);
    req.flush(null, { status: 403, statusText: 'Forbidden' });
    expect(toastSpy.error).toHaveBeenCalledWith('You do not have permission for this action.');
  });

  it('should call toast.error with server error message on 404 with message', () => {
    http.get(LOCAL_URL).subscribe({ error: () => {} });
    const req = httpMock.expectOne(LOCAL_URL);
    req.flush({ message: 'Hotel not found' }, { status: 404, statusText: 'Not Found' });
    expect(toastSpy.error).toHaveBeenCalledWith('Hotel not found');
  });

  it('should call toast.error with default 404 message when no body message', () => {
    http.get(LOCAL_URL).subscribe({ error: () => {} });
    const req = httpMock.expectOne(LOCAL_URL);
    req.flush(null, { status: 404, statusText: 'Not Found' });
    expect(toastSpy.error).toHaveBeenCalledWith('Resource not found.');
  });

  it('should call toast.error with conflict message on 409 with body', () => {
    http.get(LOCAL_URL).subscribe({ error: () => {} });
    const req = httpMock.expectOne(LOCAL_URL);
    req.flush({ message: 'Room already exists' }, { status: 409, statusText: 'Conflict' });
    expect(toastSpy.error).toHaveBeenCalledWith('Room already exists');
  });

  it('should call toast.error with default conflict message on 409 without body', () => {
    http.get(LOCAL_URL).subscribe({ error: () => {} });
    const req = httpMock.expectOne(LOCAL_URL);
    req.flush(null, { status: 409, statusText: 'Conflict' });
    expect(toastSpy.error).toHaveBeenCalledWith('Conflict — resource already exists.');
  });

  it('should call toast.error with rate limit message on 429', () => {
    http.get(LOCAL_URL).subscribe({ error: () => {} });
    const req = httpMock.expectOne(LOCAL_URL);
    req.flush(null, { status: 429, statusText: 'Too Many Requests' });
    expect(toastSpy.error).toHaveBeenCalledWith('Too many requests. Please wait a moment.');
  });

  it('should call toast.error with server error message on 500', () => {
    http.get(LOCAL_URL).subscribe({ error: () => {} });
    const req = httpMock.expectOne(LOCAL_URL);
    req.flush(null, { status: 500, statusText: 'Internal Server Error' });
    expect(toastSpy.error).toHaveBeenCalledWith('Server error. Please try again later.');
  });

  it('should call toast.error with body message when error.error.message is present', () => {
    http.get(LOCAL_URL).subscribe({ error: () => {} });
    const req = httpMock.expectOne(LOCAL_URL);
    req.flush({ message: 'Custom error from server' }, { status: 422, statusText: 'Unprocessable' });
    expect(toastSpy.error).toHaveBeenCalledWith('Custom error from server');
  });

  it('should call toast.error with default message for unknown status without body', () => {
    http.get(LOCAL_URL).subscribe({ error: () => {} });
    const req = httpMock.expectOne(LOCAL_URL);
    req.flush(null, { status: 422, statusText: 'Unprocessable' });
    expect(toastSpy.error).toHaveBeenCalledWith('An unexpected error occurred.');
  });

  it('should pass through successful responses unchanged', () => {
    let result: any;
    http.get(LOCAL_URL).subscribe(r => result = r);
    const req = httpMock.expectOne(LOCAL_URL);
    req.flush({ data: 'ok' });
    expect(result).toEqual({ data: 'ok' });
  });
});
