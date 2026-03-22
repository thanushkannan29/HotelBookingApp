import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { Router } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { RegisterAdminComponent } from './register-admin.component';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ─────────────────────────────────────────────────────────────────────────────

describe('RegisterAdminComponent', () => {
  let component: RegisterAdminComponent;
  let fixture:   ComponentFixture<RegisterAdminComponent>;

  let authSpy:  jasmine.SpyObj<AuthService>;
  let toastSpy: jasmine.SpyObj<ToastService>;
  let router:   Router;

  // ── Valid form values ──────────────────────────────────────────────────────
  const VALID_ADMIN = {
    name:     'Thanush K',
    email:    'thanush@hotel.com',
    password: 'pass123',
  };

  const VALID_HOTEL = {
    hotelName:     'Grand Palace',
    address:       '1 MG Road',
    city:          'Chennai',
    description:   'A luxury hotel',
    contactNumber: '9840650390',
  };

  beforeEach(async () => {
    authSpy  = jasmine.createSpyObj('AuthService', ['registerHotelAdmin']);
    toastSpy = jasmine.createSpyObj('ToastService', ['success', 'error']);

    authSpy.registerHotelAdmin.and.returnValue(of({ token: 'mock-token' }));

    await TestBed.configureTestingModule({
      imports: [RegisterAdminComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthService,  useValue: authSpy  },
        { provide: ToastService, useValue: toastSpy },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(RegisterAdminComponent);
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

  it('adminForm — should be invalid initially', () => {
    expect(component.adminForm.invalid).toBeTrue();
  });

  it('hotelForm — should be invalid initially', () => {
    expect(component.hotelForm.invalid).toBeTrue();
  });

  it('adminForm — all fields should start empty', () => {
    expect(component.adminForm.get('name')?.value).toBe('');
    expect(component.adminForm.get('email')?.value).toBe('');
    expect(component.adminForm.get('password')?.value).toBe('');
  });

  it('hotelForm — all fields should start empty', () => {
    expect(component.hotelForm.get('hotelName')?.value).toBe('');
    expect(component.hotelForm.get('city')?.value).toBe('');
    expect(component.hotelForm.get('contactNumber')?.value).toBe('');
  });

  // ── FORM VALIDATION — adminForm ────────────────────────────────────────────

  it('adminForm — should be valid when all required fields are filled', () => {
    component.adminForm.patchValue(VALID_ADMIN);
    expect(component.adminForm.valid).toBeTrue();
  });

  it('adminForm — should be invalid when name is empty', () => {
    component.adminForm.patchValue({ ...VALID_ADMIN, name: '' });
    expect(component.adminForm.invalid).toBeTrue();
  });

  it('adminForm — should be invalid when email format is wrong', () => {
    component.adminForm.patchValue({ ...VALID_ADMIN, email: 'not-valid' });
    expect(component.adminForm.get('email')?.invalid).toBeTrue();
  });

  it('adminForm — should be invalid when email is empty', () => {
    component.adminForm.patchValue({ ...VALID_ADMIN, email: '' });
    expect(component.adminForm.invalid).toBeTrue();
  });

  it('adminForm — should be invalid when password is shorter than 6 chars', () => {
    component.adminForm.patchValue({ ...VALID_ADMIN, password: '123' });
    expect(component.adminForm.get('password')?.invalid).toBeTrue();
  });

  it('adminForm — should be valid when password is exactly 6 characters', () => {
    component.adminForm.patchValue({ ...VALID_ADMIN, password: '123456' });
    expect(component.adminForm.valid).toBeTrue();
  });

  // ── FORM VALIDATION — hotelForm ────────────────────────────────────────────

  it('hotelForm — should be valid when all required fields are filled', () => {
    component.hotelForm.patchValue(VALID_HOTEL);
    expect(component.hotelForm.valid).toBeTrue();
  });

  it('hotelForm — should be invalid when hotelName is empty', () => {
    component.hotelForm.patchValue({ ...VALID_HOTEL, hotelName: '' });
    expect(component.hotelForm.invalid).toBeTrue();
  });

  it('hotelForm — should be invalid when address is empty', () => {
    component.hotelForm.patchValue({ ...VALID_HOTEL, address: '' });
    expect(component.hotelForm.invalid).toBeTrue();
  });

  it('hotelForm — should be invalid when city is empty', () => {
    component.hotelForm.patchValue({ ...VALID_HOTEL, city: '' });
    expect(component.hotelForm.invalid).toBeTrue();
  });

  it('hotelForm — should be invalid when contactNumber is empty', () => {
    component.hotelForm.patchValue({ ...VALID_HOTEL, contactNumber: '' });
    expect(component.hotelForm.invalid).toBeTrue();
  });

  it('hotelForm — should be invalid when contactNumber exceeds 15 characters', () => {
    component.hotelForm.patchValue({ ...VALID_HOTEL, contactNumber: '1234567890123456' });
    expect(component.hotelForm.get('contactNumber')?.invalid).toBeTrue();
  });

  it('hotelForm — description is optional (form still valid without it)', () => {
    component.hotelForm.patchValue({ ...VALID_HOTEL, description: '' });
    expect(component.hotelForm.valid).toBeTrue();
  });

  // ── submit() — HAPPY PATH ──────────────────────────────────────────────────

  it('submit() — should call registerHotelAdmin with merged form values', () => {
    component.adminForm.patchValue(VALID_ADMIN);
    component.hotelForm.patchValue(VALID_HOTEL);

    component.submit();

    expect(authSpy.registerHotelAdmin).toHaveBeenCalledOnceWith(
      jasmine.objectContaining({
        name:          'Thanush K',
        email:         'thanush@hotel.com',
        password:      'pass123',
        hotelName:     'Grand Palace',
        city:          'Chennai',
        contactNumber: '9840650390',
      })
    );
  });

  it('submit() — should merge both forms into a single payload', () => {
    component.adminForm.patchValue(VALID_ADMIN);
    component.hotelForm.patchValue(VALID_HOTEL);

    component.submit();

    const payload = authSpy.registerHotelAdmin.calls.mostRecent().args[0];
    // Admin fields present
    expect(payload.name).toBe('Thanush K');
    expect(payload.email).toBe('thanush@hotel.com');
    // Hotel fields present
    expect(payload.hotelName).toBe('Grand Palace');
    expect(payload.address).toBe('1 MG Road');
  });

  it('submit() — should show success toast on success', () => {
    component.adminForm.patchValue(VALID_ADMIN);
    component.hotelForm.patchValue(VALID_HOTEL);

    component.submit();

    expect(toastSpy.success)
      .toHaveBeenCalledOnceWith('Hotel registered! Your dashboard is ready.');
  });

  it('submit() — should navigate to /admin/dashboard on success', () => {
    const navigateSpy = spyOn(router, 'navigate');
    component.adminForm.patchValue(VALID_ADMIN);
    component.hotelForm.patchValue(VALID_HOTEL);

    component.submit();

    expect(navigateSpy).toHaveBeenCalledOnceWith(['/admin/dashboard']);
  });

  it('submit() — should reset isLoading to false on complete', () => {
    component.adminForm.patchValue(VALID_ADMIN);
    component.hotelForm.patchValue(VALID_HOTEL);

    component.submit();

    expect(component.isLoading()).toBeFalse();
  });

  // ── submit() — IN-FLIGHT ───────────────────────────────────────────────────

  it('submit() — should set isLoading to true during in-flight request', () => {
    const subject = new Subject<{ token: string }>();
    authSpy.registerHotelAdmin.and.returnValue(subject.asObservable());
    component.adminForm.patchValue(VALID_ADMIN);
    component.hotelForm.patchValue(VALID_HOTEL);

    component.submit();

    expect(component.isLoading()).toBeTrue();

    subject.next({ token: 'mock-token' });
    subject.complete();
  });

  // ── submit() — INVALID FORM ────────────────────────────────────────────────

  it('submit() — should NOT call service when adminForm is invalid', () => {
    // both forms empty by default
    component.submit();
    expect(authSpy.registerHotelAdmin).not.toHaveBeenCalled();
  });

  it('submit() — should NOT call service when hotelForm is invalid', () => {
    component.adminForm.patchValue(VALID_ADMIN); // admin valid
    // hotelForm still empty
    component.submit();
    expect(authSpy.registerHotelAdmin).not.toHaveBeenCalled();
  });

  it('submit() — should NOT call service when adminForm is invalid but hotelForm is valid', () => {
    component.hotelForm.patchValue(VALID_HOTEL); // hotel valid
    // adminForm still empty
    component.submit();
    expect(authSpy.registerHotelAdmin).not.toHaveBeenCalled();
  });

  it('submit() — should mark all adminForm fields as touched when invalid', () => {
    component.submit();
    expect(component.adminForm.get('name')?.touched).toBeTrue();
    expect(component.adminForm.get('email')?.touched).toBeTrue();
    expect(component.adminForm.get('password')?.touched).toBeTrue();
  });

  it('submit() — should mark all hotelForm fields as touched when invalid', () => {
    component.submit();
    expect(component.hotelForm.get('hotelName')?.touched).toBeTrue();
    expect(component.hotelForm.get('address')?.touched).toBeTrue();
    expect(component.hotelForm.get('city')?.touched).toBeTrue();
    expect(component.hotelForm.get('contactNumber')?.touched).toBeTrue();
  });

  it('submit() — should NOT show success toast when forms are invalid', () => {
    component.submit();
    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  it('submit() — should NOT navigate when forms are invalid', () => {
    const navigateSpy = spyOn(router, 'navigate');
    component.submit();
    expect(navigateSpy).not.toHaveBeenCalled();
  });

  // ── submit() — ERROR ───────────────────────────────────────────────────────

  it('submit() — should reset isLoading to false on API error', () => {
    authSpy.registerHotelAdmin.and.returnValue(
      throwError(() => new Error('Email already registered'))
    );
    component.adminForm.patchValue(VALID_ADMIN);
    component.hotelForm.patchValue(VALID_HOTEL);

    component.submit();

    expect(component.isLoading()).toBeFalse();
  });

  it('submit() — should NOT show success toast on API error', () => {
    authSpy.registerHotelAdmin.and.returnValue(
      throwError(() => new Error('Email already registered'))
    );
    component.adminForm.patchValue(VALID_ADMIN);
    component.hotelForm.patchValue(VALID_HOTEL);

    component.submit();

    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  it('submit() — should NOT navigate on API error', () => {
    const navigateSpy = spyOn(router, 'navigate');
    authSpy.registerHotelAdmin.and.returnValue(
      throwError(() => new Error('Email already registered'))
    );
    component.adminForm.patchValue(VALID_ADMIN);
    component.hotelForm.patchValue(VALID_HOTEL);

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