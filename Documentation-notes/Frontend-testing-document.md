# Hotel Booking System — Complete Frontend & Testing Documentation
**Angular 19+ | TypeScript | Karma/Jasmine | Angular Material | Bootstrap | AI Chatbot (Groq LLM)**

---

## Table of Contents

1. [Project Overview & Architecture](#1-project-overview--architecture)
2. [Project Structure](#2-project-structure)
3. [Angular Configuration — app.config.ts](#3-angular-configuration--appconfigts)
4. [Environment Setup](#4-environment-setup)
5. [TypeScript Models — models.ts](#5-typescript-models--modelsts)
6. [Routing — app.routes.ts](#6-routing--approutests)
7. [Auth Guard — auth.guard.ts](#7-auth-guard--authguardts)
8. [HTTP Interceptors](#8-http-interceptors)
9. [Services](#9-services)
10. [Components](#10-components)
11. [Angular Signals & Computed](#11-angular-signals--computed)
12. [Reactive Forms — Validators](#12-reactive-forms--validators)
13. [Lifecycle Hooks](#13-lifecycle-hooks)
14. [Template Syntax — Interpolation, Binding, Directives](#14-template-syntax--interpolation-binding-directives)
15. [Inter-Component Communication](#15-inter-component-communication)
16. [Observables & RxJS](#16-observables--rxjs)
17. [Angular Material](#17-angular-material)
18. [Bootstrap](#18-bootstrap)
19. [AI Chatbot Integration](#19-ai-chatbot-integration)
20. [Lazy Loading & Feature Modules](#20-lazy-loading--feature-modules)
21. [Dark Theme (DOM Manipulation)](#21-dark-theme-dom-manipulation)
22. [Testing — Karma & Jasmine Complete Guide](#22-testing--karma--jasmine-complete-guide)
23. [Test Files — Full Explanation](#23-test-files--full-explanation)
24. [How to Run Tests](#24-how-to-run-tests)

---

## 1. Project Overview & Architecture

This is a production-ready hotel booking platform built in Angular. It supports four access levels:

- **Public** — unauthenticated users can browse hotels, search by city/dates, view details.
- **Guest** — registered users who book rooms, pay, cancel, leave reviews.
- **Admin** — hotel managers who manage rooms, rates, inventory, reservations, refunds.
- **SuperAdmin** — platform owner who oversees all hotels, users, audit logs.

### Architecture Flow

```
Browser URL
  ↓
app.routes.ts          (lazy-loaded feature routes + guard protection)
  ↓
AuthGuard              (checks AuthService signals before activating route)
  ↓
Feature Component      (standalone, uses inject(), signals for state)
  ↓
Service (inject)       (calls HTTP via HttpClient, maps ApiResponse<T>)
  ↓
auth.interceptor       (attaches JWT Bearer header)
  ↓
loading.interceptor    (shows/hides global spinner)
  ↓
ASP.NET Core API       (returns { success, data } envelope)
```

### Key Design Decisions

| Decision | What was chosen | Why |
|---|---|---|
| State management | Angular Signals | No NgRx needed, built-in reactivity |
| Standalone components | Yes (all) | No NgModule boilerplate |
| DI style | `inject()` function | Cleaner than constructor injection |
| HTTP error handling | Global in interceptor | Single place, not repeated in every component |
| Loading indicator | Counter-based signal service | Handles concurrent requests |
| Auth persistence | localStorage + JWT decode | Survives page refresh |
| AI Chatbot | Groq LLM API (llama-3.1-8b) | Free, fast inference |

---

## 2. Project Structure

```
src/
├── app/
│   ├── app.component.ts          ← Root component (navbar/footer toggle, spinner)
│   ├── app.config.ts             ← provideRouter, provideHttpClient, interceptors
│   ├── app.routes.ts             ← All top-level routes with lazy loading
│   │
│   ├── core/
│   │   ├── guards/
│   │   │   └── auth.guard.ts     ← authGuard, guestGuard, adminGuard, superAdminGuard, publicGuard
│   │   ├── interceptors/
│   │   │   ├── auth.interceptor.ts     ← Attaches JWT token to every API request
│   │   │   └── loading.interceptor.ts  ← Shows/hides global spinner
│   │   ├── models/
│   │   │   └── models.ts         ← ALL TypeScript interfaces (DTOs, enums)
│   │   └── services/
│   │       ├── auth.service.ts         ← login/register/logout + signals
│   │       ├── api.services.ts         ← TransactionService, ReviewService, UserService, etc.
│   │       ├── hotel.service.ts        ← All hotel-related HTTP calls
│   │       ├── booking.service.ts      ← Reservation CRUD
│   │       ├── chatbot.service.ts      ← Groq LLM API calls
│   │       ├── chatbot-prompts.ts      ← System prompt strings per role
│   │       ├── loading.service.ts      ← isLoading signal (counter-based)
│   │       ├── toast.service.ts        ← Success/error toast notifications
│   │       ├── location.service.ts     ← Indian states & cities static data
│   │       ├── wallet.service.ts       ← Guest wallet top-up/balance
│   │       └── promo-code.service.ts   ← Promo code validation/listing
│   │
│   ├── shared/
│   │   └── components/
│   │       ├── navbar/           ← Top navigation bar
│   │       ├── footer/           ← Page footer
│   │       ├── spinner/          ← Global loading spinner
│   │       ├── chatbot/          ← AI chatbot widget (floating button)
│   │       ├── confirm-dialog/   ← Reusable confirmation dialog
│   │       ├── input-dialog/     ← Reusable input dialog
│   │       ├── city-autocomplete/← City search with autocomplete
│   │       └── infinite-carousel/← Hotel image carousel
│   │
│   └── features/
│       ├── auth/                 ← login, register, register-admin
│       ├── hotel/                ← hotel-list, hotel-card, hotel-details
│       ├── booking/              ← booking-create, booking-list, booking-detail
│       ├── guest/                ← dashboard, profile, reviews, transactions, wallet, promo-codes
│       ├── admin/                ← dashboard, hotel-mgmt, room-mgmt, inventory, reservations, etc.
│       ├── superadmin/           ← dashboard, hotel-control, amenity-mgmt, revenue, logs
│       ├── contact/              ← Public contact form
│       └── not-found/            ← 404 page
│
├── environments/
│   ├── environment.ts            ← dev: apiUrl, groqApiKey, razorpayKeyId
│   └── environment.prod.ts       ← production values
│
├── index.html                    ← Bootstrap 5 CDN + Razorpay script
├── main.ts                       ← bootstrapApplication(AppComponent, appConfig)
└── styles.scss                   ← Global SCSS + dark theme variables
```

---

## 3. Angular Configuration — app.config.ts

```typescript
// app/app.config.ts
import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withViewTransitions, withInMemoryScrolling } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { loadingInterceptor } from './core/interceptors/loading.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(
      routes,
      withViewTransitions(),                              // Smooth route transitions
      withInMemoryScrolling({
        scrollPositionRestoration: 'top',                // Scroll to top on navigation
        anchorScrolling: 'enabled',                      // Support #anchor links
      })
    ),
    provideHttpClient(withInterceptors([loadingInterceptor, authInterceptor])),
    provideAnimationsAsync(),                            // Async animations for better performance
  ],
};
```

### Explanation of Every Provider

**`provideZoneChangeDetection({ eventCoalescing: true })`**
Angular uses Zone.js to detect changes. `eventCoalescing: true` batches multiple events fired in quick succession into one change detection cycle — improves performance when many DOM events fire together (e.g., rapid typing).

**`provideRouter(routes, ...features)`**
Registers the router. The two extra features are:
- `withViewTransitions()` — uses the browser's native View Transitions API for animated page changes. Works in modern Chrome/Edge.
- `withInMemoryScrolling(...)` — `scrollPositionRestoration: 'top'` automatically scrolls to the top on every navigation. `anchorScrolling: 'enabled'` allows `<a href="#section">` anchors to work correctly with the router.

**`provideHttpClient(withInterceptors([...]))`**
Registers Angular's HTTP client using the modern functional interceptor pattern. Interceptors are applied in the order listed: `loadingInterceptor` runs first (shows spinner), then `authInterceptor` (adds token). On the response side, `authInterceptor` error handler runs first, then `loadingInterceptor`'s `finalize` hides the spinner.

**`provideAnimationsAsync()`**
Loads Angular Material animations lazily. This means the animation module isn't part of the initial bundle, making the app load faster.

---

## 4. Environment Setup

```typescript
// environments/environment.ts (development)
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7208/api',     // .NET Core API base URL
  razorpayKeyId: 'rzp_test_SVtcM9b8whLPCh', // Payment gateway test key
  groqApiUrl: 'https://api.groq.com/openai/v1/chat/completions',
  groqApiKey: 'gsk_...'                     // Groq LLM API key
};
```

The `environment` object is imported into services:
```typescript
import { environment } from '../../../environments/environment';
// Then used as:
private base = `${environment.apiUrl}`;
```

During `ng build --configuration production`, Angular replaces `environment.ts` with `environment.prod.ts` automatically via the `fileReplacements` in `angular.json`.

---

## 5. TypeScript Models — models.ts

All API request/response shapes are defined as TypeScript interfaces in one central file. This is the "contract" between the Angular frontend and the .NET backend.

### API Response Wrapper

```typescript
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  statusCode?: number;
}
```

Every backend endpoint returns `{ success: true, data: { ... } }`. Services always call `.pipe(map(r => r.data!))` to unwrap this.

### Key Interfaces

```typescript
// JWT Token payload shape (decoded from the JWT string)
export interface JwtPayload {
  nameid: string;       // maps to User.UserId
  unique_name: string;  // maps to User.Name
  role: string;         // 'Guest' | 'Admin' | 'SuperAdmin'
  HotelId?: string;     // only for Admin role
  exp: number;          // expiry Unix timestamp
}

// The object stored in AuthService signal
export interface CurrentUser {
  userId: string;
  userName: string;
  role: UserRole;      // 'Guest' | 'Admin' | 'SuperAdmin'
  hotelId?: string;
}

// Hotel listing card data
export interface HotelListItemDto {
  hotelId: string;
  name: string;
  city: string;
  imageUrl: string;
  averageRating: number;
  reviewCount: number;
  startingPrice?: number;
}

// Reservation creation request
export interface CreateReservationDto {
  hotelId: string;
  roomTypeId: string;
  checkInDate: string;      // 'YYYY-MM-DD'
  checkOutDate: string;
  numberOfRooms: number;
  selectedRoomIds?: string[]; // optional: guest picks specific rooms
  promoCodeUsed?: string;
  walletAmountToUse?: number;
  payCancellationFee?: boolean;
}

// Enum-like constant for payment methods
export const PaymentMethod: Record<number, string> = {
  1: 'Credit Card',
  2: 'Debit Card',
  3: 'UPI',
  4: 'Net Banking',
  5: 'Wallet',
};

export const PaymentStatus: Record<number, string> = {
  1: 'Pending',
  2: 'Success',
  3: 'Failed',
  4: 'Refunded',
};
```

### Why Interfaces?

TypeScript interfaces are compile-time only (no runtime cost). They give:
- Auto-complete in the IDE
- Compile errors if you miss a required field
- Clear documentation of what data flows between layers
- Type safety when mapping between backend JSON and Angular components

---

## 6. Routing — app.routes.ts

```typescript
export const routes: Routes = [
  { path: '', redirectTo: '/hotels', pathMatch: 'full' },

  // Public (no guard)
  { path: 'hotels', loadChildren: () => import('./features/hotel/hotel.routes')
      .then(m => m.HOTEL_ROUTES) },

  // Auth pages (publicGuard: only accessible when NOT logged in)
  { path: 'auth', canActivate: [publicGuard],
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES) },

  // Guest-only
  { path: 'guest', canActivate: [guestGuard],
    loadChildren: () => import('./features/guest/guest.routes').then(m => m.GUEST_ROUTES) },

  // Booking (guest only)
  { path: 'booking', canActivate: [guestGuard],
    loadChildren: () => import('./features/booking/booking.routes').then(m => m.BOOKING_ROUTES) },

  // Admin-only
  { path: 'admin', canActivate: [adminGuard],
    loadChildren: () => import('./features/admin/admin.routes').then(m => m.ADMIN_ROUTES) },

  // SuperAdmin-only
  { path: 'superadmin', canActivate: [superAdminGuard],
    loadChildren: () => import('./features/superadmin/superadmin.routes').then(m => m.SUPERADMIN_ROUTES) },

  // Miscellaneous
  { path: 'contact', loadComponent: () => import('./features/contact/contact.component')
      .then(m => m.ContactComponent) },
  { path: '**', loadComponent: () => import('./features/not-found/not-found.component')
      .then(m => m.NotFoundComponent) },
];
```

### Lazy Loading Explained

`loadChildren` returns a Promise — Angular only downloads the feature bundle when the user first navigates to that route. This drastically reduces the initial bundle size.

The pattern `() => import('...').then(m => m.EXPORT_NAME)` is called a **dynamic import**. Angular's build system creates a separate JavaScript chunk for each lazy-loaded feature.

**Eager loading** (not used here) would be `component: SomeComponent` directly — the component loads with the app, making the initial bundle larger.

---

## 7. Auth Guard — auth.guard.ts

Guards are functions (not classes in modern Angular) that return `true` (allow) or `false`/`UrlTree` (block and redirect).

```typescript
import { inject } from '@angular/core';
import { Router, CanActivateFn, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

// Protects any authenticated route
export const authGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated()) return true;
  localStorage.setItem('returnUrl', state.url);  // remember where user was going
  router.navigate(['/auth/login']);
  return false;
};

// Protects Guest-specific pages
export const guestGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated() && auth.isGuest()) return true;
  if (auth.isAuthenticated()) {
    router.navigate([auth.getRedirectUrl()]);  // send Admin to admin dashboard, etc.
    return false;
  }
  localStorage.setItem('returnUrl', state.url);
  router.navigate(['/auth/login']);
  return false;
};

// publicGuard: BLOCKS already-logged-in users from visiting /auth/login
export const publicGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isAuthenticated()) return true;
  router.navigate([auth.getRedirectUrl()]);
  return false;
};
```

### How Guards Work

When the user navigates to `/guest/dashboard`, the Router runs `guestGuard` before activating the route:
1. If `auth.isGuest()` is true → allow
2. If logged in but wrong role → redirect to their own dashboard
3. If not logged in → save current URL to `localStorage.returnUrl`, redirect to login
4. After login, the `LoginComponent` reads `returnUrl` and navigates there

The `inject()` function works inside guards because Angular creates an injection context for them. This is the modern alternative to constructor injection.

---

## 8. HTTP Interceptors

### Auth Interceptor

```typescript
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router      = inject(Router);
  const toast       = inject(ToastService);

  // SKIP: External APIs (Groq AI, etc.) don't need our JWT
  if (!req.url.startsWith(environment.apiUrl)) {
    return next(req);
  }

  const token  = authService.token();  // reads from signal
  const cloned = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(cloned).pipe(
    catchError((error: HttpErrorResponse) => {
      let message = 'An unexpected error occurred.';

      if (error.error?.message)    message = error.error.message;
      else if (error.status === 0)  message = 'Cannot connect to server.';
      else if (error.status === 401) { authService.logout(); return throwError(() => error); }
      else if (error.status === 403) { message = 'No permission.'; router.navigate(['/unauthorized']); }
      else if (error.status === 404) message = error.error?.message ?? 'Resource not found.';
      else if (error.status === 409) message = error.error?.message ?? 'Conflict — already exists.';
      else if (error.status === 429) message = 'Too many requests. Please wait.';
      else if (error.status >= 500)  message = 'Server error. Please try again later.';

      toast.error(message);
      return throwError(() => error);  // re-throw so component .error handler also fires
    })
  );
};
```

**Key concept — `req.clone()`**: HTTP requests are immutable. To add a header, you clone the request object with the new header, then pass the clone to `next(cloned)`.

**Key concept — `catchError`**: An RxJS operator that intercepts errors in the observable pipeline. It receives the `HttpErrorResponse`, shows a toast, and re-throws the error. Components that subscribe can still handle the error in their own `.error` callback.

**Why skip external URLs?** The Groq AI API has its own authentication header. If you accidentally attach your JWT to a request to Groq's servers, it may fail with a 401.

### Loading Interceptor

```typescript
export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loadingService = inject(LoadingService);

  // Only show spinner for our own API (localhost)
  if (!req.url.includes('localhost') && !req.url.includes('127.0.0.1')) {
    return next(req);
  }

  loadingService.show();
  return next(req).pipe(finalize(() => loadingService.hide()));
};
```

**`finalize`** runs whether the request succeeds or fails. This guarantees the spinner always hides. Without `finalize`, an error response would leave the spinner spinning forever.

---

## 9. Services

### AuthService (Most Important)

```typescript
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'hotel_token';
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  // Private writable signals (only this service can write)
  private _currentUser = signal<CurrentUser | null>(null);
  private _token = signal<string | null>(null);
  private _profileImageUrl = signal<string | null>(null);

  // Public readonly signals (components read, can't write)
  readonly currentUser = this._currentUser.asReadonly();
  readonly token = this._token.asReadonly();
  readonly isAuthenticated = computed(() => !!this._currentUser());
  readonly isGuest = computed(() => this._currentUser()?.role === 'Guest');
  readonly isAdmin = computed(() => this._currentUser()?.role === 'Admin');
  readonly isSuperAdmin = computed(() => this._currentUser()?.role === 'SuperAdmin');

  constructor() {
    this.loadFromStorage();  // restore session on app startup
  }

  private loadFromStorage(): void {
    const token = localStorage.getItem(this.TOKEN_KEY);
    if (token) {
      try {
        const payload = jwtDecode<JwtPayload>(token);
        if (payload.exp * 1000 > Date.now()) {  // check not expired
          this._token.set(token);
          this._currentUser.set(this.payloadToUser(payload));
        } else {
          this.clearStorage();  // expired — remove
        }
      } catch {
        this.clearStorage();    // malformed — remove
      }
    }
  }

  login(dto: LoginDto): Observable<AuthResponseDto> {
    return this.http.post<ApiResponse<AuthResponseDto>>(
      `${environment.apiUrl}/auth/login`, dto
    ).pipe(
      map(r => r.data!),           // unwrap the ApiResponse<T> wrapper
      tap(res => this.setToken(res.token))  // side-effect: save token
    );
  }

  logout(): void {
    this.clearStorage();
    this.router.navigate(['/auth/login']);
  }
}
```

**Signals pattern**: The private `_currentUser` can be mutated (`.set()`), but components only access the public `currentUser` which is `.asReadonly()`. This enforces one-directional data flow: only `AuthService` decides who is logged in.

**`computed()`**: `isAuthenticated` is derived from `currentUser`. When `currentUser` changes from `null` to a user object, `isAuthenticated` automatically becomes `true`. No event subscriptions needed.

**`jwtDecode`**: A lightweight library that base64-decodes the JWT payload without verifying the signature (that's the server's job). It lets us read `userId`, `role`, and `exp` from the token.

### LoadingService

```typescript
@Injectable({ providedIn: 'root' })
export class LoadingService {
  private _count = 0;
  private _loading = signal(false);
  readonly isLoading = this._loading.asReadonly();

  show(): void {
    this._count++;
    this._loading.set(true);
  }

  hide(): void {
    this._count = Math.max(0, this._count - 1);
    if (this._count === 0) {
      this._loading.set(false);
    }
  }
}
```

**Why a counter, not a boolean?** If three HTTP requests fire simultaneously, `show()` is called three times. If `hide()` was called after the first response, the spinner would disappear even though two requests are still in flight. The counter tracks how many requests are outstanding; the spinner only hides when all are done (`count === 0`).

### HotelService

```typescript
@Injectable({ providedIn: 'root' })
export class HotelService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}`;

  getTopHotels(): Observable<HotelListItemDto[]> {
    return this.http.get<ApiResponse<HotelListItemDto[]>>(`${this.base}/public/hotels/top`)
      .pipe(map(r => r.data!));
  }

  searchHotelsWithFilters(req: SearchHotelRequestDto): Observable<SearchHotelResponseDto> {
    return this.http.post<ApiResponse<SearchHotelResponseDto>>(
      `${this.base}/public/hotels/search`, req
    ).pipe(map(r => r.data!));
  }

  getAvailability(hotelId: string, checkIn: string, checkOut: string): Observable<RoomAvailabilityDto[]> {
    const params = new HttpParams().set('checkIn', checkIn).set('checkOut', checkOut);
    return this.http.get<ApiResponse<RoomAvailabilityDto[]>>(
      `${this.base}/public/hotels/${hotelId}/availability`, { params }
    ).pipe(map(r => r.data!));
  }
}
```

Services return `Observable<T>` (never `Observable<ApiResponse<T>>`). The `map(r => r.data!)` unwrapping happens inside the service so components receive clean typed data.

### ChatbotService

```typescript
@Injectable({ providedIn: 'root' })
export class ChatbotService {
  private http = inject(HttpClient);

  send(history: ChatMessage[], userMessage: string, systemPrompt: string): Observable<string> {
    const headers = new HttpHeaders({
      'Authorization': `Bearer ${environment.groqApiKey}`,
      'Content-Type': 'application/json'
    });

    // Keep only last 6 messages to stay within Groq's token limit
    const recentHistory = history.slice(-6);

    const messages = [
      { role: 'system', content: systemPrompt },
      ...recentHistory.map(m => ({
        role: m.role === 'model' ? 'assistant' : 'user',
        content: m.text
      })),
      { role: 'user', content: userMessage }
    ];

    return this.http.post<GroqResponse>(this.apiUrl, {
      model: 'llama-3.1-8b-instant',
      messages,
      max_tokens: 512,
      temperature: 0.7
    }, { headers }).pipe(
      map(res => res.choices?.[0]?.message?.content
        ?? 'Sorry, I could not get a response.')
    );
  }
}
```

Note how the loading interceptor intentionally skips Groq API calls (not `localhost`), so chatbot messages don't show the global page spinner.

---

## 10. Components

### NavbarComponent

```typescript
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
  auth = inject(AuthService);              // Exposed to template for role checks
  private userService = inject(UserService);
  mobileOpen = signal(false);             // Mobile hamburger menu state
  isDarkMode = signal(false);             // Dark/light theme toggle

  ngOnInit() {
    const saved = localStorage.getItem('theme');
    if (saved === 'dark') {
      this.isDarkMode.set(true);
      document.body.classList.add('dark-theme');  // DOM manipulation
    }
    // Load profile image on init
    if (this.auth.isAuthenticated() && this.auth.isGuest()) {
      this.userService.getProfile().subscribe({
        next: p => this.auth.updateProfileImage(p.profileImageUrl ?? null),
        error: () => {}
      });
    }
  }

  toggleTheme() {
    const dark = !this.isDarkMode();
    this.isDarkMode.set(dark);
    dark
      ? document.body.classList.add('dark-theme')
      : document.body.classList.remove('dark-theme');
    localStorage.setItem('theme', dark ? 'dark' : 'light');
  }

  toggleMobile() { this.mobileOpen.update(v => !v); }
  closeMobile() { this.mobileOpen.set(false); }
}
```

**`signal.update(fn)`** is like `.set()` but receives the current value and returns the new one. Perfect for toggling: `mobileOpen.update(v => !v)`.

### LoginComponent

```typescript
@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, ...]
})
export class LoginComponent {
  private fb   = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);
  private toast  = inject(ToastService);

  hidePassword = signal(true);   // toggles password visibility
  isLoading    = signal(false);  // disables button during HTTP call

  form = this.fb.group({
    email:    ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();  // show validation errors immediately
      return;
    }
    this.isLoading.set(true);
    this.auth.login(this.form.value as any).subscribe({
      next: () => {
        this.toast.success('Welcome back!');
        const returnUrl = localStorage.getItem('returnUrl') || this.auth.getRedirectUrl();
        localStorage.removeItem('returnUrl');
        this.router.navigateByUrl(returnUrl);  // go to intended destination
      },
      error: () => this.isLoading.set(false),    // interceptor shows toast, we just reset button
      complete: () => this.isLoading.set(false),
    });
  }
}
```

**`FormBuilder.group()`** creates a `FormGroup` with named controls. Each value is `[initialValue, [validators]]`.

**`markAllAsTouched()`** marks every field as touched so error messages appear even if the user clicked Submit without touching any field.

**`navigateByUrl()`** navigates to an exact URL string (vs `navigate(['/path'])` which uses router link array).

### BookingCreateComponent (Complex Example)

This component uses most Angular features at once:

```typescript
@Component({ selector: 'app-booking-create', standalone: true, ... })
export class BookingCreateComponent implements OnInit, OnDestroy {
  @ViewChild('stepper') stepper!: MatStepper;  // DOM reference to stepper

  // All state as signals
  hotel              = signal<HotelDetailsDto | null>(null);
  availability       = signal<RoomAvailabilityDto[]>([]);
  createdReservation = signal<ReservationResponseDto | null>(null);
  isLoadingHotel     = signal(true);
  isBooking          = signal(false);
  isPaying           = signal(false);
  promoValid         = signal<boolean | null>(null);
  promoDiscount      = signal(0);
  useWallet          = signal(false);

  // Computed values derived from other signals
  readonly baseTotal = computed(() => {
    const res = this.createdReservation();
    return res ? res.totalAmount : 0;
  });

  readonly gstAmount = computed(() => {
    const res = this.createdReservation();
    return res ? res.gstAmount : 0;
  });

  readonly finalPayable = computed(() =>
    this.baseTotal() + this.gstAmount() - this.promoDiscount() - this.walletAmountSignal()
  );
```

**`@ViewChild('stepper')`** gets a reference to the `<mat-stepper #stepper>` in the template. It's available after `ngAfterViewInit`.

**Multiple computed signals** chain together: `finalPayable` depends on `baseTotal`, `gstAmount`, `promoDiscount`, and `walletAmountSignal`. Whenever any of these changes, `finalPayable` automatically recalculates.

---

## 11. Angular Signals & Computed

Signals are Angular's built-in reactivity primitive introduced in Angular 16 and used heavily in Angular 17+.

### Types of Signals

```typescript
// 1. Writable signal — can be changed by the owner
const count = signal(0);
count.set(5);           // replace value
count.update(v => v + 1); // update based on current value
console.log(count());   // read by calling it as a function

// 2. Computed signal — derived, auto-updates
const doubled = computed(() => count() * 2);
// doubled() === 10 when count() === 5

// 3. Readonly signal — others can read, but can't write
readonly readonlyCount = count.asReadonly();
```

### Real Usage in This Project

```typescript
// In AuthService
private _currentUser = signal<CurrentUser | null>(null);
readonly currentUser = this._currentUser.asReadonly();           // exposed to outside
readonly isAuthenticated = computed(() => !!this._currentUser());// auto-updates
readonly isGuest = computed(() => this._currentUser()?.role === 'Guest');

// In template (HTML)
// @if (auth.isAuthenticated()) { ... }     ← reads signal
// @if (auth.isGuest()) { ... }             ← reads computed signal
```

### Signals vs Observables

| Aspect | Signal | Observable |
|---|---|---|
| Synchronous | Yes | No (async) |
| Auto-tracks dependencies | Yes | No |
| Needs subscription | No | Yes |
| Good for | UI state | HTTP calls, events |
| Memory leak risk | No | Yes (if not unsubscribed) |

In this project: HTTP calls return `Observable<T>`, but the results are stored into signals. Components use signals for rendering, observables for data fetching.

### Effect (not heavily used here but important)

```typescript
// In a component:
effect(() => {
  const user = this.auth.currentUser();  // tracked dependency
  console.log('User changed:', user);    // runs whenever currentUser changes
});
```

---

## 12. Reactive Forms — Validators

```typescript
// Login form
form = this.fb.group({
  email:    ['', [Validators.required, Validators.email]],
  password: ['', [Validators.required, Validators.minLength(6)]],
});

// Register hotel admin form  
adminForm = this.fb.group({
  name:          ['', [Validators.required, Validators.minLength(2)]],
  email:         ['', [Validators.required, Validators.email]],
  password:      ['', [Validators.required, Validators.minLength(6)]],
  hotelName:     ['', Validators.required],
  address:       ['', Validators.required],
  city:          ['', Validators.required],
  contactNumber: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]],
});
```

### Template Binding

```html
<form [formGroup]="form" (ngSubmit)="submit()">
  <!-- Property binding: binds form control -->
  <mat-form-field>
    <mat-label>Email</mat-label>
    <input matInput formControlName="email" type="email">
    <!-- Error messages (only shown after touched) -->
    @if (form.get('email')?.hasError('required') && form.get('email')?.touched) {
      <mat-error>Email is required</mat-error>
    }
    @if (form.get('email')?.hasError('email') && form.get('email')?.touched) {
      <mat-error>Enter a valid email</mat-error>
    }
  </mat-form-field>

  <!-- Event binding: calls submit() on form submit -->
  <button mat-raised-button type="submit" [disabled]="form.invalid || isLoading()">
    {{ isLoading() ? 'Logging in...' : 'Login' }}  <!-- interpolation -->
  </button>
</form>
```

### Template-Driven vs Reactive

This project uses **Reactive Forms** (`FormBuilder`, `FormGroup`, `FormControl`) for all forms. This approach:
- Keeps validation logic in TypeScript (testable)
- Provides fine-grained control over form state
- Works better with Angular Material form fields

---

## 13. Lifecycle Hooks

Hooks used in this project:

```typescript
// OnInit — runs once after component is created
export class NavbarComponent implements OnInit {
  ngOnInit() {
    // Load theme from localStorage, load profile image
    // GOOD for: API calls, reading localStorage, setup
  }
}

// OnDestroy — runs before component is destroyed
export class ChatbotComponent implements OnInit, OnDestroy {
  private routerSub!: Subscription;

  ngOnInit() {
    this.routerSub = this.router.events.pipe(
      filter(e => e instanceof NavigationStart)
    ).subscribe(() => this.isOpen.set(false));  // close chatbot on navigation
  }

  ngOnDestroy() {
    this.routerSub.unsubscribe();  // CRITICAL: prevent memory leak
  }
}

// AfterViewChecked — runs after every view update
export class ChatbotComponent implements AfterViewChecked {
  private shouldScroll = false;

  ngAfterViewChecked() {
    if (this.shouldScroll) {
      this.scrollToBottom();   // scroll chat after new message rendered
      this.shouldScroll = false;
    }
  }
}

// OnChanges — when @Input() values change (used in shared components)
export class HotelCardComponent implements OnChanges {
  @Input() hotel!: HotelListItemDto;
  ngOnChanges(changes: SimpleChanges) {
    if (changes['hotel']) {
      // recalculate something when hotel input changes
    }
  }
}
```

### Hook Execution Order

```
constructor()          → inject() calls, signal creation
ngOnInit()             → first HTTP calls, localStorage reads
ngAfterContentInit()   → ng-content is projected
ngAfterViewInit()      → @ViewChild is available
ngAfterViewChecked()   → after every render cycle
ngOnDestroy()          → cleanup subscriptions, timers
```

---

## 14. Template Syntax — Interpolation, Binding, Directives

### Interpolation

```html
<!-- Double curly braces output a value as text -->
<h1>Welcome, {{ auth.currentUser()?.userName }}</h1>
<p>Total: ₹{{ createdReservation()?.finalAmount | number:'1.0-0' }}</p>
<span class="badge">{{ hotel()?.averageRating | number:'1.1-1' }} ⭐</span>
```

The `| number:'1.0-0'` is a **pipe** — a function that transforms the displayed value. `'1.0-0'` means minimum 1 integer digit, 0–0 decimal places.

### Property Binding `[property]`

```html
<!-- Binds TypeScript expression to DOM property -->
<img [src]="hotel()?.imageUrl" [alt]="hotel()?.name">
<button [disabled]="isLoading() || form.invalid">Submit</button>
<input [value]="searchQuery()" [placeholder]="'Search ' + city">
<div [class.active]="selectedRoomType() === rt.roomTypeId">...</div>
<div [style.color]="isDark() ? 'white' : 'black'">text</div>
```

### Event Binding `(event)`

```html
<!-- Calls TypeScript method on DOM event -->
<button (click)="submit()">Login</button>
<input (keyup.enter)="search()">      <!-- specific key events -->
<form (ngSubmit)="onSubmit()">
<mat-select (selectionChange)="onRoomTypeChange($event)">
```

### Two-Way Binding `[(ngModel)]`

```html
<!-- Template-driven: reads AND writes -->
<input [(ngModel)]="searchText">
<!-- Equivalent to: [ngModel]="searchText" (ngModelChange)="searchText = $event" -->

<!-- In reactive forms, use formControlName instead -->
<input formControlName="email">
```

### Control Flow (`@if`, `@for`, `@switch`)

Angular 17+ uses the new `@` control flow syntax:

```html
<!-- Conditional rendering -->
@if (auth.isAuthenticated()) {
  <button (click)="auth.logout()">Logout</button>
} @else {
  <a routerLink="/auth/login">Login</a>
}

<!-- List rendering -->
@for (hotel of hotels(); track hotel.hotelId) {
  <app-hotel-card [hotel]="hotel" />
} @empty {
  <p>No hotels found.</p>
}

<!-- Switch for multiple conditions -->
@switch (reservation().status) {
  @case ('Confirmed') { <span class="confirmed">Confirmed ✓</span> }
  @case ('Pending')   { <span class="pending">Pending...</span> }
  @case ('Cancelled') { <span class="cancelled">Cancelled</span> }
  @default            { <span>{{ reservation().status }}</span> }
}
```

### `*ngFor` / `*ngIf` (Old Syntax — Still Valid)

```html
<!-- Old syntax (still works, not used as heavily in this project) -->
<div *ngIf="isLoading()"><mat-spinner /></div>
<mat-option *ngFor="let city of cities(); trackBy: trackByCity" [value]="city">
  {{ city }}
</mat-option>
```

### RouterLink & RouterLinkActive

```html
<!-- Navigate to a route (no page reload) -->
<a routerLink="/hotels">Hotels</a>
<a [routerLink]="['/hotels', hotel.hotelId]">View Hotel</a>

<!-- Add CSS class when route is active -->
<a routerLink="/guest/dashboard" routerLinkActive="active-link">Dashboard</a>

<!-- Exact matching (root route) -->
<a routerLink="/" routerLinkActive="active-link" [routerLinkActiveOptions]="{ exact: true }">
  Home
</a>
```

---

## 15. Inter-Component Communication

### Parent → Child: `@Input()`

```typescript
// hotel-card.component.ts (child)
export class HotelCardComponent {
  @Input() hotel!: HotelListItemDto;
  @Input() showRating = true;         // with default
}

// hotel-list.component.html (parent)
@for (h of hotels(); track h.hotelId) {
  <app-hotel-card [hotel]="h" [showRating]="true" />
}
```

### Child → Parent: `@Output()` & `EventEmitter`

```typescript
// confirm-dialog.component.ts (child)
export class ConfirmDialogComponent {
  @Output() confirmed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  confirm() { this.confirmed.emit(); }
  cancel()  { this.cancelled.emit(); }
}

// parent template
<app-confirm-dialog
  (confirmed)="onDeleteConfirmed()"
  (cancelled)="showDialog = false"
/>
```

### Sibling Communication via Service (Shared State)

```typescript
// AuthService holds shared user state
// Navbar reads it: auth.currentUser()
// LoginComponent writes it: auth.login() → sets signal
// Guards check it: auth.isAuthenticated()
// All components use the same singleton service instance
```

### Angular Material Dialog (Service-Based Communication)

```typescript
// In a component needing a dialog:
private dialog = inject(MatDialog);

openConfirm() {
  const ref = this.dialog.open(ConfirmDialogComponent, {
    data: { message: 'Are you sure?' }
  });
  ref.afterClosed().subscribe(result => {
    if (result === true) this.deleteRoom();
  });
}

// In ConfirmDialogComponent:
private dialogRef = inject(MatDialogRef<ConfirmDialogComponent>);
data = inject(MAT_DIALOG_DATA);  // receives { message: 'Are you sure?' }

confirm() { this.dialogRef.close(true); }
cancel()  { this.dialogRef.close(false); }
```

---

## 16. Observables & RxJS

Observables represent a stream of values over time. In this project they're used for:
- HTTP calls (one value then complete)
- Router events (stream of navigation events)
- Form value changes (stream of user input)

### Key RxJS Operators Used

```typescript
import { map, tap, catchError, finalize, filter, distinctUntilChanged, switchMap } from 'rxjs';

// map — transform the emitted value
this.http.get<ApiResponse<T>>(url).pipe(map(r => r.data!));

// tap — side effect (log, update signal) without changing the value
.pipe(tap(res => this.setToken(res.token)));

// catchError — handle errors
.pipe(catchError(err => { toast.error(err.message); return throwError(() => err); }));

// finalize — always runs (like finally)
.pipe(finalize(() => loadingService.hide()));

// filter — only pass values that match condition
router.events.pipe(filter(e => e instanceof NavigationStart));

// distinctUntilChanged — only emit if value actually changed
form.valueChanges.pipe(distinctUntilChanged());

// switchMap — cancel previous inner observable when outer emits
// Used in search: if user types quickly, cancel previous search requests
searchControl.valueChanges.pipe(
  distinctUntilChanged(),
  switchMap(query => this.hotelService.searchHotelsWithFilters({ city: query, ... }))
);
```

### Observable vs Promise

| | Observable | Promise |
|---|---|---|
| Values | 0 to many | 1 |
| Lazy | Yes (doesn't run until subscribed) | No (runs immediately) |
| Cancellable | Yes | No |
| Operators | Rich (map, filter, etc.) | `.then()` only |

Angular HTTP always returns `Observable`. This project subscribes in components with:
```typescript
this.service.getData().subscribe({
  next: data => this.data.set(data),
  error: () => {},       // interceptor already showed toast
  complete: () => this.loading.set(false)
});
```

---

## 17. Angular Material

Angular Material is Google's Material Design component library for Angular.

### Modules Used

```typescript
// In standalone components, import each module individually:
imports: [
  MatToolbarModule,       // <mat-toolbar>
  MatButtonModule,        // mat-button, mat-raised-button, mat-icon-button
  MatIconModule,          // <mat-icon>material_icon_name</mat-icon>
  MatInputModule,         // matInput directive on <input>
  MatFormFieldModule,     // <mat-form-field> wrapper
  MatSelectModule,        // <mat-select>, <mat-option>
  MatMenuModule,          // <mat-menu>, <button [matMenuTriggerFor]>
  MatDialogModule,        // MatDialog service, <mat-dialog-content>
  MatStepperModule,       // <mat-stepper>, <mat-step>
  MatTableModule,         // <mat-table>, mat-sort, mat-paginator
  MatPaginatorModule,     // <mat-paginator>
  MatSortModule,          // matSort, mat-sort-header
  MatCardModule,          // <mat-card>, <mat-card-content>
  MatChipsModule,         // <mat-chip>, <mat-chip-set>
  MatBadgeModule,         // matBadge="5" directive
  MatTooltipModule,       // matTooltip="Tooltip text"
  MatDatepickerModule,    // <mat-datepicker>
  MatSlideToggleModule,   // <mat-slide-toggle>
  MatProgressSpinnerModule, // <mat-spinner>
  MatRadioModule,         // <mat-radio-group>, <mat-radio-button>
  MatTabsModule,          // <mat-tab-group>, <mat-tab>
  MatDividerModule,       // <mat-divider>
  MatSnackBarModule,      // MatSnackBar service
]
```

### Key Usage Examples

```html
<!-- Material Table with sort and paginator -->
<table mat-table [dataSource]="dataSource" matSort>
  <ng-container matColumnDef="hotelName">
    <th mat-header-cell *matHeaderCellDef mat-sort-header>Hotel</th>
    <td mat-cell *matCellDef="let row">{{ row.hotelName }}</td>
  </ng-container>
  <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
  <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
</table>
<mat-paginator [length]="totalCount" [pageSize]="10" (page)="onPageChange($event)">
</mat-paginator>

<!-- Material Stepper (used in Booking Create) -->
<mat-stepper linear #stepper>
  <mat-step [stepControl]="step1Form" label="Select Room">...</mat-step>
  <mat-step [stepControl]="step2Form" label="Review Booking">...</mat-step>
  <mat-step label="Payment">...</mat-step>
</mat-stepper>

<!-- Material Chips for amenity tags -->
<mat-chip-set>
  @for (amenity of hotel.amenities; track amenity) {
    <mat-chip>{{ amenity }}</mat-chip>
  }
</mat-chip-set>
```

---

## 18. Bootstrap

Bootstrap 5 is loaded via CDN in `index.html`:

```html
<!-- index.html -->
<link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
```

Used primarily for:
- Grid system: `container`, `row`, `col-md-4`, `col-lg-3`
- Spacing utilities: `mt-3`, `p-4`, `mb-2`
- Flex utilities: `d-flex`, `justify-content-between`, `align-items-center`
- Display utilities: `d-none d-md-block`
- Badge: `badge bg-success`
- Card layout structure (Angular Material cards use Bootstrap spacing)

Bootstrap and Angular Material co-exist because Bootstrap's CSS operates on different selectors than Material's component styles.

---

## 19. AI Chatbot Integration

The chatbot is a floating widget that uses the Groq API (free LLM inference) with the `llama-3.1-8b-instant` model.

### System Prompts by Role

```typescript
// chatbot-prompts.ts
export const GUEST_CONTEXT = `
You are a helpful assistant for StayHub hotel booking app.
The user is a Guest. Help them with: searching hotels, making bookings,
payment (UPI/Credit Card/Wallet), cancellations, refund status,
writing reviews. Keep responses short and friendly.
`;

export const ADMIN_CONTEXT = `
You are an assistant for hotel admins on StayHub.
Help with: managing rooms/room types, setting rates and inventory,
viewing reservations, processing refunds, understanding audit logs.
`;
```

### ChatbotComponent Key Features

```typescript
export class ChatbotComponent implements OnInit, AfterViewChecked, OnDestroy {
  isOpen   = signal(false);
  messages = signal<ChatMessageWithMeta[]>([]);
  loading  = signal(false);

  readonly userName = computed(() => this.auth.currentUser()?.userName ?? null);
  readonly role     = computed(() => this.auth.currentUser()?.role ?? null);

  // Typewriter animation effect on bot responses
  private typewriterEffect(index: number, fullText: string): void {
    let i = 0;
    const interval = setInterval(() => {
      this.messages.update(msgs => {
        const updated = [...msgs];
        updated[index] = { ...updated[index], displayText: fullText.slice(0, i + 1) };
        return updated;
      });
      i++;
      if (i >= fullText.length) {
        clearInterval(interval);
        this.messages.update(msgs => {
          const updated = [...msgs];
          updated[index] = { ...updated[index], typing: false };
          return updated;
        });
      }
    }, 18);  // 18ms per character
  }

  send(): void {
    const text = this.userInput().trim();
    if (!text || this.loading()) return;
    this.lastUserText = text;
    this.userInput.set('');
    // Add user message
    this.addMessage('user', text);
    this.loading.set(true);
    // Call Groq API
    this.chatbot.send(
      this.messages().filter(m => m.role !== 'model' || !m.isError),
      text,
      this.systemPrompt
    ).subscribe({
      next: reply => {
        const idx = this.messages().length;
        this.addMessage('model', reply);
        this.typewriterEffect(idx, reply);  // animate reply
        this.loading.set(false);
      },
      error: () => {
        this.addMessage('model', 'Sorry, I encountered an error. Please try again.');
        this.loading.set(false);
      }
    });
  }
}
```

### Chatbot Features

1. **Role-aware system prompt** — Guest, Admin, SuperAdmin, or Public each get different context
2. **Typewriter animation** — replies appear character by character
3. **History limiting** — last 6 messages only (token limit management)
4. **Retry on error** — re-send the last message if AI failed
5. **Follow-up suggestions** — quick-reply chips for common questions
6. **Copy to clipboard** — copy any bot message
7. **Auto-close on navigation** — subscribes to `NavigationStart` event

---

## 20. Lazy Loading & Feature Modules

Each feature has its own routes file:

```typescript
// features/admin/admin.routes.ts
export const ADMIN_ROUTES: Routes = [
  { path: 'dashboard',    loadComponent: () => import('./dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent) },
  { path: 'rooms',        loadComponent: () => import('./room-management/room-management.component').then(m => m.RoomManagementComponent) },
  { path: 'roomtypes',    loadComponent: () => import('./room-management/roomtype-management.component').then(m => m.RoomtypeManagementComponent) },
  { path: 'inventory',    loadComponent: () => import('./inventory-management/inventory-management.component').then(m => m.InventoryManagementComponent) },
  { path: 'reservations', loadComponent: () => import('./reservation-management/reservation-management.component').then(m => m.ReservationManagementComponent) },
  { path: 'transactions', loadComponent: () => import('./transactions/admin-transactions.component').then(m => m.AdminTransactionsComponent) },
  { path: 'reviews',      loadComponent: () => import('./reviews/admin-reviews.component').then(m => m.AdminReviewsComponent) },
  { path: 'audit-logs',   loadComponent: () => import('./audit-logs/audit-logs.component').then(m => m.AuditLogsComponent) },
  { path: '',             redirectTo: 'dashboard', pathMatch: 'full' },
];
```

This means visiting `/admin/rooms` for the first time downloads a small JavaScript chunk containing only the `RoomManagementComponent` and its dependencies. The admin dashboard chunk and the public hotel list chunk are completely separate files.

---

## 21. Dark Theme (DOM Manipulation)

```typescript
// In NavbarComponent
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
```

```scss
// styles.scss
:root {
  --bg-primary: #ffffff;
  --text-primary: #1a1a1a;
  --card-bg: #f5f5f5;
}

.dark-theme {
  --bg-primary: #1a1a2e;
  --text-primary: #e0e0e0;
  --card-bg: #16213e;
}

body { background-color: var(--bg-primary); color: var(--text-primary); }
.card { background: var(--card-bg); }
```

CSS Custom Properties (variables) update instantly when `.dark-theme` is added/removed from `<body>`. All components that use `var(--bg-primary)` automatically reflect the change.

---

## 22. Testing — Karma & Jasmine Complete Guide

### What is Karma?

**Karma** is the test runner. It launches a browser (Chrome by default), loads your compiled Angular app, and runs all `*.spec.ts` files in it. Results appear in the terminal.

### What is Jasmine?

**Jasmine** is the testing framework. It provides:
- `describe('Suite name', () => { })` — groups related tests
- `it('test name', () => { })` — individual test case
- `beforeEach(() => { })` — setup before each test
- `afterEach(() => { })` — cleanup after each test
- `expect(value).toBe(expected)` — assertion
- `jasmine.createSpyObj(...)` — creates mock objects

### TestBed

`TestBed` is Angular's testing utility. It creates a minimal Angular app context:

```typescript
await TestBed.configureTestingModule({
  imports: [AppComponent],           // the component under test
  providers: [
    provideHttpClient(),             // real HttpClient
    provideHttpClientTesting(),      // BUT intercept requests with HttpTestingController
    provideRouter([...]),            // router
    { provide: AuthService, useValue: authSpy },  // mock service
  ]
}).compileComponents();
```

### HttpTestingController

This is the key tool for testing services. It intercepts HTTP calls and lets you control responses:

```typescript
it('should call the correct endpoint', () => {
  service.getTopHotels().subscribe(result => {
    expect(result.length).toBe(2);
  });

  // Verify the request was made
  const req = http.expectOne(`${BASE}/public/hotels/top`);
  expect(req.request.method).toBe('GET');

  // Provide a mock response (this triggers the subscribe callback above)
  req.flush({ success: true, data: [hotel1, hotel2] });
});
```

After every test: `http.verify()` fails the test if any unexpected HTTP call was made (or an expected one was not made). This catches silent bugs where the service makes extra calls.

### Spy Objects

```typescript
authSpy = jasmine.createSpyObj('AuthService', ['isAuthenticated', 'logout', 'token'], {
  currentUser: () => null      // getter
});
authSpy.isAuthenticated.and.returnValue(false);   // stub return value
authSpy.token.and.returnValue('my-jwt');
```

`createSpyObj` creates an object with methods that are all spies. You can then:
- `.and.returnValue(x)` — always return x
- `.and.callFake(fn)` — call a function
- `expect(spy).toHaveBeenCalled()` — verify it was called
- `expect(spy).toHaveBeenCalledWith(args)` — verify arguments

---

## 23. Test Files — Full Explanation

### auth.service.spec.ts

This is the most important test file. It tests the `AuthService` which controls authentication for the entire app.

#### Mock JWT Tokens

```typescript
// A real JWT has 3 base64 parts separated by dots
// Header: {"alg":"HS256","typ":"JWT"}
// Payload: {"nameid":"usr-001","unique_name":"Thanush","role":"Guest","exp":9999999999}
const MOCK_GUEST_TOKEN =
  'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.' +
  'eyJuYW1laWQiOiJ1c3ItMDAxIiwidW5pcXVlX25hbWUiOiJUaGFudXNoIiwicm9sZSI6Ikd1ZXN0IiwiZXhwIjo5OTk5OTk5OTk5fQ.' +
  'signature';  // signature doesn't matter for jwtDecode (client-side only)

const MOCK_EXPIRED_TOKEN =
  'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.' +
  'eyJuYW1laWQiOiJ1c3ItMDA0IiwidW5pcXVlX25hbWUiOiJPbGRVc2VyIiwicm9sZSI6Ikd1ZXN0IiwiZXhwIjoxfQ.' +
  'signature';  // exp: 1 = Jan 1, 1970 = always expired
```

#### Test: Restore Session from localStorage

```typescript
it('should restore a valid Guest session from localStorage on startup', () => {
  // PUT token in storage BEFORE creating the service
  localStorage.setItem('hotel_token', MOCK_GUEST_TOKEN);

  // Re-create TestBed so constructor's loadFromStorage() sees the token
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({ providers: [...] });
  const freshService = TestBed.inject(AuthService);

  expect(freshService.isAuthenticated()).toBeTrue();
  expect(freshService.currentUser()?.userName).toBe('Thanush');
  expect(freshService.currentUser()?.role).toBe('Guest');
});
```

**Why `resetTestingModule()`?** The service's constructor runs only once when the service is first injected. To test what happens when the service starts with a token already in storage, we must tear down and re-create the testing module so the constructor runs again.

#### Test: Login Updates All Signals

```typescript
it('login() — should set token in localStorage and update signals on success', () => {
  const dto: LoginDto = { email: 'thanush@test.com', password: 'pass123' };

  service.login(dto).subscribe(res => {
    expect(res.token).toBe(MOCK_GUEST_TOKEN);
  });

  // The service made an HTTP call — intercept it and provide response
  http.expectOne(`${environment.apiUrl}/auth/login`)
      .flush({ success: true, data: { token: MOCK_GUEST_TOKEN } });

  // After response arrives, verify all signals updated
  expect(localStorage.getItem('hotel_token')).toBe(MOCK_GUEST_TOKEN);
  expect(service.isAuthenticated()).toBeTrue();
  expect(service.currentUser()?.userName).toBe('Thanush');
  expect(service.isGuest()).toBeTrue();
  expect(service.isAdmin()).toBeFalse();
});
```

#### Test: Expired Token Cleared

```typescript
it('should NOT restore an expired token from localStorage', () => {
  localStorage.setItem('hotel_token', MOCK_EXPIRED_TOKEN);
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({ providers: [...] });
  const freshService = TestBed.inject(AuthService);

  expect(freshService.isAuthenticated()).toBeFalse();
  expect(localStorage.getItem('hotel_token')).toBeNull();  // must be removed
});
```

### auth.guard.spec.ts

Tests all 5 guards. Uses `TestBed.runInInjectionContext()` to call guard functions (which use `inject()` internally):

```typescript
describe('guestGuard', () => {
  it('should return true when authenticated as Guest', () => {
    authSpy.isAuthenticated.and.returnValue(true);
    authSpy.isGuest.and.returnValue(true);

    const result = TestBed.runInInjectionContext(() =>
      guestGuard(mockRoute, mockSnapshot('/guest'))
    );
    expect(result).toBeTrue();
  });

  it('should redirect to getRedirectUrl when authenticated but not Guest (Admin visiting guest route)', () => {
    authSpy.isAuthenticated.and.returnValue(true);
    authSpy.isGuest.and.returnValue(false);  // not a guest (is admin)

    const result = TestBed.runInInjectionContext(() =>
      guestGuard(mockRoute, mockSnapshot('/guest'))
    );
    expect(result).toBeFalse();
    expect(router.navigate).toHaveBeenCalledWith(['/dashboard']); // goes to admin dashboard
  });
});
```

**`TestBed.runInInjectionContext()`** creates an injection context so `inject()` calls inside the guard work during testing.

### auth.interceptor.spec.ts

Tests all error scenarios:

```typescript
it('should attach Authorization header when token is present', () => {
  authSpy.token.and.returnValue('my-jwt-token');
  http.get(LOCAL_URL).subscribe();
  const req = httpMock.expectOne(LOCAL_URL);
  expect(req.request.headers.get('Authorization')).toBe('Bearer my-jwt-token');
  req.flush({});  // complete the request
});

it('should call authService.logout on 401', () => {
  http.get(LOCAL_URL).subscribe({ error: () => {} });
  const req = httpMock.expectOne(LOCAL_URL);
  req.flush(null, { status: 401, statusText: 'Unauthorized' });
  expect(authSpy.logout).toHaveBeenCalled();
});

it('should NOT call toast.error on 401 (logout handles navigation)', () => {
  http.get(LOCAL_URL).subscribe({ error: () => {} });
  httpMock.expectOne(LOCAL_URL).flush(null, { status: 401, statusText: 'Unauthorized' });
  expect(toastSpy.error).not.toHaveBeenCalled();  // interceptor returns early for 401
});

it('should skip interceptor for external URLs', () => {
  authSpy.token.and.returnValue('my-jwt-token');
  http.get(EXTERNAL_URL).subscribe();
  const req = httpMock.expectOne(EXTERNAL_URL);
  expect(req.request.headers.has('Authorization')).toBeFalse();  // no auth header added
  req.flush({});
});
```

### loading.interceptor.spec.ts

Tests the counter-based loading logic:

```typescript
it('should call show() before request to localhost', () => {
  http.get(LOCAL_URL).subscribe();
  httpMock.expectOne(LOCAL_URL).flush({});
  expect(loadingSpy.show).toHaveBeenCalledTimes(1);
});

it('should call hide() even when request errors', () => {
  http.get(LOCAL_URL).subscribe({ error: () => {} });
  httpMock.expectOne(LOCAL_URL).flush(null, { status: 500, statusText: 'Error' });
  expect(loadingSpy.hide).toHaveBeenCalledTimes(1);  // finalize() always runs
});

it('should NOT call show() for external URLs', () => {
  http.get(EXTERNAL_URL).subscribe();
  httpMock.expectOne(EXTERNAL_URL).flush({});
  expect(loadingSpy.show).not.toHaveBeenCalled();  // Groq API bypassed
});
```

### loading.service.spec.ts

Tests the counter logic in detail:

```typescript
it('counter logic — 2 shows then 1 hide should keep isLoading true', () => {
  service.show(); // count = 1
  service.show(); // count = 2
  service.hide(); // count = 1 → still loading
  expect(service.isLoading()).toBeTrue();
});

it('counter logic — 3 shows then 3 hides should set isLoading to false', () => {
  service.show(); service.show(); service.show();
  service.hide(); service.hide();
  expect(service.isLoading()).toBeTrue();  // still 1 pending
  service.hide();
  expect(service.isLoading()).toBeFalse(); // all resolved
});

it('hide() extra calls should not go below zero', () => {
  service.show();
  service.hide();
  service.hide(); // extra call
  service.hide(); // extra call
  expect(service.isLoading()).toBeFalse();  // Math.max(0, ...) prevents negative
});

it('isLoading — should be a readonly signal (no set method exposed)', () => {
  expect(typeof service.isLoading).toBe('function');  // signal is callable
  expect((service.isLoading as any).set).toBeUndefined();      // no .set()
  expect((service.isLoading as any).update).toBeUndefined();   // no .update()
});
```

The last test verifies the `asReadonly()` contract: external code cannot mutate the signal.

### hotel.service.spec.ts

Tests all hotel API calls with different scenarios:

```typescript
it('searchHotelsWithFilters() — should pass pageNumber and pageSize in body', () => {
  const searchReq: SearchHotelRequestDto = {
    city: 'Mumbai', checkIn: '2025-07-10', checkOut: '2025-07-12',
    pageNumber: 2, pageSize: 5
  };
  service.searchHotelsWithFilters(searchReq).subscribe();
  const req = http.expectOne(`${BASE}/public/hotels/search`);
  expect(req.request.body.pageNumber).toBe(2);
  expect(req.request.body.pageSize).toBe(5);
  req.flush({ success: true, data: { hotels: [], pageNumber: 2, recordsCount: 0 } });
});

it('getAvailability() — should GET with checkIn and checkOut as query params', () => {
  service.getAvailability('hotel-001', '2025-06-01', '2025-06-03').subscribe();
  const req = http.expectOne(r => r.url === `${BASE}/public/hotels/hotel-001/availability`);
  expect(req.request.params.get('checkIn')).toBe('2025-06-01');
  expect(req.request.params.get('checkOut')).toBe('2025-06-03');
  req.flush({ success: true, data: [MOCK_AVAILABILITY] });
});
```

Note the difference: `http.expectOne(url)` for exact URL matching, `http.expectOne(r => r.url === url)` when the URL has query params (Angular appends them automatically).

### booking.service.spec.ts

Tests all reservation operations:

```typescript
it('createReservation() — should include selectedRoomIds when provided', () => {
  const dto: CreateReservationDto = {
    hotelId: 'hotel-001', roomTypeId: 'rt-001',
    checkInDate: '2025-06-01', checkOutDate: '2025-06-03',
    numberOfRooms: 2,
    selectedRoomIds: ['r-001', 'r-002']  // guest selected specific rooms
  };
  service.createReservation(dto).subscribe();
  const req = http.expectOne(`${BASE}/guest/reservations`);
  expect(req.request.body.selectedRoomIds).toEqual(['r-001', 'r-002']);
  req.flush({ success: true, data: MOCK_RESERVATION_RESPONSE });
});

it('cancelReservation() — should PATCH with reason in body', () => {
  service.cancelReservation('RES-ABCD1234', { reason: 'Change of plans' }).subscribe();
  const req = http.expectOne(`${BASE}/guest/reservations/RES-ABCD1234/cancel`);
  expect(req.request.method).toBe('PATCH');
  expect(req.request.body.reason).toBe('Change of plans');
  req.flush({ success: true, message: 'Cancelled.' });
});
```

### api.services.spec.ts (Shared Services)

Tests all services in one file using a shared helper:

```typescript
// Reusable setup function — avoids repeating TestBed.configureTestingModule()
function setupTestBed() {
  TestBed.configureTestingModule({
    providers: [provideHttpClient(), provideHttpClientTesting()]
  });
  return { http: TestBed.inject(HttpTestingController) };
}

describe('TransactionService', () => {
  let service: TransactionService;
  let http: HttpTestingController;

  beforeEach(() => {
    ({ http } = setupTestBed());
    service = TestBed.inject(TransactionService);
  });

  afterEach(() => http.verify());

  it('createPayment() — should POST to /transactions and return transaction', () => {
    const dto: CreatePaymentDto = { reservationId: 'res-001', paymentMethod: 1 };
    service.createPayment(dto).subscribe(result => {
      expect(result.transactionId).toBe('tx-001');
      expect(result.status).toBe(2);  // Success
    });
    const req = http.expectOne(`${BASE}/transactions`);
    expect(req.request.method).toBe('POST');
    req.flush({ success: true, data: { transactionId: 'tx-001', status: 2, ... } });
  });
});
```

### chatbot.service.spec.ts

Tests the AI integration:

```typescript
it('send — should limit history to last 6 messages', () => {
  // Create 10 message history
  const history: ChatMessage[] = Array.from({ length: 10 }, (_, i) => ({
    role: i % 2 === 0 ? 'user' : 'model' as 'user' | 'model',
    text: `msg ${i}`
  }));
  service.send(history, 'New message', 'System').subscribe();
  const req = http.expectOne(GROQ_URL);
  // system(1) + max 6 history + user message(1) = max 8
  expect(req.request.body.messages.length).toBeLessThanOrEqual(8);
  req.flush({ choices: [{ message: { content: 'Reply' } }] });
});

it('send — should return fallback when choices is empty', () => {
  let result = '';
  service.send([], 'Hello', 'System').subscribe(r => result = r);
  http.expectOne(GROQ_URL).flush({ choices: [] });
  expect(result).toContain('Sorry');  // fallback message
});
```

### app.component.spec.ts

Tests root component's `showChrome` computed signal and template rendering:

```typescript
it('showChrome — should return false for /auth/login', async () => {
  await router.navigate(['/auth/login']);
  fixture.detectChanges();
  expect(component.showChrome()).toBeFalse();  // navbar/footer hidden on login page
});

it('should NOT render app-navbar on /auth/* routes', async () => {
  await router.navigate(['/auth/login']);
  fixture.detectChanges();
  await fixture.whenStable();
  const el = fixture.nativeElement as HTMLElement;
  expect(el.querySelector('app-navbar')).toBeFalsy();
});

it('main — should have auth-main class on /auth routes', async () => {
  await router.navigate(['/auth/login']);
  fixture.detectChanges();
  await fixture.whenStable();
  const main = fixture.nativeElement.querySelector('main') as HTMLElement;
  expect(main.classList.contains('auth-main')).toBeTrue();
});
```

**`fixture.whenStable()`** waits for all pending async operations (router navigation, promises) to complete before checking the DOM.

**`fixture.detectChanges()`** triggers Angular's change detection so the template updates after programmatic changes.

---

## 24. How to Run Tests

### Run All Tests

```bash
ng test
```

Opens a Chrome browser, runs all `*.spec.ts` files, and shows results in terminal. Karma watches for file changes and re-runs affected tests.

### Run Once (CI/CD)

```bash
ng test --watch=false --browsers=ChromeHeadless
```

`--watch=false` exits after tests complete (no watch mode). `ChromeHeadless` runs without UI (required for CI pipelines like GitHub Actions).

### Run Specific Test File

```bash
ng test --include="**/auth.service.spec.ts"
```

### Run with Coverage

```bash
ng test --code-coverage
```

Generates a coverage report in `coverage/` folder. Open `coverage/index.html` in a browser to see which lines are covered.

### Key Testing Imports

```typescript
// Always needed
import { TestBed } from '@angular/core/testing';
import { ComponentFixture } from '@angular/core/testing';

// HTTP mocking
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

// Router
import { provideRouter, Router } from '@angular/router';

// Angular fakeAsync/tick for time-based tests
import { fakeAsync, tick } from '@angular/core/testing';

// Example of fakeAsync usage:
it('logout() should navigate after delay', fakeAsync(() => {
  const navSpy = spyOn(router, 'navigate');
  service.logout();
  tick();  // advance fake time
  expect(navSpy).toHaveBeenCalledWith(['/auth/login']);
}));
```

### Test Structure Best Practices (Used in This Project)

```typescript
describe('ServiceName', () => {
  let service: ServiceName;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: SomeDependency, useValue: mockDependency }
      ]
    });
    service = TestBed.inject(ServiceName);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();                    // catches uncompleted requests
    localStorage.clear();             // clean state between tests
  });

  it('methodName() — readable description of what is tested', () => {
    // Arrange: set up data
    const dto = { ... };

    // Act: call the method
    service.methodName(dto).subscribe(result => {
      // Assert (inside subscribe, runs after HTTP response)
      expect(result.someField).toBe(expectedValue);
    });

    // Control HTTP: verify request was made, provide response
    const req = http.expectOne(`expected/url`);
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);  // this triggers the subscribe callback
  });
});
```

---

## Quick Reference: Angular Concepts in This Project

| Concept | Where Used | File |
|---|---|---|
| `signal()` | Auth state, UI state, loading flags | auth.service.ts, all components |
| `computed()` | Derived role checks, totals | auth.service.ts, booking-create |
| `@Injectable({ providedIn: 'root' })` | All services | Every `*.service.ts` |
| `inject()` | DI in standalone components | Every component/service |
| `@Component({ standalone: true })` | All components | Every `*.component.ts` |
| `@Input()` | Hotel card, shared dialogs | hotel-card, confirm-dialog |
| `@Output()` | Dialog confirmation | confirm-dialog, input-dialog |
| `@ViewChild()` | Stepper control, scroll | booking-create, chatbot |
| `OnInit` | API calls, localStorage | navbar, all feature components |
| `OnDestroy` | Unsubscribe | chatbot, components with subscriptions |
| `AfterViewChecked` | Auto-scroll | chatbot |
| `CanActivateFn` | Route protection | auth.guard.ts |
| `HttpInterceptorFn` | JWT, loading | auth.interceptor, loading.interceptor |
| `FormBuilder` | All forms | login, register, booking-create |
| `ReactiveFormsModule` | All forms | login, register, all admin forms |
| `RouterLink` | Navigation links | navbar, cards |
| `RouterLinkActive` | Active nav highlighting | navbar |
| `AsyncPipe` (minimal) | Rare; mostly signals | -  |
| `pipe(map(...))` | Unwrap ApiResponse | All services |
| `pipe(tap(...))` | Side effects | auth.service |
| `pipe(catchError(...))` | Error handling | auth.interceptor |
| `pipe(finalize(...))` | Always-cleanup | loading.interceptor |
| `pipe(filter(...))` | Router event filtering | chatbot |
| `pipe(distinctUntilChanged(...))` | Form search debounce | city-autocomplete |
| `MatDialog` | Confirm/input dialogs | admin components |
| `MatTable + MatPaginator` | Data tables | reservation-management, audit-logs |
| `MatStepper` | Multi-step booking | booking-create |
| `loadChildren` | Feature lazy loading | app.routes.ts |
| `loadComponent` | Component lazy loading | all feature routes |

---

*This documentation covers the complete Angular 17+ frontend of the Hotel Booking System. Every concept is illustrated with actual code from the project files, not generic examples. Reading this document alongside the actual source code will give you a complete understanding of modern Angular development patterns.*
