import { Component, inject, signal, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { DatePipe, DecimalPipe } from '@angular/common';
import { TransactionService } from '../../../core/services/api.services';
import { TransactionResponseDto, PaymentMethod, PaymentStatus } from '../../../core/models/models';

@Component({
  selector: 'app-guest-transactions',
  standalone: true,
  imports: [
    CommonModule, DatePipe, DecimalPipe,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatPaginatorModule,
  ],
  templateUrl: './guest-transactions.component.html',
  styleUrl: './guest-transactions.component.scss'
})
export class GuestTransactionsComponent implements OnInit {
  private txService = inject(TransactionService);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  transactions = signal<TransactionResponseDto[]>([]);
  totalCount   = signal(0);
  loading      = signal(false);
  pageSize     = 10;
  currentPage  = 1;

  paymentMethodLabel = (id: number) => PaymentMethod[id] ?? 'Unknown';
  paymentStatusLabel = (id: number) => PaymentStatus[id] ?? 'Unknown';

  statusClass(id: number): string {
    const s: Record<number, string> = {
      1: 'badge-warning', 2: 'badge-success', 3: 'badge-error', 4: 'badge-info',
    };
    return s[id] ?? 'badge-muted';
  }

  txIcon(tx: TransactionResponseDto): string {
    if (tx.transactionType === 'WalletRefund') return 'account_balance_wallet';
    if (tx.status === 4) return 'replay';
    if (tx.status === 2) return 'check_circle';
    if (tx.status === 3) return 'cancel';
    return 'schedule';
  }

  txIconClass(tx: TransactionResponseDto): string {
    if (tx.transactionType === 'WalletRefund') return 'badge-info';
    return this.statusClass(tx.status);
  }

  txLabel(tx: TransactionResponseDto): string {
    if (tx.transactionType === 'WalletRefund') return 'Hotel Refund (Wallet Credit)';
    return this.paymentMethodLabel(tx.paymentMethod);
  }

  amountColor(tx: TransactionResponseDto): string {
    if (tx.transactionType === 'WalletRefund') return 'var(--color-success)';
    if (tx.status === 3) return 'var(--color-error)';
    return 'var(--color-text-primary)';
  }

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.txService.getTransactions(this.currentPage, this.pageSize).subscribe({
      next: r => {
        this.transactions.set(r.transactions ?? []);
        this.totalCount.set(r.totalCount ?? 0);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onPage(e: PageEvent) {
    this.currentPage = e.pageIndex + 1;
    this.pageSize    = e.pageSize;
    this.load();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
}
