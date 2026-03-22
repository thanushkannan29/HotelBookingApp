import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { of } from 'rxjs';
import { ErrorLogsComponent } from './error-logs.component';
import { LogService } from '../../../core/services/api.services';
import { LogResponseDto } from '../../../core/models/models';

// ── Mock data ──────────────────────────────────────────────────────────────────

function makeLog(id: string, statusCode: number, overrides?: Partial<LogResponseDto>): LogResponseDto {
  return {
    logId:       id,
    message:     `Error message for ${id}`,
    exceptionType: 'ApplicationException',
    stackTrace:  'at SomeService.doThing() line 42',
    statusCode,
    userName:    'Thanush K',
    role:        'Guest',
    userId:      'usr-001',
    controller:  'HotelController',
    action:      'GetHotel',
    httpMethod:  'GET',
    requestPath: '/api/hotels/hotel-001',
    createdAt:   '2025-01-10T10:00:00Z',
    ...overrides
  };
}

const MOCK_LOGS: LogResponseDto[] = [
  makeLog('log-001', 500, { message: 'Internal server error', exceptionType: 'NullReferenceException' }),
  makeLog('log-002', 404, { message: 'Hotel not found',       exceptionType: 'NotFoundException'      }),
  makeLog('log-003', 401, { message: 'Unauthorized access',   exceptionType: 'UnAuthorizedException'  }),
  makeLog('log-004', 400, { message: 'Validation failed',     exceptionType: 'ValidationException'    }),
  makeLog('log-005', 200, { message: 'Unexpected success log',exceptionType: 'InfoException'          }),
];

const MOCK_PAGED = { totalCount: 5, logs: MOCK_LOGS };

// ─────────────────────────────────────────────────────────────────────────────

