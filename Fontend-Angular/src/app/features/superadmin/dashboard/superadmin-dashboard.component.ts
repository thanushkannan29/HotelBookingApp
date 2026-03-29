import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { DecimalPipe } from '@angular/common';
import { DashboardService } from '../../../core/services/api.services';
import { RevenueService } from '../../../core/services/revenue.service';
import { SuperAdminDashboardDto } from '../../../core/models/models';
import { SuperadminReportService } from './superadmin-report.service';

@Component({
  selector: 'app-superadmin-dashboard',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, DecimalPipe],
  templateUrl: './superadmin-dashboard.component.html',
  styleUrl: './superadmin-dashboard.component.scss'
})
export class SuperAdminDashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private revenueService   = inject(RevenueService);
  private reportService    = inject(SuperadminReportService);

  data             = signal<SuperAdminDashboardDto | null>(null);
  commissionEarned = signal(0);

  ngOnInit() {
    this.dashboardService.getSuperAdminDashboard().subscribe(d => this.data.set(d));
    this.revenueService.getSummary().subscribe(s => this.commissionEarned.set(s.totalCommissionEarned));
  }

  downloadReport() {
    const d = this.data();
    if (d) this.reportService.downloadReport(d, this.commissionEarned());
  }
}
