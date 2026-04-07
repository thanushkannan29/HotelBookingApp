import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { SuperadminSupportRequestsComponent } from './superadmin-support-requests.component';
import { SupportRequestService } from '../../../core/services/support-request.service';
import { ToastService } from '../../../core/services/toast.service';
import { MatDialog } from '@angular/material/dialog';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

const MOCK_REQUESTS = [
  { supportRequestId: 'sr-001', subject: 'Room issue', category: 'Room',    status: 'Open',       role: 'Guest',  createdAt: '2025-01-10T10:00:00Z', adminResponse: null },
  { supportRequestId: 'sr-002', subject: 'Billing',    category: 'Billing', status: 'InProgress', role: 'Admin',  createdAt: '2025-01-11T10:00:00Z', adminResponse: null },
  { supportRequestId: 'sr-003', subject: 'Feedback',   category: 'General', status: 'Resolved',   role: 'Public', createdAt: '2025-01-12T10:00:00Z', adminResponse: 'Done.' },
];
const MOCK_PAGED = { totalCount: 3, requests: MOCK_REQUESTS };

describe('SuperadminSupportRequestsComponent', () => {
  let component: SuperadminSupportRequestsComponent;
  let fixture: ComponentFixture<SuperadminSupportRequestsComponent>;
  let serviceSpy: jasmine.SpyObj<SupportRequestService>;
  let toastSpy: jasmine.SpyObj<ToastService>;
  let dialog: MatDialog;

  beforeEach(async () => {
    serviceSpy = jasmine.createSpyObj('SupportRequestService', ['getAll', 'respond']);
    toastSpy   = jasmine.createSpyObj('ToastService', ['success', 'error']);

    serviceSpy.getAll.and.returnValue(of(MOCK_PAGED as any));
    serviceSpy.respond.and.returnValue(of(MOCK_REQUESTS[0] as any));

    await TestBed.configureTestingModule({
      imports: [SuperadminSupportRequestsComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]),
        { provide: SupportRequestService, useValue: serviceSpy },
        { provide: ToastService,          useValue: toastSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(SuperadminSupportRequestsComponent);
    component = fixture.componentInstance;
    dialog = TestBed.inject(MatDialog);
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should call getAll', () => {
    expect(serviceSpy.getAll).toHaveBeenCalledWith('All', 'All', '', 1, 10);
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
    serviceSpy.getAll.and.returnValue(throwError(() => new Error('fail')));
    component.load();
    expect(component.loading()).toBeFalse();
  });

  // ── onStatusTab ───────────────────────────────────────────────────────────

  it('onStatusTab — should set selectedStatus and reload', () => {
    serviceSpy.getAll.calls.reset();
    component.onStatusTab(1); // 'Open'
    expect(component.selectedStatus).toBe('Open');
    expect(serviceSpy.getAll).toHaveBeenCalled();
  });

  it('onStatusTab — should reset currentPage to 1', () => {
    component.currentPage = 3;
    component.onStatusTab(2);
    expect(component.currentPage).toBe(1);
  });

  // ── onRoleTab ─────────────────────────────────────────────────────────────

  it('onRoleTab — should set selectedRole and reload', () => {
    serviceSpy.getAll.calls.reset();
    component.onRoleTab(1); // 'Guest'
    expect(component.selectedRole).toBe('Guest');
    expect(serviceSpy.getAll).toHaveBeenCalled();
  });

  // ── onPage ────────────────────────────────────────────────────────────────

  it('onPage — should update currentPage and reload', () => {
    serviceSpy.getAll.calls.reset();
    component.onPage({ pageIndex: 1, pageSize: 10, length: 20 } as any);
    expect(component.currentPage).toBe(2);
    expect(serviceSpy.getAll).toHaveBeenCalled();
  });

  // ── respond ───────────────────────────────────────────────────────────────

  it('respond — should open input dialog', () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of('Here is my response') } as any);
    component.respond(MOCK_REQUESTS[0] as any);
    expect(MatDialog.prototype.open).toHaveBeenCalled();
  });

  it('respond — should call respond service with Resolved status', () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of('Here is my response') } as any);
    component.respond(MOCK_REQUESTS[0] as any);
    expect(serviceSpy.respond).toHaveBeenCalledWith('sr-001', { response: 'Here is my response', status: 'Resolved' });
  });

  it('respond — should show success toast', () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of('Here is my response') } as any);
    component.respond(MOCK_REQUESTS[0] as any);
    expect(toastSpy.success).toHaveBeenCalledWith('Response sent.');
  });

  it('respond — should NOT call service when dialog returns null', () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of(null) } as any);
    component.respond(MOCK_REQUESTS[0] as any);
    expect(serviceSpy.respond).not.toHaveBeenCalled();
  });

  // ── markInProgress ────────────────────────────────────────────────────────

  it('markInProgress — should call respond with InProgress status', () => {
    component.markInProgress(MOCK_REQUESTS[0] as any);
    expect(serviceSpy.respond).toHaveBeenCalledWith('sr-001', { response: '', status: 'InProgress' });
  });

  it('markInProgress — should show success toast', () => {
    component.markInProgress(MOCK_REQUESTS[0] as any);
    expect(toastSpy.success).toHaveBeenCalledWith('Marked as In Progress.');
  });

  // ── statusClass ───────────────────────────────────────────────────────────

  it('statusClass — Open → badge-warning',       () => expect(component.statusClass('Open')).toBe('badge-warning'));
  it('statusClass — InProgress → badge-primary', () => expect(component.statusClass('InProgress')).toBe('badge-primary'));
  it('statusClass — Resolved → badge-success',   () => expect(component.statusClass('Resolved')).toBe('badge-success'));
  it('statusClass — unknown → badge-muted',      () => expect(component.statusClass('Unknown')).toBe('badge-muted'));

  // ── roleClass ─────────────────────────────────────────────────────────────

  it('roleClass — Guest → badge-primary',  () => expect(component.roleClass('Guest')).toBe('badge-primary'));
  it('roleClass — Admin → badge-accent',   () => expect(component.roleClass('Admin')).toBe('badge-accent'));
  it('roleClass — Public → badge-muted',   () => expect(component.roleClass('Public')).toBe('badge-muted'));
  it('roleClass — unknown → badge-muted',  () => expect(component.roleClass('Unknown')).toBe('badge-muted'));

  // ── roleIcon ──────────────────────────────────────────────────────────────

  it('roleIcon — Guest → person',   () => expect(component.roleIcon('Guest')).toBe('person'));
  it('roleIcon — Admin → hotel',    () => expect(component.roleIcon('Admin')).toBe('hotel'));
  it('roleIcon — Public → public',  () => expect(component.roleIcon('Public')).toBe('public'));
  it('roleIcon — unknown → person', () => expect(component.roleIcon('Unknown')).toBe('person'));

  // ── onSearch debounce ─────────────────────────────────────────────────────

  it('onSearch — should update searchTerm after debounce', fakeAsync(() => {
    serviceSpy.getAll.calls.reset();
    component.onSearch({ target: { value: 'Room' } } as any);
    tick(400);
    expect(component.searchTerm).toBe('Room');
    expect(serviceSpy.getAll).toHaveBeenCalled();
  }));
});
