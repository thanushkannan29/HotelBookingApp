import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { NavbarComponent } from './navbar.component';
import { AuthService } from '../../../core/services/auth.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ─────────────────────────────────────────────────────────────────────────────

describe('NavbarComponent', () => {
  let component: NavbarComponent;
  let fixture:   ComponentFixture<NavbarComponent>;
  let authSpy:   jasmine.SpyObj<AuthService>;

  function buildAuthSpy(role: 'Guest' | 'Admin' | 'SuperAdmin' | null) {
    const spy = jasmine.createSpyObj('AuthService', [
      'isAuthenticated', 'isGuest', 'isAdmin', 'isSuperAdmin', 'logout', 'updateProfileImage'
    ], {
      currentUser: () => role ? { userId: 'usr-001', userName: 'Thanush K', role } : null,
      profileImageUrl: jasmine.createSpy('profileImageUrl').and.returnValue(null),
      hotelImageUrl: jasmine.createSpy('hotelImageUrl').and.returnValue(null),
    });
    spy.isAuthenticated.and.returnValue(role !== null);
    spy.isGuest.and.returnValue(role === 'Guest');
    spy.isAdmin.and.returnValue(role === 'Admin');
    spy.isSuperAdmin.and.returnValue(role === 'SuperAdmin');
    return spy;
  }

  async function setup(role: 'Guest' | 'Admin' | 'SuperAdmin' | null = null) {
    authSpy = buildAuthSpy(role);

    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [NavbarComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthService, useValue: authSpy },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(NavbarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  beforeEach(async () => await setup(null)); // unauthenticated by default

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── INITIAL SIGNAL STATE ───────────────────────────────────────────────────

  it('mobileOpen — should start as false', () => {
    expect(component.mobileOpen()).toBeFalse();
  });

  // ── toggleMobile() ─────────────────────────────────────────────────────────

  it('toggleMobile() — should set mobileOpen to true when it was false', () => {
    component.toggleMobile();
    expect(component.mobileOpen()).toBeTrue();
  });

  it('toggleMobile() — should set mobileOpen to false when it was true', () => {
    component.toggleMobile(); // → true
    component.toggleMobile(); // → false
    expect(component.mobileOpen()).toBeFalse();
  });

  it('toggleMobile() — calling three times should leave it true', () => {
    component.toggleMobile();
    component.toggleMobile();
    component.toggleMobile();
    expect(component.mobileOpen()).toBeTrue();
  });

  // ── closeMobile() ──────────────────────────────────────────────────────────

  it('closeMobile() — should set mobileOpen to false', () => {
    component.mobileOpen.set(true);
    component.closeMobile();
    expect(component.mobileOpen()).toBeFalse();
  });

  it('closeMobile() — calling when already false should keep it false', () => {
    component.closeMobile();
    expect(component.mobileOpen()).toBeFalse();
  });

  // ── AUTH SERVICE INTEGRATION ───────────────────────────────────────────────

  it('auth — should expose the injected AuthService', () => {
    expect(component.auth).toBeTruthy();
  });

  // ── TEMPLATE — UNAUTHENTICATED ─────────────────────────────────────────────

  it('should display brand name "ThanushStayHub" in the template', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Thanush StayHub');
  });

  it('should show Sign In link when not authenticated', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const el    = fixture.nativeElement as HTMLElement;
    const links = Array.from(el.querySelectorAll('a')) as HTMLAnchorElement[];
    const signIn = links.find(a => a.textContent?.includes('Sign In') || a.getAttribute('href') === '/auth/login');
    expect(signIn).toBeTruthy();
  });

  it('should NOT show Dashboard link when not authenticated', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).not.toContain('Dashboard');
  });

  // ── TEMPLATE — GUEST ───────────────────────────────────────────────────────

  it('should show Payments link for Guest role', async () => {
    await setup('Guest');
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Wallet');
  });

  it('should show Dashboard link for Guest role', async () => {
    await setup('Guest');
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Dashboard');
  });

  it('should NOT show "Error Logs" link for Guest role', async () => {
    await setup('Guest');
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).not.toContain('Error Logs');
  });

  // ── TEMPLATE — ADMIN ───────────────────────────────────────────────────────

  it('should show Dashboard link for Admin role', async () => {
    await setup('Admin');
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Dashboard');
  });

  it('should show Rooms link for Admin role', async () => {
    await setup('Admin');
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Rooms');
  });

  it('should NOT show Payments link for Admin role', async () => {
    await setup('Admin');
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).not.toContain('Wallet');
  });

  // ── TEMPLATE — SUPERADMIN ──────────────────────────────────────────────────

  it('should show Error Logs link for SuperAdmin role', async () => {
    await setup('SuperAdmin');
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    // Desktop nav shows "Logs", mobile menu shows "Error Logs"
    expect(el.textContent).toContain('Logs');
  });

  it('should show Hotels link for SuperAdmin role', async () => {
    await setup('SuperAdmin');
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Hotels');
  });

  it('should NOT show Payments link for SuperAdmin role', async () => {
    await setup('SuperAdmin');
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).not.toContain('Wallet');
  });

  // ── TEMPLATE — USER MENU ───────────────────────────────────────────────────

  it('should show user name in navbar when authenticated', async () => {
    await setup('Guest');
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Thanush K');
  });

  it('should NOT show user name when not authenticated', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).not.toContain('Thanush K');
  });

  // ── MOBILE MENU ────────────────────────────────────────────────────────────

  it('should NOT show mobile menu when mobileOpen is false', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const el   = fixture.nativeElement as HTMLElement;
    const menu = el.querySelector('.mobile-menu');
    expect(menu).toBeFalsy();
  });

  it('should show mobile menu when mobileOpen is true', async () => {
    component.mobileOpen.set(true);
    fixture.detectChanges();
    await fixture.whenStable();
    const el   = fixture.nativeElement as HTMLElement;
    const menu = el.querySelector('.mobile-menu');
    expect(menu).toBeTruthy();
  });

  it('should render Hotels link in mobile menu when open', async () => {
    component.mobileOpen.set(true);
    fixture.detectChanges();
    await fixture.whenStable();
    const el   = fixture.nativeElement as HTMLElement;
    const menu = el.querySelector('.mobile-menu');
    expect(menu?.textContent).toContain('Hotels');
  });

  it('should render Support link in mobile menu when open', async () => {
    await setup('Guest');
    component.mobileOpen.set(true);
    fixture.detectChanges();
    await fixture.whenStable();
    const el   = fixture.nativeElement as HTMLElement;
    const menu = el.querySelector('.mobile-menu');
    expect(menu?.textContent).toContain('Support');
  });
});