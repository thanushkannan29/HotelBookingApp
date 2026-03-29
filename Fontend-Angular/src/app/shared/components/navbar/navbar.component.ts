import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { UserService } from '../../../core/services/api.services';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [
    CommonModule, RouterLink, RouterLinkActive,
    MatToolbarModule, MatButtonModule,
    MatIconModule, MatMenuModule, MatDividerModule, MatTooltipModule
  ],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.scss'
})
export class NavbarComponent implements OnInit {
  auth = inject(AuthService);
  private userService = inject(UserService);
  mobileOpen = signal(false);
  isDarkMode = signal(false);

  ngOnInit() {
    const saved = localStorage.getItem('theme');
    if (saved === 'dark') {
      this.isDarkMode.set(true);
      document.body.classList.add('dark-theme');
    }
    // Load profile image for guest/superadmin on navbar init
    if (this.auth.isAuthenticated() && (this.auth.isGuest() || this.auth.isSuperAdmin())) {
      this.userService.getProfile().subscribe({
        next: p => this.auth.updateProfileImage(p.profileImageUrl ?? null),
        error: () => {}
      });
    }
  }

  toggleTheme() {
    const dark = !this.isDarkMode();
    this.isDarkMode.set(dark);
    if (dark) {
      document.body.classList.add('dark-theme');
      localStorage.setItem('theme', 'dark');
    } else {
      document.body.classList.remove('dark-theme');
      localStorage.setItem('theme', 'light');
    }
  }

  toggleMobile() { this.mobileOpen.update(v => !v); }
  closeMobile() { this.mobileOpen.set(false); }
}
