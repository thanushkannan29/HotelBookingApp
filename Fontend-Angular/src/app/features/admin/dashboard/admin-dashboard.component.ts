import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { DecimalPipe } from '@angular/common';
import { DashboardService } from '../../../core/services/api.services';
import { HotelService } from '../../../core/services/hotel.service';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/services/auth.service';
import { AdminDashboardDto } from '../../../core/models/models';
import { ReportService } from './report.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, DecimalPipe],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss'
})
export class AdminDashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private hotelService     = inject(HotelService);
  private toast            = inject(ToastService);
  private reportService    = inject(ReportService);
  auth                     = inject(AuthService);

  data             = signal<AdminDashboardDto | null>(null);
  isTogglingStatus = signal(false);

  ngOnInit() {
    this.dashboardService.getAdminDashboard().subscribe(d => {
      this.data.set(d);
      this.auth.updateHotelImage(d.hotelImageUrl ?? null);
    });
  }

  toggleHotelStatus() {
    const d = this.data();
    if (!d) return;
    const newStatus = !d.isActive;
    this.isTogglingStatus.set(true);
    this.hotelService.toggleHotelStatus(newStatus).subscribe({
      next: () => {
        this.data.update(v => v ? { ...v, isActive: newStatus } : v);
        this.toast.success(newStatus ? 'Hotel is now live.' : 'Hotel deactivated.');
        this.isTogglingStatus.set(false);
      },
      error: () => this.isTogglingStatus.set(false),
    });
  }

  downloadReport() {
    const d = this.data();
    if (d) this.reportService.downloadReport(d);
  }
}
