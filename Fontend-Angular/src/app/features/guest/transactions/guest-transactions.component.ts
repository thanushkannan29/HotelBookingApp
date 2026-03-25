import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DatePipe, DecimalPipe } from '@angular/common';
import { TransactionService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { TransactionResponseDto, PaymentMethod, PaymentStatus } from '../../../core/models/models';

@Component({
  selector: 'app-guest-transactions',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule, DatePipe, DecimalPipe,
    MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule,
    MatTableModule, MatPaginatorModule, MatChipsModule, MatTooltipModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './guest-transactions.component.html',
  styleUrl: './guest-transactions.component.scss'
})
export class GuestTransactionsComponent implements OnInit {
  private txService = inject(TransactionService);
  private toast     = inject(ToastService);
  private fb        = inject(FormBuilder);

  transactions = signal<TransactionResponseDto[]>([]);
  totalCount   = signal(0);
  loading      = signal(false);
  pageSize     = 10;
  currentPage  = 1;
  refundingId  = signal<string | null>(null);
  isSaving     = signal(false);

  displayedColumns = ['method', 'date', 'transactionId', 'amount', 'status', 'actions'];

  refundForm = this.fb.group({
    reason: ['', [Validators.required, Validators.minLength(5)]]
  });

  paymentMethodLabel = (id: number) => PaymentMethod[id] ?? 'Unknown';
  paymentStatusLabel = (id: number) => PaymentStatus[id] ?? 'Unknown';

  statusClass(id: number): string {
    const s: Record<number, string> = {
      1: 'badge-warning', 2: 'badge-success', 3: 'badge-error', 4: 'badge-info',
    };
    return s[id] ?? 'badge-muted';
  }

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.txService.getTransactions(this.currentPage, this.pageSize).subscribe({
      next: r => {
        this.transactions.set(r.transactions as TransactionResponseDto[]);
        this.totalCount.set(r.totalCount ?? r.transactions?.length ?? 0);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onPage(e: PageEvent) { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); }

  canDirectRefund(tx: TransactionResponseDto): boolean {
    if (tx.status !== 2) return false;
    const mins = (Date.now() - new Date(tx.transactionDate).getTime()) / 60000;
    return mins <= 30;
  }

  minutesSince(tx: TransactionResponseDto): number {
    return Math.floor((Date.now() - new Date(tx.transactionDate).getTime()) / 60000);
  }

  startRefund(txId: string) { this.refundingId.set(txId); this.refundForm.reset(); }

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
}
