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

describe('RegisterAdminComponent', () => {
  let component: RegisterAdminComponent;
  let fixture: ComponentFixture<RegisterAdminComponent>;
  let authSpy: jasmine.SpyObj<AuthService>;
  let toastSpy: jasmine.SpyObj<ToastService>;
  let router: Router;

  // Password must have uppercase, digit, special char, min 8
  const VALID_ADMIN = {
    name:     'Thanush K',
    email:    'thanush@hotel.com',
    password: 'Pass123!',
  };

  const VALID_HOTEL = {
    hotelName:     'Grand Palace',
    address:       '1 MG Road',
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
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthService,  useValue: authSpy },
        { provide: ToastService, useValue: toastSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterAdminComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── Initial state ─────────────────────────────────────────────────────────

  it('hidePassword — should start as true', () => expect(component.hidePassword()).toBeTrue());
  it('isLoading — should start as false', () => expect(component.isLoading()).toBeFalse());

  // ── Form initial state ────────────────────────────────────────────────────

  it('adminForm — should be invalid initially', () => expect(component.adminForm.invalid).toBeTrue());
  it('hotelForm — should be invalid initially', () => expect(component.hotelForm.invalid).toBeTrue());
  it('cityControl — should be invalid initially', () => expect(component.cityControl.invalid).toBeTrue());

  // ── adminForm validation ──────────────────────────────────────────────────

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

  it('adminForm — should be invalid when password is too short', () => {
    component.adminForm.patchValue({ ...VALID_ADMIN, password: 'Ab1!' });
    expect(component.adminForm.get('password')?.invalid).toBeTrue();
  });

  it('adminForm — should be invalid when password has no uppercase', () => {
    component.adminForm.patchValue({ ...VALID_ADMIN, password: 'pass123!' });
    expect(component.adminForm.get('password')?.invalid).toBeTrue();
  });

  // ── hotelForm validation ──────────────────────────────────────────────────

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

  it('hotelForm — should be invalid when contactNumber is empty', () => {
    component.hotelForm.patchValue({ ...VALID_HOTEL, contactNumber: '' });
    expect(component.hotelForm.invalid).toBeTrue();
  });

  it('hotelForm — description is optional', () => {
    component.hotelForm.patchValue({ ...VALID_HOTEL, description: '' });
    expect(component.hotelForm.valid).toBeTrue();
  });

  // ── submit — happy path ───────────────────────────────────────────────────

  it('submit — should call registerHotelAdmin with merged form values', () => {
    component.adminForm.patchValue(VALID_ADMIN);
    component.hotelForm.patchValue(VALID_HOTEL);
    component.cityControl.setValue('Chennai');

    component.submit();

    expect(authSpy.registerHotelAdmin).toHaveBeenCalledOnceWith(
      jasmine.objectContaining({
        name: 'Thanush K', email: 'thanush@hotel.com',
        hotelName: 'Grand Palace', city: 'Chennai',
      })
    );
  });

  it('submit — should show success toast', () => {
    component.adminForm.patchValue(VALID_ADMIN);
    component.hotelForm.patchValue(VALID_HOTEL);
    component.cityControl.setValue('Chennai');

    component.submit();

    expect(toastSpy.success).toHaveBeenCalledWith('Hotel registered! Your dashboard is ready.');
  });

  it('submit — should navigate to /admin/dashboard', () => {
    const navigateSpy = spyOn(router, 'navigate');
    component.adminForm.patchValue(VALID_ADMIN);
    component.hotelForm.patchValue(VALID_HOTEL);
    component.cityControl.setValue('Chennai');

    component.submit();

    expect(navigateSpy).toHaveBeenCalledWith(['/admin/dashboard']);
  });

  it('submit — should reset isLoading to false on complete', () => {
    component.adminForm.patchValue(VALID_ADMIN);
    component.hotelForm.patchValue(VALID_HOTEL);
    component.cityControl.setValue('Chennai');

    component.submit();

    expect(component.isLoading()).toBeFalse();
  });

  // ── submit — invalid form ─────────────────────────────────────────────────

  it('submit — should NOT call service when forms are invalid', () => {
    component.submit();
    expect(authSpy.registerHotelAdmin).not.toHaveBeenCalled();
  });

  it('submit — should mark all forms touched when invalid', () => {
    component.submit();
    expect(component.adminForm.get('name')?.touched).toBeTrue();
    expect(component.hotelForm.get('hotelName')?.touched).toBeTrue();
    expect(component.cityControl.touched).toBeTrue();
  });

  it('submit — should NOT call service when cityControl is invalid', () => {
    component.adminForm.patchValue(VALID_ADMIN);
    component.hotelForm.patchValue(VALID_HOTEL);
    // cityControl left empty (invalid)
    component.submit();
    expect(authSpy.registerHotelAdmin).not.toHaveBeenCalled();
  });

  // ── submit — error ────────────────────────────────────────────────────────

  it('submit — should reset isLoading to false on error', () => {
    authSpy.registerHotelAdmin.and.returnValue(throwError(() => new Error('fail')));
    component.adminForm.patchValue(VALID_ADMIN);
    component.hotelForm.patchValue(VALID_HOTEL);
    component.cityControl.setValue('Chennai');

    component.submit();

    expect(component.isLoading()).toBeFalse();
  });

  it('submit — should NOT show success toast on error', () => {
    authSpy.registerHotelAdmin.and.returnValue(throwError(() => new Error('fail')));
    component.adminForm.patchValue(VALID_ADMIN);
    component.hotelForm.patchValue(VALID_HOTEL);
    component.cityControl.setValue('Chennai');

    component.submit();

    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  // ── hidePassword ──────────────────────────────────────────────────────────

  it('hidePassword — toggling should switch between true and false', () => {
    component.hidePassword.set(false);
    expect(component.hidePassword()).toBeFalse();
    component.hidePassword.set(true);
    expect(component.hidePassword()).toBeTrue();
  });
});
