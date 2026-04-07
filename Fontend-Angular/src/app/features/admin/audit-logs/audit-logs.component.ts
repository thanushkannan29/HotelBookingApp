import { Component, inject, signal, OnInit, Input, ViewChild } from '@angular/core';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DatePipe, SlicePipe } from '@angular/common';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { AuditLogService } from '../../../core/services/api.services';
import { AuditLogResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-audit-logs',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule, DatePipe, SlicePipe,
    MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule,
    MatTableModule, MatPaginatorModule,
    MatDatepickerModule, MatNativeDateModule, MatProgressSpinnerModule,
  ],
  templateUrl: './audit-logs.component.html',
})
export class AuditLogsComponent implements OnInit {
  private auditLogService = inject(AuditLogService);
  private route           = inject(ActivatedRoute);
  private fb              = inject(FormBuilder);

  @Input() isSuperAdmin = false;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  logs         = signal<AuditLogResponseDto[]>([]);
  totalCount   = signal(0);
  loading      = signal(false);
  pageSize     = 20;
  currentPage  = 1;
  isSuperMode  = false;
  searchTerm   = '';
  displayedColumns = ['action', 'entityName', 'changes', 'createdAt'];

  filterForm = this.fb.group({
    action:   [''],
    dateFrom: [null as Date | null],
    dateTo:   [null as Date | null],
  });

  private searchSubject = new Subject<string>();

  get backLink() { return this.isSuperMode ? '/superadmin/dashboard' : '/admin/dashboard'; }

  ngOnInit() {
    this.isSuperMode = this.route.snapshot.data['mode'] === 'superadmin' || this.isSuperAdmin;
    this.load();
    this.searchSubject.pipe(debounceTime(400), distinctUntilChanged())
      .subscribe(s => { this.searchTerm = s; this.resetPage(); this.load(); });
  }

  private resetPage() {
    this.currentPage = 1;
    this.paginator?.firstPage();
  }

  load() {
    this.loading.set(true);
    const f = this.filterForm.value;
    const obs = this.isSuperMode
      ? this.auditLogService.getAllAuditLogs(
          this.currentPage, this.pageSize,
          undefined, undefined,
          f.action || undefined,
          f.dateFrom ? f.dateFrom.toISOString() : undefined,
          f.dateTo   ? f.dateTo.toISOString()   : undefined
        )
      : this.auditLogService.getAdminAuditLogs(this.currentPage, this.pageSize, this.searchTerm || undefined);

    obs.subscribe({
      next: r => { this.logs.set(r.logs); this.totalCount.set(r.totalCount); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  onSearch(e: Event) {
    this.searchSubject.next((e.target as HTMLInputElement).value);
  }

  applyFilters() { this.resetPage(); this.load(); }
  clearFilters() { this.filterForm.reset(); this.resetPage(); this.load(); }
  onPage(e: PageEvent) { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); window.scrollTo({ top: 0, behavior: 'smooth' }); }

  actionClass(action: string): string {
    const m: Record<string, string> = {
      CREATE: 'badge-success', UPDATE: 'badge-warning',
      DELETE: 'badge-error', LOGIN: 'badge-info',
    };
    return m[action?.toUpperCase()] ?? 'badge-muted';
  }
}
