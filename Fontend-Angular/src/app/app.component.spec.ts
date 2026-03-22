import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { AppComponent } from './app.component';
import { AuthService } from './core/services/auth.service';
import { LoadingService } from './core/services/loading.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

describe('AppComponent', () => {
  let component: AppComponent;
  let fixture:   ComponentFixture<AppComponent>;
  let router:    Router;

  let authSpy:    jasmine.SpyObj<AuthService>;
  let loadingSpy: jasmine.SpyObj<LoadingService>;

  beforeEach(async () => {
    authSpy = jasmine.createSpyObj('AuthService', [
      'isAuthenticated', 'isGuest', 'isAdmin', 'isSuperAdmin', 'logout'
    ], {
      currentUser: () => null
    });
    authSpy.isAuthenticated.and.returnValue(false);
    authSpy.isGuest.and.returnValue(false);
    authSpy.isAdmin.and.returnValue(false);
    authSpy.isSuperAdmin.and.returnValue(false);

    loadingSpy = jasmine.createSpyObj('LoadingService', ['show', 'hide'], {
      isLoading: jasmine.createSpy('isLoading').and.returnValue(false)
    });

    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: '**', component: AppComponent }]),
        { provide: AuthService,    useValue: authSpy    },
        { provide: LoadingService, useValue: loadingSpy },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(AppComponent);
    component = fixture.componentInstance;
    router    = TestBed.inject(Router);
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── showChrome COMPUTED ────────────────────────────────────────────────────

  it('showChrome — should return true for root URL "/"', async () => {
    await router.navigate(['/']);
    fixture.detectChanges();
    expect(component.showChrome()).toBeTrue();
  });

  it('showChrome — should return true for "/hotels"', async () => {
    await router.navigate(['/hotels']);
    fixture.detectChanges();
    expect(component.showChrome()).toBeTrue();
  });

  it('showChrome — should return true for "/contact"', async () => {
    await router.navigate(['/contact']);
    fixture.detectChanges();
    expect(component.showChrome()).toBeTrue();
  });

  it('showChrome — should return true for "/guest/dashboard"', async () => {
    await router.navigate(['/guest/dashboard']);
    fixture.detectChanges();
    expect(component.showChrome()).toBeTrue();
  });

  it('showChrome — should return true for "/admin/dashboard"', async () => {
    await router.navigate(['/admin/dashboard']);
    fixture.detectChanges();
    expect(component.showChrome()).toBeTrue();
  });

  it('showChrome — should return false for "/auth/login"', async () => {
    await router.navigate(['/auth/login']);
    fixture.detectChanges();
    expect(component.showChrome()).toBeFalse();
  });

  it('showChrome — should return false for "/auth/register"', async () => {
    await router.navigate(['/auth/register']);
    fixture.detectChanges();
    expect(component.showChrome()).toBeFalse();
  });

  it('showChrome — should return false for "/auth/register-admin"', async () => {
    await router.navigate(['/auth/register-admin']);
    fixture.detectChanges();
    expect(component.showChrome()).toBeFalse();
  });

  it('showChrome — any URL starting with /auth should return false', async () => {
    await router.navigate(['/auth/anything']);
    fixture.detectChanges();
    expect(component.showChrome()).toBeFalse();
  });

  // ── TEMPLATE — SPINNER ─────────────────────────────────────────────────────

  it('should always render app-spinner regardless of route', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('app-spinner')).toBeTruthy();
  });

  // ── TEMPLATE — NAVBAR / FOOTER ─────────────────────────────────────────────

  it('should render app-navbar on non-auth routes', async () => {
    await router.navigate(['/hotels']);
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('app-navbar')).toBeTruthy();
  });

  it('should render app-footer on non-auth routes', async () => {
    await router.navigate(['/hotels']);
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('app-footer')).toBeTruthy();
  });

  it('should NOT render app-navbar on /auth/* routes', async () => {
    await router.navigate(['/auth/login']);
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('app-navbar')).toBeFalsy();
  });

  it('should NOT render app-footer on /auth/* routes', async () => {
    await router.navigate(['/auth/login']);
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('app-footer')).toBeFalsy();
  });

  // ── TEMPLATE — MAIN CLASS ──────────────────────────────────────────────────

  it('main — should NOT have auth-main class on non-auth routes', async () => {
    await router.navigate(['/hotels']);
    fixture.detectChanges();
    await fixture.whenStable();
    const main = fixture.nativeElement.querySelector('main') as HTMLElement;
    expect(main.classList.contains('auth-main')).toBeFalse();
  });

  it('main — should have auth-main class on /auth routes', async () => {
    await router.navigate(['/auth/login']);
    fixture.detectChanges();
    await fixture.whenStable();
    const main = fixture.nativeElement.querySelector('main') as HTMLElement;
    expect(main.classList.contains('auth-main')).toBeTrue();
  });

  // ── TEMPLATE — ROUTER OUTLET ───────────────────────────────────────────────

  it('should always render router-outlet', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('router-outlet')).toBeTruthy();
  });
});