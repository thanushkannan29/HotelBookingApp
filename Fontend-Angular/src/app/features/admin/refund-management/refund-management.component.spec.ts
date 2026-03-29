import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { RefundManagementComponent } from './refund-management.component';
import { RefundService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

const MOCK_REFUNDS = [
  { refundRequestId: 'rf-001', reservationId: 'res-001', reservationCode: 'RES001', userId: 'u1', guestName: 'Alice', reason: 'Cancelled', status: 'Pending', refundAmount: 800, createdAt: '2025-01-10T10:00:00Z' },
  { refundRequestId: 'rf-002', reservationId: 'res-002', reservationCode: 'RES002', userId: 'u2', guestName: 'Bob',   reason: 'No show',  status: 'Approved', refundAmount: 500, createdAt: '2025-01-11T10:00:00Z' },
];

describe('RefundManagementComponent', () => {
  let component: RefundManagementComponent;
  let fixture: ComponentFixture<RefundManagementComponent>;
  let refundSpy: jasmine.SpyObj<RefundService>;
  let toastSpy: jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    refundSpy = jasmine.createSpyObj('RefundService', ['getHotelRefundRequests', 'approveRefund', 'rejectRefund']);
    toastSpy  = jasmine.createSpyObj('ToastService',  ['success', 'error']);

    refundSpy.getHotelRefundRequests.and.returnValue(of(MOCK_REFUNDS));
    refundSpy.approveRefund.and.returnValue(of({ ...MOCK_REFUNDS[0], status: 'Approved' } as any));
    refundSpy.rejectRefund.and.returnValue(of({ ...MOCK_REFUNDS[0], status: 'Rejected' } as any));

    await TestBed.configureTestingModule({
      imports: [RefundManagementComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        { provide: RefundService, useValue: refundSpy },
        { provide: ToastService,  useValue: toastSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RefundManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should call getHotelRefundRequests', () => {
    expect(refundSpy.getHotelRefundRequests).toHaveBeenCalled();
  });

  it('ngOnInit — should populate refunds signal', () => {
    expect(component.refunds().length).toBe(2);
  });

  // ── Initial state ─────────────────────────────────────────────────────────

  it('isSaving — should start as false', () => expect(component.isSaving()).toBeFalse());
  it('processingId — should start as null', () => expect(component.processingId()).toBeNull());
  it('actionType — should start as null', () => expect(component.actionType()).toBeNull());

  // ── startAction / cancelAction ────────────────────────────────────────────

  it('startAction — should set processingId and actionType', () => {
    component.startAction('rf-001', 'approve');
    expect(component.processingId()).toBe('rf-001');
    expect(component.actionType()).toBe('approve');
  });

  it('cancelAction — should clear processingId and actionType', () => {
    component.startAction('rf-001', 'approve');
    component.cancelAction();
    expect(component.processingId()).toBeNull();
    expect(component.actionType()).toBeNull();
  });

  // ── submit — approve ──────────────────────────────────────────────────────

  it('submit — should call approveRefund when actionType is approve', () => {
    component.startAction('rf-001', 'approve');
    component.responseForm.patchValue({ adminResponse: 'Approved OK' });
    component.submit();
    expect(refundSpy.approveRefund).toHaveBeenCalledWith('rf-001', jasmine.objectContaining({ adminResponse: 'Approved OK' }));
  });

  it('submit — should show approve success toast', () => {
    component.startAction('rf-001', 'approve');
    component.responseForm.patchValue({ adminResponse: 'Approved OK' });
    component.submit();
    expect(toastSpy.success).toHaveBeenCalledOnceWith('Refund approved! Amount credited to guest wallet.');
  });

  // ── submit — reject ───────────────────────────────────────────────────────

  it('submit — should call rejectRefund when actionType is reject', () => {
    component.startAction('rf-001', 'reject');
    component.responseForm.patchValue({ adminResponse: 'Not eligible' });
    component.submit();
    expect(refundSpy.rejectRefund).toHaveBeenCalledWith('rf-001', jasmine.objectContaining({ adminResponse: 'Not eligible' }));
  });

  it('submit — should show reject success toast', () => {
    component.startAction('rf-001', 'reject');
    component.responseForm.patchValue({ adminResponse: 'Not eligible' });
    component.submit();
    expect(toastSpy.success).toHaveBeenCalledOnceWith('Refund rejected.');
  });

  // ── submit — invalid form ─────────────────────────────────────────────────

  it('submit — should NOT call service when form is invalid', () => {
    component.startAction('rf-001', 'approve');
    component.submit();
    expect(refundSpy.approveRefund).not.toHaveBeenCalled();
  });

  it('submit — should mark all touched when form is invalid', () => {
    component.startAction('rf-001', 'approve');
    component.submit();
    expect(component.responseForm.get('adminResponse')?.touched).toBeTrue();
  });

  // ── submit — error ────────────────────────────────────────────────────────

  it('submit — should reset isSaving to false on error', () => {
    refundSpy.approveRefund.and.returnValue(throwError(() => new Error('fail')));
    component.startAction('rf-001', 'approve');
    component.responseForm.patchValue({ adminResponse: 'OK' });
    component.submit();
    expect(component.isSaving()).toBeFalse();
  });

  // ── statusClass ───────────────────────────────────────────────────────────

  it('statusClass — Pending → badge-warning', () => expect(component.statusClass('Pending')).toBe('badge-warning'));
  it('statusClass — Approved → badge-success', () => expect(component.statusClass('Approved')).toBe('badge-success'));
  it('statusClass — Rejected → badge-error', () => expect(component.statusClass('Rejected')).toBe('badge-error'));
  it('statusClass — unknown → badge-muted', () => expect(component.statusClass('Unknown')).toBe('badge-muted'));

  // ── onPage ────────────────────────────────────────────────────────────────

  it('onPage — should update currentPage and reload', () => {
    refundSpy.getHotelRefundRequests.calls.reset();
    component.onPage({ pageIndex: 1, pageSize: 10, length: 20 });
    expect(component.currentPage).toBe(2);
    expect(refundSpy.getHotelRefundRequests).toHaveBeenCalled();
  });
});
