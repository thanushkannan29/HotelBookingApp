import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { AuditLogsComponent } from './audit-logs.component';
import { AuditLogService } from '../../../core/services/api.services';
import { of } from 'rxjs';
import { AuditLogResponseDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ── Mock data ──────────────────────────────────────────────────────────────────

const MOCK_LOGS: AuditLogResponseDto[] = [
  {
    auditLogId: 'al-001',
    userId: 'usr-001',
    action: 'HotelUpdated',
    entityName: 'Hotel',
    entityId: 'hotel-001',
    changes: '{"before":{"name":"Old Name"},"after":{"name":"New Name"}}',
    createdAt: '2025-01-10T10:00:00Z'
  },
  {
    auditLogId: 'al-002',
    userId: 'usr-001',
    action: 'RoomAdded',
    entityName: 'Room',
    entityId: 'r-001',
    changes: '{"roomNumber":"101","floor":1}',
    createdAt: '2025-01-11T12:00:00Z'
  },
  {
    auditLogId: 'al-003',
    userId: 'usr-002',
    action: 'RefundApproved',
    entityName: 'RefundRequest',
    entityId: 'rf-001',
    changes: '{"reservationCode":"RES-ABCD1234"}',
    createdAt: '2025-01-12T09:30:00Z'
  }
];

const MOCK_PAGED_RESPONSE = { totalCount: 3, logs: MOCK_LOGS };
const MOCK_EMPTY_RESPONSE  = { totalCount: 0, logs: [] };

// ── Helper: build a mock AuditLogService ───────────────────────────────────────

function mockAuditLogService(response = MOCK_PAGED_RESPONSE) {
  return {
    getAdminAuditLogs: jasmine.createSpy('getAdminAuditLogs').and.returnValue(of(response)),
    getAllAuditLogs:    jasmine.createSpy('getAllAuditLogs').and.returnValue(of(response))
  };
}

// ─────────────────────────────────────────────────────────────────────────────

describe('AuditLogsComponent', () => {
  let component: AuditLogsComponent;
  let fixture: ComponentFixture<AuditLogsComponent>;
  let auditSpy: ReturnType<typeof mockAuditLogService>;

  beforeEach(async () => {
    auditSpy = mockAuditLogService();

    await TestBed.configureTestingModule({
      imports: [AuditLogsComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuditLogService, useValue: auditSpy },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { data: {} } } // no route data → defaults to 'admin' mode
        }
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(AuditLogsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ─────────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── DEFAULT STATE ─────────────────────────────────────────────────────────────

  it('should default to mode = admin', () => {
    expect(component.mode).toBe('admin');
  });

  it('should start on page 1', () => {
    expect(component.page()).toBe(1);
  });

  it('should have pageSize of 20', () => {
    expect(component.pageSize).toBe(20);
  });

  // ── ngOnInit / load ───────────────────────────────────────────────────────────

  it('ngOnInit — should call getAdminAuditLogs when mode is admin', () => {
    expect(auditSpy.getAdminAuditLogs).toHaveBeenCalledOnceWith(1, 20);
    expect(auditSpy.getAllAuditLogs).not.toHaveBeenCalled();
  });

  it('ngOnInit — should populate logs signal with returned data', () => {
    expect(component.logs().length).toBe(3);
    expect(component.logs()[0].auditLogId).toBe('al-001');
    expect(component.logs()[1].action).toBe('RoomAdded');
  });

  it('ngOnInit — should set total signal to totalCount from response', () => {
    expect(component.total()).toBe(3);
  });

  // ── MODE: superadmin ──────────────────────────────────────────────────────────

  it('should call getAllAuditLogs when mode is superadmin', async () => {
    auditSpy = mockAuditLogService();

    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [AuditLogsComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuditLogService, useValue: auditSpy },
        { provide: ActivatedRoute, useValue: { snapshot: { data: { mode: 'superadmin' } } } }
      ]
    }).compileComponents();

    const f = TestBed.createComponent(AuditLogsComponent);
    f.detectChanges();

    expect(auditSpy.getAllAuditLogs).toHaveBeenCalledOnceWith(1, 20);
    expect(auditSpy.getAdminAuditLogs).not.toHaveBeenCalled();
  });

  // ── ROUTE DATA overrides @Input mode ─────────────────────────────────────────

  it('should override mode from route data when route data has mode=superadmin', async () => {
    auditSpy = mockAuditLogService();

    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [AuditLogsComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuditLogService, useValue: auditSpy },
        { provide: ActivatedRoute, useValue: { snapshot: { data: { mode: 'superadmin' } } } }
      ]
    }).compileComponents();

    const f   = TestBed.createComponent(AuditLogsComponent);
    const cmp = f.componentInstance;
    f.detectChanges();

    expect(cmp.mode).toBe('superadmin');
  });

  // ── backLink ──────────────────────────────────────────────────────────────────

  it('backLink — should return /admin/dashboard when mode is admin', () => {
    component.mode = 'admin';
    expect(component.backLink).toBe('/admin/dashboard');
  });

  it('backLink — should return /superadmin/dashboard when mode is superadmin', () => {
    component.mode = 'superadmin';
    expect(component.backLink).toBe('/superadmin/dashboard');
  });

  // ── totalPages ────────────────────────────────────────────────────────────────

  it('totalPages — should be 1 when totalCount <= pageSize', () => {
    component.total.set(15);
    expect(component.totalPages).toBe(1);
  });

  it('totalPages — should be 2 when totalCount is 21 (pageSize 20)', () => {
    component.total.set(21);
    expect(component.totalPages).toBe(2);
  });

  it('totalPages — should be 0 when no logs exist', () => {
    component.total.set(0);
    expect(component.totalPages).toBe(0);
  });

  it('totalPages — should round up correctly (e.g. 41 items → 3 pages)', () => {
    component.total.set(41);
    expect(component.totalPages).toBe(3);
  });

  // ── next() / prev() ───────────────────────────────────────────────────────────

  it('next() — should increment page and reload', () => {
    component.total.set(50); // 3 pages
    auditSpy.getAdminAuditLogs.calls.reset();

    component.next();

    expect(component.page()).toBe(2);
    expect(auditSpy.getAdminAuditLogs).toHaveBeenCalledWith(2, 20);
  });

  it('next() — calling twice should go to page 3', () => {
    component.total.set(100);
    component.next();
    component.next();
    expect(component.page()).toBe(3);
  });

  it('prev() — should decrement page and reload when page > 1', () => {
    component.total.set(50);
    component.next(); // page = 2
    auditSpy.getAdminAuditLogs.calls.reset();

    component.prev();

    expect(component.page()).toBe(1);
    expect(auditSpy.getAdminAuditLogs).toHaveBeenCalledWith(1, 20);
  });

  it('prev() — should NOT go below page 1', () => {
    auditSpy.getAdminAuditLogs.calls.reset();
    component.prev(); // already on page 1

    expect(component.page()).toBe(1);
    expect(auditSpy.getAdminAuditLogs).not.toHaveBeenCalled();
  });

  // ── actionClass() ─────────────────────────────────────────────────────────────

  it('actionClass() — HotelUpdated → action-update', () => {
    expect(component.actionClass('HotelUpdated')).toBe('action-update');
  });

  it('actionClass() — RoomAdded → action-create', () => {
    expect(component.actionClass('RoomAdded')).toBe('action-create');
  });

  it('actionClass() — RoomTypeCreate → action-create', () => {
    expect(component.actionClass('RoomTypeCreate')).toBe('action-create');
  });

  it('actionClass() — HotelDeactivated → action-delete', () => {
    expect(component.actionClass('HotelDeactivated')).toBe('action-delete');
  });

  it('actionClass() — HotelBlocked → action-delete', () => {
    expect(component.actionClass('HotelBlocked')).toBe('action-delete');
  });

  it('actionClass() — RefundApproved → action-approve', () => {
    expect(component.actionClass('RefundApproved')).toBe('action-approve');
  });

  it('actionClass() — RefundRejected → action-reject', () => {
    expect(component.actionClass('RefundRejected')).toBe('action-reject');
  });

  it('actionClass() — unknown action → action-default', () => {
    expect(component.actionClass('SomeRandomAction')).toBe('action-default');
  });

  it('actionClass() — should be case-insensitive (HOTELBLOCKED → action-delete)', () => {
    expect(component.actionClass('HOTELBLOCKED')).toBe('action-delete');
  });

  // ── EMPTY STATE ───────────────────────────────────────────────────────────────

  it('should show empty state when logs array is empty', async () => {
    auditSpy = mockAuditLogService(MOCK_EMPTY_RESPONSE);

    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [AuditLogsComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuditLogService, useValue: auditSpy },
        { provide: ActivatedRoute, useValue: { snapshot: { data: {} } } }
      ]
    }).compileComponents();

    const f = TestBed.createComponent(AuditLogsComponent);
    f.detectChanges();
    await f.whenStable();

    const compiled = f.nativeElement as HTMLElement;
    expect(compiled.querySelector('.empty-state')).toBeTruthy();
    expect(compiled.querySelector('.table-card')).toBeFalsy();
  });

  // ── TABLE RENDERS ─────────────────────────────────────────────────────────────

  it('should render table rows for each log entry', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows.length).toBe(3);
  });

  it('should display the action chip text in the first row', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    const firstChip = fixture.nativeElement.querySelector('.action-chip');
    expect(firstChip.textContent.trim()).toBe('HotelUpdated');
  });
});