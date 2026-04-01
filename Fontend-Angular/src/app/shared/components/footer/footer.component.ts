import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [RouterLink],
  template: `
    <footer class="footer">
      <div class="footer-container">

        <!-- Brand -->
        <div class="footer-brand">
          <div class="brand-header">
  <img src="assets/dark.png" alt="Thanush StayHub Logo" class="brand-icon" />
  <span class="brand-name">Thanush StayHub</span>
</div>
          <p class="brand-tagline">
            Smart Hotel Booking & Management Platform for guests and hotel admins.
          </p>
        </div>

        <!-- Links Section -->
        <div class="footer-links">

          <!-- Explore -->
          <div class="link-group">
            <h4>Explore</h4>
            <a routerLink="/hotels">All Hotels</a>
            <a routerLink="/hotels">Top Rated</a>
          </div>

          <!-- Account -->
          <div class="link-group">
            <h4>Account</h4>
            <a routerLink="/auth/login">Sign In</a>
            <a routerLink="/auth/register">Register</a>
          </div>

          <!-- Contact -->
          <div class="link-group">
            <h4>Contact</h4>
            <a routerLink="/contact">Contact Page</a>
            <a href="mailto:thanush&#64;superadmin.com">thanush&#64;superadmin.com</a>
            <a href="tel:+919840650390">+91 98406 50390</a>
          </div>

          <!-- Support -->
          <div class="link-group support">
            <h4>Support</h4>
            <p class="muted">Mon – Sat: 9 AM – 7 PM</p>
            <p class="muted">Sunday: 10 AM – 4 PM</p>
            <p class="muted">Quick help for booking & hotel issues</p>
          </div>

        </div>
      </div>

      <!-- Bottom -->
      <div class="footer-bottom">
        <span>© {{ year }} Thanush StayHub. All rights reserved.</span>
      </div>
    </footer>
  `,
  styles: [`
    .footer {
      background: #111827;
      color: rgba(255,255,255,0.85);
      padding-top: 60px;
    }

    .footer-container {
      max-width: 1200px;
      margin: auto;
      padding: 0 24px 40px;
      display: grid;
      grid-template-columns: 1fr 2fr;
      gap: 60px;
    }

    /* Brand */
    .footer-brand {
      max-width: 320px;
    }

    .brand-header {
      display: flex;
      align-items: center;
      gap: 10px;
      margin-bottom: 10px;
    }

.brand-icon {
  width: 36px;
  height: 36px;
  object-fit: contain;
  border-radius: 8px;
}

    .brand-name {
      font-size: 1.3rem;
      font-weight: 600;
      color: #fff;
    }

    .brand-tagline {
      font-size: 0.9rem;
      color: rgba(255,255,255,0.6);
      line-height: 1.6;
    }

    /* Links */
    .footer-links {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 40px;
    }

    .link-group {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .link-group h4 {
      font-size: 0.8rem;
      text-transform: uppercase;
      letter-spacing: 0.08em;
      color: rgba(255,255,255,0.5);
      margin-bottom: 6px;
    }

    .link-group a {
      font-size: 0.9rem;
      color: rgba(255,255,255,0.75);
      text-decoration: none;
      transition: 0.3s;
    }

    .link-group a:hover {
      color: #fff;
    }

    .link-group p {
      font-size: 0.85rem;
      margin: 0;
    }

    .muted {
      color: rgba(255,255,255,0.5);
    }

    /* Bottom */
    .footer-bottom {
      border-top: 1px solid rgba(255,255,255,0.1);
      padding: 18px 24px;
      text-align: center;
      font-size: 0.8rem;
      color: rgba(255,255,255,0.4);
    }

    /* Responsive */
    @media (max-width: 900px) {
      .footer-container {
        grid-template-columns: 1fr;
        gap: 40px;
      }

      .footer-links {
        grid-template-columns: repeat(2, 1fr);
      }
    }

    @media (max-width: 500px) {
      .footer-links {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class FooterComponent {
  year = new Date().getFullYear();
}
