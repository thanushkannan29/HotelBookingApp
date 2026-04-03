import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FooterComponent } from './footer.component';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

describe('FooterComponent', () => {
  let component: FooterComponent;
  let fixture:   ComponentFixture<FooterComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FooterComponent],
      providers: [
        provideAnimationsAsync(),provideRouter([])] // required by RouterLink
    }).compileComponents();

    fixture   = TestBed.createComponent(FooterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── year PROPERTY ──────────────────────────────────────────────────────────

  it('year — should equal the current year', () => {
    expect(component.year).toBe(new Date().getFullYear());
  });

  it('year — should be a valid 4-digit year', () => {
    expect(component.year).toBeGreaterThanOrEqual(2024);
    expect(component.year).toBeLessThanOrEqual(2100);
  });

  // ── TEMPLATE — BRAND ───────────────────────────────────────────────────────

  it('should display the brand name "Thanush StayHub"', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Thanush StayHub');
  });

  it('should display the brand tagline', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Smart Hotel Booking');
  });

  // ── TEMPLATE — COPYRIGHT ───────────────────────────────────────────────────

  it('should display the current year in the copyright notice', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain(String(new Date().getFullYear()));
  });

  it('should display the copyright text', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('All rights reserved');
  });

  // ── TEMPLATE — CONTACT INFO ────────────────────────────────────────────────

  it('should display the support email address', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('thanush@superadmin.com');
  });

  it('should render a mailto link for the support email', () => {
    const el    = fixture.nativeElement as HTMLElement;
    const links = Array.from(el.querySelectorAll('a[href]')) as HTMLAnchorElement[];
    const mail  = links.find(a => a.href.startsWith('mailto:'));
    expect(mail).toBeTruthy();
    expect(mail!.href).toContain('thanush@superadmin.com');
  });

  it('should render a tel link for the support phone number', () => {
    const el    = fixture.nativeElement as HTMLElement;
    const links = Array.from(el.querySelectorAll('a[href]')) as HTMLAnchorElement[];
    const tel   = links.find(a => a.href.startsWith('tel:'));
    expect(tel).toBeTruthy();
    expect(tel!.href).toContain('9840650390');
  });

  it('should display support hours', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Mon – Sat');
  });

  // ── TEMPLATE — NAVIGATION LINKS ────────────────────────────────────────────

  it('should render a link to /hotels', () => {
    const el    = fixture.nativeElement as HTMLElement;
    const links = Array.from(el.querySelectorAll('a[href]')) as HTMLAnchorElement[];
    const hotel = links.find(a => a.getAttribute('href') === '/hotels');
    expect(hotel).toBeTruthy();
  });

  it('should render a link to /auth/login', () => {
    const el    = fixture.nativeElement as HTMLElement;
    const links = Array.from(el.querySelectorAll('a[href]')) as HTMLAnchorElement[];
    const login = links.find(a => a.getAttribute('href') === '/auth/login');
    expect(login).toBeTruthy();
  });

  it('should render a link to /auth/register', () => {
    const el    = fixture.nativeElement as HTMLElement;
    const links = Array.from(el.querySelectorAll('a[href]')) as HTMLAnchorElement[];
    const reg   = links.find(a => a.getAttribute('href') === '/auth/register');
    expect(reg).toBeTruthy();
  });

  it('should render a link to /contact', () => {
    const el    = fixture.nativeElement as HTMLElement;
    const links = Array.from(el.querySelectorAll('a[href]')) as HTMLAnchorElement[];
    const contact = links.find(a => a.getAttribute('href') === '/contact');
    expect(contact).toBeTruthy();
  });

  // ── TEMPLATE — SECTION HEADINGS ────────────────────────────────────────────

  it('should display the "Explore" section heading', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Explore');
  });

  it('should display the "Account" section heading', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Account');
  });

  it('should display the "Contact" section heading', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Contact');
  });

  it('should display the "Support" section heading', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Support');
  });
});