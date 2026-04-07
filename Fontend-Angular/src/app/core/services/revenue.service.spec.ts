import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { RevenueService } from './revenue.service';
import { environment } from '../../../environments/environment';

const BASE = environment.apiUrl;

describe('RevenueService', () => {
  let service: RevenueService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(RevenueService);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should be created', () => expect(service).toBeTruthy());

  // ── getAll ────────────────────────────────────────────────────────────────

  it('getAll — should POST to /superadmin/revenue/list', () => {
    service.getAll(1, 20).subscribe();
    const req = http.expectOne(`${BASE}/superadmin/revenue/list`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ page: 1, pageSize: 20 });
    req.flush({ success: true, data: { totalCount: 0, items: [] } });
  });

  it('getAll — should use defaults when no args provided', () => {
    service.getAll().subscribe();
    const req = http.expectOne(`${BASE}/superadmin/revenue/list`);
    expect(req.request.body).toEqual({ page: 1, pageSize: 20 });
    req.flush({ success: true, data: { totalCount: 0, items: [] } });
  });

  // ── getSummary ────────────────────────────────────────────────────────────

  it('getSummary — should GET /superadmin/revenue/summary', () => {
    service.getSummary().subscribe();
    const req = http.expectOne(`${BASE}/superadmin/revenue/summary`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: { totalCommissionEarned: 5000 } });
  });
});
