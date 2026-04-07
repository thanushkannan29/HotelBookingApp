import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { SuperadminAmenityRequestsComponent } from './superadmin-amenity-requests.component';
import { AmenityRequestService } from '../../../core/services/amenity-request.service';
import { ToastService } from '../../../core/services/toast.service';
import { MatDialog } from '@angular/material/dialog';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

const MOCK_REQUESTS = [
  { amenityRequestId: 'ar-001', amenityName: 'Sauna', category: 'Services', status: 'Pending',  hotelName: 'Grand Palace', adminName: 'Admin1', note: null, createdAt: '2025-01-10T10:00:00Z' },
  { amenityRequestId: 'ar-002', amenityName: 'Gym',   category: 'Services', status: 'Approved', hotelName: 'Sea View',     adminName: 'Admin2', note: null, createdAt: '2025-01-11T10:00:00Z' },
];
const MOCK_PAGED = { totalCount: 2, requests: MOCK_REQUESTS };

describe('SuperadminAmenityRequestsComponent', () => {
  let component: SuperadminAmenityRequestsComponent;
  let fixture: ComponentFixture<SuperadminAmenityRequestsComponent>;
  let serviceSpy: jasmine.SpyObj<AmenityRequestService>;
  let toastSpy: jasmine.SpyObj<ToastService>;
  let dialog: MatDialog;

  beforeEach(async () => {
    serviceSpy = jasmine.createSpyObj('AmenityRequestService', ['getAll', 'approve', 'reject']);
    toastSpy   = jasmine.createSpyObj('ToastService', ['success', 'error']);

    serviceSpy.getAll.and.returnValue(of(MOCK_PAGED as any));
    serviceSpy.approve.and.returnValue(of(MOCK_REQUESTS[0] as any));
    serviceSpy.reject.and.returnValue(of(MOCK_REQUESTS[0] as any));

    await TestBed.configureTestingModule({
      imports: [SuperadminAmenityRequestsComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]),
        { provide: AmenityRequestService, useValue: serviceSpy },
        { provide: ToastService,          useValue: toastSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(SuperadminAmenityRequestsComponent);
    component = fixture.componentInstance;
    dialog = TestBed.inject(MatDialog);
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should call getAll', () => {
    expect(serviceSpy.getAll).toHaveBeenCalledWith('All', 1, 10);
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
    serviceSpy.getAll.and.returnValue(throwError(() => new Error('fail')));
    component.load();
    expect(component.loading()).toBeFalse();
  });

  // ── statusTabs ────────────────────────────────────────────────────────────

  it('statusTabs — should contain All, Pending, Approved, Rejected', () => {
    expect(component.statusTabs).toEqual(['All', 'Pending', 'Approved', 'Rejected']);
  });

  // ── onTabChange ───────────────────────────────────────────────────────────

  it('onTabChange — should set selectedStatus and reload', () => {
    serviceSpy.getAll.calls.reset();
    component.onTabChange(1); // 'Pending'
    expect(component.selectedStatus).toBe('Pending');
    expect(serviceSpy.getAll).toHaveBeenCalledWith('Pending', 1, 10);
  });

  it('onTabChange — should reset currentPage to 1', () => {
    component.currentPage = 3;
    component.onTabChange(2);
    expect(component.currentPage).toBe(1);
  });

  // ── onPage ────────────────────────────────────────────────────────────────

  it('onPage — should update currentPage and reload', () => {
    serviceSpy.getAll.calls.reset();
    component.onPage({ pageIndex: 1, pageSize: 10, length: 20 } as any);
    expect(component.currentPage).toBe(2);
    expect(serviceSpy.getAll).toHaveBeenCalled();
  });

  // ── approve ───────────────────────────────────────────────────────────────

  it('approve — should open confirm dialog', () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of(true) } as any);
    component.approve(MOCK_REQUESTS[0] as any);
    expect(MatDialog.prototype.open).toHaveBeenCalled();
  });

  it('approve — should call approve service when dialog confirmed', () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of(true) } as any);
    component.approve(MOCK_REQUESTS[0] as any);
    expect(serviceSpy.approve).toHaveBeenCalledWith('ar-001');
  });

  it('approve — should show success toast', () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of(true) } as any);
    component.approve(MOCK_REQUESTS[0] as any);
    expect(toastSpy.success).toHaveBeenCalledWith('Amenity approved and added!');
  });

  it('approve — should NOT call service when dialog cancelled', () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of(false) } as any);
    component.approve(MOCK_REQUESTS[0] as any);
    expect(serviceSpy.approve).not.toHaveBeenCalled();
  });

  // ── reject ────────────────────────────────────────────────────────────────

  it('reject — should open input dialog', () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of('Not relevant') } as any);
    component.reject(MOCK_REQUESTS[0] as any);
    expect(MatDialog.prototype.open).toHaveBeenCalled();
  });

  it('reject — should call reject service with note', () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of('Not relevant') } as any);
    component.reject(MOCK_REQUESTS[0] as any);
    expect(serviceSpy.reject).toHaveBeenCalledWith('ar-001', 'Not relevant');
  });

  it('reject — should show success toast', () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of('Not relevant') } as any);
    component.reject(MOCK_REQUESTS[0] as any);
    expect(toastSpy.success).toHaveBeenCalledWith('Request rejected.');
  });

  it('reject — should NOT call service when dialog returns null', () => {
    spyOn(MatDialog.prototype, 'open').and.returnValue({ afterClosed: () => of(null) } as any);
    component.reject(MOCK_REQUESTS[0] as any);
    expect(serviceSpy.reject).not.toHaveBeenCalled();
  });

  // ── statusClass ───────────────────────────────────────────────────────────

  it('statusClass — Approved → badge-success', () => expect(component.statusClass('Approved')).toBe('badge-success'));
  it('statusClass — Pending → badge-warning',  () => expect(component.statusClass('Pending')).toBe('badge-warning'));
  it('statusClass — Rejected → badge-error',   () => expect(component.statusClass('Rejected')).toBe('badge-error'));
  it('statusClass — unknown → badge-muted',    () => expect(component.statusClass('Unknown')).toBe('badge-muted'));
});
