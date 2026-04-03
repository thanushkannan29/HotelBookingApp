import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { Router } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { LoginComponent } from './login.component';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ─────────────────────────────────────────────────────────────────────────────

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture:   ComponentFixture<LoginComponent>;

  let authSpy:   jasmine.SpyObj<AuthService>;
  let toastSpy:  jasmine.SpyObj<ToastService>;
  let router:    Router;

  beforeEach(async () => {
    authSpy  = jasmine.createSpyObj('AuthService', ['login', 'getRedirectUrl'], {
      currentUser: () => null
    });
    toastSpy = jasmine.createSpyObj('ToastService', ['success', 'error']);

    // Default happy-path responses
    authSpy.login.and.returnValue(of({ token: 'mock-token' }));
    authSpy.getRedirectUrl.and.returnValue('/guest/dashboard');

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthService,  useValue: authSpy  },
        { provide: ToastService, useValue: toastSpy },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    router    = TestBed.inject(Router);
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── INITIAL SIGNAL STATE ───────────────────────────────────────────────────

  it('hidePassword — should start as true', () => {
    expect(component.hidePassword()).toBeTrue();
  });

  it('isLoading — should start as false', () => {
    expect(component.isLoading()).toBeFalse();
  });

  // ── FORM INITIAL STATE ─────────────────────────────────────────────────────

  it('form — should be invalid initially', () => {
    expect(component.form.invalid).toBeTrue();
  });

  it('form — email field should start empty', () => {
    expect(component.form.get('email')?.value).toBe('');
  });

  it('form — password field should start empty', () => {
    expect(component.form.get('password')?.value).toBe('');
  });

  // ── FORM VALIDATION ────────────────────────────────────────────────────────

  it('form — should be valid when email and password are correct', () => {
    component.form.patchValue({ email: 'thanush@test.com', password: 'pass123' });
    expect(component.form.valid).toBeTrue();
  });

  it('form — should be invalid when email is empty', () => {
    component.form.patchValue({ email: '', password: 'pass123' });
    expect(component.form.invalid).toBeTrue();
  });

  it('form — should be invalid when email format is wrong', () => {
    component.form.patchValue({ email: 'not-an-email', password: 'pass123' });
    expect(component.form.get('email')?.invalid).toBeTrue();
  });

  it('form — should be invalid when password is empty', () => {
    component.form.patchValue({ email: 'thanush@test.com', password: '' });
    expect(component.form.invalid).toBeTrue();
  });

  it('form — should be invalid when password is less than 6 characters', () => {
    component.form.patchValue({ email: 'thanush@test.com', password: '123' });
    expect(component.form.get('password')?.invalid).toBeTrue();
  });

  it('form — should be valid when password is exactly 6 characters', () => {
    component.form.patchValue({ email: 'thanush@test.com', password: '123456' });
    expect(component.form.valid).toBeTrue();
  });

  // ── submit() — HAPPY PATH ──────────────────────────────────────────────────

  it('submit() — should call auth.login with form values', () => {
    component.form.patchValue({ email: 'thanush@test.com', password: 'pass123' });

    component.submit();

    expect(authSpy.login).toHaveBeenCalledOnceWith(
      jasmine.objectContaining({ email: 'thanush@test.com', password: 'pass123' })
    );
  });

  it('submit() — should show "Welcome back!" toast on success', () => {
    component.form.patchValue({ email: 'thanush@test.com', password: 'pass123' });

    component.submit();

    expect(toastSpy.success).toHaveBeenCalledOnceWith('Welcome back!');
  });

  it('submit() — should call getRedirectUrl to determine navigation target', () => {
    component.form.patchValue({ email: 'thanush@test.com', password: 'pass123' });

    component.submit();

    expect(authSpy.getRedirectUrl).toHaveBeenCalled();
  });

  it('submit() — should navigate to the URL returned by getRedirectUrl', () => {
    const navigateSpy = spyOn(router, 'navigateByUrl');
    authSpy.getRedirectUrl.and.returnValue('/guest/dashboard');
    component.form.patchValue({ email: 'thanush@test.com', password: 'pass123' });

    component.submit();

    expect(navigateSpy).toHaveBeenCalledOnceWith('/guest/dashboard');
  });

  it('submit() — should navigate to /admin/dashboard for Admin role', () => {
    const navigateSpy = spyOn(router, 'navigateByUrl');
    authSpy.getRedirectUrl.and.returnValue('/admin/dashboard');
    component.form.patchValue({ email: 'admin@hotel.com', password: 'pass123' });

    component.submit();

    expect(navigateSpy).toHaveBeenCalledOnceWith('/admin/dashboard');
  });

  it('submit() — should navigate to /superadmin/dashboard for SuperAdmin role', () => {
    const navigateSpy = spyOn(router, 'navigateByUrl');
    authSpy.getRedirectUrl.and.returnValue('/superadmin/dashboard');
    component.form.patchValue({ email: 'sa@admin.com', password: 'pass123' });

    component.submit();

    expect(navigateSpy).toHaveBeenCalledOnceWith('/superadmin/dashboard');
  });

  it('submit() — should reset isLoading to false on complete', () => {
    component.form.patchValue({ email: 'thanush@test.com', password: 'pass123' });

    component.submit();

    expect(component.isLoading()).toBeFalse();
  });

  // ── submit() — IN-FLIGHT ───────────────────────────────────────────────────

  it('submit() — should set isLoading to true during in-flight request', () => {
    const subject = new Subject<{ token: string }>();
    authSpy.login.and.returnValue(subject.asObservable());
    component.form.patchValue({ email: 'thanush@test.com', password: 'pass123' });

    component.submit();

    expect(component.isLoading()).toBeTrue();

    subject.next({ token: 'mock-token' });
    subject.complete();
  });

  // ── submit() — INVALID FORM ────────────────────────────────────────────────

  it('submit() — should NOT call auth.login when form is invalid', () => {
    // form is empty (invalid) by default
    component.submit();

    expect(authSpy.login).not.toHaveBeenCalled();
  });

  it('submit() — should mark all fields as touched when form is invalid', () => {
    component.submit();

    expect(component.form.get('email')?.touched).toBeTrue();
    expect(component.form.get('password')?.touched).toBeTrue();
  });

  it('submit() — should NOT show toast when form is invalid', () => {
    component.submit();

    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  it('submit() — should NOT navigate when form is invalid', () => {
    const navigateSpy = spyOn(router, 'navigateByUrl');

    component.submit();

    expect(navigateSpy).not.toHaveBeenCalled();
  });

  // ── submit() — ERROR ───────────────────────────────────────────────────────

  it('submit() — should reset isLoading to false on API error', () => {
    authSpy.login.and.returnValue(throwError(() => new Error('Invalid credentials')));
    component.form.patchValue({ email: 'wrong@test.com', password: 'wrongpass' });

    component.submit();

    expect(component.isLoading()).toBeFalse();
  });

  it('submit() — should NOT show success toast on API error', () => {
    authSpy.login.and.returnValue(throwError(() => new Error('Invalid credentials')));
    component.form.patchValue({ email: 'wrong@test.com', password: 'wrongpass' });

    component.submit();

    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  it('submit() — should NOT navigate on API error', () => {
    const navigateSpy = spyOn(router, 'navigateByUrl');
    authSpy.login.and.returnValue(throwError(() => new Error('Invalid credentials')));
    component.form.patchValue({ email: 'wrong@test.com', password: 'wrongpass' });

    component.submit();

    expect(navigateSpy).not.toHaveBeenCalled();
  });

  // ── hidePassword SIGNAL ────────────────────────────────────────────────────

  it('hidePassword — toggling should switch between true and false', () => {
    expect(component.hidePassword()).toBeTrue();

    component.hidePassword.set(false);
    expect(component.hidePassword()).toBeFalse();

    component.hidePassword.set(true);
    expect(component.hidePassword()).toBeTrue();
  });
});