import { Component, inject, signal, OnInit, ViewChild } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { SupportRequestService } from '../../../core/services/support-request.service';
import { ToastService } from '../../../core/services/toast.service';
import { SupportRequestResponseDto } from '../../../core/models/models';
import { InputDialogComponent } from '../../../shared/components/input-dialog/input-dialog.component';

@Component({
  selector: 'app-superadmin-support-requests',
  standalone: true,
  imports: [
    CommonModule, RouterLink, DatePipe, ReactiveFormsModule,
    MatTableModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatPaginatorModule, MatProgressSpinnerModule,
    MatTabsModule, MatExpansionModule, MatDialogModule,
  ],
  templateUrl: './superadmin-support-requests.component.html',
  styleUrl: './superadmin-support-requests.component.scss',
})
export class SuperadminSupportRequestsComponent implements OnInit {
  private service = inject(SupportRequestService);
  private toast   = inject(ToastService);
  private dialog  = inject(MatDialog);
  private fb      = inject(FormBuilder);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  loading    = signal(false);
  requests   = signal<SupportRequestResponseDto[]>([]);
  totalCount = signal(0);
  pageSize   = 10;
  currentPage = 1;
  selectedStatus = 'All';
  selectedRole   = 'All';
  searchTerm     = '';

  readonly statusTabs = ['All', 'Open', 'InProgress', 'Resolved'];
  readonly roleTabs   = ['All', 'Guest', 'Admin', 'Public'];

  private searchSubject = new Subject<string>();

  ngOnInit() {
    this.load();
    this.searchSubject.pipe(debounceTime(400), distinctUntilChanged())
      .subscribe(s => { this.searchTerm = s; this.resetPage(); this.load(); });
  }

  private resetPage() { this.currentPage = 1; this.paginator?.firstPage(); }

  load() {
    this.loading.set(true);
    this.service.getAll(this.selectedStatus, this.selectedRole, this.searchTerm, this.currentPage, this.pageSize).subscribe({
      next: data => { this.requests.set(data.requests); this.totalCount.set(data.totalCount); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  onStatusTab(i: number) { this.selectedStatus = this.statusTabs[i]; this.resetPage(); this.load(); }
  onRoleTab(i: number)   { this.selectedRole   = this.roleTabs[i];   this.resetPage(); this.load(); }
  onSearch(e: Event)     { this.searchSubject.next((e.target as HTMLInputElement).value); }
  onPage(e: PageEvent)   { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); window.scrollTo({ top: 0, behavior: 'smooth' }); }

  respond(r: SupportRequestResponseDto) {
    const ref = this.dialog.open(InputDialogComponent, {
      data: {
        title: `Respond to: ${r.subject}`,
        label: 'Your Response',
        placeholder: 'Type your response to the submitter…',
        confirmLabel: 'Send Response',
        confirmColor: 'primary',
        multiline: true,
      },
      width: '520px',
    });
    ref.afterClosed().subscribe((response: string | null) => {
      if (!response) return;
      this.service.respond(r.supportRequestId, { response, status: 'Resolved' }).subscribe({
        next: () => { this.toast.success('Response sent.'); this.load(); },
      });
    });
  }

  markInProgress(r: SupportRequestResponseDto) {
    this.service.respond(r.supportRequestId, { response: r.adminResponse ?? '', status: 'InProgress' }).subscribe({
      next: () => { this.toast.success('Marked as In Progress.'); this.load(); },
    });
  }

  statusClass(s: string): string {
    const m: Record<string, string> = {
      Open: 'badge-warning', InProgress: 'badge-primary', Resolved: 'badge-success',
    };
    return m[s] ?? 'badge-muted';
  }

  roleClass(r: string): string {
    const m: Record<string, string> = { Guest: 'badge-primary', Admin: 'badge-accent', Public: 'badge-muted' };
    return m[r] ?? 'badge-muted';
  }

  roleIcon(r: string): string {
    const m: Record<string, string> = { Guest: 'person', Admin: 'hotel', Public: 'public' };
    return m[r] ?? 'person';
  }
}
