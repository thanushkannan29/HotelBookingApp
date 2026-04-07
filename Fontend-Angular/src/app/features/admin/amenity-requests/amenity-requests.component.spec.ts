import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AmenityRequestsComponent } from './amenity-requests.component';
import { AmenityRequestService } from '../../../core/services/amenity-request.service';
import { ToastService } from '../../../core/services/toast.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

const MOCK_REQUESTS = [
  { amenityRequestId: 'ar-001', amenityName: 'Sauna', category: 'Services', status: 'Pending',  note: null, createdAt: '2025-01-10T10:00:00Z', hotelName: 'Grand Palace', adminName: 'Admin' },
  { amenityRequestId: 'ar-002', amenityName: 'Pool',  category: 'Services', status: 'Approved', note: null, createdAt: '2025-01-11T10:00:00Z', hotelName: 'Grand Palace', adminName: 'Admin' },
];
const MOCK_PAGED = { totalCount: 2, requests: MOCK_REQUESTS };

describe('AmenityRequestsComponent', () => {
  let component: AmenityRequestsComponent;
  let fixture: ComponentFixture<AmenityRequestsComponent>;
  let serviceSpy: jasmine.SpyObj<AmenityRequestService>;
  let toastSpy: jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    serviceSpy = jasmine.createSpyObj('AmenityRequestService', ['getMine', 'create']);
    toastSpy   = jasmine.createSpyObj('ToastService', ['success', 'error']);

    serviceSpy.getMine.and.returnValue(of(MOCK_PAGED as any));
    serviceSpy.create.and.returnValue(of({} as any));

    await TestBed.configureTestingModule({
      imports: [AmenityRequestsComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]),
        { provide: AmenityRequestService, useValue: serviceSpy },
        { provide: ToastService,          useValue: toastSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AmenityRequestsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should call getMine', () => {
    expect(serviceSpy.getMine).toHaveBeenCalledWith(1, 10, undefined);
  });

  it('ngOnInit — should populate requests signal', () => {
    expect(component.requests().length).toBe(2);
  });

  it('ngOnInit — should set totalCount', () => {
    expect(component.totalCount()).toBe(2);
  });

  it('loading — should be false after load', () => {
    expect(component.loading()).toBeFalse();
  });

  it('load — should set loading to false on error', () => {
    serviceSpy.getMine.and.returnValue(throwError(() => new Error('fail')));
    component.load();
    expect(component.loading()).toBeFalse();
  });

  // ── submit ────────────────────────────────────────────────────────────────

  it('submit — should call create when form is valid', () => {
    component.form.patchValue({ amenityName: 'Sauna', category: 'Services' });
    component.submit();
    expect(serviceSpy.create).toHaveBeenCalled();
  });

  it('submit — should show success toast', () => {
    component.form.patchValue({ amenityName: 'Sauna', category: 'Services' });
    component.submit();
    expect(toastSpy.success).toHaveBeenCalledWith('Request submitted!');
  });

  it('submit — should reset form after success', () => {
    component.form.patchValue({ amenityName: 'Sauna', category: 'Services' });
    component.submit();
    expect(component.form.get('amenityName')?.value).toBeFalsy();
  });

  it('submit — should NOT call service when form is invalid', () => {
    component.submit();
    expect(serviceSpy.create).not.toHaveBeenCalled();
  });

  it('submit — should mark all touched when form is invalid', () => {
    component.submit();
    expect(component.form.get('amenityName')?.touched).toBeTrue();
  });

  it('submit — should reset submitting to false on error', () => {
    serviceSpy.create.and.returnValue(throwError(() => new Error('fail')));
    component.form.patchValue({ amenityName: 'Sauna', category: 'Services' });
    component.submit();
    expect(component.submitting()).toBeFalse();
  });

  // ── statusClass ───────────────────────────────────────────────────────────

  it('statusClass — Approved → badge-success', () => expect(component.statusClass('Approved')).toBe('badge-success'));
  it('statusClass — Pending → badge-warning',  () => expect(component.statusClass('Pending')).toBe('badge-warning'));
  it('statusClass — Rejected → badge-error',   () => expect(component.statusClass('Rejected')).toBe('badge-error'));
  it('statusClass — unknown → badge-muted',    () => expect(component.statusClass('Unknown')).toBe('badge-muted'));

  // ── onPage ────────────────────────────────────────────────────────────────

  it('onPage — should update currentPage and reload', () => {
    serviceSpy.getMine.calls.reset();
    component.onPage({ pageIndex: 1, pageSize: 10, length: 20 } as any);
    expect(component.currentPage).toBe(2);
    expect(serviceSpy.getMine).toHaveBeenCalled();
  });

  // ── onSearch debounce ─────────────────────────────────────────────────────

  it('onSearch — should update searchTerm after debounce', fakeAsync(() => {
    serviceSpy.getMine.calls.reset();
    component.onSearch({ target: { value: 'Sauna' } } as any);
    tick(400);
    expect(component.searchTerm).toBe('Sauna');
    expect(serviceSpy.getMine).toHaveBeenCalled();
  }));
});
