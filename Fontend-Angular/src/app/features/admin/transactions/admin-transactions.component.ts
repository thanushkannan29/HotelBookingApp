import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DatePipe, DecimalPipe } from '@angular/common';
import { TransactionService } from '../../../core/services/api.services';
import { TransactionResponseDto, PaymentMethod, PaymentStatus } from '../../../core/models/models';

@Component({
  selector: 'app-admin-transactions',
  standalone: true,
  imports: [
    CommonModule, DatePipe, DecimalPipe,
    MatButtonModule, MatIconModule,
    MatPaginatorModule, MatProgressSpinnerModule,
  ],
  templateUrl: './admin-transactions.component.html',
  styleUrl: './admin-transactions.component.scss',
})
export class AdminTransactionsComponent implements OnInit {
  private txService = inject(TransactionService);

  loading      = signal(false);
  transactions = signal<TransactionResponseDto[]>([]);
  totalCount   = signal(0);
  pageSize     = 10;
  currentPage  = 1;
  displayedColumns = ['transactionId', 'amount', 'paymentMethod', 'status', 'date'];

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

  onPage(e: PageEvent) { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); }

  getPaymentMethodLabel(method: number): string {
    return this.paymentMethodMap[method as keyof typeof this.paymentMethodMap] ?? 'N/A';
  }

  statusLabel(status: number): string {
    return PaymentStatus[status] ?? 'Unknown';
  }

  statusClass(status: number): string {
    const map: Record<string, string> = {
      Success: 'badge-success', Refunded: 'badge-warning',
      Failed: 'badge-error', Pending: 'badge-muted',
    };
    return map[this.statusLabel(status)] ?? 'badge-muted';
  }

  shortId(id: string): string {
    return id ? id.substring(0, 8).toUpperCase() + '…' : '—';
  }
}
