import { Component, inject, signal, OnInit, AfterViewInit, ViewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatExpansionModule } from '@angular/material/expansion';
import { DatePipe } from '@angular/common';
import { LogService } from '../../../core/services/api.services';
import { LogResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-error-logs',
  standalone: true,
  imports: [
    RouterLink, DatePipe,
    MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule,
    MatTableModule, MatSortModule, MatPaginatorModule,
    MatExpansionModule,
  ],
  templateUrl: './error-logs.component.html',
  styleUrl: './error-logs.component.scss'
})
export class ErrorLogsComponent implements OnInit, AfterViewInit {
  private logService = inject(LogService);

  dataSource = new MatTableDataSource<LogResponseDto>([]);
  displayedColumns = ['statusCode', 'method', 'path', 'user', 'role', 'timestamp', 'expand'];
  expandedRow = signal<LogResponseDto | null>(null);

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  ngOnInit() { this.load(); }

  ngAfterViewInit() {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
  }

  load() {
    // Load up to 500 logs for client-side table
    this.logService.getAllLogs(1, 500).subscribe(r => {
      this.dataSource.data = r.logs as LogResponseDto[];
    });
  }

  applyFilter(event: Event) {
    const val = (event.target as HTMLInputElement).value;
    this.dataSource.filter = val.trim().toLowerCase();
    if (this.dataSource.paginator) this.dataSource.paginator.firstPage();
  }

  toggleRow(row: LogResponseDto) {
    this.expandedRow.set(this.expandedRow()?.logId === row.logId ? null : row);
  }

  statusClass(code: number): string {
    if (code >= 500) return 'badge-error';
    if (code >= 400) return 'badge-warning';
    return 'badge-success';
  }
}