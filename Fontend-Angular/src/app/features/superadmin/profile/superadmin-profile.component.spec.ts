import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { SuperAdminProfileComponent } from './superadmin-profile.component';
import { UserService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/services/auth.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

const MOCK_PROFILE = {
  userId: 'u-001', name: 'Super Admin', email: 'super@admin.com',
  profileImageUrl: 'https://example.com/img.jpg', role: 'SuperAdmin'
};

describe('SuperAdminProfileComponent', () => {
  let component: SuperAdminProfileComponent;
  let fixture: ComponentFixture<SuperAdminProfileComponent>;
  let userSpy: jasmine.SpyObj<UserService>;
  let toastSpy: jasmine.SpyObj<ToastService>;
  let authSpy: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    userSpy  = jasmine.createSpyObj('UserService', ['getProfile', 'updateProfile']);
    toastSpy = jasmine.createSpyObj('ToastService', ['success', 'error']);
    authSpy  = jasmine.createSpyObj('AuthService', ['updateProfileImage', 'updateUserName'], {
      currentUser: () => ({ userName: 'Super Admin', role: 'SuperAdmin' })
    });

    userSpy.getProfile.and.returnValue(of(MOCK_PROFILE as any));
    userSpy.updateProfile.and.returnValue(of({ ...MOCK_PROFILE, name: 'Updated Admin' } as any));

    await TestBed.configureTestingModule({
      imports: [SuperAdminProfileComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]),
        { provide: UserService,  useValue: userSpy },
        { provide: ToastService, useValue: toastSpy },
        { provide: AuthService,  useValue: authSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(SuperAdminProfileComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should call getProfile', () => {
    expect(userSpy.getProfile).toHaveBeenCalled();
  });

  it('ngOnInit — should populate profile signal', () => {
    expect(component.profile()?.name).toBe('Super Admin');
    expect(component.profile()?.email).toBe('super@admin.com');
  });

  it('ngOnInit — should patch form with profile values', () => {
    expect(component.form.get('name')?.value).toBe('Super Admin');
  });

  it('ngOnInit — should call updateProfileImage', () => {
    expect(authSpy.updateProfileImage).toHaveBeenCalled();
  });

  // ── Initial state ─────────────────────────────────────────────────────────

  it('isEditing — should start as false', () => expect(component.isEditing()).toBeFalse());
  it('isSaving — should start as false',  () => expect(component.isSaving()).toBeFalse());

  // ── save ──────────────────────────────────────────────────────────────────

  it('save — should call updateProfile', () => {
    component.save();
    expect(userSpy.updateProfile).toHaveBeenCalled();
  });

  it('save — should update profile signal on success', () => {
    component.save();
    expect(component.profile()?.name).toBe('Updated Admin');
  });

  it('save — should set isEditing to false on success', () => {
    component.isEditing.set(true);
    component.save();
    expect(component.isEditing()).toBeFalse();
  });

  it('save — should show success toast', () => {
    component.save();
    expect(toastSpy.success).toHaveBeenCalledWith('Profile updated.');
  });

  it('save — should call updateUserName on success', () => {
    component.save();
    expect(authSpy.updateUserName).toHaveBeenCalledWith('Updated Admin');
  });

  it('save — should reset isSaving to false on error', () => {
    userSpy.updateProfile.and.returnValue(throwError(() => new Error('fail')));
    component.save();
    expect(component.isSaving()).toBeFalse();
  });
});
