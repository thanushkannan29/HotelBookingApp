import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of, throwError, Subject } from 'rxjs';
import { GuestProfileComponent } from './guest-profile.component';
import { UserService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { UserProfileResponseDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ── Mock data ──────────────────────────────────────────────────────────────────

const MOCK_PROFILE: UserProfileResponseDto = {
  userId:      'usr-001',
  email:       'thanush@test.com',
  role:        'Guest',
  name:        'Thanush K',
  phoneNumber: '9840650390',
  address:     '1 Anna Nagar',
  state:       'Tamil Nadu',
  city:        'Chennai',
  pincode:     '600040',
  createdAt:   '2024-01-01T00:00:00Z',
  totalReviewPoints: 100,
};

const UPDATED_PROFILE: UserProfileResponseDto = {
  ...MOCK_PROFILE,
  name:        'Thanush Kumar',
  phoneNumber: '9876543210',
  city:        'Coimbatore',
};

// ─────────────────────────────────────────────────────────────────────────────

describe('GuestProfileComponent', () => {
  let component: GuestProfileComponent;
  let fixture:   ComponentFixture<GuestProfileComponent>;

  let userSpy:  jasmine.SpyObj<UserService>;
  let toastSpy: jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    userSpy  = jasmine.createSpyObj('UserService',  ['getProfile', 'updateProfile']);
    toastSpy = jasmine.createSpyObj('ToastService', ['success', 'error']);

    userSpy.getProfile.and.returnValue(of(MOCK_PROFILE));
    userSpy.updateProfile.and.returnValue(of(UPDATED_PROFILE));

    await TestBed.configureTestingModule({
      imports: [GuestProfileComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: UserService,  useValue: userSpy  },
        { provide: ToastService, useValue: toastSpy },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(GuestProfileComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── INITIAL SIGNAL STATE ───────────────────────────────────────────────────

  it('isEditing — should start as false', () => {
    expect(component.isEditing()).toBeFalse();
  });

  it('isSaving — should start as false', () => {
    expect(component.isSaving()).toBeFalse();
  });

  // ── ngOnInit ───────────────────────────────────────────────────────────────

  it('ngOnInit — should call getProfile on startup', () => {
    expect(userSpy.getProfile).toHaveBeenCalledOnceWith();
  });

  it('ngOnInit — should populate profile signal with API response', () => {
    expect(component.profile()).not.toBeNull();
    expect(component.profile()?.name).toBe('Thanush K');
    expect(component.profile()?.email).toBe('thanush@test.com');
    expect(component.profile()?.city).toBe('Chennai');
  });

  it('ngOnInit — should pre-fill form with profile values', () => {
    expect(component.form.get('name')?.value).toBe('Thanush K');
    expect(component.form.get('phoneNumber')?.value).toBe('9840650390');
    expect(component.form.get('address')?.value).toBe('1 Anna Nagar');
    expect(component.form.get('state')?.value).toBe('Tamil Nadu');
    expect(component.form.get('pincode')?.value).toBe('600040');
  });

  it('ngOnInit — should set cityControl value from profile', () => {
    expect(component.cityControl.value).toBe('Chennai');
  });

  it('ngOnInit — should set profileImageUrl to empty string when undefined', () => {
    // MOCK_PROFILE has no profileImageUrl → should be patched as ''
    expect(component.form.get('profileImageUrl')?.value).toBe('');
  });

  it('ngOnInit — should set profileImageUrl when profile has one', async () => {
    const profileWithImage: UserProfileResponseDto = {
      ...MOCK_PROFILE,
      profileImageUrl: 'https://example.com/avatar.jpg'
    };
    userSpy.getProfile.and.returnValue(of(profileWithImage));

    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [GuestProfileComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: UserService,  useValue: userSpy  },
        { provide: ToastService, useValue: toastSpy },
      ]
    }).compileComponents();

    const f   = TestBed.createComponent(GuestProfileComponent);
    const cmp = f.componentInstance;
    f.detectChanges();

    expect(cmp.form.get('profileImageUrl')?.value)
      .toBe('https://example.com/avatar.jpg');
  });

  // ── FORM VALIDATION ────────────────────────────────────────────────────────

  it('form — should be valid by default (no required fields)', () => {
    expect(component.form.valid).toBeTrue();
  });

  it('form — should be invalid when phoneNumber exceeds 15 characters', () => {
    component.form.get('phoneNumber')?.setValue('1234567890123456'); // 16 chars
    expect(component.form.get('phoneNumber')?.invalid).toBeTrue();
  });

  it('form — should be valid when phoneNumber is exactly 15 characters', () => {
    component.form.get('phoneNumber')?.setValue('123456789012345');
    expect(component.form.get('phoneNumber')?.valid).toBeTrue();
  });

  it('form — all fields except phoneNumber have no validators', () => {
    component.form.patchValue({
      name: '', address: '', state: '', pincode: ''
    });
    expect(component.form.valid).toBeTrue();
  });

  // ── save() — HAPPY PATH ────────────────────────────────────────────────────

  it('save() — should call updateProfile with current form values', () => {
    component.form.patchValue({ name: 'Thanush Kumar' });
    component.cityControl.setValue('Coimbatore');

    component.save();

    expect(userSpy.updateProfile).toHaveBeenCalledOnceWith(
      jasmine.objectContaining({ name: 'Thanush Kumar', city: 'Coimbatore' })
    );
  });

  it('save() — should update profile signal with the response', () => {
    component.save();

    expect(component.profile()?.name).toBe('Thanush Kumar');
    expect(component.profile()?.city).toBe('Coimbatore');
  });

  it('save() — should set isEditing to false after success', () => {
    component.isEditing.set(true);

    component.save();

    expect(component.isEditing()).toBeFalse();
  });

  it('save() — should reset isSaving to false after success', () => {
    component.save();

    expect(component.isSaving()).toBeFalse();
  });

  it('save() — should show success toast after success', () => {
    component.save();

    expect(toastSpy.success)
      .toHaveBeenCalledOnceWith('Profile updated successfully.');
  });

  it('save() — should set isSaving to true during in-flight request', () => {
    const subject = new Subject<UserProfileResponseDto>();
    userSpy.updateProfile.and.returnValue(subject.asObservable());

    component.save();

    expect(component.isSaving()).toBeTrue();

    subject.next(UPDATED_PROFILE);
    subject.complete();
  });

  it('save() — should send form values including profileImageUrl', () => {
    component.form.patchValue({ profileImageUrl: 'https://example.com/new.jpg' });

    component.save();

    expect(userSpy.updateProfile).toHaveBeenCalledWith(
      jasmine.objectContaining({ profileImageUrl: 'https://example.com/new.jpg' })
    );
  });

  // ── save() — ERROR ─────────────────────────────────────────────────────────

  it('save() — should reset isSaving to false on API error', () => {
    userSpy.updateProfile.and.returnValue(
      throwError(() => new Error('Server error'))
    );

    component.save();

    expect(component.isSaving()).toBeFalse();
  });

  it('save() — should NOT show success toast on API error', () => {
    userSpy.updateProfile.and.returnValue(
      throwError(() => new Error('Server error'))
    );

    component.save();

    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  it('save() — should NOT update profile signal on API error', () => {
    userSpy.updateProfile.and.returnValue(
      throwError(() => new Error('Server error'))
    );
    const originalName = component.profile()?.name;

    component.save();

    expect(component.profile()?.name).toBe(originalName);
  });

  it('save() — should NOT set isEditing to false on API error', () => {
    userSpy.updateProfile.and.returnValue(
      throwError(() => new Error('Server error'))
    );
    component.isEditing.set(true);

    component.save();

    expect(component.isEditing()).toBeTrue();
  });

  // ── isEditing SIGNAL ───────────────────────────────────────────────────────

  it('isEditing — toggling should switch between true and false', () => {
    expect(component.isEditing()).toBeFalse();

    component.isEditing.set(true);
    expect(component.isEditing()).toBeTrue();

    component.isEditing.set(false);
    expect(component.isEditing()).toBeFalse();
  });
});