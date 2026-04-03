import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { WalletService } from './wallet.service';

const BASE = environment.apiUrl;

describe('WalletService', () => {
  let service: WalletService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(WalletService);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should be created', () => expect(service).toBeTruthy());

  // ── getWallet ─────────────────────────────────────────────────────────────

  it('getWallet — should POST /guest/wallet/list with page and pageSize in body', () => {
    service.getWallet(1, 10).subscribe(result => {
      expect(result.wallet.balance).toBe(500);
      expect(result.totalCount).toBe(3);
    });

    const req = http.expectOne(`${BASE}/guest/wallet/list`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.page).toBe(1);
    expect(req.request.body.pageSize).toBe(10);
    req.flush({ success: true, data: {
      totalCount: 3,
      wallet: { walletId: 'w-001', balance: 500, updatedAt: '2025-01-10T10:00:00Z' },
      transactions: []
    }});
  });

  it('getWallet — page 2 should send correct page in body', () => {
    service.getWallet(2, 5).subscribe();
    const req = http.expectOne(`${BASE}/guest/wallet/list`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.page).toBe(2);
    expect(req.request.body.pageSize).toBe(5);
    req.flush({ success: true, data: { totalCount: 0, wallet: { walletId: 'w-001', balance: 0, updatedAt: '' }, transactions: [] }});
  });

  it('getWallet — should return transactions list', () => {
    service.getWallet(1, 10).subscribe(result => {
      expect(result.transactions.length).toBe(2);
      expect(result.transactions[0].type).toBe('Credit');
    });

    http.expectOne(`${BASE}/guest/wallet/list`).flush({ success: true, data: {
      totalCount: 2,
      wallet: { walletId: 'w-001', balance: 300, updatedAt: '2025-01-10T10:00:00Z' },
      transactions: [
        { walletTransactionId: 'wt-001', amount: 200, type: 'Credit', description: 'Top-up', createdAt: '2025-01-10T10:00:00Z' },
        { walletTransactionId: 'wt-002', amount: 100, type: 'Debit',  description: 'Booking', createdAt: '2025-01-09T10:00:00Z' }
      ]
    }});
  });

  // ── topUp ─────────────────────────────────────────────────────────────────

  it('topUp — should POST to /guest/wallet/topup with amount', () => {
    service.topUp({ amount: 500 }).subscribe(result => {
      expect(result.balance).toBe(1000);
    });

    const req = http.expectOne(`${BASE}/guest/wallet/topup`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.amount).toBe(500);
    req.flush({ success: true, data: { walletId: 'w-001', balance: 1000, updatedAt: '2025-01-10T10:00:00Z' }});
  });

  it('topUp — should send the exact amount in request body', () => {
    service.topUp({ amount: 2500 }).subscribe();
    const req = http.expectOne(`${BASE}/guest/wallet/topup`);
    expect(req.request.body.amount).toBe(2500);
    req.flush({ success: true, data: { walletId: 'w-001', balance: 2500, updatedAt: '' }});
  });
});
