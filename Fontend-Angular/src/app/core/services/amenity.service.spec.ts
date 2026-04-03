import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { AmenityService } from './amenity.service';
import { environment } from '../../../environments/environment';

const BASE = `${environment.apiUrl}/superadmin/amenities`;

describe('AmenityService', () => {
  let service: AmenityService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AmenityService);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should be created', () => expect(service).toBeTruthy());

  // ── getAllPaged ───────────────────────────────────────────────────────────

  it('getAllPaged — should GET with page and pageSize params', () => {
    service.getAllPaged(1, 10).subscribe();
    const req = http.expectOne(r => r.url === BASE);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('10');
    req.flush({ success: true, data: { totalCount: 0, amenities: [] } });
  });

  it('getAllPaged — should include search param when provided', () => {
    service.getAllPaged(1, 10, 'WiFi').subscribe();
    const req = http.expectOne(r => r.url === BASE);
    expect(req.request.params.get('search')).toBe('WiFi');
    req.flush({ success: true, data: { totalCount: 0, amenities: [] } });
  });

  it('getAllPaged — should include category param when not "All"', () => {
    service.getAllPaged(1, 10, undefined, 'Tech').subscribe();
    const req = http.expectOne(r => r.url === BASE);
    expect(req.request.params.get('category')).toBe('Tech');
    req.flush({ success: true, data: { totalCount: 0, amenities: [] } });
  });

  it('getAllPaged — should NOT include category param when "All"', () => {
    service.getAllPaged(1, 10, undefined, 'All').subscribe();
    const req = http.expectOne(r => r.url === BASE);
    expect(req.request.params.has('category')).toBeFalse();
    req.flush({ success: true, data: { totalCount: 0, amenities: [] } });
  });

  // ── create ────────────────────────────────────────────────────────────────

  it('create — should POST to base URL', () => {
    const dto = { name: 'WiFi', category: 'Tech' };
    service.create(dto as any).subscribe();
    const req = http.expectOne(BASE);
    expect(req.request.method).toBe('POST');
    req.flush({ success: true, data: { amenityId: 'a-001', ...dto } });
  });

  // ── update ────────────────────────────────────────────────────────────────

  it('update — should PUT to base URL', () => {
    const dto = { amenityId: 'a-001', name: 'WiFi Pro', category: 'Tech', isActive: true };
    service.update(dto as any).subscribe();
    const req = http.expectOne(BASE);
    expect(req.request.method).toBe('PUT');
    req.flush({ success: true, data: dto });
  });

  // ── toggleStatus ──────────────────────────────────────────────────────────

  it('toggleStatus — should PATCH to /:id/toggle-status', () => {
    service.toggleStatus('a-001').subscribe();
    const req = http.expectOne(`${BASE}/a-001/toggle-status`);
    expect(req.request.method).toBe('PATCH');
    req.flush({ success: true, data: { isActive: false } });
  });

  // ── delete ────────────────────────────────────────────────────────────────

  it('delete — should DELETE to /:id', () => {
    service.delete('a-001').subscribe();
    const req = http.expectOne(`${BASE}/a-001`);
    expect(req.request.method).toBe('DELETE');
    req.flush({});
  });
});
