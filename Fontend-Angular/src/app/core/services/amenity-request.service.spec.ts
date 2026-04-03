import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { AmenityRequestService } from './amenity-request.service';
import { environment } from '../../../environments/environment';

const BASE = environment.apiUrl;

describe('AmenityRequestService', () => {
  let service: AmenityRequestService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AmenityRequestService);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should be created', () => expect(service).toBeTruthy());

  // ── create ────────────────────────────────────────────────────────────────

  it('create — should POST to admin/amenity-requests', () => {
    const dto = { amenityName: 'Sauna', category: 'Services' };
    service.create(dto as any).subscribe();
    const req = http.expectOne(`${BASE}/admin/amenity-requests`);
    expect(req.request.method).toBe('POST');
    req.flush({ success: true, data: { amenityRequestId: 'ar-001', ...dto } });
  });

  // ── getMine ───────────────────────────────────────────────────────────────

  it('getMine — should POST to admin/amenity-requests/list', () => {
    service.getMine(1, 10).subscribe();
    const req = http.expectOne(`${BASE}/admin/amenity-requests/list`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ page: 1, pageSize: 10, search: undefined });
    req.flush({ success: true, data: { totalCount: 0, requests: [] } });
  });

  it('getMine — should pass search term', () => {
    service.getMine(1, 10, 'Sauna').subscribe();
    const req = http.expectOne(`${BASE}/admin/amenity-requests/list`);
    expect(req.request.body.search).toBe('Sauna');
    req.flush({ success: true, data: { totalCount: 0, requests: [] } });
  });

  // ── getAll ────────────────────────────────────────────────────────────────

  it('getAll — should POST to superadmin/amenity-requests/list', () => {
    service.getAll('Pending', 1, 10).subscribe();
    const req = http.expectOne(`${BASE}/superadmin/amenity-requests/list`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ status: 'Pending', page: 1, pageSize: 10 });
    req.flush({ success: true, data: { totalCount: 0, requests: [] } });
  });

  // ── approve ───────────────────────────────────────────────────────────────

  it('approve — should PATCH to superadmin/amenity-requests/:id/approve', () => {
    service.approve('ar-001').subscribe();
    const req = http.expectOne(`${BASE}/superadmin/amenity-requests/ar-001/approve`);
    expect(req.request.method).toBe('PATCH');
    req.flush({ success: true, data: { amenityRequestId: 'ar-001' } });
  });

  // ── reject ────────────────────────────────────────────────────────────────

  it('reject — should PATCH to superadmin/amenity-requests/:id/reject with note', () => {
    service.reject('ar-001', 'Not relevant').subscribe();
    const req = http.expectOne(`${BASE}/superadmin/amenity-requests/ar-001/reject`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ note: 'Not relevant' });
    req.flush({ success: true, data: { amenityRequestId: 'ar-001' } });
  });
});
