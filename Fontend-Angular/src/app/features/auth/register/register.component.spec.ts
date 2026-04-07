import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { Router } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { RegisterComponent } from './register.component';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ─────────────────────────────────────────────────────────────────────────────

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture:   ComponentFixture<RegisterComponent>;

  let authSpy:  jasmine.SpyObj<AuthService>;
  let toastSpy: jasmine.SpyObj<ToastService>;
  let router:   Router;

  const VALID_FORM = {
    name:     'Thanush K',
    email:    'thanush@test.com',
    password: 'Pass123!',
  };

  beforeEach(async () => {
    authSpy  = jasmine.createSpyObj('AuthService', ['registerGuest']);
    toastSpy = jasmine.createSpyObj('ToastService', ['success', 'error']);

    authSpy.registerGuest.and.returnValue(of({ token: 'mock-token' }));

    await TestBed.configureTestingModule({
      imports: [RegisterComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthService,  useValue: authSpy  },
        { provide: ToastService, useValue: toastSpy },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(RegisterComponent);
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

  it('form — all fields should start empty', () => {
    expect(component.form.get('name')?.value).toBe('');
    expect(component.form.get('email')?.value).toBe('');
    expect(component.form.get('password')?.value).toBe('');
  });

  // ── FORM VALIDATION ────────────────────────────────────────────────────────

  it('form — should be valid when all fields are correctly filled', () => {
    component.form.patchValue(VALID_FORM);
    expect(component.form.valid).toBeTrue();
  });

  it('form — should be invalid when name is empty', () => {
    component.form.patchValue({ ...VALID_FORM, name: '' });
    expect(component.form.invalid).toBeTrue();
  });

  it('form — should be invalid when name is only 1 character (minLength 2)', () => {
    component.form.patchValue({ ...VALID_FORM, name: 'TK' }); // minLength is 3
    expect(component.form.get('name')?.invalid).toBeTrue();
  });

  it('form — should be valid when name is exactly 2 characters', () => {
    component.form.patchValue({ ...VALID_FORM, name: 'TKL' }); // minLength is 3
    expect(component.form.valid).toBeTrue();
  });

  it('form — should be invalid when email is empty', () => {
    component.form.patchValue({ ...VALID_FORM, email: '' });
    expect(component.form.invalid).toBeTrue();
  });

  it('form — should be invalid when email format is wrong', () => {
    component.form.patchValue({ ...VALID_FORM, email: 'not-an-email' });
    expect(component.form.get('email')?.invalid).toBeTrue();
  });

  it('form — should be invalid when email has no domain', () => {
    component.form.patchValue({ ...VALID_FORM, email: 'thanush@' });
    expect(component.form.get('email')?.invalid).toBeTrue();
  });

  it('form — should be invalid when password is empty', () => {
    component.form.patchValue({ ...VALID_FORM, password: '' });
    expect(component.form.invalid).toBeTrue();
  });

  it('form — should be invalid when password is less than 6 characters', () => {
    component.form.patchValue({ ...VALID_FORM, password: 'Ab1!' }); // less than 8
    expect(component.form.get('password')?.invalid).toBeTrue();
  });

  it('form — should be valid when password is exactly 6 characters', () => {
    component.form.patchValue({ ...VALID_FORM, password: 'Pass1!' }); // 6 chars but needs 8
    expect(component.form.get('password')?.invalid).toBeTrue(); // still invalid (minLength 8)
  });

  // ── submit() — HAPPY PATH ──────────────────────────────────────────────────

  it('submit() — should call registerGuest with form values', () => {
    component.form.patchValue(VALID_FORM);

    component.submit();

    expect(authSpy.registerGuest).toHaveBeenCalledOnceWith(
      jasmine.objectContaining({
        name:     'Thanush K',
        email:    'thanush@test.com',
        password: 'Pass123!',
      })
    );
  });

  it('submit() — should show "Account created! Welcome aboard." toast on success', () => {
    component.form.patchValue(VALID_FORM);

    component.submit();

    expect(toastSpy.success)
      .toHaveBeenCalledOnceWith('Account created! Welcome aboard.');
  });

  it('submit() — should navigate to /guest/dashboard on success', () => {
    const navigateSpy = spyOn(router, 'navigate');
    component.form.patchValue(VALID_FORM);

    component.submit();

    expect(navigateSpy).toHaveBeenCalledOnceWith(['/guest/dashboard']);
  });

  it('submit() — should reset isLoading to false on complete', () => {
    component.form.patchValue(VALID_FORM);

    component.submit();

    expect(component.isLoading()).toBeFalse();
  });

  // ── submit() — IN-FLIGHT ───────────────────────────────────────────────────

  it('submit() — should set isLoading to true during in-flight request', () => {
    const subject = new Subject<{ token: string }>();
    authSpy.registerGuest.and.returnValue(subject.asObservable());
    component.form.patchValue(VALID_FORM);

    component.submit();

    expect(component.isLoading()).toBeTrue();

    subject.next({ token: 'mock-token' });
    subject.complete();
  });

  // ── submit() — INVALID FORM ────────────────────────────────────────────────

  it('submit() — should NOT call registerGuest when form is invalid', () => {
    component.submit();
    expect(authSpy.registerGuest).not.toHaveBeenCalled();
  });

  it('submit() — should mark all fields as touched when form is invalid', () => {
    component.submit();

    expect(component.form.get('name')?.touched).toBeTrue();
    expect(component.form.get('email')?.touched).toBeTrue();
    expect(component.form.get('password')?.touched).toBeTrue();
  });

  it('submit() — should NOT show toast when form is invalid', () => {
    component.submit();
    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  it('submit() — should NOT navigate when form is invalid', () => {
    const navigateSpy = spyOn(router, 'navigate');

    component.submit();

    expect(navigateSpy).not.toHaveBeenCalled();
  });

  // ── submit() — ERROR ───────────────────────────────────────────────────────

  it('submit() — should reset isLoading to false on API error', () => {
    authSpy.registerGuest.and.returnValue(
      throwError(() => new Error('Email already registered'))
    );
    component.form.patchValue(VALID_FORM);

    component.submit();

    expect(component.isLoading()).toBeFalse();
  });

  it('submit() — should NOT show success toast on API error', () => {
    authSpy.registerGuest.and.returnValue(
      throwError(() => new Error('Email already registered'))
    );
    component.form.patchValue(VALID_FORM);

    component.submit();

    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  it('submit() — should NOT navigate on API error', () => {
    const navigateSpy = spyOn(router, 'navigate');
    authSpy.registerGuest.and.returnValue(
      throwError(() => new Error('Email already registered'))
    );
    component.form.patchValue(VALID_FORM);

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

  // ── BOTH COMPLETE AND ERROR RESET isLoading ────────────────────────────────

  it('isLoading — should be false after successful registration completes', () => {
    component.form.patchValue(VALID_FORM);

    component.submit();

    // complete fires after next in of() — isLoading must be false
    expect(component.isLoading()).toBeFalse();
  });

  it('isLoading — should be false after API error', () => {
    authSpy.registerGuest.and.returnValue(
      throwError(() => new Error('fail'))
    );
    component.form.patchValue(VALID_FORM);

    component.submit();

    expect(component.isLoading()).toBeFalse();
  });
});