describe('ErrorLogsComponent', () => {
  let component: ErrorLogsComponent;
  let fixture:   ComponentFixture<ErrorLogsComponent>;
  let logSpy:    jasmine.SpyObj<LogService>;

  beforeEach(async () => {
    logSpy = jasmine.createSpyObj('LogService', ['getAllLogs']);
    logSpy.getAllLogs.and.returnValue(of(MOCK_PAGED));

    await TestBed.configureTestingModule({
      imports: [ErrorLogsComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideAnimationsAsync(),   // required by MatExpansionModule
        { provide: LogService, useValue: logSpy },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(ErrorLogsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── CONSTANTS ──────────────────────────────────────────────────────────────

  it('pageSize — should be 20', () => {
    expect(component.pageSize).toBe(20);
  });

  // ── INITIAL SIGNAL STATE ───────────────────────────────────────────────────

  it('page — should start at 1', () => {
    expect(component.page()).toBe(1);
  });

  // ── ngOnInit / load() ──────────────────────────────────────────────────────

  it('ngOnInit — should call getAllLogs with page 1 and pageSize 20', () => {
    expect(logSpy.getAllLogs).toHaveBeenCalledWith(1, 20);
  });

  it('load() — should populate logs signal', () => {
    expect(component.logs().length).toBe(5);
    expect(component.logs()[0].logId).toBe('log-001');
  });

  it('load() — should set total signal to totalCount', () => {
    expect(component.total()).toBe(5);
  });

  it('load() — should handle empty log list', async () => {
    logSpy.getAllLogs.and.returnValue(of({ totalCount: 0, logs: [] }));

    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [ErrorLogsComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideAnimationsAsync(),
        { provide: LogService, useValue: logSpy },
      ]
    }).compileComponents();

    const f   = TestBed.createComponent(ErrorLogsComponent);
    const cmp = f.componentInstance;
    f.detectChanges();

    expect(cmp.logs().length).toBe(0);
    expect(cmp.total()).toBe(0);
  });

  // ── totalPages GETTER ──────────────────────────────────────────────────────

  it('totalPages — should be 1 when total is 5 and pageSize is 20', () => {
    expect(component.totalPages).toBe(1);
  });

  it('totalPages — should be 2 when total is 21', () => {
    component.total.set(21);
    expect(component.totalPages).toBe(2);
  });

  it('totalPages — should be 3 when total is 41', () => {
    component.total.set(41);
    expect(component.totalPages).toBe(3);
  });

  it('totalPages — should be 0 when total is 0', () => {
    component.total.set(0);
    expect(component.totalPages).toBe(0);
  });

  it('totalPages — should round up correctly (e.g. 21 items → 2 pages)', () => {
    component.total.set(21);
    expect(component.totalPages).toBe(2);
  });

  // ── statusClass() ──────────────────────────────────────────────────────────

  it('statusClass(500) — should return badge-error', () => {
    expect(component.statusClass(500)).toBe('badge-error');
  });

  it('statusClass(503) — should return badge-error for any 5xx', () => {
    expect(component.statusClass(503)).toBe('badge-error');
  });

  it('statusClass(400) — should return badge-warning', () => {
    expect(component.statusClass(400)).toBe('badge-warning');
  });

  it('statusClass(404) — should return badge-warning for any 4xx', () => {
    expect(component.statusClass(404)).toBe('badge-warning');
  });

  it('statusClass(401) — should return badge-warning', () => {
    expect(component.statusClass(401)).toBe('badge-warning');
  });

  it('statusClass(200) — should return badge-success', () => {
    expect(component.statusClass(200)).toBe('badge-success');
  });

  it('statusClass(201) — should return badge-success', () => {
    expect(component.statusClass(201)).toBe('badge-success');
  });

  it('statusClass(0) — should return badge-success for codes below 400', () => {
    expect(component.statusClass(0)).toBe('badge-success');
  });

  it('statusClass — 5xx takes priority over 4xx check', () => {
    // 500 >= 500 → badge-error (not badge-warning)
    expect(component.statusClass(500)).toBe('badge-error');
    expect(component.statusClass(500)).not.toBe('badge-warning');
  });

  // ── next() / prev() ────────────────────────────────────────────────────────

  it('next() — should increment page and reload', () => {
    logSpy.getAllLogs.calls.reset();

    component.next();

    expect(component.page()).toBe(2);
    expect(logSpy.getAllLogs).toHaveBeenCalledWith(2, 20);
  });

  it('next() — calling twice should reach page 3', () => {
    component.next();
    component.next();
    expect(component.page()).toBe(3);
  });

  it('prev() — should decrement page and reload when on page 2', () => {
    component.next();                    // go to page 2
    logSpy.getAllLogs.calls.reset();

    component.prev();

    expect(component.page()).toBe(1);
    expect(logSpy.getAllLogs).toHaveBeenCalledWith(1, 20);
  });

  it('prev() — should NOT go below page 1', () => {
    logSpy.getAllLogs.calls.reset();

    component.prev();                    // already on page 1

    expect(component.page()).toBe(1);
    expect(logSpy.getAllLogs).not.toHaveBeenCalled();
  });

  it('next() then prev() should return to page 1', () => {
    component.next();
    expect(component.page()).toBe(2);

    component.prev();
    expect(component.page()).toBe(1);
  });

  // ── LOG DATA ───────────────────────────────────────────────────────────────

  it('logs — should contain correct statusCodes', () => {
    const codes = component.logs().map(l => l.statusCode);
    expect(codes).toContain(500);
    expect(codes).toContain(404);
    expect(codes).toContain(401);
  });

  it('logs — should contain correct exception types', () => {
    expect(component.logs()[0].exceptionType).toBe('NullReferenceException');
    expect(component.logs()[1].exceptionType).toBe('NotFoundException');
  });

  it('logs — should contain stack traces', () => {
    expect(component.logs()[0].stackTrace).toContain('SomeService');
  });

  it('logs — signal can be updated directly', () => {
    const single = [makeLog('log-x', 500)];
    component.logs.set(single);
    expect(component.logs().length).toBe(1);
    expect(component.logs()[0].logId).toBe('log-x');
  });

  // ── TEMPLATE RENDERS ───────────────────────────────────────────────────────

  it('should render error messages in the template', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Internal server error');
  });
});