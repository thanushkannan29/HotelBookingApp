import { Component, inject, signal, OnInit, AfterViewInit, ViewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RefundService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { RefundRequestResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-refund-management',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink, DatePipe, DecimalPipe,
    MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatTableModule, MatSortModule, MatPaginatorModule,
  ],
  templateUrl: './refund-management.component.html',
  styleUrl: './refund-management.component.scss'
})
export class RefundManagementComponent implements OnInit, AfterViewInit {
  private refundService = inject(RefundService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  dataSource = new MatTableDataSource<RefundRequestResponseDto>([]);
  displayedColumns = ['reservationCode', 'guestName', 'amount', 'status', 'createdAt', 'actions'];

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  processingId = signal<string | null>(null);
  actionType = signal<'approve' | 'reject' | null>(null);
  isSaving = signal(false);

  // F7C: Added refundPaymentMethod and refundTransactionRef
  responseForm = this.fb.group({
    adminResponse:        ['', [Validators.required, Validators.minLength(3)]],
    refundPaymentMethod:  [''],
    refundTransactionRef: [''],
  });

  readonly refundPaymentMethods = ['UPI', 'Bank Transfer', 'Cash', 'Cheque'];

  ngOnInit() {
    this.refundService.getHotelRefundRequests().subscribe(r => {
      this.dataSource.data = r.refundRequests ?? (r as any);
    });
  }

  ngAfterViewInit() {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
  }

  applyFilter(event: Event) {
    const val = (event.target as HTMLInputElement).value;
    this.dataSource.filter = val.trim().toLowerCase();
    if (this.dataSource.paginator) this.dataSource.paginator.firstPage();
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
      next: (updated) => {
        this.toast.success(`Refund ${this.actionType()}d successfully.`);
        this.dataSource.data = this.dataSource.data.map(x =>
          x.refundRequestId === id ? updated : x
        );
        this.cancelAction();
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