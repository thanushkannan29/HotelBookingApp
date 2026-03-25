import { Component, inject, signal, OnInit, AfterViewInit, ViewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DatePipe, DecimalPipe } from '@angular/common';
import { TransactionService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { TransactionResponseDto, PaymentMethod, PaymentStatus } from '../../../core/models/models';

@Component({
  selector: 'app-guest-transactions',
  standalone: true,
  imports: [
    RouterLink, ReactiveFormsModule, DatePipe, DecimalPipe,
    MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule,
    MatTableModule, MatSortModule, MatPaginatorModule,
    MatChipsModule, MatTooltipModule,
  ],
  templateUrl: './guest-transactions.component.html',
  styleUrl:    './guest-transactions.component.scss'
})
export class GuestTransactionsComponent implements OnInit, AfterViewInit {
  private txService = inject(TransactionService);
  private toast     = inject(ToastService);
  private fb        = inject(FormBuilder);

  dataSource   = new MatTableDataSource<TransactionResponseDto>([]);
  displayedColumns = ['method', 'date', 'transactionId', 'amount', 'status', 'actions'];
  refundingId  = signal<string | null>(null);
  isSaving     = signal(false);

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  refundForm = this.fb.group({
    reason: ['', [Validators.required, Validators.minLength(5)]]
  });

  paymentMethodLabel = (id: number) => PaymentMethod[id] ?? 'Unknown';
  paymentStatusLabel = (id: number) => PaymentStatus[id] ?? 'Unknown';

  statusClass(id: number): string {
    const s: Record<number, string> = {
      1: 'badge-warning',
      2: 'badge-success',
      3: 'badge-error',
      4: 'badge-info',
    };
    return s[id] ?? 'badge-muted';
  }

  ngOnInit() { this.load(); }

  ngAfterViewInit() {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
  }

  load() {
    this.txService.getTransactions(1, 200).subscribe(r => {
      this.dataSource.data = r.transactions as TransactionResponseDto[];
    });
  }

  applyFilter(event: Event) {
    const val = (event.target as HTMLInputElement).value;
    this.dataSource.filter = val.trim().toLowerCase();
    if (this.dataSource.paginator) this.dataSource.paginator.firstPage();
  }

  canDirectRefund(tx: TransactionResponseDto): boolean {
    if (tx.status !== 2) return false;
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
}