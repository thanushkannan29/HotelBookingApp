import { Component, inject, signal, OnInit, Input } from '@angular/core';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { DatePipe, SlicePipe } from '@angular/common';
import { AuditLogService } from '../../../core/services/api.services';
import { AuditLogResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-audit-logs',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, DatePipe, SlicePipe],
  template: `
    <div class="page-wrapper">
      <div class="container">
        <div class="page-header-row">
          <div>
            <a [routerLink]="backLink" class="back-link">
              <mat-icon>arrow_back</mat-icon> Dashboard
            </a>
            <h1 class="section-title">Audit Logs</h1>
            <p class="section-subtitle">{{ total() }} records · Page {{ page() }} of {{ totalPages }}</p>
          </div>
        </div>

        @if (logs().length === 0) {
          <div class="empty-state">
            <mat-icon class="empty-icon">history</mat-icon>
            <h3>No audit logs yet</h3>
          </div>
        } @else {
          <div class="table-card">
            <table class="data-table">
              <thead>
                <tr><th>Action</th><th>Entity</th><th>Changes</th><th>When</th></tr>
              </thead>
              <tbody>
                @for (log of logs(); track log.auditLogId) {
                  <tr>
                    <td>
                      <span class="action-chip" [class]="actionClass(log.action)">
                        {{ log.action }}
                      </span>
                    </td>
                    <td>{{ log.entityName }}</td>
                    <td class="changes-cell">
                      <span class="changes-preview">{{ log.changes | slice:0:80 }}{{ log.changes.length > 80 ? '…' : '' }}</span>
                    </td>
                    <td class="date-cell">{{ log.createdAt | date:'MMM d, y HH:mm' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          @if (totalPages > 1) {
            <div class="pagination">
              <button mat-stroked-button [disabled]="page() === 1" (click)="prev()">
                <mat-icon>chevron_left</mat-icon>
              </button>
              <span>{{ page() }} / {{ totalPages }}</span>
              <button mat-stroked-button [disabled]="page() >= totalPages" (click)="next()">
                <mat-icon>chevron_right</mat-icon>
              </button>
            </div>
          }
        }
      </div>
    </div>
  `,
  styles: [`
    .page-header-row { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 24px; flex-wrap: wrap; gap: 16px; }
    .back-link { display: inline-flex; align-items: center; gap: 6px; color: var(--color-text-secondary); font-size: 0.875rem; margin-bottom: 8px; &:hover { color: var(--color-primary); } }
    .table-card { background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-lg); overflow-x: auto; }
    .data-table { width: 100%; border-collapse: collapse; min-width: 700px;
      th { background: var(--color-surface-alt); padding: 11px 14px; text-align: left; font-size: 0.72rem; text-transform: uppercase; letter-spacing: 0.06em; color: var(--color-text-muted); font-weight: 600; border-bottom: 1px solid var(--color-border); }
      td { padding: 12px 14px; font-size: 0.85rem; border-bottom: 1px solid var(--color-border); vertical-align: middle; }
      tr:last-child td { border-bottom: none; }
      tr:hover td { background: rgba(45,58,140,0.02); }
    }
    .action-chip { padding: 3px 10px; border-radius: 12px; font-size: 0.75rem; font-weight: 600; }
    .action-create  { background: #e8f5e9; color: #2e7d32; }
    .action-update  { background: #e3f2fd; color: #1565c0; }
    .action-delete  { background: #ffebee; color: #c62828; }
    .action-approve { background: #e8f5e9; color: #2e7d32; }
    .action-reject  { background: #ffebee; color: #c62828; }
    .action-default { background: var(--color-surface-alt); color: var(--color-text-secondary); }
    .changes-cell { max-width: 300px; }
    .changes-preview { font-size: 0.78rem; color: var(--color-text-muted); font-family: monospace; }
    .date-cell { white-space: nowrap; font-size: 0.78rem; color: var(--color-text-muted); }
    .pagination { display: flex; align-items: center; justify-content: center; gap: 16px; margin-top: 24px; font-size: 0.875rem; color: var(--color-text-secondary); }
  `]
})
export class AuditLogsComponent implements OnInit {
  @Input() mode: 'admin' | 'superadmin' = 'admin';

  private auditLogService = inject(AuditLogService);
  private route = inject(ActivatedRoute);

  logs = signal<AuditLogResponseDto[]>([]);
  total = signal(0);
  page = signal(1);
  readonly pageSize = 20;

  get backLink() { return this.mode === 'superadmin' ? '/superadmin/dashboard' : '/admin/dashboard'; }
  get totalPages() { return Math.ceil(this.total() / this.pageSize); }

  ngOnInit() {
    // Allow mode to be set via route data (when used with loadComponent)
    const routeMode = this.route.snapshot.data?.['mode'];
    if (routeMode) this.mode = routeMode;
    this.load();
  }

  load() {
    const obs = this.mode === 'superadmin'
      ? this.auditLogService.getAllAuditLogs(this.page(), this.pageSize)
      : this.auditLogService.getAdminAuditLogs(this.page(), this.pageSize);
    obs.subscribe(r => { this.logs.set(r.logs as AuditLogResponseDto[]); this.total.set(r.totalCount); });
  }

  next() { this.page.update(p => p + 1); this.load(); }
  prev() { if (this.page() > 1) { this.page.update(p => p - 1); this.load(); } }

  actionClass(action: string): string {
    const a = action.toLowerCase();
    if (a.includes('add') || a.includes('create')) return 'action-create';
    if (a.includes('update')) return 'action-update';
    if (a.includes('delete') || a.includes('block') || a.includes('deactivate')) return 'action-delete';
    if (a.includes('approve')) return 'action-approve';
    if (a.includes('reject')) return 'action-reject';
    return 'action-default';
  }
}
