import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { SuperAdminDashboardComponent } from './superadmin-dashboard.component';
import { DashboardService } from '../../../core/services/api.services';
import { SuperAdminDashboardDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ── Mock data ──────────────────────────────────────────────────────────────────

const MOCK_DASHBOARD: SuperAdminDashboardDto = {
  totalHotels:          50,
  activeHotels:         46,
  blockedHotels:        4,
  totalUsers:           1200,
  totalReservations:    5000,
  totalRevenue:         25000000,
  totalReviews:         800,
};

// ─────────────────────────────────────────────────────────────────────────────

describe('SuperAdminDashboardComponent', () => {
  let component: SuperAdminDashboardComponent;
  let fixture:   ComponentFixture<SuperAdminDashboardComponent>;
  let dashboardSpy: jasmine.SpyObj<DashboardService>;

  beforeEach(async () => {
    dashboardSpy = jasmine.createSpyObj('DashboardService', ['getSuperAdminDashboard']);
    dashboardSpy.getSuperAdminDashboard.and.returnValue(of(MOCK_DASHBOARD));

    await TestBed.configureTestingModule({
      imports: [SuperAdminDashboardComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: DashboardService, useValue: dashboardSpy },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(SuperAdminDashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── INITIAL SIGNAL STATE ───────────────────────────────────────────────────

  it('data — should start as null before ngOnInit fires', () => {
    const freshFixture = TestBed.createComponent(SuperAdminDashboardComponent);
    expect(freshFixture.componentInstance.data()).toBeNull();
  });

  // ── ngOnInit ───────────────────────────────────────────────────────────────

  it('ngOnInit — should call getSuperAdminDashboard once on startup', () => {
    expect(dashboardSpy.getSuperAdminDashboard).toHaveBeenCalledOnceWith();
  });

  it('ngOnInit — should populate data signal with API response', () => {
    expect(component.data()).not.toBeNull();
  });

  it('data — totalHotels should be correct', () => {
    expect(component.data()?.totalHotels).toBe(50);
  });

  it('data — activeHotels should be correct', () => {
    expect(component.data()?.activeHotels).toBe(46);
  });

  it('data — blockedHotels should be correct', () => {
    expect(component.data()?.blockedHotels).toBe(4);
  });

  it('data — totalUsers should be correct', () => {
    expect(component.data()?.totalUsers).toBe(1200);
  });

  it('data — totalReservations should be correct', () => {
    expect(component.data()?.totalReservations).toBe(5000);
  });

  it('data — totalRevenue should be correct', () => {
    expect(component.data()?.totalRevenue).toBe(25000000);
  });

  it('data — totalReviews should be correct', () => {
    expect(component.data()?.totalReviews).toBe(800);
  });

  // ── DATA CONSISTENCY ───────────────────────────────────────────────────────

  it('activeHotels + blockedHotels should equal totalHotels', () => {
    const d = component.data()!;
    expect(d.activeHotels + d.blockedHotels).toBe(d.totalHotels);
  });

  // ── SIGNAL UPDATES ─────────────────────────────────────────────────────────

  it('data signal — should update when set directly', () => {
    const updated: SuperAdminDashboardDto = {
      ...MOCK_DASHBOARD,
      totalHotels:  60,
      activeHotels: 55,
    };
    component.data.set(updated);
    expect(component.data()?.totalHotels).toBe(60);
    expect(component.data()?.activeHotels).toBe(55);
  });

  it('data signal — should allow setting back to null', () => {
    component.data.set(null);
    expect(component.data()).toBeNull();
  });

  // ── TEMPLATE RENDERS ───────────────────────────────────────────────────────

  it('should render total hotels in the template', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('50');
  });

  it('should render total revenue in the template', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    // DecimalPipe formats 25000000 as "25,000,000"
    expect(el.textContent).toContain('25,000,000');
  });

  it('should render total users in the template', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    // totalUsers rendered without DecimalPipe in template
    expect(el.textContent).toContain('1200');
  });
});