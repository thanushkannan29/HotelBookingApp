import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule],
  template: `
    <div class="not-found-page">
      <div class="not-found-content">
        <div class="error-code">404</div>
        <h1>Page not found</h1>
        <p>The page you're looking for doesn't exist or has been moved.</p>
        <div class="nf-actions">
          <a mat-flat-button color="primary" routerLink="/">
            <mat-icon>home</mat-icon> Go Home
          </a>
          @if (auth.isAuthenticated()) {
            <a mat-stroked-button [routerLink]="auth.getRedirectUrl()">
              <mat-icon>dashboard</mat-icon> Dashboard
            </a>
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    .not-found-page {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--color-bg);
      padding: 40px 24px;
    }
    .not-found-content {
      text-align: center;
      max-width: 480px;
    }
    .error-code {
      font-family: var(--font-display);
      font-size: clamp(6rem, 20vw, 10rem);
      font-weight: 700;
      line-height: 1;
      color: var(--color-border);
      margin-bottom: 16px;
      letter-spacing: -0.04em;
    }
    h1 {
      font-family: var(--font-display);
      font-size: 1.75rem;
      font-weight: 600;
      margin-bottom: 12px;
    }
    p {
      font-size: 1rem;
      color: var(--color-text-secondary);
      margin-bottom: 32px;
      line-height: 1.6;
    }
    .nf-actions { display: flex; gap: 12px; justify-content: center; flex-wrap: wrap; }
  `]
})
export class NotFoundComponent {
  auth = inject(AuthService);
}
