import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { GuestTransactionsComponent } from './guest-transactions.component';
import { TransactionService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { TransactionResponseDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ── Mock data ──────────────────────────────────────────────────────────────────

function makeTx(id: string, status: number, minsAgo: number): TransactionResponseDto {
  const date = new Date(Date.now() - minsAgo * 60000);
  return {
    transactionId:   id,
    reservationId:   `res-${id}`,
    amount:          5000,
    paymentMethod:   1,   // Credit Card
    status,
    transactionDate: date.toISOString(),
  };
}

const TX_SUCCESS_RECENT = makeTx('tx-001', 2, 5);   // Success, 5 min ago — refundable
const TX_SUCCESS_OLD    = makeTx('tx-002', 2, 45);   // Success, 45 min ago — NOT refundable
const TX_PENDING        = makeTx('tx-003', 1, 2);    // Pending
const TX_REFUNDED       = makeTx('tx-004', 4, 60);   // Refunded

const MOCK_TRANSACTIONS = [TX_SUCCESS_RECENT, TX_SUCCESS_OLD, TX_PENDING, TX_REFUNDED];

const MOCK_PAGED = { totalCount: 4, transactions: MOCK_TRANSACTIONS };

// ─────────────────────────────────────────────────────────────────────────────

describe('GuestTransactionsComponent', () => {
  let component: GuestTransactionsComponent;
  let fixture:   ComponentFixture<GuestTransactionsComponent>;

  let txSpy:    jasmine.SpyObj<TransactionService>;
  let toastSpy: jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    txSpy    = jasmine.createSpyObj('TransactionService', ['getTransactions', 'directRefund']);
    toastSpy = jasmine.createSpyObj('ToastService', ['success', 'error']);

    txSpy.getTransactions.and.returnValue(of(MOCK_PAGED));
    txSpy.directRefund.and.returnValue(of(TX_REFUNDED));

    await TestBed.configureTestingModule({
      imports: [GuestTransactionsComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: TransactionService, useValue: txSpy    },
        { provide: ToastService,       useValue: toastSpy },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(GuestTransactionsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── CONSTANTS ──────────────────────────────────────────────────────────────

  it('pageSize — should be 10', () => {
    expect(component.pageSize).toBe(10);
  });

  // ── INITIAL SIGNAL STATE ───────────────────────────────────────────────────

  it('page — should start at 1', () => {
    expect(component.page()).toBe(1);
  });

  it('refundingId — should start as null', () => {
    expect(component.refundingId()).toBeNull();
  });

  it('isSaving — should start as false', () => {
    expect(component.isSaving()).toBeFalse();
  });

  // ── ngOnInit / load() ──────────────────────────────────────────────────────

  it('ngOnInit — should call getTransactions with page 1 and pageSize 10', () => {
    expect(txSpy.getTransactions).toHaveBeenCalledWith(1, 10);
  });

  it('load() — should populate transactions signal', () => {
    expect(component.transactions().length).toBe(4);
  });

  it('load() — should set total signal to totalCount', () => {
    expect(component.total()).toBe(4);
  });

  // ── totalPages GETTER ──────────────────────────────────────────────────────

  it('totalPages — should be 1 when total is 4 and pageSize is 10', () => {
    expect(component.totalPages).toBe(1);
  });

  it('totalPages — should be 2 when total is 11', () => {
    component.total.set(11);
    expect(component.totalPages).toBe(2);
  });

  it('totalPages — should be 0 when total is 0', () => {
    component.total.set(0);
    expect(component.totalPages).toBe(0);
  });

  // ── paymentMethodLabel() ───────────────────────────────────────────────────

  it('paymentMethodLabel(1) — should return "Credit Card"', () => {
    expect(component.paymentMethodLabel(1)).toBe('Credit Card');
  });

  it('paymentMethodLabel(3) — should return "UPI"', () => {
    expect(component.paymentMethodLabel(3)).toBe('UPI');
  });

  it('paymentMethodLabel(99) — should return "Unknown"', () => {
    expect(component.paymentMethodLabel(99)).toBe('Unknown');
  });

  // ── paymentStatusLabel() ───────────────────────────────────────────────────

  it('paymentStatusLabel(1) — should return "Pending"', () => {
    expect(component.paymentStatusLabel(1)).toBe('Pending');
  });

  it('paymentStatusLabel(2) — should return "Success"', () => {
    expect(component.paymentStatusLabel(2)).toBe('Success');
  });

  it('paymentStatusLabel(4) — should return "Refunded"', () => {
    expect(component.paymentStatusLabel(4)).toBe('Refunded');
  });

  it('paymentStatusLabel(99) — should return "Unknown"', () => {
    expect(component.paymentStatusLabel(99)).toBe('Unknown');
  });

  // ── statusClass() ──────────────────────────────────────────────────────────

  it('statusClass(1) — Pending → badge-warning', () => {
    expect(component.statusClass(1)).toBe('badge-warning');
  });

  it('statusClass(2) — Success → badge-success', () => {
    expect(component.statusClass(2)).toBe('badge-success');
  });

  it('statusClass(3) — Failed → badge-error', () => {
    expect(component.statusClass(3)).toBe('badge-error');
  });

  it('statusClass(4) — Refunded → badge-info', () => {
    expect(component.statusClass(4)).toBe('badge-info');
  });

  it('statusClass(99) — unknown → badge-muted', () => {
    expect(component.statusClass(99)).toBe('badge-muted');
  });

  // ── canDirectRefund() ──────────────────────────────────────────────────────

  it('canDirectRefund() — should return true for Success tx within 30 minutes', () => {
    expect(component.canDirectRefund(TX_SUCCESS_RECENT)).toBeTrue();
  });

  it('canDirectRefund() — should return false for Success tx older than 30 minutes', () => {
    expect(component.canDirectRefund(TX_SUCCESS_OLD)).toBeFalse();
  });

  it('canDirectRefund() — should return false for Pending tx', () => {
    expect(component.canDirectRefund(TX_PENDING)).toBeFalse();
  });

  it('canDirectRefund() — should return false for Refunded tx', () => {
    expect(component.canDirectRefund(TX_REFUNDED)).toBeFalse();
  });

  it('canDirectRefund() — should return true for Success tx exactly at 30 minutes', () => {
    const exactly30 = makeTx('tx-edge', 2, 30);
    expect(component.canDirectRefund(exactly30)).toBeTrue();
  });

  // ── minutesSince() ─────────────────────────────────────────────────────────

  it('minutesSince() — should return ~5 for a tx from 5 minutes ago', () => {
    const result = component.minutesSince(TX_SUCCESS_RECENT);
    // Allow ±1 minute tolerance for test execution time
    expect(result).toBeGreaterThanOrEqual(4);
    expect(result).toBeLessThanOrEqual(6);
  });

  it('minutesSince() — should return ~45 for a tx from 45 minutes ago', () => {
    const result = component.minutesSince(TX_SUCCESS_OLD);
    expect(result).toBeGreaterThanOrEqual(44);
    expect(result).toBeLessThanOrEqual(46);
  });

  // ── startRefund() ──────────────────────────────────────────────────────────

  it('startRefund() — should set refundingId to the given transaction ID', () => {
    component.startRefund('tx-001');
    expect(component.refundingId()).toBe('tx-001');
  });

  it('startRefund() — should reset the refundForm', () => {
    component.refundForm.get('reason')?.setValue('Some previous reason');
    component.startRefund('tx-001');
    expect(component.refundForm.get('reason')?.value).toBeFalsy();
  });

  it('startRefund() — switching transactions updates refundingId', () => {
    component.startRefund('tx-001');
    component.startRefund('tx-002');
    expect(component.refundingId()).toBe('tx-002');
  });

  // ── FORM VALIDATION — refundForm ───────────────────────────────────────────

  it('refundForm — should be invalid initially', () => {
    expect(component.refundForm.invalid).toBeTrue();
  });

  it('refundForm — should be invalid when reason is empty', () => {
    component.refundForm.get('reason')?.setValue('');
    expect(component.refundForm.invalid).toBeTrue();
  });

  it('refundForm — should be invalid when reason is less than 5 characters', () => {
    component.refundForm.get('reason')?.setValue('bad');
    expect(component.refundForm.get('reason')?.invalid).toBeTrue();
  });

  it('refundForm — should be valid when reason is exactly 5 characters', () => {
    component.refundForm.get('reason')?.setValue('error');
    expect(component.refundForm.valid).toBeTrue();
  });

  it('refundForm — should be valid with a proper reason', () => {
    component.refundForm.get('reason')?.setValue('Duplicate booking made by mistake');
    expect(component.refundForm.valid).toBeTrue();
  });

  // ── submitRefund() — HAPPY PATH ────────────────────────────────────────────

  it('submitRefund() — should call directRefund with refundingId and reason', () => {
    component.startRefund('tx-001');
    component.refundForm.get('reason')?.setValue('Duplicate booking made by mistake');

    component.submitRefund();

    expect(txSpy.directRefund).toHaveBeenCalledOnceWith(
      'tx-001',
      { reason: 'Duplicate booking made by mistake' }
    );
  });

  it('submitRefund() — should show success toast on success', () => {
    component.startRefund('tx-001');
    component.refundForm.get('reason')?.setValue('Duplicate booking made by mistake');

    component.submitRefund();

    expect(toastSpy.success)
      .toHaveBeenCalledOnceWith('Refund processed successfully.');
  });

  it('submitRefund() — should clear refundingId after success', () => {
    component.startRefund('tx-001');
    component.refundForm.get('reason')?.setValue('Duplicate booking made by mistake');

    component.submitRefund();

    expect(component.refundingId()).toBeNull();
  });

  it('submitRefund() — should reset isSaving to false on success', () => {
    component.startRefund('tx-001');
    component.refundForm.get('reason')?.setValue('Duplicate booking made by mistake');

    component.submitRefund();

    expect(component.isSaving()).toBeFalse();
  });

  it('submitRefund() — should reload transactions after success', () => {
    component.startRefund('tx-001');
    component.refundForm.get('reason')?.setValue('Duplicate booking made by mistake');
    txSpy.getTransactions.calls.reset();

    component.submitRefund();

    expect(txSpy.getTransactions).toHaveBeenCalled();
  });

  it('submitRefund() — should set isSaving to true during in-flight request', () => {
    const subject = new Subject<TransactionResponseDto>();
    txSpy.directRefund.and.returnValue(subject.asObservable());
    component.startRefund('tx-001');
    component.refundForm.get('reason')?.setValue('Duplicate booking made by mistake');

    component.submitRefund();

    expect(component.isSaving()).toBeTrue();

    subject.next(TX_REFUNDED);
    subject.complete();
  });

  // ── submitRefund() — INVALID FORM ──────────────────────────────────────────

  it('submitRefund() — should NOT call service when refundForm is invalid', () => {
    component.startRefund('tx-001');
    // refundForm is empty — invalid

    component.submitRefund();

    expect(txSpy.directRefund).not.toHaveBeenCalled();
  });

  it('submitRefund() — should mark reason as touched when form is invalid', () => {
    component.startRefund('tx-001');

    component.submitRefund();

    expect(component.refundForm.get('reason')?.touched).toBeTrue();
  });

  it('submitRefund() — should NOT show toast when form is invalid', () => {
    component.startRefund('tx-001');

    component.submitRefund();

    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  // ── submitRefund() — ERROR ─────────────────────────────────────────────────

  it('submitRefund() — should reset isSaving to false on API error', () => {
    txSpy.directRefund.and.returnValue(throwError(() => new Error('fail')));
    component.startRefund('tx-001');
    component.refundForm.get('reason')?.setValue('Duplicate booking made by mistake');

    component.submitRefund();

    expect(component.isSaving()).toBeFalse();
  });

  it('submitRefund() — should NOT show success toast on API error', () => {
    txSpy.directRefund.and.returnValue(throwError(() => new Error('fail')));
    component.startRefund('tx-001');
    component.refundForm.get('reason')?.setValue('Duplicate booking made by mistake');

    component.submitRefund();

    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  it('submitRefund() — should NOT clear refundingId on API error', () => {
    txSpy.directRefund.and.returnValue(throwError(() => new Error('fail')));
    component.startRefund('tx-001');
    component.refundForm.get('reason')?.setValue('Duplicate booking made by mistake');

    component.submitRefund();

    expect(component.refundingId()).toBe('tx-001');
  });

  // ── next() / prev() ────────────────────────────────────────────────────────

  it('next() — should increment page and reload when not on last page', () => {
    component.total.set(25); // 3 pages
    txSpy.getTransactions.calls.reset();

    component.next();

    expect(component.page()).toBe(2);
    expect(txSpy.getTransactions).toHaveBeenCalledWith(2, 10);
  });

  it('next() — should NOT go past the last page', () => {
    component.total.set(10); // exactly 1 page
    txSpy.getTransactions.calls.reset();

    component.next(); // already on last page

    expect(component.page()).toBe(1);
    expect(txSpy.getTransactions).not.toHaveBeenCalled();
  });

  it('prev() — should decrement page and reload when on page 2', () => {
    component.total.set(25);
    component.next(); // go to page 2
    txSpy.getTransactions.calls.reset();

    component.prev();

    expect(component.page()).toBe(1);
    expect(txSpy.getTransactions).toHaveBeenCalledWith(1, 10);
  });

  it('prev() — should NOT go below page 1', () => {
    txSpy.getTransactions.calls.reset();

    component.prev(); // already on page 1

    expect(component.page()).toBe(1);
    expect(txSpy.getTransactions).not.toHaveBeenCalled();
  });
});