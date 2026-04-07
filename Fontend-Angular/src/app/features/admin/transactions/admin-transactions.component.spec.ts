import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AdminTransactionsComponent } from './admin-transactions.component';
import { TransactionService } from '../../../core/services/api.services';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

const MOCK_TX = [
  { transactionId: 'tx-001', reservationId: 'res-001', reservationCode: 'RES-001', guestName: 'Alice', amount: 5000, paymentMethod: 1, status: 2, transactionDate: '2025-01-10T10:00:00Z', transactionType: 'Payment' },
  { transactionId: 'tx-002', reservationId: 'res-002', reservationCode: 'RES-002', guestName: 'Bob',   amount: 200,  paymentMethod: 0, status: 3, transactionDate: '2025-01-11T10:00:00Z', transactionType: 'AutoRefund' },
  { transactionId: 'tx-003', reservationId: 'res-003', reservationCode: 'RES-003', guestName: 'Carol', amount: 100,  paymentMethod: 0, status: 2, transactionDate: '2025-01-12T10:00:00Z', transactionType: 'CommissionSent' },
  { transactionId: 'tx-004', reservationId: 'res-004', reservationCode: 'RES-004', guestName: 'Dave',  amount: 300,  paymentMethod: 0, status: 3, transactionDate: '2025-01-13T10:00:00Z', transactionType: 'WalletRefund' },
];
const MOCK_PAGED = { totalCount: 4, transactions: MOCK_TX };

describe('AdminTransactionsComponent', () => {
  let component: AdminTransactionsComponent;
  let fixture: ComponentFixture<AdminTransactionsComponent>;
  let txSpy: jasmine.SpyObj<TransactionService>;

  beforeEach(async () => {
    txSpy = jasmine.createSpyObj('TransactionService', ['getTransactions']);
    txSpy.getTransactions.and.returnValue(of(MOCK_PAGED as any));

    await TestBed.configureTestingModule({
      imports: [AdminTransactionsComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]),
        { provide: TransactionService, useValue: txSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AdminTransactionsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should call getTransactions', () => {
    expect(txSpy.getTransactions).toHaveBeenCalledWith(1, 10);
  });

  it('ngOnInit — should populate transactions signal', () => {
    expect(component.transactions().length).toBe(4);
  });

  it('ngOnInit — should set totalCount', () => {
    expect(component.totalCount()).toBe(4);
  });

  it('loading — should be false after load', () => {
    expect(component.loading()).toBeFalse();
  });

  it('load — should set loading to false on error', () => {
    txSpy.getTransactions.and.returnValue(throwError(() => new Error('fail')));
    component.load();
    expect(component.loading()).toBeFalse();
  });

  // ── onPage ────────────────────────────────────────────────────────────────

  it('onPage — should update currentPage and reload', () => {
    txSpy.getTransactions.calls.reset();
    component.onPage({ pageIndex: 1, pageSize: 10, length: 20 } as any);
    expect(component.currentPage).toBe(2);
    expect(txSpy.getTransactions).toHaveBeenCalledWith(2, 10);
  });

  // ── statusLabel ───────────────────────────────────────────────────────────

  it('statusLabel — 2 → Success',  () => expect(component.statusLabel(2)).toBe('Success'));
  it('statusLabel — 3 → Failed',   () => expect(component.statusLabel(3)).toBe('Failed'));
  it('statusLabel — 1 → Pending',  () => expect(component.statusLabel(1)).toBe('Pending'));
  it('statusLabel — 4 → Refunded', () => expect(component.statusLabel(4)).toBe('Refunded'));

  // ── statusClass ───────────────────────────────────────────────────────────

  it('statusClass — 2 (Success) → badge-success',  () => expect(component.statusClass(2)).toBe('badge-success'));
  it('statusClass — 4 (Refunded) → badge-warning', () => expect(component.statusClass(4)).toBe('badge-warning'));
  it('statusClass — 3 (Failed) → badge-error',     () => expect(component.statusClass(3)).toBe('badge-error'));
  it('statusClass — 1 (Pending) → badge-muted',    () => expect(component.statusClass(1)).toBe('badge-muted'));

  // ── txLabel ───────────────────────────────────────────────────────────────

  it('txLabel — CommissionSent → 📤 Commission (2%)', () => {
    expect(component.txLabel(MOCK_TX[2] as any)).toBe('📤 Commission (2%)');
  });

  it('txLabel — AutoRefund → 💰 Auto Refund', () => {
    expect(component.txLabel(MOCK_TX[1] as any)).toBe('💰 Auto Refund');
  });

  it('txLabel — WalletRefund → 💳 Wallet Refund', () => {
    expect(component.txLabel(MOCK_TX[3] as any)).toBe('💳 Wallet Refund');
  });

  // ── txBadgeClass ──────────────────────────────────────────────────────────

  it('txBadgeClass — CommissionSent → badge-muted',  () => expect(component.txBadgeClass(MOCK_TX[2] as any)).toBe('badge-muted'));
  it('txBadgeClass — AutoRefund → badge-warning',    () => expect(component.txBadgeClass(MOCK_TX[1] as any)).toBe('badge-warning'));
  it('txBadgeClass — WalletRefund → badge-warning',  () => expect(component.txBadgeClass(MOCK_TX[3] as any)).toBe('badge-warning'));

  // ── txBadgeLabel ──────────────────────────────────────────────────────────

  it('txBadgeLabel — CommissionSent → Commission', () => expect(component.txBadgeLabel(MOCK_TX[2] as any)).toBe('Commission'));
  it('txBadgeLabel — AutoRefund → Refunded',       () => expect(component.txBadgeLabel(MOCK_TX[1] as any)).toBe('Refunded'));
  it('txBadgeLabel — WalletRefund → Refunded',     () => expect(component.txBadgeLabel(MOCK_TX[3] as any)).toBe('Refunded'));

  // ── amountColor ───────────────────────────────────────────────────────────

  it('amountColor — CommissionSent → var(--color-error)', () => {
    expect(component.amountColor(MOCK_TX[2] as any)).toBe('var(--color-error)');
  });

  it('amountColor — AutoRefund → var(--color-error)', () => {
    expect(component.amountColor(MOCK_TX[1] as any)).toBe('var(--color-error)');
  });

  it('amountColor — normal payment → inherit', () => {
    expect(component.amountColor(MOCK_TX[0] as any)).toBe('inherit');
  });
});
