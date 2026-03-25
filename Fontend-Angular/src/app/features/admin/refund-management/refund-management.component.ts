import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule } from '@angular/material/sort';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RefundService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { RefundRequestResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-refund-management',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink, DatePipe, DecimalPipe,
    MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatTableModule, MatSortModule, MatPaginatorModule, MatProgressSpinnerModule,
  ],
  templateUrl: './refund-management.component.html',
  styleUrl: './refund-management.component.scss'
})
export class RefundManagementComponent implements OnInit {
  private refundService = inject(RefundService);
  private toast         = inject(ToastService);
  private fb            = inject(FormBuilder);

  refunds      = signal<RefundRequestResponseDto[]>([]);
  totalCount   = signal(0);
  loading      = signal(false);
  pageSize     = 10;
  currentPage  = 1;
  displayedColumns = ['reservationCode', 'guestName', 'amount', 'status', 'createdAt', 'actions'];

  processingId = signal<string | null>(null);
  actionType   = signal<'approve' | 'reject' | null>(null);
  isSaving     = signal(false);

  responseForm = this.fb.group({
    adminResponse:        ['', [Validators.required, Validators.minLength(3)]],
    refundPaymentMethod:  [''],
    refundTransactionRef: [''],
  });

  readonly refundPaymentMethods = ['UPI', 'Bank Transfer', 'Cash', 'Cheque'];

  ngOnInit() {
    this.load();
  }

  load() {
    this.refundService.getHotelRefundRequests(this.currentPage, this.pageSize).subscribe((res: any) => {
      if (Array.isArray(res)) {
        this.refunds.set(res);
        this.totalCount.set(res.length);
      } else {
        this.refunds.set(res.refundRequests ?? res.items ?? []);
        this.totalCount.set(res.totalCount ?? 0);
      }
    });
  }

  onPage(e: any) { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); }

  applyFilter(event: Event) {
    // kept for template compatibility — filtering is now server-side
  }

  startAction(id: string, type: 'approve' | 'reject') {
    this.processingId.set(id);
    this.actionType.set(type);
    this.responseForm.reset();
  }

  cancelAction() {
    this.processingId.set(null);
    this.actionType.set(null);
  }

  submit() {
    if (this.responseForm.invalid) { this.responseForm.markAllAsTouched(); return; }
    const id = this.processingId()!;
    const v = this.responseForm.value;
    const dto = {
      adminResponse:        v.adminResponse!,
      refundPaymentMethod:  v.refundPaymentMethod || undefined,
      refundTransactionRef: v.refundTransactionRef || undefined,
    };
    this.isSaving.set(true);

    const obs = this.actionType() === 'approve'
      ? this.refundService.approveRefund(id, dto)
      : this.refundService.rejectRefund(id, dto);

    obs.subscribe({
      next: () => {
        const msg = this.actionType() === 'approve'
          ? 'Refund approved! Amount credited to guest wallet.'
          : 'Refund rejected.';
        this.toast.success(msg);
        this.cancelAction();
        this.load();
        this.isSaving.set(false);
      },
      error: () => this.isSaving.set(false),
    });
  }

  statusClass(s: string) {
    const m: Record<string, string> = { Pending: 'badge-warning', Approved: 'badge-success', Rejected: 'badge-error' };
    return m[s] ?? 'badge-muted';
  }
}