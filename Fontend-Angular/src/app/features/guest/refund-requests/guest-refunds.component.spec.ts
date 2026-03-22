import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of } from 'rxjs';
import { GuestRefundsComponent } from './guest-refunds.component';
import { RefundService } from '../../../core/services/api.services';
import { RefundRequestResponseDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ── Mock data ──────────────────────────────────────────────────────────────────

const MOCK_PENDING: RefundRequestResponseDto = {
  refundRequestId: 'rf-001',
  reservationId:   'res-001',
  reservationCode: 'RES-ABCD1234',
  userId:          'usr-001',
  guestName:       'Thanush K',
  reason:          'Trip cancelled due to emergency',
  status:          'Pending',
  refundAmount:    5000,
  createdAt:       '2025-01-10T10:00:00Z',
};

const MOCK_APPROVED: RefundRequestResponseDto = {
  refundRequestId: 'rf-002',
  reservationId:   'res-002',
  reservationCode: 'RES-WXYZ5678',
  userId:          'usr-001',
  guestName:       'Thanush K',
  reason:          'Booking error',
  status:          'Approved',
  adminResponse:   'Refund processed. Amount credited within 5 business days.',
  refundAmount:    8000,
  createdAt:       '2025-01-05T08:00:00Z',
  processedAt:     '2025-01-06T10:00:00Z',
};

const MOCK_REJECTED: RefundRequestResponseDto = {
  refundRequestId: 'rf-003',
  reservationId:   'res-003',
  reservationCode: 'RES-MNOP9012',
  userId:          'usr-001',
  guestName:       'Thanush K',
  reason:          'Changed mind',
  status:          'Rejected',
  adminResponse:   'Outside the cancellation window. No refund applicable.',
  refundAmount:    3200,
  createdAt:       '2025-01-08T11:00:00Z',
  processedAt:     '2025-01-09T12:00:00Z',
};

const ALL_REFUNDS = [MOCK_PENDING, MOCK_APPROVED, MOCK_REJECTED];

// ─────────────────────────────────────────────────────────────────────────────

describe('GuestRefundsComponent', () => {
  let component: GuestRefundsComponent;
  let fixture:   ComponentFixture<GuestRefundsComponent>;
  let refundSpy: jasmine.SpyObj<RefundService>;

  beforeEach(async () => {
    refundSpy = jasmine.createSpyObj('RefundService', ['getGuestRefundRequests']);
    refundSpy.getGuestRefundRequests.and.returnValue(of(ALL_REFUNDS));

    await TestBed.configureTestingModule({
      imports: [GuestRefundsComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: RefundService, useValue: refundSpy },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(GuestRefundsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── INITIAL SIGNAL STATE ───────────────────────────────────────────────────

  it('refunds — should start as empty array before ngOnInit fires', () => {
    const freshFixture = TestBed.createComponent(GuestRefundsComponent);
    expect(freshFixture.componentInstance.refunds()).toEqual([]);
  });

  // ── ngOnInit ───────────────────────────────────────────────────────────────

  it('ngOnInit — should call getGuestRefundRequests once on startup', () => {
    expect(refundSpy.getGuestRefundRequests).toHaveBeenCalledOnceWith();
  });

  it('ngOnInit — should populate refunds signal with all returned requests', () => {
    expect(component.refunds().length).toBe(3);
  });

  it('ngOnInit — should store all three refund IDs', () => {
    const ids = component.refunds().map(r => r.refundRequestId);
    expect(ids).toContain('rf-001');
    expect(ids).toContain('rf-002');
    expect(ids).toContain('rf-003');
  });

  it('ngOnInit — should handle empty refund list', async () => {
    refundSpy.getGuestRefundRequests.and.returnValue(of([]));

    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [GuestRefundsComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: RefundService, useValue: refundSpy },
      ]
    }).compileComponents();

    const f   = TestBed.createComponent(GuestRefundsComponent);
    const cmp = f.componentInstance;
    f.detectChanges();

    expect(cmp.refunds().length).toBe(0);
  });

  // ── REFUNDS SIGNAL DATA ────────────────────────────────────────────────────

  it('refunds — Pending entry has correct amount', () => {
    expect(component.refunds()[0].refundAmount).toBe(5000);
    expect(component.refunds()[0].status).toBe('Pending');
  });

  it('refunds — Approved entry has adminResponse', () => {
    expect(component.refunds()[1].status).toBe('Approved');
    expect(component.refunds()[1].adminResponse).toBeTruthy();
  });

  it('refunds — Rejected entry has processedAt date', () => {
    expect(component.refunds()[2].status).toBe('Rejected');
    expect(component.refunds()[2].processedAt).toBe('2025-01-09T12:00:00Z');
  });

  it('refunds — Pending entry should NOT have processedAt', () => {
    expect(component.refunds()[0].processedAt).toBeUndefined();
  });

  it('refunds — signal can be updated directly', () => {
    component.refunds.set([MOCK_PENDING]);
    expect(component.refunds().length).toBe(1);
    expect(component.refunds()[0].reservationCode).toBe('RES-ABCD1234');
  });

  // ── statusClass() ──────────────────────────────────────────────────────────

  it('statusClass() — Pending → badge-warning', () => {
    expect(component.statusClass('Pending')).toBe('badge-warning');
  });

  it('statusClass() — Approved → badge-success', () => {
    expect(component.statusClass('Approved')).toBe('badge-success');
  });

  it('statusClass() — Rejected → badge-error', () => {
    expect(component.statusClass('Rejected')).toBe('badge-error');
  });

  it('statusClass() — unknown status → badge-muted', () => {
    expect(component.statusClass('Processing')).toBe('badge-muted');
    expect(component.statusClass('')).toBe('badge-muted');
  });

  it('statusClass() — all three known statuses return distinct classes', () => {
    const pending  = component.statusClass('Pending');
    const approved = component.statusClass('Approved');
    const rejected = component.statusClass('Rejected');
    const unique   = new Set([pending, approved, rejected]);
    expect(unique.size).toBe(3);
  });

  // ── TEMPLATE RENDERS ───────────────────────────────────────────────────────

  it('should render all refund cards in the template', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const cards = fixture.nativeElement.querySelectorAll('.refund-card');
    expect(cards.length).toBe(3);
  });

  it('should display reservation code in the template', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('RES-ABCD1234');
  });

  it('should display refund reason in the template', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Trip cancelled due to emergency');
  });

  it('should display admin response when present', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Refund processed. Amount credited within 5 business days.');
  });

  it('should show empty state when refunds list is empty', async () => {
    component.refunds.set([]);
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.empty-state')).toBeTruthy();
    expect(el.querySelector('.refund-card')).toBeFalsy();
  });
});