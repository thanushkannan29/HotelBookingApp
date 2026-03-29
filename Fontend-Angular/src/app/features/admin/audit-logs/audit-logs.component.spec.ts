import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { AuditLogsComponent } from './audit-logs.component';
import { AuditLogService } from '../../../core/services/api.services';
import { of, throwError } from 'rxjs';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideNativeDateAdapter } from '@angular/material/core';

const MOCK_RESPONSE = {
  totalCount: 3,
  logs: [
    { auditLogId: 'al-001', userId: 'u1', action: 'HotelUpdated', entityName: 'Hotel', entityId: 'h1', changes: '{}', createdAt: '2025-01-10T10:00:00Z' },
    { auditLogId: 'al-002', userId: 'u1', action: 'RoomAdded',    entityName: 'Room',  entityId: 'r1', changes: '{}', createdAt: '2025-01-11T12:00:00Z' },
    { auditLogId: 'al-003', userId: 'u2', action: 'RefundApproved', entityName: 'RefundRequest', entityId: 'rf1', changes: '{}', createdAt: '2025-01-12T09:30:00Z' }
  ]
};

function buildTestBed(mode: string, response = MOCK_RESPONSE) {
  const auditSpy = jasmine.createSpyObj('AuditLogService', ['getAdminAuditLogs', 'getAllAuditLogs']);
  auditSpy.getAdminAuditLogs.and.returnValue(of(response));
  auditSpy.getAllAuditLogs.and.returnValue(of(response));
  return { auditSpy, routeData: { snapshot: { data: mode ? { mode } : {} } } };
}

