import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { SupportRequestService } from './support-request.service';
import { environment } from '../../../environments/environment';

const BASE = environment.apiUrl;

describe('SupportRequestService', () => {
  let service: SupportRequestService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(SupportRequestService);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should be created', () => expect(service).toBeTruthy());

  // ── submitPublic ──────────────────────────────────────────────────────────

  it('submitPublic — should POST to /support', () => {
    const dto = { subject: 'Issue', message: 'Help', category: 'General' };
    service.submitPublic(dto as any).subscribe();
    const req = http.expectOne(`${BASE}/support`);
    expect(req.request.method).toBe('POST');
    req.flush({ success: true, data: { supportRequestId: 'sr-001' } });
  });

  // ── submitGuest ───────────────────────────────────────────────────────────

  it('submitGuest — should POST to /guest/support', () => {
    const dto = { subject: 'Issue', message: 'Help', category: 'Room' };
    service.submitGuest(dto as any).subscribe();
    const req = http.expectOne(`${BASE}/guest/support`);
    expect(req.request.method).toBe('POST');
    req.flush({ success: true, data: { supportRequestId: 'sr-001' } });
  });

  // ── getGuestRequests ──────────────────────────────────────────────────────

  it('getGuestRequests — should POST to /guest/support/list', () => {
    service.getGuestRequests(1, 10).subscribe();
    const req = http.expectOne(`${BASE}/guest/support/list`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ page: 1, pageSize: 10 });
    req.flush({ success: true, data: { totalCount: 0, requests: [] } });
  });

  // ── submitAdmin ───────────────────────────────────────────────────────────

  it('submitAdmin — should POST to /admin/support', () => {
    const dto = { subject: 'Issue', message: 'Help', category: 'Billing' };
    service.submitAdmin(dto as any).subscribe();
    const req = http.expectOne(`${BASE}/admin/support`);
    expect(req.request.method).toBe('POST');
    req.flush({ success: true, data: { supportRequestId: 'sr-001' } });
  });

  // ── getAdminRequests ──────────────────────────────────────────────────────

  it('getAdminRequests — should POST to /admin/support/list', () => {
    service.getAdminRequests(1, 10).subscribe();
    const req = http.expectOne(`${BASE}/admin/support/list`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ page: 1, pageSize: 10 });
    req.flush({ success: true, data: { totalCount: 0, requests: [] } });
  });

  // ── getAll (superadmin) ───────────────────────────────────────────────────

  it('getAll — should POST to /superadmin/support/list', () => {
    service.getAll('Open', 'Guest', 'room', 1, 10).subscribe();
    const req = http.expectOne(`${BASE}/superadmin/support/list`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ status: 'Open', role: 'Guest', search: 'room', page: 1, pageSize: 10 });
    req.flush({ success: true, data: { totalCount: 0, requests: [] } });
  });

  // ── respond ───────────────────────────────────────────────────────────────

  it('respond — should PATCH to /superadmin/support/:id/respond', () => {
    service.respond('sr-001', { response: 'Fixed', status: 'Resolved' }).subscribe();
    const req = http.expectOne(`${BASE}/superadmin/support/sr-001/respond`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ response: 'Fixed', status: 'Resolved' });
    req.flush({ success: true, data: { supportRequestId: 'sr-001' } });
  });
});
