import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ErrorLogsComponent } from './error-logs.component';
import { LogService } from '../../../core/services/api.services';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

const MOCK_LOGS = [
  { logId: 'l-001', message: 'Not found', exceptionType: 'NotFoundException', stackTrace: '', statusCode: 404, userName: 'Alice', role: 'Guest', controller: 'Hotel', action: 'Get', httpMethod: 'GET', requestPath: '/api/hotels/x', createdAt: '2025-01-10T10:00:00Z' },
  { logId: 'l-002', message: 'Server error', exceptionType: 'Exception', stackTrace: '', statusCode: 500, userName: 'Bob', role: 'Admin', controller: 'Room', action: 'Post', httpMethod: 'POST', requestPath: '/api/rooms', createdAt: '2025-01-11T10:00:00Z' },
];
const MOCK_PAGED = { totalCount: 2, logs: MOCK_LOGS };

describe('ErrorLogsComponent', () => {
  let component: ErrorLogsComponent;
  let fixture: ComponentFixture<ErrorLogsComponent>;
  let logSpy: jasmine.SpyObj<LogService>;

  beforeEach(async () => {
    logSpy = jasmine.createSpyObj('LogService', ['getAllLogs']);
    logSpy.getAllLogs.and.returnValue(of(MOCK_PAGED as any));

    await TestBed.configureTestingModule({
      imports: [ErrorLogsComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]),
        { provide: LogService, useValue: logSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ErrorLogsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── Initial state ─────────────────────────────────────────────────────────

  it('pageSize — should be 20', () => expect(component.pageSize).toBe(20));
  it('currentPage — should start at 1', () => expect(component.currentPage).toBe(1));

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should call getAllLogs', () => {
    expect(logSpy.getAllLogs).toHaveBeenCalledWith(1, 20, undefined);
  });

  it('ngOnInit — should populate logs signal', () => {
    expect(component.logs().length).toBe(2);
  });

  it('ngOnInit — should set totalCount', () => {
    expect(component.totalCount()).toBe(2);
  });

  it('loading — should be false after load', () => {
    expect(component.loading()).toBeFalse();
  });

  it('load — should set loading to false on error', () => {
    logSpy.getAllLogs.and.returnValue(throwError(() => new Error('fail')));
    component.load();
    expect(component.loading()).toBeFalse();
  });

  // ── onPage ────────────────────────────────────────────────────────────────

  it('onPage — should update currentPage and reload', () => {
    logSpy.getAllLogs.calls.reset();
    component.onPage({ pageIndex: 1, pageSize: 20, length: 40 } as any);
    expect(component.currentPage).toBe(2);
    expect(logSpy.getAllLogs).toHaveBeenCalled();
  });

  it('onPage — should update pageSize', () => {
    component.onPage({ pageIndex: 0, pageSize: 50, length: 100 } as any);
    expect(component.pageSize).toBe(50);
  });

  // ── toggleRow ─────────────────────────────────────────────────────────────

  it('toggleRow — should set expandedRow', () => {
    component.toggleRow(MOCK_LOGS[0] as any);
    expect(component.expandedRow()?.logId).toBe('l-001');
  });

  it('toggleRow — should collapse when same row clicked twice', () => {
    component.toggleRow(MOCK_LOGS[0] as any);
    component.toggleRow(MOCK_LOGS[0] as any);
    expect(component.expandedRow()).toBeNull();
  });

  it('toggleRow — should switch to new row', () => {
    component.toggleRow(MOCK_LOGS[0] as any);
    component.toggleRow(MOCK_LOGS[1] as any);
    expect(component.expandedRow()?.logId).toBe('l-002');
  });

  // ── statusClass ───────────────────────────────────────────────────────────

  it('statusClass — 500 → badge-error',   () => expect(component.statusClass(500)).toBe('badge-error'));
  it('statusClass — 404 → badge-warning', () => expect(component.statusClass(404)).toBe('badge-warning'));
  it('statusClass — 200 → badge-success', () => expect(component.statusClass(200)).toBe('badge-success'));

  // ── onSearch debounce ─────────────────────────────────────────────────────

  it('onSearch — should update searchTerm after debounce', fakeAsync(() => {
    logSpy.getAllLogs.calls.reset();
    component.onSearch({ target: { value: 'Hotel' } } as any);
    tick(400);
    expect(component.searchTerm).toBe('Hotel');
    expect(logSpy.getAllLogs).toHaveBeenCalled();
  }));
});
