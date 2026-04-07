import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { WalletService } from '../../../core/services/wallet.service';
import { ToastService } from '../../../core/services/toast.service';
import { WalletResponseDto, WalletTransactionDto } from '../../../core/models/models';

import { environment } from '../../../../environments/environment';

declare var Razorpay: any;

@Component({
  selector: 'app-guest-wallet',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatFormFieldModule, MatInputModule,
    MatTableModule, MatPaginatorModule, MatIconModule, MatChipsModule,
    MatProgressSpinnerModule
  ],
  template: `
    <div class="container py-4">
      <h2 class="mb-4">💼 My Wallet</h2>

      <!-- Balance Card — shown once loaded -->
      @if (!loading() && wallet()) {
        <div class="row mb-4">
          <div class="col-md-4">
            <mat-card class="wallet-balance-card">
              <mat-card-content class="text-center py-4">
                <mat-icon class="wallet-icon">account_balance_wallet</mat-icon>
                <div class="balance-label">Available Balance</div>
                <div class="balance-amount">₹{{ wallet()?.balance | number:'1.2-2' }}</div>
              </mat-card-content>
            </mat-card>
          </div>
          <div class="col-md-8">
            <mat-card>
              <mat-card-header>
                <mat-card-title>💳 Add Money</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <form [formGroup]="topUpForm" (ngSubmit)="openRazorpay()" class="d-flex gap-3 align-items-start mt-2">
                  <mat-form-field appearance="outline" class="flex-grow-1">
                    <mat-label>Amount (₹)</mat-label>
                    <input matInput type="number" formControlName="amount" min="1" max="100000" />
                    <mat-error>Enter a valid amount (1–1,00,000)</mat-error>
                  </mat-form-field>
                  <button mat-raised-button color="primary" type="submit" [disabled]="topUpForm.invalid || topping()">
                    @if (topping()) { <mat-spinner diameter="20" /> } @else { 💳 Add Money }
                  </button>
                </form>
                <p style="font-size:12px;color:#888;margin-top:4px;">Pay via UPI, Card, or Net Banking — powered by Razorpay</p>
              </mat-card-content>
            </mat-card>
          </div>
        </div>
      }

      <!-- Transaction History — table + paginator always in DOM -->
      <mat-card>
        <mat-card-header>
          <mat-card-title>🧾 Transaction History</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          @if (loading()) {
            <div class="text-center py-5"><mat-spinner diameter="48" /></div>
          }
          <table mat-table [dataSource]="transactions()" class="w-100" [style.display]="loading() ? 'none' : ''">
            <ng-container matColumnDef="description">
              <th mat-header-cell *matHeaderCellDef>Description</th>
              <td mat-cell *matCellDef="let t">{{ t.description }}</td>
            </ng-container>
            <ng-container matColumnDef="amount">
              <th mat-header-cell *matHeaderCellDef>Amount</th>
              <td mat-cell *matCellDef="let t">
                <span [class]="t.type === 'Credit' ? 'text-success' : 'text-danger'">
                  {{ t.type === 'Credit' ? '+' : '-' }}₹{{ t.amount | number:'1.2-2' }}
                </span>
              </td>
            </ng-container>
            <ng-container matColumnDef="type">
              <th mat-header-cell *matHeaderCellDef>Type</th>
              <td mat-cell *matCellDef="let t">
                <mat-chip [color]="t.type === 'Credit' ? 'primary' : 'warn'" highlighted>{{ t.type }}</mat-chip>
              </td>
            </ng-container>
            <ng-container matColumnDef="date">
              <th mat-header-cell *matHeaderCellDef>Date</th>
              <td mat-cell *matCellDef="let t">{{ t.createdAt | date:'medium' }}</td>
            </ng-container>
            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
            <tr class="mat-row" *matNoDataRow>
              <td class="mat-cell" colspan="4" style="text-align:center;padding:24px;color:#999;">No transactions yet.</td>
            </tr>
          </table>
          <!-- Always in DOM — never destroyed -->
          <mat-paginator
            [length]="totalCount()"
            [pageSize]="pageSize"
            [pageSizeOptions]="[5, 10, 20]"
            showFirstLastButtons
            (page)="onPage($event)"
          />
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .wallet-balance-card { background: linear-gradient(135deg, #2e7d32, #43a047); color: white; }
    .wallet-icon { font-size: 48px; width: 48px; height: 48px; color: white; }
    .balance-label { font-size: 14px; opacity: 0.9; margin-top: 8px; }
    .balance-amount { font-size: 36px; font-weight: 700; margin-top: 4px; }
  `]
})
export class GuestWalletComponent implements OnInit {
  private walletService = inject(WalletService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  loading = signal(true);
  topping = signal(false);
  wallet = signal<WalletResponseDto | null>(null);
  transactions = signal<WalletTransactionDto[]>([]);
  totalCount = signal(0);
  pageSize = 10;
  displayedColumns = ['description', 'amount', 'type', 'date'];

  topUpForm = this.fb.group({
    amount: [null as number | null, [Validators.required, Validators.min(1), Validators.max(100000)]]
  });

  ngOnInit() {
    this.loadRazorpay();
    this.load(1, this.pageSize);
  }

  private loadRazorpay() {
    if (typeof Razorpay !== 'undefined') return;
    const script = document.createElement('script');
    script.src = 'https://checkout.razorpay.com/v1/checkout.js';
    script.async = true;
    document.head.appendChild(script);
  }

  load(page: number, pageSize: number) {
    this.loading.set(true);
    this.walletService.getWallet(page, pageSize).subscribe({
      next: data => {
        this.wallet.set(data.wallet);
        this.transactions.set(data.transactions);
        this.totalCount.set(data.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  openRazorpay() {
    if (this.topUpForm.invalid) return;
    const amount = this.topUpForm.value.amount!;
    const amountPaise = Math.round(amount * 100);

    const options: any = {
      key: environment.razorpayKeyId,
      amount: amountPaise,
      currency: 'INR',
      name: '🏨 Thanush StayHub',
      description: `Wallet Top-up — ₹${amount}`,
      image: 'https://i.imgur.com/n5tjHFD.png',
      theme: { color: '#2d3a8c' },
      handler: () => {
        this.topping.set(true);
        this.walletService.topUp({ amount }).subscribe({
          next: w => {
            this.wallet.set(w);
            this.toast.success(`₹${amount} added to wallet!`);
            this.topUpForm.reset();
            this.topping.set(false);
            this.load(1, this.pageSize);
          },
          error: () => this.topping.set(false)
        });
      },
      modal: { ondismiss: () => this.toast.error('Payment cancelled.') }
    };

    try {
      const rzp = new Razorpay(options);
      rzp.open();
    } catch {
      this.toast.error('Razorpay failed to load. Please try again.');
    }
  }

  onPage(e: PageEvent) { this.pageSize = e.pageSize; this.load(e.pageIndex + 1, e.pageSize); window.scrollTo({ top: 0, behavior: 'smooth' }); }
}
