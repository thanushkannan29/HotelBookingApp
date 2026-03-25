import { Component, inject, signal, computed, OnInit, AfterViewInit, ViewChild } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { DatePipe, DecimalPipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TransactionService } from '../../../core/services/api.services';
import { TransactionResponseDto, PaymentMethod } from '../../../core/models/models';

// Status map: numeric → label
const STATUS_LABELS: Record<number, string> = {
  1: 'Success',
  2: 'Refunded',
  3: 'Failed',
  0: 'Pending',
};

@Component({
  selector: 'app-admin-transactions',
  standalone: true,
  imports: [
    DatePipe, DecimalPipe, FormsModule, NgClass,
    MatFormFieldModule, MatInputModule, MatIconModule,
    MatTableModule, MatSortModule, MatPaginatorModule,
    MatProgressSpinnerModule, MatChipsModule, MatButtonToggleModule,
  ],
  templateUrl: './admin-transactions.component.html',
  styleUrl: './admin-transactions.component.scss',
})
export class AdminTransactionsComponent implements OnInit, AfterViewInit {
  private transactionService = inject(TransactionService);

  isLoading = signal(true);
  allTransactions = signal<TransactionResponseDto[]>([]);
  activeFilter = signal<string>('All');

  readonly filterChips = ['All', 'Success', 'Refunded', 'Failed'];

  dataSource = new MatTableDataSource<TransactionResponseDto>([]);
  displayedColumns = ['transactionId', 'amount', 'paymentMethod', 'status', 'date'];

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  // Summary computed values
  totalIncome = computed(() =>
    this.allTransactions()
      .filter(t => this.statusLabel(t.status) === 'Success')
      .reduce((sum, t) => sum + t.amount, 0)
  );

  totalRefunded = computed(() =>
    this.allTransactions()
      .filter(t => this.statusLabel(t.status) === 'Refunded')
      .reduce((sum, t) => sum + t.amount, 0)
  );

  netRevenue = computed(() => this.totalIncome() - this.totalRefunded());

  readonly paymentMethodMap = PaymentMethod;

  ngOnInit() {
    // Load up to 500 transactions (admin view — load all at once for client-side filter)
    this.transactionService.getTransactions(1, 500).subscribe({
      next: (res) => {
        const txns = res.transactions ?? [];
        this.allTransactions.set(txns);
        this.dataSource.data = txns;
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });
  }

  ngAfterViewInit() {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
  }

  applyChipFilter(chip: string) {
    this.activeFilter.set(chip);
    if (chip === 'All') {
      this.dataSource.data = this.allTransactions();
    } else {
      this.dataSource.data = this.allTransactions().filter(
        t => this.statusLabel(t.status) === chip
      );
    }
    if (this.dataSource.paginator) this.dataSource.paginator.firstPage();
  }

  applySearch(event: Event) {
    const val = (event.target as HTMLInputElement).value;
    this.dataSource.filter = val.trim().toLowerCase();
    if (this.dataSource.paginator) this.dataSource.paginator.firstPage();
  }
getPaymentMethodLabel(method: number): string {
  return this.paymentMethodMap[method as keyof typeof this.paymentMethodMap] ?? 'N/A';
}

  statusLabel(status: number): string {
    return STATUS_LABELS[status] ?? 'Unknown';
  }

  statusClass(status: number): string {
    const map: Record<string, string> = {
      Success:  'badge-success',
      Refunded: 'badge-warning',
      Failed:   'badge-error',
      Pending:  'badge-muted',
    };
    return map[this.statusLabel(status)] ?? 'badge-muted';
  }

  shortId(id: string): string {
    return id ? id.substring(0, 8).toUpperCase() + '…' : '—';
  }
}
