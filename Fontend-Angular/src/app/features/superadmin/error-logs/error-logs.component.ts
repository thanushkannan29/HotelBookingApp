import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DatePipe } from '@angular/common';
import { LogService } from '../../../core/services/api.services';
import { LogResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-error-logs',
  standalone: true,
  imports: [
    CommonModule, RouterLink, DatePipe,
    MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule,
    MatTableModule, MatPaginatorModule, MatExpansionModule, MatProgressSpinnerModule,
  ],
  templateUrl: './error-logs.component.html',
  styleUrl: './error-logs.component.scss'
})
export class ErrorLogsComponent implements OnInit {
  private logService = inject(LogService);

  logs         = signal<LogResponseDto[]>([]);
  totalCount   = signal(0);
  loading      = signal(false);
  pageSize     = 20;
  currentPage  = 1;
  expandedRow  = signal<LogResponseDto | null>(null);
  displayedColumns = ['statusCode', 'method', 'path', 'user', 'role', 'timestamp', 'expand'];

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.logService.getAllLogs(this.currentPage, this.pageSize).subscribe({
      next: r => {
        this.logs.set(r.logs as LogResponseDto[]);
        this.totalCount.set(r.totalCount ?? 0);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onPage(e: PageEvent) { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); }

  toggleRow(row: LogResponseDto) {
    this.expandedRow.set(this.expandedRow()?.logId === row.logId ? null : row);
  }

  statusClass(code: number): string {
    if (code >= 500) return 'badge-error';
    if (code >= 400) return 'badge-warning';
    return 'badge-success';
  }
}
