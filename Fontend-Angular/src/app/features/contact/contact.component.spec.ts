import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ContactComponent } from './contact.component';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

describe('ContactComponent', () => {
  let component: ContactComponent;
  let fixture:   ComponentFixture<ContactComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ContactComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(ContactComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── TEMPLATE — CONTACT INFO ────────────────────────────────────────────────

  it('should display the support email address', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('thanush@superadmin.com');
  });

  it('should display the support phone number', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('98406 50390');
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

  // ── TEMPLATE — SECTIONS ────────────────────────────────────────────────────

  it('should display the page heading', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Contact');
  });

  it('should display hotel partner support section', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent?.toLowerCase()).toContain('hotel');
  });

  it('should display bug report section', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent?.toLowerCase()).toContain('bug');
  });

  it('should display FAQ section', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent?.toLowerCase()).toContain('faq');
  });

  it('should display support hours information', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('9:00 AM');
  });

  // ── TEMPLATE — ROUTER LINKS ────────────────────────────────────────────────

  it('should contain at least one routerLink for internal navigation', () => {
    const el    = fixture.nativeElement as HTMLElement;
    const links = el.querySelectorAll('a[href]');
    expect(links.length).toBeGreaterThan(0);
  });
});