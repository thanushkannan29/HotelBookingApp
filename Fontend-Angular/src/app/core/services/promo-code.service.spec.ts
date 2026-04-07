import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { PromoCodeService } from './promo-code.service';
import { environment } from '../../../environments/environment';

const BASE = environment.apiUrl;

describe('PromoCodeService', () => {
  let service: PromoCodeService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(PromoCodeService);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should be created', () => expect(service).toBeTruthy());

  // ── getMyCodes ────────────────────────────────────────────────────────────

  it('getMyCodes — should POST to /guest/promo-codes/list', () => {
    service.getMyCodes(1, 10, 'All').subscribe();
    const req = http.expectOne(`${BASE}/guest/promo-codes/list`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ page: 1, pageSize: 10, status: 'All' });
    req.flush({ success: true, data: { totalCount: 0, promoCodes: [] } });
  });

  it('getMyCodes — should pass status filter', () => {
    service.getMyCodes(1, 10, 'Active').subscribe();
    const req = http.expectOne(`${BASE}/guest/promo-codes/list`);
    expect(req.request.body.status).toBe('Active');
    req.flush({ success: true, data: { totalCount: 0, promoCodes: [] } });
  });

  it('getMyCodes — should use defaults when no args provided', () => {
    service.getMyCodes().subscribe();
    const req = http.expectOne(`${BASE}/guest/promo-codes/list`);
    expect(req.request.body).toEqual({ page: 1, pageSize: 10, status: 'All' });
    req.flush({ success: true, data: { totalCount: 0, promoCodes: [] } });
  });

  // ── validate ──────────────────────────────────────────────────────────────

  it('validate — should POST to /guest/promo-codes/validate', () => {
    const dto = { code: 'SAVE10', hotelId: 'hotel-001', totalAmount: 5000 };
    service.validate(dto).subscribe();
    const req = http.expectOne(`${BASE}/guest/promo-codes/validate`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(dto);
    req.flush({ success: true, data: { isValid: true, discountPercent: 10 } });
  });
});
