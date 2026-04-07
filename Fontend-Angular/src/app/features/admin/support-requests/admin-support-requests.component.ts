import { Component, inject, signal, OnInit, ViewChild } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatExpansionModule } from '@angular/material/expansion';
import { SupportRequestService } from '../../../core/services/support-request.service';
import { SupportRequestResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-admin-support-requests',
  standalone: true,
  imports: [
    CommonModule, RouterLink, DatePipe,
    MatTableModule, MatButtonModule, MatIconModule,
    MatPaginatorModule, MatProgressSpinnerModule, MatExpansionModule,
  ],
  templateUrl: './admin-support-requests.component.html',
  styleUrl: './admin-support-requests.component.scss',
})
export class AdminSupportRequestsComponent implements OnInit {
  private service = inject(SupportRequestService);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  loading    = signal(false);
  requests   = signal<SupportRequestResponseDto[]>([]);
  totalCount = signal(0);
  pageSize   = 10;
  currentPage = 1;

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.service.getAdminRequests(this.currentPage, this.pageSize).subscribe({
      next: data => { this.requests.set(data.requests); this.totalCount.set(data.totalCount); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  onPage(e: PageEvent) { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); window.scrollTo({ top: 0, behavior: 'smooth' }); }

  statusClass(s: string): string {
    const m: Record<string, string> = {
      Open: 'badge-warning', InProgress: 'badge-primary', Resolved: 'badge-success',
    };
    return m[s] ?? 'badge-muted';
  }

  statusIcon(s: string): string {
    const m: Record<string, string> = {
      Open: 'radio_button_unchecked', InProgress: 'pending', Resolved: 'check_circle',
    };
    return m[s] ?? 'help';
  }
}
