import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RefundService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { RefundRequestResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-refund-management',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink, DatePipe, DecimalPipe,
    MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule
  ],
  templateUrl: './refund-management.component.html',
  styleUrl: './refund-management.component.scss'
})
export class RefundManagementComponent implements OnInit {
  private refundService = inject(RefundService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  refunds = signal<RefundRequestResponseDto[]>([]);
  processingId = signal<string | null>(null);
  actionType = signal<'approve' | 'reject' | null>(null);
  isSaving = signal(false);

  responseForm = this.fb.group({
    adminResponse: ['', [Validators.required, Validators.minLength(3)]],
  });

  ngOnInit() {
    this.refundService.getHotelRefundRequests().subscribe(r => this.refunds.set(r));
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
    const dto = { adminResponse: this.responseForm.get('adminResponse')!.value! };
    this.isSaving.set(true);

    const obs = this.actionType() === 'approve'
      ? this.refundService.approveRefund(id, dto)
      : this.refundService.rejectRefund(id, dto);

    obs.subscribe({
      next: (updated) => {
        this.toast.success(`Refund ${this.actionType()}d successfully.`);
        this.refunds.update(r => r.map(x => x.refundRequestId === id ? updated : x));
        this.cancelAction();
        this.isSaving.set(false);
      },
      error: () => this.isSaving.set(false),
    });
  }

  statusClass(s: string) {
    const m: Record<string,string> = { Pending: 'badge-warning', Approved: 'badge-success', Rejected: 'badge-error' };
    return m[s] ?? 'badge-muted';
  }

  get pendingRefunds() { return this.refunds().filter(r => r.status === 'Pending'); }
  get otherRefunds()   { return this.refunds().filter(r => r.status !== 'Pending'); }
}