describe('AuditLogsComponent', () => {
  let component: AuditLogsComponent;
  let fixture: ComponentFixture<AuditLogsComponent>;
  let auditSpy: jasmine.SpyObj<AuditLogService>;

  beforeEach(async () => {
    const { auditSpy: spy, routeData } = buildTestBed('');
    auditSpy = spy;

    await TestBed.configureTestingModule({
      imports: [AuditLogsComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]), provideNativeDateAdapter(),
        { provide: AuditLogService, useValue: auditSpy },
        { provide: ActivatedRoute, useValue: routeData }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AuditLogsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── Creation ──────────────────────────────────────────────────────────────

  it('should create', () => expect(component).toBeTruthy());

  // ── Default state ─────────────────────────────────────────────────────────

  it('should default isSuperMode to false when no route data', () => {
    expect(component.isSuperMode).toBeFalse();
  });

  it('should start on currentPage 1', () => {
    expect(component.currentPage).toBe(1);
  });

  it('should have pageSize of 20', () => {
    expect(component.pageSize).toBe(20);
  });

  // ── ngOnInit / load ───────────────────────────────────────────────────────

  it('ngOnInit — should call getAdminAuditLogs when mode is admin', () => {
    expect(auditSpy.getAdminAuditLogs).toHaveBeenCalledWith(1, 20, undefined);
    expect(auditSpy.getAllAuditLogs).not.toHaveBeenCalled();
  });

  it('ngOnInit — should populate logs signal', () => {
    expect(component.logs().length).toBe(3);
    expect(component.logs()[0].auditLogId).toBe('al-001');
  });

  it('ngOnInit — should set totalCount signal', () => {
    expect(component.totalCount()).toBe(3);
  });

  it('loading — should be false after data loads', () => {
    expect(component.loading()).toBeFalse();
  });

  // ── SuperAdmin mode ───────────────────────────────────────────────────────

  it('should call getAllAuditLogs when route data mode is superadmin', async () => {
    const { auditSpy: spy2, routeData } = buildTestBed('superadmin');
    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [AuditLogsComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]), provideNativeDateAdapter(),
        { provide: AuditLogService, useValue: spy2 },
        { provide: ActivatedRoute, useValue: routeData }
      ]
    }).compileComponents();
    const f = TestBed.createComponent(AuditLogsComponent);
    f.detectChanges();
    expect(spy2.getAllAuditLogs).toHaveBeenCalled();
    expect(spy2.getAdminAuditLogs).not.toHaveBeenCalled();
  });

  it('isSuperMode — should be true when route data mode is superadmin', async () => {
    const { auditSpy: spy2, routeData } = buildTestBed('superadmin');
    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [AuditLogsComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]), provideNativeDateAdapter(),
        { provide: AuditLogService, useValue: spy2 },
        { provide: ActivatedRoute, useValue: routeData }
      ]
    }).compileComponents();
    const f = TestBed.createComponent(AuditLogsComponent);
    const cmp = f.componentInstance;
    f.detectChanges();
    expect(cmp.isSuperMode).toBeTrue();
  });

  // ── backLink ──────────────────────────────────────────────────────────────

  it('backLink — returns /admin/dashboard when isSuperMode is false', () => {
    component.isSuperMode = false;
    expect(component.backLink).toBe('/admin/dashboard');
  });

  it('backLink — returns /superadmin/dashboard when isSuperMode is true', () => {
    component.isSuperMode = true;
    expect(component.backLink).toBe('/superadmin/dashboard');
  });

  // ── onPage ────────────────────────────────────────────────────────────────

  it('onPage — should update currentPage and reload', () => {
    auditSpy.getAdminAuditLogs.calls.reset();
    component.onPage({ pageIndex: 1, pageSize: 20, length: 40 } as any);
    expect(component.currentPage).toBe(2);
    expect(auditSpy.getAdminAuditLogs).toHaveBeenCalledWith(2, 20, undefined);
  });

  it('onPage — should update pageSize', () => {
    component.onPage({ pageIndex: 0, pageSize: 10, length: 40 } as any);
    expect(component.pageSize).toBe(10);
  });

  // ── applyFilters / clearFilters ───────────────────────────────────────────

  it('applyFilters — should reset to page 1 and reload', () => {
    component.currentPage = 3;
    auditSpy.getAdminAuditLogs.calls.reset();
    component.applyFilters();
    expect(component.currentPage).toBe(1);
    expect(auditSpy.getAdminAuditLogs).toHaveBeenCalled();
  });

  it('clearFilters — should reset form and reload', () => {
    component.filterForm.patchValue({ action: 'HotelUpdated' });
    auditSpy.getAdminAuditLogs.calls.reset();
    component.clearFilters();
    expect(component.filterForm.get('action')?.value).toBeFalsy();
    expect(auditSpy.getAdminAuditLogs).toHaveBeenCalled();
  });

  // ── actionClass ───────────────────────────────────────────────────────────

  it('actionClass — CREATE → badge-success', () => {
    expect(component.actionClass('CREATE')).toBe('badge-success');
  });

  it('actionClass — UPDATE → badge-warning', () => {
    expect(component.actionClass('UPDATE')).toBe('badge-warning');
  });

  it('actionClass — DELETE → badge-error', () => {
    expect(component.actionClass('DELETE')).toBe('badge-error');
  });

  it('actionClass — LOGIN → badge-info', () => {
    expect(component.actionClass('LOGIN')).toBe('badge-info');
  });

  it('actionClass — unknown → badge-muted', () => {
    expect(component.actionClass('SomeRandomAction')).toBe('badge-muted');
  });

  it('actionClass — case-insensitive (create → badge-success)', () => {
    expect(component.actionClass('create')).toBe('badge-success');
  });

  // ── Error handling ────────────────────────────────────────────────────────

  it('load — should set loading to false on error', () => {
    auditSpy.getAdminAuditLogs.and.returnValue(throwError(() => new Error('fail')));
    component.load();
    expect(component.loading()).toBeFalse();
  });

  // ── Search debounce ───────────────────────────────────────────────────────

  it('onSearch — should update searchTerm after debounce', fakeAsync(() => {
    auditSpy.getAdminAuditLogs.calls.reset();
    const event = { target: { value: 'Hotel' } } as any;
    component.onSearch(event);
    tick(400);
    expect(component.searchTerm).toBe('Hotel');
    expect(auditSpy.getAdminAuditLogs).toHaveBeenCalled();
  }));
});
