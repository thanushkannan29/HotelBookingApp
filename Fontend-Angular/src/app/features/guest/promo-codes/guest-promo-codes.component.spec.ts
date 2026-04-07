import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { GuestPromoCodesComponent } from './guest-promo-codes.component';
import { PromoCodeService } from '../../../core/services/promo-code.service';
import { ToastService } from '../../../core/services/toast.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

const MOCK_CODES = [
  { promoCodeId: 'pc-001', code: 'SAVE10', hotelName: 'Grand Palace', discountPercent: 10, expiryDate: '2025-12-31', status: 'Active' },
  { promoCodeId: 'pc-002', code: 'USED20', hotelName: 'Grand Palace', discountPercent: 20, expiryDate: '2025-06-01', status: 'Used' },
  { promoCodeId: 'pc-003', code: 'EXP5',  hotelName: 'Grand Palace', discountPercent: 5,  expiryDate: '2024-01-01', status: 'Expired' },
];
const MOCK_PAGED = { totalCount: 3, promoCodes: MOCK_CODES };

describe('GuestPromoCodesComponent', () => {
  let component: GuestPromoCodesComponent;
  let fixture: ComponentFixture<GuestPromoCodesComponent>;
  let promoSpy: jasmine.SpyObj<PromoCodeService>;
  let toastSpy: jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    promoSpy = jasmine.createSpyObj('PromoCodeService', ['getMyCodes']);
    toastSpy = jasmine.createSpyObj('ToastService', ['success', 'error']);

    promoSpy.getMyCodes.and.returnValue(of(MOCK_PAGED as any));

    await TestBed.configureTestingModule({
      imports: [GuestPromoCodesComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]),
        { provide: PromoCodeService, useValue: promoSpy },
        { provide: ToastService,     useValue: toastSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(GuestPromoCodesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should call getMyCodes', () => {
    expect(promoSpy.getMyCodes).toHaveBeenCalledWith(1, 10, 'All');
  });

  it('ngOnInit — should populate codes signal', () => {
    expect(component.codes().length).toBe(3);
  });

  it('ngOnInit — should set totalCount', () => {
    expect(component.totalCount()).toBe(3);
  });

  it('loading — should be false after load', () => {
    expect(component.loading()).toBeFalse();
  });

  it('load — should set loading to false on error', () => {
    promoSpy.getMyCodes.and.returnValue(throwError(() => new Error('fail')));
    component.load();
    expect(component.loading()).toBeFalse();
  });

  // ── statusTabs ────────────────────────────────────────────────────────────

  it('statusTabs — should contain All, Active, Used, Expired', () => {
    expect(component.statusTabs).toEqual(['All', 'Active', 'Used', 'Expired']);
  });

  // ── onTabChange ───────────────────────────────────────────────────────────

  it('onTabChange — should set selectedStatus and reload', () => {
    promoSpy.getMyCodes.calls.reset();
    component.onTabChange(1); // 'Active'
    expect(component.selectedStatus).toBe('Active');
    expect(promoSpy.getMyCodes).toHaveBeenCalledWith(1, 10, 'Active');
  });

  it('onTabChange — should reset currentPage to 1', () => {
    component.currentPage = 3;
    component.onTabChange(2);
    expect(component.currentPage).toBe(1);
  });

  // ── onPage ────────────────────────────────────────────────────────────────

  it('onPage — should update currentPage and reload', () => {
    promoSpy.getMyCodes.calls.reset();
    component.onPage({ pageIndex: 1, pageSize: 10, length: 20 } as any);
    expect(component.currentPage).toBe(2);
    expect(promoSpy.getMyCodes).toHaveBeenCalled();
  });

  // ── copy ──────────────────────────────────────────────────────────────────

  it('copy — should show success toast', () => {
    spyOn(navigator.clipboard, 'writeText').and.returnValue(Promise.resolve());
    component.copy('SAVE10');
    expect(toastSpy.success).toHaveBeenCalledWith('Code copied!');
  });

  // ── statusClass ───────────────────────────────────────────────────────────

  it('statusClass — Active → badge-success',  () => expect(component.statusClass('Active')).toBe('badge-success'));
  it('statusClass — Used → badge-muted',      () => expect(component.statusClass('Used')).toBe('badge-muted'));
  it('statusClass — Expired → badge-error',   () => expect(component.statusClass('Expired')).toBe('badge-error'));
  it('statusClass — unknown → badge-muted',   () => expect(component.statusClass('Unknown')).toBe('badge-muted'));
});
