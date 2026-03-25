import { Component, inject, signal, OnInit, AfterViewInit, ViewChild, Input } from '@angular/core';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { DatePipe, SlicePipe } from '@angular/common';
import { AuditLogService } from '../../../core/services/api.services';
import { AuditLogResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-audit-logs',
  standalone: true,
  imports: [
    RouterLink, MatButtonModule, MatIconModule, DatePipe, SlicePipe,
    MatFormFieldModule, MatInputModule,
    MatTableModule, MatSortModule, MatPaginatorModule,
  ],
  templateUrl: './audit-logs.component.html',
 
})
export class AuditLogsComponent implements OnInit, AfterViewInit {
  private auditLogService = inject(AuditLogService);
  private route = inject(ActivatedRoute);

  @Input() isSuperAdmin = false;

  dataSource = new MatTableDataSource<AuditLogResponseDto>([]);
  displayedColumns = ['action', 'entityName', 'changes', 'createdAt'];

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  get backLink() {
    return this.isSuperAdmin ? '/superadmin/dashboard' : '/admin/dashboard';
  }

  ngOnInit() {
    const isSuper = this.route.snapshot.data['isSuperAdmin'] ?? this.isSuperAdmin;
    const obs = isSuper
      ? this.auditLogService.getAllAuditLogs(1, 500)
      : this.auditLogService.getAdminAuditLogs(1, 500);

    obs.subscribe(r => { this.dataSource.data = r.logs; });
  }

  ngAfterViewInit() {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
  }

  applyFilter(event: Event) {
    const val = (event.target as HTMLInputElement).value;
    this.dataSource.filter = val.trim().toLowerCase();
    if (this.dataSource.paginator) this.dataSource.paginator.firstPage();
  }

  actionClass(action: string): string {
    const m: Record<string, string> = {
      CREATE: 'badge-success', UPDATE: 'badge-warning',
      DELETE: 'badge-error', LOGIN: 'badge-info',
    };
    return m[action?.toUpperCase()] ?? 'badge-muted';
  }
}