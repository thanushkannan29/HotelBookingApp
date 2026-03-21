import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDialogModule } from '@angular/material/dialog';
import { DatePipe, DecimalPipe, SlicePipe } from '@angular/common';
import { TransactionService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { TransactionResponseDto, PaymentMethod, PaymentStatus } from '../../../core/models/models';

@Component({
  selector: 'app-guest-transactions',
  standalone: true,
  imports: [
    RouterLink, ReactiveFormsModule, DatePipe, DecimalPipe, SlicePipe,
    MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatDialogModule
  ],
  templateUrl: './guest-transactions.component.html',
  styleUrl:    './guest-transactions.component.scss'
})
export class GuestTransactionsComponent implements OnInit {
  private txService = inject(TransactionService);
  private toast     = inject(ToastService);
  private fb        = inject(FormBuilder);

  transactions  = signal<TransactionResponseDto[]>([]);
  total         = signal(0);
  page          = signal(1);
  readonly pageSize = 10;
  refundingId   = signal<string | null>(null);
  isSaving      = signal(false);

  refundForm = this.fb.group({
    reason: ['', [Validators.required, Validators.minLength(5)]]
  });

  paymentMethodLabel = (id: number) => PaymentMethod[id] ?? 'Unknown';
  paymentStatusLabel = (id: number) => PaymentStatus[id] ?? 'Unknown';

  statusClass(id: number): string {
    const s: Record<number, string> = { 1:'badge-warning', 2:'badge-success', 3:'badge-error', 4:'badge-info' };
    return s[id] ?? 'badge-muted';
  }

  get totalPages() { return Math.ceil(this.total() / this.pageSize); }

  ngOnInit() { this.load(); }

  load() {
    this.txService.getTransactions(this.page(), this.pageSize).subscribe(r => {
      this.transactions.set(r.transactions as TransactionResponseDto[]);
      this.total.set(r.totalCount);
    });
  }

  canDirectRefund(tx: TransactionResponseDto): boolean {
    if (tx.status !== 2) return false; // must be Success
    const mins = (Date.now() - new Date(tx.transactionDate).getTime()) / 60000;
    return mins <= 30;
  }

  minutesSince(tx: TransactionResponseDto): number {
    return Math.floor((Date.now() - new Date(tx.transactionDate).getTime()) / 60000);
  }

  startRefund(txId: string) {
    this.refundingId.set(txId);
    this.refundForm.reset();
  }

  submitRefund() {
    if (this.refundForm.invalid) { this.refundForm.markAllAsTouched(); return; }
    const id = this.refundingId()!;
    this.isSaving.set(true);
    this.txService.directRefund(id, { reason: this.refundForm.get('reason')!.value! }).subscribe({
      next: () => {
        this.toast.success('Refund processed successfully.');
        this.refundingId.set(null);
        this.isSaving.set(false);
        this.load();
      },
      error: () => this.isSaving.set(false),
    });
  }

  next() { if (this.page() < this.totalPages) { this.page.update(p => p + 1); this.load(); } }
  prev() { if (this.page() > 1) { this.page.update(p => p - 1); this.load(); } }
}
