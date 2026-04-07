import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { GuestWalletComponent } from './guest-wallet.component';
import { WalletService } from '../../../core/services/wallet.service';
import { ToastService } from '../../../core/services/toast.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

const MOCK_WALLET = { walletId: 'w-001', balance: 1500 };
const MOCK_TX = [
  { walletTransactionId: 'wt-001', description: 'Top-up', amount: 1500, type: 'Credit', createdAt: '2025-01-10T10:00:00Z' },
  { walletTransactionId: 'wt-002', description: 'Booking payment', amount: 500, type: 'Debit', createdAt: '2025-01-11T10:00:00Z' },
];
const MOCK_PAGED = { wallet: MOCK_WALLET, transactions: MOCK_TX, totalCount: 2 };

describe('GuestWalletComponent', () => {
  let component: GuestWalletComponent;
  let fixture: ComponentFixture<GuestWalletComponent>;
  let walletSpy: jasmine.SpyObj<WalletService>;
  let toastSpy: jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    walletSpy = jasmine.createSpyObj('WalletService', ['getWallet', 'topUp']);
    toastSpy  = jasmine.createSpyObj('ToastService', ['success', 'error']);

    walletSpy.getWallet.and.returnValue(of(MOCK_PAGED as any));
    walletSpy.topUp.and.returnValue(of(MOCK_WALLET as any));

    await TestBed.configureTestingModule({
      imports: [GuestWalletComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]),
        { provide: WalletService, useValue: walletSpy },
        { provide: ToastService,  useValue: toastSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(GuestWalletComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should call getWallet', () => {
    expect(walletSpy.getWallet).toHaveBeenCalledWith(1, 10);
  });

  it('ngOnInit — should populate wallet signal', () => {
    expect(component.wallet()?.balance).toBe(1500);
  });

  it('ngOnInit — should populate transactions signal', () => {
    expect(component.transactions().length).toBe(2);
  });

  it('ngOnInit — should set totalCount', () => {
    expect(component.totalCount()).toBe(2);
  });

  it('loading — should be false after load', () => {
    expect(component.loading()).toBeFalse();
  });

  it('load — should set loading to false on error', () => {
    walletSpy.getWallet.and.returnValue(throwError(() => new Error('fail')));
    component.load(1, 10);
    expect(component.loading()).toBeFalse();
  });

  // ── topUpForm ─────────────────────────────────────────────────────────────

  it('topUpForm — should be invalid initially', () => {
    expect(component.topUpForm.invalid).toBeTrue();
  });

  it('topUpForm — should be invalid when amount is 0', () => {
    component.topUpForm.patchValue({ amount: 0 });
    expect(component.topUpForm.invalid).toBeTrue();
  });

  it('topUpForm — should be invalid when amount exceeds 100000', () => {
    component.topUpForm.patchValue({ amount: 100001 });
    expect(component.topUpForm.invalid).toBeTrue();
  });

  it('topUpForm — should be valid for a valid amount', () => {
    component.topUpForm.patchValue({ amount: 500 });
    expect(component.topUpForm.valid).toBeTrue();
  });

  // ── openRazorpay guard ────────────────────────────────────────────────────

  it('openRazorpay — should NOT call topUp when form is invalid', () => {
    component.openRazorpay();
    expect(walletSpy.topUp).not.toHaveBeenCalled();
  });

  // ── onPage ────────────────────────────────────────────────────────────────

  it('onPage — should call getWallet with new page', () => {
    walletSpy.getWallet.calls.reset();
    component.onPage({ pageIndex: 1, pageSize: 10, length: 20 } as any);
    expect(walletSpy.getWallet).toHaveBeenCalledWith(2, 10);
  });

  it('onPage — should update pageSize', () => {
    component.onPage({ pageIndex: 0, pageSize: 20, length: 40 } as any);
    expect(component.pageSize).toBe(20);
  });

  // ── displayedColumns ──────────────────────────────────────────────────────

  it('displayedColumns — should contain description, amount, type, date', () => {
    expect(component.displayedColumns).toEqual(['description', 'amount', 'type', 'date']);
  });
});
