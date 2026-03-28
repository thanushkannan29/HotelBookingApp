import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DatePipe, DecimalPipe } from '@angular/common';
import { TransactionService } from '../../../core/services/api.services';
import { TransactionResponseDto, PaymentMethod, PaymentStatus } from '../../../core/models/models';

@Component({
  selector: 'app-guest-transactions',
  standalone: true,
  imports: [CommonModule, DatePipe, DecimalPipe, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './guest-transactions.component.html',
  styleUrl: './guest-transactions.component.scss'
})
export class GuestTransactionsComponent implements OnInit {
  private txService = inject(TransactionService);

  transactions = signal<TransactionResponseDto[]>([]);
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

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.txService.getTransactions(this.currentPage, this.pageSize).subscribe({
      next: r => { this.transactions.set(r.transactions ?? []); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }
}
