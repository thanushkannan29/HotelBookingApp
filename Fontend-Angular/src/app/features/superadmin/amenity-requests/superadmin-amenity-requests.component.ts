import { Component, inject, signal, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatTabsModule } from '@angular/material/tabs';
import { AmenityRequestService } from '../../../core/services/amenity-request.service';
import { ToastService } from '../../../core/services/toast.service';
import { AmenityRequestResponseDto } from '../../../core/models/models';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { InputDialogComponent } from '../../../shared/components/input-dialog/input-dialog.component';

@Component({
  selector: 'app-superadmin-amenity-requests',
  standalone: true,
  imports: [
    CommonModule, RouterLink,
    MatTableModule, MatButtonModule, MatFormFieldModule, MatInputModule,
    MatPaginator, MatPaginatorModule, MatIconModule, MatChipsModule,
    MatProgressSpinnerModule, MatSelectModule, MatDialogModule, MatTabsModule,
  ],
  templateUrl: './superadmin-amenity-requests.component.html',
  styleUrl: './superadmin-amenity-requests.component.scss',
})
export class SuperadminAmenityRequestsComponent implements OnInit {
  private service = inject(AmenityRequestService);
  private toast   = inject(ToastService);
  private dialog  = inject(MatDialog);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  loading    = signal(false);
  requests   = signal<AmenityRequestResponseDto[]>([]);
  totalCount = signal(0);
  pageSize   = 10;
  currentPage = 1;
  selectedStatus = 'All';
  displayedColumns = ['amenityName', 'category', 'hotel', 'admin', 'status', 'actions'];
  readonly statusTabs = ['All', 'Pending', 'Approved', 'Rejected'];

  ngOnInit() { this.load(); }

  private resetPage() { this.currentPage = 1; this.paginator?.firstPage(); }

  load() {
    this.loading.set(true);
    this.service.getAll(this.selectedStatus, this.currentPage, this.pageSize).subscribe({
      next: data => { this.requests.set(data.requests); this.totalCount.set(data.totalCount); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  onTabChange(i: number) { this.selectedStatus = this.statusTabs[i]; this.resetPage(); this.load(); }
  onPage(e: PageEvent) { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); window.scrollTo({ top: 0, behavior: 'smooth' }); }

  approve(r: AmenityRequestResponseDto) {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Approve Request',
        message: `Approve "${r.amenityName}" from ${r.hotelName}? It will be added to the amenities list.`,
        confirmLabel: 'Approve',
        confirmColor: 'primary',
        icon: 'check_circle'
      }
    });
    ref.afterClosed().subscribe(ok => {
      if (!ok) return;
      this.service.approve(r.amenityRequestId).subscribe({
        next: () => { this.toast.success('Amenity approved and added!'); this.load(); }
      });
    });
  }

  reject(r: AmenityRequestResponseDto) {
    const ref = this.dialog.open(InputDialogComponent, {
      data: {
        title: 'Reject Request',
        label: 'Rejection Reason',
        placeholder: 'Explain why this request is being rejected…',
        confirmLabel: 'Reject',
        confirmColor: 'warn',
        multiline: true
      }
    });
    ref.afterClosed().subscribe((note: string | null) => {
      if (!note) return;
      this.service.reject(r.amenityRequestId, note).subscribe({
        next: () => { this.toast.success('Request rejected.'); this.load(); }
      });
    });
  }

  statusClass(status: string): string {
    const m: Record<string, string> = { Approved: 'badge-success', Pending: 'badge-warning', Rejected: 'badge-error' };
    return m[status] ?? 'badge-muted';
  }
}
