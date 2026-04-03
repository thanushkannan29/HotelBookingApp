import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { SuperadminRevenueComponent } from './superadmin-revenue.component';
import { RevenueService } from '../../../core/services/revenue.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

const MOCK_SUMMARY = { totalCommissionEarned: 12000 };
const MOCK_ITEMS = [
  { revenueId: 'rv-001', reservationCode: 'RES-001', hotelName: 'Grand Palace', reservationAmount: 5000, commissionAmount: 100, createdAt: '2025-01-10T10:00:00Z' },
  { revenueId: 'rv-002', reservationCode: 'RES-002', hotelName: 'Sea View',     reservationAmount: 7000, commissionAmount: 140, createdAt: '2025-01-11T10:00:00Z' },
];
const MOCK_PAGED = { totalCount: 2, items: MOCK_ITEMS };

describe('SuperadminRevenueComponent', () => {
  let component: SuperadminRevenueComponent;
  let fixture: ComponentFixture<SuperadminRevenueComponent>;
  let serviceSpy: jasmine.SpyObj<RevenueService>;

  beforeEach(async () => {
    serviceSpy = jasmine.createSpyObj('RevenueService', ['getAll', 'getSummary']);
    serviceSpy.getAll.and.returnValue(of(MOCK_PAGED as any));
    serviceSpy.getSummary.and.returnValue(of(MOCK_SUMMARY as any));

    await TestBed.configureTestingModule({
      imports: [SuperadminRevenueComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]),
        { provide: RevenueService, useValue: serviceSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(SuperadminRevenueComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should call getSummary', () => {
    expect(serviceSpy.getSummary).toHaveBeenCalled();
  });

  it('ngOnInit — should call getAll', () => {
    expect(serviceSpy.getAll).toHaveBeenCalledWith(1, 20);
  });

  it('ngOnInit — should populate summary signal', () => {
    expect(component.summary()?.totalCommissionEarned).toBe(12000);
  });

  it('ngOnInit — should populate items signal', () => {
    expect(component.items().length).toBe(2);
  });

  it('ngOnInit — should set totalCount', () => {
    expect(component.totalCount()).toBe(2);
  });

  it('loading — should be false after load', () => {
    expect(component.loading()).toBeFalse();
  });

  it('load — should set loading to false on error', () => {
    serviceSpy.getAll.and.returnValue(throwError(() => new Error('fail')));
    component.load();
    expect(component.loading()).toBeFalse();
  });

  // ── onPage ────────────────────────────────────────────────────────────────

  it('onPage — should update currentPage and reload', () => {
    serviceSpy.getAll.calls.reset();
    component.onPage({ pageIndex: 1, pageSize: 20, length: 40 } as any);
    expect(component.currentPage).toBe(2);
    expect(serviceSpy.getAll).toHaveBeenCalledWith(2, 20);
  });

  it('onPage — should update pageSize', () => {
    component.onPage({ pageIndex: 0, pageSize: 50, length: 100 } as any);
    expect(component.pageSize).toBe(50);
  });

  // ── displayedColumns ──────────────────────────────────────────────────────

  it('displayedColumns — should contain all 5 columns', () => {
    expect(component.displayedColumns).toEqual([
      'reservationCode', 'hotelName', 'reservationAmount', 'commissionAmount', 'date'
    ]);
  });
});
