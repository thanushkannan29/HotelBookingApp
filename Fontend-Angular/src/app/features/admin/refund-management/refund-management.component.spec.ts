import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { RefundManagementComponent } from './refund-management.component';
import { RefundService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { RefundRequestResponseDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ── Mock data ──────────────────────────────────────────────────────────────────

const MOCK_PENDING_1: RefundRequestResponseDto = {
  refundRequestId: 'rf-001',
  reservationId:   'res-001',
  reservationCode: 'RES-ABCD1234',
  userId:          'usr-001',
  guestName:       'Thanush K',
  reason:          'Trip cancelled due to emergency',
  status:          'Pending',
  refundAmount:    5000,
  createdAt:       '2025-01-10T10:00:00Z',
};

const MOCK_PENDING_2: RefundRequestResponseDto = {
  refundRequestId: 'rf-002',
  reservationId:   'res-002',
  reservationCode: 'RES-WXYZ5678',
  userId:          'usr-002',
  guestName:       'Ravi Kumar',
  reason:          'Hotel quality was not as advertised',
  status:          'Pending',
  refundAmount:    3200,
  createdAt:       '2025-01-11T09:00:00Z',
};

const MOCK_APPROVED: RefundRequestResponseDto = {
  refundRequestId: 'rf-003',
  reservationId:   'res-003',
  reservationCode: 'RES-MNOP9012',
  userId:          'usr-003',
  guestName:       'Priya S',
  reason:          'Booking error',
  status:          'Approved',
  adminResponse:   'Refund processed successfully.',
  refundAmount:    8000,
  createdAt:       '2025-01-05T08:00:00Z',
  processedAt:     '2025-01-06T10:00:00Z',
};

const MOCK_REJECTED: RefundRequestResponseDto = {
  refundRequestId: 'rf-004',
  reservationId:   'res-004',
  reservationCode: 'RES-QRST3456',
  userId:          'usr-004',
  guestName:       'Arjun M',
  reason:          'Changed mind',
  status:          'Rejected',
  adminResponse:   'Outside cancellation window.',
  refundAmount:    2500,
  createdAt:       '2025-01-08T11:00:00Z',
  processedAt:     '2025-01-09T12:00:00Z',
};

const ALL_MOCK_REFUNDS = [
  MOCK_PENDING_1, MOCK_PENDING_2, MOCK_APPROVED, MOCK_REJECTED
];

// ─────────────────────────────────────────────────────────────────────────────

describe('RefundManagementComponent', () => {
  let component: RefundManagementComponent;
  let fixture:   ComponentFixture<RefundManagementComponent>;

  let refundSpy: jasmine.SpyObj<RefundService>;
  let toastSpy:  jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    refundSpy = jasmine.createSpyObj('RefundService', [
      'getHotelRefundRequests', 'approveRefund', 'rejectRefund'
    ]);
    toastSpy = jasmine.createSpyObj('ToastService', ['success', 'error']);

    // Default happy-path responses
    refundSpy.getHotelRefundRequests.and.returnValue(of(ALL_MOCK_REFUNDS));
    refundSpy.approveRefund.and.returnValue(of({ ...MOCK_PENDING_1, status: 'Approved', adminResponse: 'Approved.' }));
    refundSpy.rejectRefund.and.returnValue(of({ ...MOCK_PENDING_2, status: 'Rejected', adminResponse: 'Rejected.' }));

    await TestBed.configureTestingModule({
      imports: [RefundManagementComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: RefundService, useValue: refundSpy },
        { provide: ToastService,  useValue: toastSpy  },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(RefundManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── INITIAL SIGNAL STATE ───────────────────────────────────────────────────

  it('processingId — should start as null', () => {
    expect(component.processingId()).toBeNull();
  });

  it('actionType — should start as null', () => {
    expect(component.actionType()).toBeNull();
  });

  it('isSaving — should start as false', () => {
    expect(component.isSaving()).toBeFalse();
  });

  // ── ngOnInit ───────────────────────────────────────────────────────────────

  it('ngOnInit — should call getHotelRefundRequests on startup', () => {
    expect(refundSpy.getHotelRefundRequests).toHaveBeenCalledOnceWith();
  });

  it('ngOnInit — should populate refunds signal with all returned refunds', () => {
    expect(component.refunds().length).toBe(4);
  });

  // ── COMPUTED GETTERS ───────────────────────────────────────────────────────

  it('pendingRefunds — should return only Pending status refunds', () => {
    expect(component.pendingRefunds.length).toBe(2);
    expect(component.pendingRefunds.every(r => r.status === 'Pending')).toBeTrue();
  });

  it('pendingRefunds — should contain correct IDs', () => {
    const ids = component.pendingRefunds.map(r => r.refundRequestId);
    expect(ids).toContain('rf-001');
    expect(ids).toContain('rf-002');
  });

  it('otherRefunds — should return only non-Pending refunds', () => {
    expect(component.otherRefunds.length).toBe(2);
    expect(component.otherRefunds.every(r => r.status !== 'Pending')).toBeTrue();
  });

  it('otherRefunds — should contain Approved and Rejected entries', () => {
    const statuses = component.otherRefunds.map(r => r.status);
    expect(statuses).toContain('Approved');
    expect(statuses).toContain('Rejected');
  });

  it('pendingRefunds — should be 0 when all refunds are resolved', () => {
    component.refunds.set([MOCK_APPROVED, MOCK_REJECTED]);
    expect(component.pendingRefunds.length).toBe(0);
  });

  // ── statusClass() ──────────────────────────────────────────────────────────

  it('statusClass() — Pending → badge-warning', () => {
    expect(component.statusClass('Pending')).toBe('badge-warning');
  });

  it('statusClass() — Approved → badge-success', () => {
    expect(component.statusClass('Approved')).toBe('badge-success');
  });

  it('statusClass() — Rejected → badge-error', () => {
    expect(component.statusClass('Rejected')).toBe('badge-error');
  });

  it('statusClass() — unknown status → badge-muted', () => {
    expect(component.statusClass('Processing')).toBe('badge-muted');
    expect(component.statusClass('')).toBe('badge-muted');
  });

  // ── startAction() ──────────────────────────────────────────────────────────

  it('startAction() — should set processingId to the given ID', () => {
    component.startAction('rf-001', 'approve');
    expect(component.processingId()).toBe('rf-001');
  });

  it('startAction() — should set actionType to approve', () => {
    component.startAction('rf-001', 'approve');
    expect(component.actionType()).toBe('approve');
  });

  it('startAction() — should set actionType to reject', () => {
    component.startAction('rf-002', 'reject');
    expect(component.actionType()).toBe('reject');
  });

  it('startAction() — should reset the responseForm', () => {
    component.responseForm.get('adminResponse')?.setValue('Some previous value');
    component.startAction('rf-001', 'approve');
    expect(component.responseForm.get('adminResponse')?.value).toBeFalsy();
  });

  it('startAction() — switching between items updates both signals', () => {
    component.startAction('rf-001', 'approve');
    component.startAction('rf-002', 'reject');
    expect(component.processingId()).toBe('rf-002');
    expect(component.actionType()).toBe('reject');
  });

  // ── cancelAction() ─────────────────────────────────────────────────────────

  it('cancelAction() — should clear processingId to null', () => {
    component.startAction('rf-001', 'approve');
    component.cancelAction();
    expect(component.processingId()).toBeNull();
  });

  it('cancelAction() — should clear actionType to null', () => {
    component.startAction('rf-001', 'approve');
    component.cancelAction();
    expect(component.actionType()).toBeNull();
  });

  // ── FORM VALIDATION ────────────────────────────────────────────────────────

  it('responseForm — should be invalid when adminResponse is empty', () => {
    component.responseForm.get('adminResponse')?.setValue('');
    expect(component.responseForm.invalid).toBeTrue();
  });

  it('responseForm — should be invalid when adminResponse is too short (< 3 chars)', () => {
    component.responseForm.get('adminResponse')?.setValue('Ok');
    expect(component.responseForm.invalid).toBeTrue();
  });

  it('responseForm — should be valid when adminResponse meets minLength', () => {
    component.responseForm.get('adminResponse')?.setValue('Approved after review.');
    expect(component.responseForm.valid).toBeTrue();
  });

  // ── submit() — APPROVE HAPPY PATH ──────────────────────────────────────────

  it('submit() — should call approveRefund when actionType is approve', () => {
    component.startAction('rf-001', 'approve');
    component.responseForm.get('adminResponse')?.setValue('Approved after review.');

    component.submit();

    expect(refundSpy.approveRefund).toHaveBeenCalledOnceWith(
      'rf-001',
      { adminResponse: 'Approved after review.' }
    );
  });

  it('submit() — should show correct success toast for approve', () => {
    component.startAction('rf-001', 'approve');
    component.responseForm.get('adminResponse')?.setValue('Approved after review.');

    component.submit();

    expect(toastSpy.success).toHaveBeenCalledOnceWith('Refund approved successfully.');
  });

  it('submit() — should update the matching refund in the signal after approve', () => {
    component.startAction('rf-001', 'approve');
    component.responseForm.get('adminResponse')?.setValue('Approved after review.');

    component.submit();

    const updated = component.refunds().find(r => r.refundRequestId === 'rf-001');
    expect(updated?.status).toBe('Approved');
  });

  it('submit() — should call cancelAction after successful approve', () => {
    component.startAction('rf-001', 'approve');
    component.responseForm.get('adminResponse')?.setValue('Approved after review.');

    component.submit();

    expect(component.processingId()).toBeNull();
    expect(component.actionType()).toBeNull();
  });

  it('submit() — should reset isSaving to false after approve success', () => {
    component.startAction('rf-001', 'approve');
    component.responseForm.get('adminResponse')?.setValue('Approved after review.');

    component.submit();

    expect(component.isSaving()).toBeFalse();
  });

  // ── submit() — REJECT HAPPY PATH ───────────────────────────────────────────

  it('submit() — should call rejectRefund when actionType is reject', () => {
    component.startAction('rf-002', 'reject');
    component.responseForm.get('adminResponse')?.setValue('Outside cancellation window.');

    component.submit();

    expect(refundSpy.rejectRefund).toHaveBeenCalledOnceWith(
      'rf-002',
      { adminResponse: 'Outside cancellation window.' }
    );
  });

  it('submit() — should show correct success toast for reject', () => {
    component.startAction('rf-002', 'reject');
    component.responseForm.get('adminResponse')?.setValue('Outside cancellation window.');

    component.submit();

    expect(toastSpy.success).toHaveBeenCalledOnceWith('Refund rejectd successfully.');
  });

  it('submit() — should NOT call approveRefund when rejecting', () => {
    component.startAction('rf-002', 'reject');
    component.responseForm.get('adminResponse')?.setValue('Outside cancellation window.');

    component.submit();

    expect(refundSpy.approveRefund).not.toHaveBeenCalled();
  });

  // ── submit() — IN-FLIGHT ───────────────────────────────────────────────────

  it('submit() — should set isSaving to true during in-flight request', () => {
    const subject = new Subject<RefundRequestResponseDto>();
    refundSpy.approveRefund.and.returnValue(subject.asObservable());

    component.startAction('rf-001', 'approve');
    component.responseForm.get('adminResponse')?.setValue('Approved after review.');

    component.submit();

    expect(component.isSaving()).toBeTrue();

    subject.next({ ...MOCK_PENDING_1, status: 'Approved' });
    subject.complete();
  });

  // ── submit() — INVALID FORM ────────────────────────────────────────────────

  it('submit() — should NOT call any service when responseForm is invalid', () => {
    component.startAction('rf-001', 'approve');
    // responseForm is empty — invalid

    component.submit();

    expect(refundSpy.approveRefund).not.toHaveBeenCalled();
    expect(refundSpy.rejectRefund).not.toHaveBeenCalled();
  });

  it('submit() — should mark adminResponse as touched when form is invalid', () => {
    component.startAction('rf-001', 'approve');

    component.submit();

    expect(component.responseForm.get('adminResponse')?.touched).toBeTrue();
  });

  it('submit() — should NOT show toast when form is invalid', () => {
    component.startAction('rf-001', 'approve');

    component.submit();

    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  // ── submit() — ERROR ───────────────────────────────────────────────────────

  it('submit() — should reset isSaving to false on API error', () => {
    refundSpy.approveRefund.and.returnValue(
      throwError(() => new Error('Server error'))
    );
    component.startAction('rf-001', 'approve');
    component.responseForm.get('adminResponse')?.setValue('Approved after review.');

    component.submit();

    expect(component.isSaving()).toBeFalse();
  });

  it('submit() — should NOT show success toast on API error', () => {
    refundSpy.approveRefund.and.returnValue(
      throwError(() => new Error('Server error'))
    );
    component.startAction('rf-001', 'approve');
    component.responseForm.get('adminResponse')?.setValue('Approved after review.');

    component.submit();

    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  it('submit() — should NOT update refunds signal on API error', () => {
    refundSpy.approveRefund.and.returnValue(
      throwError(() => new Error('Server error'))
    );
    component.startAction('rf-001', 'approve');
    component.responseForm.get('adminResponse')?.setValue('Approved after review.');
    const originalStatus = component.refunds()
      .find(r => r.refundRequestId === 'rf-001')?.status;

    component.submit();

    const afterStatus = component.refunds()
      .find(r => r.refundRequestId === 'rf-001')?.status;
    expect(afterStatus).toBe(originalStatus);
  });
});