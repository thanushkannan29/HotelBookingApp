import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { NotFoundComponent } from './not-found.component';
import { AuthService } from '../../core/services/auth.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ─────────────────────────────────────────────────────────────────────────────

describe('NotFoundComponent', () => {
  let component: NotFoundComponent;
  let fixture:   ComponentFixture<NotFoundComponent>;
  let authSpy:   jasmine.SpyObj<AuthService>;

  function buildAuthSpy(isAuth: boolean, redirectUrl = '/guest/dashboard') {
    const spy = jasmine.createSpyObj('AuthService', ['isAuthenticated', 'getRedirectUrl']);
    spy.isAuthenticated.and.returnValue(isAuth);
    spy.getRedirectUrl.and.returnValue(redirectUrl);
    return spy;
  }

  beforeEach(async () => {
    authSpy = buildAuthSpy(false); // unauthenticated by default

    await TestBed.configureTestingModule({
      imports: [NotFoundComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthService, useValue: authSpy },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(NotFoundComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── AUTH SERVICE INTEGRATION ───────────────────────────────────────────────

  it('auth — should expose the injected AuthService', () => {
    expect(component.auth).toBeTruthy();
  });

  // ── TEMPLATE — STATIC CONTENT ──────────────────────────────────────────────

  it('should display the 404 error code', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('404');
  });

  it('should display the "Page not found" heading', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Page not found');
  });

  it('should display the descriptive paragraph', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain("doesn't exist");
  });

  it('should always render the "Go Home" button', () => {
    const el    = fixture.nativeElement as HTMLElement;
    const links = Array.from(el.querySelectorAll('a')) as HTMLAnchorElement[];
    const home  = links.find(a => a.textContent?.includes('Go Home'));
    expect(home).toBeTruthy();
  });

  it('"Go Home" button should link to "/"', () => {
    const el    = fixture.nativeElement as HTMLElement;
    const links = Array.from(el.querySelectorAll('a')) as HTMLAnchorElement[];
    const home  = links.find(a => a.textContent?.includes('Go Home'));
    expect(home?.getAttribute('href')).toBe('/');
  });

  // ── TEMPLATE — UNAUTHENTICATED ─────────────────────────────────────────────

  it('should NOT show Dashboard button when user is not authenticated', () => {
    // authSpy.isAuthenticated returns false by default
    fixture.detectChanges();
    const el    = fixture.nativeElement as HTMLElement;
    const links = Array.from(el.querySelectorAll('a')) as HTMLAnchorElement[];
    const dash  = links.find(a => a.textContent?.includes('Dashboard'));
    expect(dash).toBeFalsy();
  });

  // ── TEMPLATE — AUTHENTICATED AS GUEST ─────────────────────────────────────

  it('should show Dashboard button when user is authenticated', async () => {
    authSpy = buildAuthSpy(true, '/guest/dashboard');

    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [NotFoundComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthService, useValue: authSpy },
      ]
    }).compileComponents();

    const f = TestBed.createComponent(NotFoundComponent);
    f.detectChanges();
    await f.whenStable();

    const el    = f.nativeElement as HTMLElement;
    const links = Array.from(el.querySelectorAll('a')) as HTMLAnchorElement[];
    const dash  = links.find(a => a.textContent?.includes('Dashboard'));
    expect(dash).toBeTruthy();
  });

  it('Dashboard button should link to /guest/dashboard for Guest role', async () => {
    authSpy = buildAuthSpy(true, '/guest/dashboard');

    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [NotFoundComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthService, useValue: authSpy },
      ]
    }).compileComponents();

    const f = TestBed.createComponent(NotFoundComponent);
    f.detectChanges();
    await f.whenStable();

    const el    = f.nativeElement as HTMLElement;
    const links = Array.from(el.querySelectorAll('a')) as HTMLAnchorElement[];
    const dash  = links.find(a => a.textContent?.includes('Dashboard'));
    expect(dash?.getAttribute('href')).toBe('/guest/dashboard');
  });

  it('Dashboard button should link to /admin/dashboard for Admin role', async () => {
    authSpy = buildAuthSpy(true, '/admin/dashboard');

    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [NotFoundComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthService, useValue: authSpy },
      ]
    }).compileComponents();

    const f = TestBed.createComponent(NotFoundComponent);
    f.detectChanges();
    await f.whenStable();

    const el    = f.nativeElement as HTMLElement;
    const links = Array.from(el.querySelectorAll('a')) as HTMLAnchorElement[];
    const dash  = links.find(a => a.textContent?.includes('Dashboard'));
    expect(dash?.getAttribute('href')).toBe('/admin/dashboard');
  });

  it('Dashboard button should link to /superadmin/dashboard for SuperAdmin role', async () => {
    authSpy = buildAuthSpy(true, '/superadmin/dashboard');

    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [NotFoundComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthService, useValue: authSpy },
      ]
    }).compileComponents();

    const f = TestBed.createComponent(NotFoundComponent);
    f.detectChanges();
    await f.whenStable();

    const el    = f.nativeElement as HTMLElement;
    const links = Array.from(el.querySelectorAll('a')) as HTMLAnchorElement[];
    const dash  = links.find(a => a.textContent?.includes('Dashboard'));
    expect(dash?.getAttribute('href')).toBe('/superadmin/dashboard');
  });

  // ── getRedirectUrl ─────────────────────────────────────────────────────────

  it('should call getRedirectUrl when authenticated to determine dashboard link', async () => {
    authSpy = buildAuthSpy(true, '/admin/dashboard');

    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [NotFoundComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthService, useValue: authSpy },
      ]
    }).compileComponents();

    const f = TestBed.createComponent(NotFoundComponent);
    f.detectChanges();
    await f.whenStable();

    expect(authSpy.getRedirectUrl).toHaveBeenCalled();
  });
});