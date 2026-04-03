import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { GuestSupportRequestsComponent } from './guest-support-requests.component';
import { SupportRequestService } from '../../../core/services/support-request.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

const MOCK_REQUESTS = [
  { supportRequestId: 'sr-001', subject: 'Room issue', category: 'Room',    status: 'Open',       createdAt: '2025-01-10T10:00:00Z', adminResponse: null },
  { supportRequestId: 'sr-002', subject: 'Billing',    category: 'Billing', status: 'InProgress', createdAt: '2025-01-11T10:00:00Z', adminResponse: null },
  { supportRequestId: 'sr-003', subject: 'Feedback',   category: 'General', status: 'Resolved',   createdAt: '2025-01-12T10:00:00Z', adminResponse: 'Done.' },
];
const MOCK_PAGED = { totalCount: 3, requests: MOCK_REQUESTS };

describe('GuestSupportRequestsComponent', () => {
  let component: GuestSupportRequestsComponent;
  let fixture: ComponentFixture<GuestSupportRequestsComponent>;
  let serviceSpy: jasmine.SpyObj<SupportRequestService>;

  beforeEach(async () => {
    serviceSpy = jasmine.createSpyObj('SupportRequestService', ['getGuestRequests']);
    serviceSpy.getGuestRequests.and.returnValue(of(MOCK_PAGED as any));

    await TestBed.configureTestingModule({
      imports: [GuestSupportRequestsComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]),
        { provide: SupportRequestService, useValue: serviceSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(GuestSupportRequestsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should call getGuestRequests', () => {
    expect(serviceSpy.getGuestRequests).toHaveBeenCalledWith(1, 10);
  });

  it('ngOnInit — should populate requests signal', () => {
    expect(component.requests().length).toBe(3);
  });

  it('ngOnInit — should set totalCount', () => {
    expect(component.totalCount()).toBe(3);
  });

  it('loading — should be false after load', () => {
    expect(component.loading()).toBeFalse();
  });

  it('load — should set loading to false on error', () => {
    serviceSpy.getGuestRequests.and.returnValue(throwError(() => new Error('fail')));
    component.load();
    expect(component.loading()).toBeFalse();
  });

  // ── onPage ────────────────────────────────────────────────────────────────

  it('onPage — should update currentPage and reload', () => {
    serviceSpy.getGuestRequests.calls.reset();
    component.onPage({ pageIndex: 1, pageSize: 10, length: 20 } as any);
    expect(component.currentPage).toBe(2);
    expect(serviceSpy.getGuestRequests).toHaveBeenCalledWith(2, 10);
  });

  // ── statusClass ───────────────────────────────────────────────────────────

  it('statusClass — Open → badge-warning',       () => expect(component.statusClass('Open')).toBe('badge-warning'));
  it('statusClass — InProgress → badge-primary', () => expect(component.statusClass('InProgress')).toBe('badge-primary'));
  it('statusClass — Resolved → badge-success',   () => expect(component.statusClass('Resolved')).toBe('badge-success'));
  it('statusClass — unknown → badge-muted',      () => expect(component.statusClass('Unknown')).toBe('badge-muted'));

  // ── statusIcon ────────────────────────────────────────────────────────────

  it('statusIcon — Open → radio_button_unchecked', () => expect(component.statusIcon('Open')).toBe('radio_button_unchecked'));
  it('statusIcon — InProgress → pending',          () => expect(component.statusIcon('InProgress')).toBe('pending'));
  it('statusIcon — Resolved → check_circle',       () => expect(component.statusIcon('Resolved')).toBe('check_circle'));
  it('statusIcon — unknown → help',                () => expect(component.statusIcon('Unknown')).toBe('help'));
});
