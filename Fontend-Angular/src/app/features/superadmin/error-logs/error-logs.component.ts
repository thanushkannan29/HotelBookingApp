import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatExpansionModule } from '@angular/material/expansion';
import { DatePipe } from '@angular/common';
import { LogService } from '../../../core/services/api.services';
import { LogResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-error-logs',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, MatExpansionModule, DatePipe],
  templateUrl: './error-logs.component.html',
  styleUrl: './error-logs.component.scss'
})
export class ErrorLogsComponent implements OnInit {
  private logService = inject(LogService);
  logs    = signal<LogResponseDto[]>([]);
  total   = signal(0);
  page    = signal(1);
  readonly pageSize = 20;
  get totalPages() { return Math.ceil(this.total() / this.pageSize); }

  ngOnInit() { this.load(); }

  load() {
    this.logService.getAllLogs(this.page(), this.pageSize).subscribe(r => {
      this.logs.set(r.logs as LogResponseDto[]);
      this.total.set(r.totalCount);
    });
  }

  next() { this.page.update(p => p + 1); this.load(); }
  prev() { if (this.page() > 1) { this.page.update(p => p - 1); this.load(); } }

  statusClass(code: number): string {
    if (code >= 500) return 'badge-error';
    if (code >= 400) return 'badge-warning';
    return 'badge-success';
  }
}
