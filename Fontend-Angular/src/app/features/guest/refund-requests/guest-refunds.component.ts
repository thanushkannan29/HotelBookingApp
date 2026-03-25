import { Component, inject, signal, OnInit } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RefundService } from '../../../core/services/api.services';
import { RefundRequestResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-guest-refunds',
  standalone: true,
  imports: [MatIconModule, DatePipe, DecimalPipe],
  template: `
    <div class="page-wrapper">
      <div class="container" style="max-width:760px;">
        <h1 class="section-title">Refund Requests</h1>
        <p class="section-subtitle">Track the status of your refund requests</p>

        @if (refunds().length === 0) {
          <div class="empty-state">
            <mat-icon class="empty-icon">account_balance_wallet</mat-icon>
            <h3>No refund requests</h3>
            <p>Refund requests are created automatically when you cancel a paid booking</p>
          </div>
        } @else {
          <div style="display:flex; flex-direction:column; gap:12px;">
            @for (r of refunds(); track r.refundRequestId) {
              <div class="refund-card">
                <div class="rf-header">
                  <div>
                    <span class="code">{{ r.reservationCode }}</span>
                    <div class="rf-amount">₹{{ r.refundAmount | number:'1.0-0' }}</div>
                  </div>
                  <span class="badge" [class]="statusClass(r.status)">{{ r.status }}</span>
                </div>
                <p class="rf-reason">Reason: {{ r.reason }}</p>
                @if (r.adminResponse) {
                  <p class="rf-admin-resp">
                    <mat-icon>admin_panel_settings</mat-icon>
                    {{ r.adminResponse }}
                  </p>
                }
                <div class="rf-dates">
                  <span>Requested {{ r.createdAt | date:'MMM d, y' }}</span>
                  @if (r.processedAt) {
                    <span>· Processed {{ r.processedAt | date:'MMM d, y' }}</span>
                  }
                </div>
              </div>
            }
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .refund-card {
      background: var(--color-surface);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-lg);
      padding: 20px 24px;
      display: flex;
      flex-direction: column;
      gap: 10px;
    }
    .rf-header { display: flex; justify-content: space-between; align-items: flex-start; }
    .code { font-family: monospace; font-size: 0.78rem; color: var(--color-text-muted); display: block; margin-bottom: 4px; }
    .rf-amount { font-family: var(--font-display); font-size: 1.5rem; font-weight: 700; }
    .rf-reason { font-size: 0.875rem; color: var(--color-text-secondary); }
    .rf-admin-resp {
      display: flex; align-items: center; gap: 6px;
      font-size: 0.82rem; color: var(--color-primary);
      background: rgba(45,58,140,0.06); padding: 8px 12px; border-radius: 8px;
      mat-icon { font-size: 14px; width: 14px; height: 14px; }
    }
    .rf-dates { font-size: 0.78rem; color: var(--color-text-muted); }
  `]
})
export class GuestRefundsComponent implements OnInit {
  private refundService = inject(RefundService);
  refunds = signal<RefundRequestResponseDto[]>([]);

ngOnInit() {
  this.refundService.getGuestRefundRequests().subscribe(res => {
    this.refunds.set(res.refundRequests ?? []);
  });
}



  statusClass(status: string): string {
    const m: Record<string, string> = { Pending: 'badge-warning', Approved: 'badge-success', Rejected: 'badge-error' };
    return m[status] ?? 'badge-muted';
  }
}
