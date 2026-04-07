import { Component, inject, signal, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DatePipe, DecimalPipe } from '@angular/common';
import { TransactionService } from '../../../core/services/api.services';
import { TransactionResponseDto, PaymentMethod, PaymentStatus } from '../../../core/models/models';

@Component({
  selector: 'app-admin-transactions',
  standalone: true,
  imports: [
    CommonModule, DatePipe, DecimalPipe,
    MatButtonModule, MatIconModule, MatTableModule,
    MatPaginatorModule, MatProgressSpinnerModule,
  ],
  templateUrl: './admin-transactions.component.html',
  styleUrl: './admin-transactions.component.scss',
})
export class AdminTransactionsComponent implements OnInit {
  private txService = inject(TransactionService);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  loading      = signal(false);
  transactions = signal<TransactionResponseDto[]>([]);
  totalCount   = signal(0);
  pageSize     = 10;
  currentPage  = 1;

  displayedColumns = ['type', 'guest', 'reservation', 'amount', 'status', 'date'];
  readonly paymentMethodMap = PaymentMethod;

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.txService.getTransactions(this.currentPage, this.pageSize).subscribe({
      next: res => {
        this.transactions.set(res.transactions ?? []);
        this.totalCount.set(res.totalCount ?? 0);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onPage(e: PageEvent) {
    this.currentPage = e.pageIndex + 1;
    this.pageSize    = e.pageSize;
    this.load();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  getPaymentMethodLabel(method: number): string {
    return this.paymentMethodMap[method as keyof typeof this.paymentMethodMap] ?? 'N/A';
  }

  statusLabel(status: number): string { return PaymentStatus[status] ?? 'Unknown'; }

  statusClass(status: number): string {
    const map: Record<string, string> = {
      Success: 'badge-success', Refunded: 'badge-warning',
      Failed: 'badge-error', Pending: 'badge-muted',
    };
    return map[this.statusLabel(status)] ?? 'badge-muted';
  }

  txLabel(tx: TransactionResponseDto): string {
    if (tx.transactionType === 'CommissionSent') return '📤 Commission (2%)';
    if (tx.transactionType === 'AutoRefund')     return '💰 Auto Refund';
    if (tx.transactionType === 'WalletRefund')   return '💳 Wallet Refund';
    return this.getPaymentMethodLabel(tx.paymentMethod);
  }

  txBadgeClass(tx: TransactionResponseDto): string {
    if (tx.transactionType === 'CommissionSent') return 'badge-muted';
    if (tx.transactionType === 'AutoRefund')     return 'badge-warning';
    if (tx.transactionType === 'WalletRefund')   return 'badge-warning';
    return this.statusClass(tx.status);
  }

  txBadgeLabel(tx: TransactionResponseDto): string {
    if (tx.transactionType === 'CommissionSent') return 'Commission';
    if (tx.transactionType === 'AutoRefund')     return 'Refunded';
    if (tx.transactionType === 'WalletRefund')   return 'Refunded';
    return this.statusLabel(tx.status);
  }

  amountColor(tx: TransactionResponseDto): string {
    return (tx.transactionType === 'CommissionSent' || tx.transactionType === 'AutoRefund')
      ? 'var(--color-error)' : 'inherit';
  }
}
