import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { DecimalPipe } from '@angular/common';
import { DashboardService } from '../../../core/services/api.services';
import { SuperAdminDashboardDto } from '../../../core/models/models';

@Component({
  selector: 'app-superadmin-dashboard',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, DecimalPipe],
  templateUrl: './superadmin-dashboard.component.html',
  styleUrl: './superadmin-dashboard.component.scss'
})
export class SuperAdminDashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  data = signal<SuperAdminDashboardDto | null>(null);

  ngOnInit() {
    this.dashboardService.getSuperAdminDashboard().subscribe(d => this.data.set(d));
  }
}
