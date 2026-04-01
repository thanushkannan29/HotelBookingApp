import { Component, inject, computed, OnInit } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { filter, map } from 'rxjs/operators';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavbarComponent } from './shared/components/navbar/navbar.component';
import { FooterComponent } from './shared/components/footer/footer.component';
import { SpinnerComponent } from './shared/components/spinner/spinner.component';
import { ChatbotComponent } from './shared/components/chatbot/chatbot.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent, FooterComponent, SpinnerComponent, ChatbotComponent],
  template: `
    <app-spinner />
    @if (showChrome()) {
      <app-navbar />
    }
    <main [class.auth-main]="!showChrome()">
      <router-outlet />
    </main>
    @if (showChrome()) {
      <app-footer />
    }
    @if (showChrome()) {
      <app-chatbot />
    }
  `,
  styles: [`
    main { min-height: calc(100vh - 64px - 80px); }
    main.auth-main { min-height: 100vh; }
  `]
})
export class AppComponent implements OnInit {
  private router = inject(Router);

  private currentUrl = toSignal(
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd),
      map(e => (e as NavigationEnd).urlAfterRedirects)
    ),
    { initialValue: this.router.url }
  );

  showChrome = computed(() => {
    const url = this.currentUrl() ?? '';
    return !url.startsWith('/auth');
  });

  ngOnInit() {
    // Apply saved theme on app init
    const theme = localStorage.getItem('theme');
    if (theme === 'dark') {
      document.body.classList.add('dark-theme');
    }
  }
}
