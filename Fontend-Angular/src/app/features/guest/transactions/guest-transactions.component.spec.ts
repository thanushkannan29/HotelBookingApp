import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { GuestTransactionsComponent } from './guest-transactions.component';
import { TransactionService } from '../../../core/services/api.services';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

function makeTx(id: string, status: number, type = 'Payment') {
  return {
    transactionId: id, reservationId: 'res-001', reservationCode: 'RES-001',
    hotelName: 'Grand Palace', guestName: 'Alice',
    amount: 5000, paymentMethod: 1, status,
    transactionDate: '2025-01-10T10:00:00Z', transactionType: type
  };
}

const MOCK_TX = [
  makeTx('tx-001', 2, 'Payment'),
  makeTx('tx-002', 3, 'WalletRefund'),
  makeTx('tx-003', 1, 'Payment'),
  makeTx('tx-004', 0, 'Payment'),
];
const MOCK_PAGED = { totalCount: 4, transactions: MOCK_TX };

describe('GuestTransactionsComponent', () => {
  let component: GuestTransactionsComponent;
  let fixture: ComponentFixture<GuestTransactionsComponent>;
  let txSpy: jasmine.SpyObj<TransactionService>;

  beforeEach(async () => {
    txSpy = jasmine.createSpyObj('TransactionService', ['getTransactions']);
    txSpy.getTransactions.and.returnValue(of(MOCK_PAGED as any));

    await TestBed.configureTestingModule({
      imports: [GuestTransactionsComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]),
        { provide: TransactionService, useValue: txSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(GuestTransactionsComponent);
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

  it('onPage — should update pageSize', () => {
    component.onPage({ pageIndex: 0, pageSize: 20, length: 40 } as any);
    expect(component.pageSize).toBe(20);
  });

  // ── statusClass ───────────────────────────────────────────────────────────

  it('statusClass — 1 → badge-warning', () => expect(component.statusClass(1)).toBe('badge-warning'));
  it('statusClass — 2 → badge-success', () => expect(component.statusClass(2)).toBe('badge-success'));
  it('statusClass — 3 → badge-error',   () => expect(component.statusClass(3)).toBe('badge-error'));
  it('statusClass — 4 → badge-info',    () => expect(component.statusClass(4)).toBe('badge-info'));
  it('statusClass — 0 → badge-muted',   () => expect(component.statusClass(0)).toBe('badge-muted'));

  // ── txLabel ───────────────────────────────────────────────────────────────

  it('txLabel — WalletRefund → Hotel Refund (Wallet Credit)', () => {
    expect(component.txLabel(MOCK_TX[1] as any)).toBe('Hotel Refund (Wallet Credit)');
  });

  it('txLabel — normal payment → payment method label', () => {
    const label = component.txLabel(MOCK_TX[0] as any);
    expect(label).toBeTruthy();
  });

  // ── txIcon ────────────────────────────────────────────────────────────────

  it('txIcon — WalletRefund → account_balance_wallet', () => {
    expect(component.txIcon(MOCK_TX[1] as any)).toBe('account_balance_wallet');
  });

  it('txIcon — status 2 (Success) → check_circle', () => {
    expect(component.txIcon(MOCK_TX[0] as any)).toBe('check_circle');
  });

  it('txIcon — status 3 (Refunded) → cancel', () => {
    const tx = makeTx('tx-x', 3, 'Payment');
    expect(component.txIcon(tx as any)).toBe('cancel');
  });

  // ── amountColor ───────────────────────────────────────────────────────────

  it('amountColor — WalletRefund → var(--color-success)', () => {
    expect(component.amountColor(MOCK_TX[1] as any)).toBe('var(--color-success)');
  });

  it('amountColor — status 3 (Failed) → var(--color-error)', () => {
    const tx = makeTx('tx-x', 3, 'Payment');
    expect(component.amountColor(tx as any)).toBe('var(--color-error)');
  });

  it('amountColor — normal success → var(--color-text-primary)', () => {
    expect(component.amountColor(MOCK_TX[0] as any)).toBe('var(--color-text-primary)');
  });
});
