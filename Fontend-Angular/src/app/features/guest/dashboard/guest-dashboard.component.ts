import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { DecimalPipe } from '@angular/common';
import { DashboardService, UserService } from '../../../core/services/api.services';
import { AuthService } from '../../../core/services/auth.service';
import { GuestDashboardDto, UserProfileResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-guest-dashboard',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, DecimalPipe],
  templateUrl: './guest-dashboard.component.html',
  styleUrl: './guest-dashboard.component.scss'
})
export class GuestDashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private userService      = inject(UserService);
  auth    = inject(AuthService);
  data    = signal<GuestDashboardDto | null>(null);
  profile = signal<UserProfileResponseDto | null>(null);

  ngOnInit() {
    this.dashboardService.getGuestDashboard().subscribe(d => this.data.set(d));
    this.userService.getProfile().subscribe(p => {
      this.profile.set(p);
      // Keep navbar in sync with the latest name from the server
      if (p.name) this.auth.updateUserName(p.name);
    });
  }
}
