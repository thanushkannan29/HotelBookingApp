# Thanush StayHub — Frontend Documentation

> Angular 18 Hotel Booking Platform — Complete Frontend Guide

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Tech Stack & Dependencies](#2-tech-stack--dependencies)
3. [Project Folder Structure](#3-project-folder-structure)
4. [Angular Core Concepts Used](#4-angular-core-concepts-used)
5. [App Bootstrap & Configuration](#5-app-bootstrap--configuration)
6. [Routing & Lazy Loading](#6-routing--lazy-loading)
7. [Route Guards](#7-route-guards)
8. [HTTP Interceptors](#8-http-interceptors)
9. [Services](#9-services)
10. [Models (TypeScript Interfaces)](#10-models-typescript-interfaces)
11. [Shared Components](#11-shared-components)
12. [Feature Modules](#12-feature-modules)
13. [Angular Material](#13-angular-material)
14. [Bootstrap CSS](#14-bootstrap-css)
15. [Global Styles & Theming](#15-global-styles--theming)
16. [Dark Mode](#16-dark-mode)
17. [Chatbot (AI Assistant)](#17-chatbot-ai-assistant)
18. [Payment Integration (Razorpay)](#18-payment-integration-razorpay)
19. [PDF Generation (jsPDF)](#19-pdf-generation-jspdf)
20. [Frontend Testing — Karma & Jasmine](#20-frontend-testing--karma--jasmine)

---

## 1. Project Overview

Thanush StayHub is a full-stack hotel booking platform. The frontend is built with **Angular 18** (standalone components). It has three user roles:

| Role | What they can do |
|------|-----------------|
| **Guest** | Search hotels, book rooms, pay, manage wallet, write reviews |
| **Hotel Admin** | Manage hotel, rooms, inventory, reservations, reviews |
| **SuperAdmin** | Oversee all hotels, manage amenities, view revenue, handle support |

The app talks to a .NET Web API backend at `https://localhost:7208/api`.

---

## 2. Tech Stack & Dependencies

### Main Dependencies (`package.json`)

```json
{
  "@angular/core": "^18.2.0",
  "@angular/material": "^18.2.0",
  "@angular/cdk": "^18.2.0",
  "@angular/router": "^18.2.0",
  "@angular/forms": "^18.2.0",
  "rxjs": "~7.8.0",
  "jwt-decode": "^4.0.0",
  "jspdf": "^2.5.1",
  "country-state-city": "^3.2.1",
  "zone.js": "~0.14.10"
}
```

### Dev Dependencies (Testing)

```json
{
  "karma": "~6.4.0",
  "karma-chrome-launcher": "~3.2.0",
  "karma-coverage": "~2.2.0",
  "karma-jasmine": "~5.1.0",
  "jasmine-core": "~5.2.0",
  "@types/jasmine": "~5.1.0"
}
```

### External APIs (not in package.json — loaded at runtime)

| Service | Purpose |
|---------|---------|
| **Razorpay** | Payment gateway (UPI, Card, Net Banking) |
| **Groq API** | AI chatbot (llama-3.1-8b-instant model) |
| **country-state-city** | City/state autocomplete for India |

### Environment Config (`src/environments/environment.ts`)

```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7208/api',
  razorpayKeyId: 'rzp_test_SVtcM9b8whLPCh',
  groqApiKey: 'gsk_...',
};
```

This file holds all secrets and API URLs. In production you would have a separate `environment.prod.ts`.

---

## 3. Project Folder Structure

```
Fontend-Angular/src/app/
├── app.component.ts          ← Root component (navbar + footer + router-outlet)
├── app.config.ts             ← Angular providers (router, http, interceptors)
├── app.routes.ts             ← Main route definitions
│
├── core/
│   ├── guards/
│   │   └── auth.guard.ts     ← Route protection by role
│   ├── interceptors/
│   │   ├── auth.interceptor.ts    ← Adds JWT token to every request
│   │   └── loading.interceptor.ts ← Shows/hides global spinner
│   ├── models/
│   │   └── models.ts         ← All TypeScript interfaces (DTOs)
│   └── services/
│       ├── auth.service.ts        ← Login, register, JWT decode
│       ├── hotel.service.ts       ← Hotel search & management
│       ├── booking.service.ts     ← Reservations & payments
│       ├── wallet.service.ts      ← Wallet top-up & history
│       ├── chatbot.service.ts     ← Groq AI API calls
│       ├── chatbot-prompts.ts     ← Role-specific AI prompts
│       ├── toast.service.ts       ← Snackbar notifications
│       ├── loading.service.ts     ← Global loading state
│       ├── location.service.ts    ← City/state lookup
│       ├── promo-code.service.ts  ← Promo code validation
│       ├── revenue.service.ts     ← SuperAdmin revenue
│       ├── support-request.service.ts ← Support tickets
│       ├── amenity.service.ts     ← Amenity CRUD (SuperAdmin)
│       ├── amenity-request.service.ts ← Amenity requests (Admin)
│       └── api.services.ts        ← Transaction, Review, User, Dashboard,
│                                     AuditLog, Log, RoomType, Room, Inventory
│
├── shared/
│   └── components/
│       ├── navbar/            ← Top navigation bar
│       ├── footer/            ← Page footer
│       ├── spinner/           ← Full-page loading overlay
│       ├── chatbot/           ← AI chatbot widget
│       ├── confirm-dialog/    ← Reusable yes/no dialog
│       ├── input-dialog/      ← Reusable text input dialog
│       ├── city-autocomplete/ ← City search with state auto-fill
│       └── infinite-carousel/ ← Auto-scrolling hotel cards
│
└── features/
    ├── auth/                  ← Login & Register pages
    ├── hotel/                 ← Public hotel list & detail pages
    ├── booking/               ← Create booking, list, detail
    ├── guest/                 ← Guest dashboard, profile, wallet, reviews...
    ├── admin/                 ← Admin dashboard, rooms, reservations...
    ├── superadmin/            ← SuperAdmin dashboard, hotels, revenue...
    ├── contact/               ← Contact/support form
    └── not-found/             ← 404 page
```

---

## 4. Angular Core Concepts Used

### 4.1 Standalone Components

Every component in this project is **standalone** — no NgModules needed. You declare what you need directly in the `imports` array of the component.

```typescript
@Component({
  selector: 'app-spinner',
  standalone: true,                          // ← standalone = true
  imports: [MatProgressSpinnerModule],       // ← import what you need
  template: `
    @if (loading.isLoading()) {
      <div class="full-page-spinner">
        <mat-progress-spinner mode="indeterminate" diameter="40" />
      </div>
    }
  `
})
export class SpinnerComponent {
  loading = inject(LoadingService);
}
```

**Why standalone?** No need for a big `AppModule`. Each component is self-contained and easier to understand.

### 4.2 Signals (Angular 17+)

Signals are Angular's new reactive state system. Think of them like a variable that Angular watches automatically.

```typescript
// Create a signal
loading = signal(false);
hotels  = signal<HotelListItemDto[]>([]);

// Read a signal (call it like a function)
console.log(this.loading());   // false

// Update a signal
this.loading.set(true);
this.hotels.set([...newHotels]);

// Update based on previous value
this.hotels.update(list => [...list, newHotel]);
```

**computed()** — a signal that derives its value from other signals automatically:

```typescript
// In booking-create.component.ts
totalNights = computed(() => {
  const ci = this.checkInDate();
  const co = this.checkOutDate();
  if (!ci || !co) return 0;
  return Math.round((co.getTime() - ci.getTime()) / 86400000);
});

baseTotal = computed(() => {
  const rt    = this.selectedRoomType();
  const rooms = this.numberOfRooms();
  return (rt?.pricePerNight ?? 0) * this.totalNights() * rooms;
});

finalTotal = computed(() =>
  Math.max(0, this.baseTotal() + this.gstAmount() - this.promoDiscount() - this.walletUsedAmount())
);
```

When `checkInDate` or `checkOutDate` changes, `totalNights` recalculates automatically. When `totalNights` changes, `baseTotal` recalculates. This is the power of signals.

### 4.3 inject() Function

Instead of constructor injection, this project uses `inject()`:

```typescript
// Old way (constructor injection)
constructor(private authService: AuthService) {}

// New way (inject function) — used everywhere in this project
private authService = inject(AuthService);
private router      = inject(Router);
private fb          = inject(FormBuilder);
```

Both work the same. `inject()` is cleaner and works outside constructors too.

### 4.4 New Control Flow (@if, @for, @switch)

Angular 17+ replaced `*ngIf` and `*ngFor` with built-in control flow:

```html
<!-- Old way -->
<div *ngIf="loading">Loading...</div>
<div *ngFor="let hotel of hotels">{{ hotel.name }}</div>

<!-- New way (used in this project) -->
@if (loading()) {
  <div>Loading...</div>
}

@for (hotel of hotels(); track hotel.hotelId) {
  <div>{{ hotel.name }}</div>
}

@if (data(); as d) {
  <p>{{ d.hotelName }}</p>
} @else {
  <p>No data</p>
}
```

### 4.5 Lifecycle Hooks

```typescript
// ngOnInit — runs once when component is created
ngOnInit() {
  this.load();
}

// ngOnDestroy — runs when component is removed (cleanup)
ngOnDestroy() {
  if (this.timer) clearInterval(this.timer);
  this.searchSubject.complete();
}

// ngAfterViewChecked — runs after every view check
ngAfterViewChecked(): void {
  if (this.shouldScroll) {
    this.scrollToBottom();
    this.shouldScroll = false;
  }
}
```

### 4.6 ViewChild

Used to get a reference to a child element or component:

```typescript
@ViewChild(MatPaginator) paginator!: MatPaginator;

// Then use it
this.paginator?.firstPage();  // reset paginator to page 1
```

### 4.7 toSignal (rxjs-interop)

Converts an Observable into a Signal:

```typescript
// In app.component.ts
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
```

---

## 5. App Bootstrap & Configuration

### `app.config.ts` — The Application Providers

```typescript
export const appConfig: ApplicationConfig = {
  providers: [
    // Zone.js change detection with event coalescing (batches multiple events)
    provideZoneChangeDetection({ eventCoalescing: true }),

    // Router with view transitions and scroll restoration
    provideRouter(
      routes,
      withViewTransitions(),                    // smooth page transitions
      withInMemoryScrolling({
        scrollPositionRestoration: 'top',       // scroll to top on navigation
        anchorScrolling: 'enabled',
      })
    ),

    // HTTP client with both interceptors
    provideHttpClient(withInterceptors([loadingInterceptor, authInterceptor])),

    // Angular Material animations (async = lazy loaded)
    provideAnimationsAsync(),
  ],
};
```

### `app.component.ts` — Root Component

The root component decides what to show based on the current URL:

```typescript
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent, FooterComponent, SpinnerComponent, ChatbotComponent],
  template: `
    <app-spinner />                    <!-- always visible (shows when loading) -->
    @if (showChrome()) {
      <app-navbar />                   <!-- hidden on /auth pages -->
    }
    <main [class.auth-main]="!showChrome()">
      <router-outlet />                <!-- page content goes here -->
    </main>
    @if (showChrome()) {
      <app-footer />
      <app-chatbot />                  <!-- chatbot widget -->
    }
  `
})
export class AppComponent implements OnInit {
  ngOnInit() {
    // Apply saved dark theme on startup
    const theme = localStorage.getItem('theme');
    if (theme === 'dark') {
      document.body.classList.add('dark-theme');
    }
  }
}
```

`showChrome()` returns `false` when the URL starts with `/auth` — so the navbar, footer, and chatbot are hidden on login/register pages.

---

## 6. Routing & Lazy Loading

### Main Routes (`app.routes.ts`)

```typescript
export const routes: Routes = [
  { path: '', redirectTo: '/hotels', pathMatch: 'full' },

  // Public — no guard needed
  {
    path: 'hotels',
    loadChildren: () => import('./features/hotel/hotel.routes').then(m => m.HOTEL_ROUTES),
  },

  // Auth pages — only for non-logged-in users
  {
    path: 'auth',
    canActivate: [publicGuard],
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES),
  },

  // Guest pages — only for logged-in guests
  {
    path: 'guest',
    canActivate: [guestGuard],
    loadChildren: () => import('./features/guest/guest.routes').then(m => m.GUEST_ROUTES),
  },

  // Booking — only for guests
  {
    path: 'booking',
    canActivate: [guestGuard],
    loadChildren: () => import('./features/booking/booking.routes').then(m => m.BOOKING_ROUTES),
  },

  // Admin pages
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadChildren: () => import('./features/admin/admin.routes').then(m => m.ADMIN_ROUTES),
  },

  // SuperAdmin pages
  {
    path: 'superadmin',
    canActivate: [superAdminGuard],
    loadChildren: () => import('./features/superadmin/superadmin.routes').then(m => m.SUPERADMIN_ROUTES),
  },

  // Misc
  { path: 'contact', loadComponent: () => import('./features/contact/contact.component').then(m => m.ContactComponent) },
  { path: '**', loadComponent: () => import('./features/not-found/not-found.component').then(m => m.NotFoundComponent) },
];
```

**What is Lazy Loading?**

`loadChildren` and `loadComponent` mean the code for that route is only downloaded when the user navigates to it. This makes the initial page load much faster.

```
User visits /hotels → Angular downloads hotel bundle
User visits /admin  → Angular downloads admin bundle (only then)
```

---

## 7. Route Guards

Guards protect routes. If the condition fails, the user is redirected.

### `auth.guard.ts` — All Guards

```typescript
// authGuard — any logged-in user
export const authGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated()) return true;
  localStorage.setItem('returnUrl', state.url);  // remember where they were going
  router.navigate(['/auth/login']);
  return false;
};

// guestGuard — only Guest role
export const guestGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated() && auth.isGuest()) return true;
  if (auth.isAuthenticated()) {
    router.navigate([auth.getRedirectUrl()]);  // send to their own dashboard
    return false;
  }
  localStorage.setItem('returnUrl', state.url);
  router.navigate(['/auth/login']);
  return false;
};

// adminGuard — only Admin role
// superAdminGuard — only SuperAdmin role
// publicGuard — only non-logged-in users (for /auth pages)
```

**How it works in practice:**

- Guest tries to visit `/admin/dashboard` → `adminGuard` fails → redirected to `/guest/dashboard`
- Non-logged-in user visits `/guest/wallet` → `guestGuard` fails → redirected to `/auth/login`
- Logged-in user visits `/auth/login` → `publicGuard` fails → redirected to their dashboard

---

## 8. HTTP Interceptors

Interceptors run on every HTTP request/response automatically.

### `auth.interceptor.ts` — JWT Token + Error Handling

```typescript
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router      = inject(Router);
  const toast       = inject(ToastService);

  // Skip for external APIs (Groq, Razorpay, etc.)
  if (!req.url.includes('localhost') && !req.url.includes('127.0.0.1')) {
    return next(req);
  }

  // Add JWT token to every request
  const token  = authService.token();
  const cloned = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(cloned).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        authService.logout();           // token expired → logout
      } else if (error.status === 403) {
        router.navigate(['/unauthorized']);
      } else if (error.status === 0) {
        toast.error('Cannot connect to server.');
      }
      // ... other status codes
      return throwError(() => error);
    })
  );
};
```

**What this does:**
1. Adds `Authorization: Bearer <token>` header to every API call
2. If the server returns 401 (unauthorized) → logs the user out
3. If the server returns 403 (forbidden) → redirects to unauthorized page
4. Shows a toast message for all errors

### `loading.interceptor.ts` — Global Spinner

```typescript
export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loadingService = inject(LoadingService);

  // Skip for external APIs (Groq chatbot, etc.)
  if (!req.url.includes('localhost') && !req.url.includes('127.0.0.1')) {
    return next(req);
  }

  loadingService.show();                              // show spinner
  return next(req).pipe(
    finalize(() => loadingService.hide())             // hide spinner when done
  );
};
```

Every time an API call starts, the spinner appears. When it finishes (success or error), the spinner hides. The `LoadingService` uses a counter so multiple simultaneous requests work correctly.

---

## 9. Services

### 9.1 AuthService

Handles login, register, JWT decoding, and role checking.

```typescript
@Injectable({ providedIn: 'root' })
export class AuthService {
  private _currentUser = signal<CurrentUser | null>(null);
  private _token       = signal<string | null>(null);

  // Computed signals — auto-update when _currentUser changes
  readonly isAuthenticated = computed(() => !!this._currentUser());
  readonly isGuest         = computed(() => this._currentUser()?.role === 'Guest');
  readonly isAdmin         = computed(() => this._currentUser()?.role === 'Admin');
  readonly isSuperAdmin    = computed(() => this._currentUser()?.role === 'SuperAdmin');

  constructor() {
    this.loadFromStorage();  // restore session from localStorage on app start
  }

  login(dto: LoginDto): Observable<AuthResponseDto> {
    return this.http.post<ApiResponse<AuthResponseDto>>(`${environment.apiUrl}/auth/login`, dto)
      .pipe(map(r => r.data!), tap(res => this.setToken(res.token)));
  }

  private setToken(token: string): void {
    localStorage.setItem('hotel_token', token);
    this._token.set(token);
    const payload = jwtDecode<JwtPayload>(token);  // decode JWT to get user info
    this._currentUser.set(this.payloadToUser(payload));
  }

  logout(): void {
    localStorage.removeItem('hotel_token');
    this._token.set(null);
    this._currentUser.set(null);
    this.router.navigate(['/auth/login']);
  }

  getRedirectUrl(): string {
    const role = this._currentUser()?.role;
    if (role === 'Admin')      return '/admin/dashboard';
    if (role === 'SuperAdmin') return '/superadmin/dashboard';
    return '/guest/dashboard';
  }
}
```

**JWT Decoding:** The token from the server contains user info encoded inside it. `jwtDecode` reads it without needing another API call:

```typescript
// JWT payload contains:
{
  nameid: "user-id-123",
  unique_name: "John Doe",
  role: "Guest",
  HotelId: undefined,
  exp: 1234567890   // expiry timestamp
}
```

### 9.2 HotelService

```typescript
@Injectable({ providedIn: 'root' })
export class HotelService {
  // Public — no auth needed
  getTopHotels(): Observable<HotelListItemDto[]> { ... }
  searchHotelsWithFilters(req: SearchHotelRequestDto): Observable<SearchHotelResponseDto> { ... }
  getHotelDetails(hotelId: string): Observable<HotelDetailsDto> { ... }
  getAvailability(hotelId, checkIn, checkOut): Observable<RoomAvailabilityDto[]> { ... }

  // Admin
  updateHotel(dto: UpdateHotelDto): Observable<void> { ... }
  toggleHotelStatus(isActive: boolean): Observable<void> { ... }

  // SuperAdmin
  getAllHotelsForSuperAdmin(page, pageSize, search?, status?): Observable<...> { ... }
  blockHotel(id: string): Observable<void> { ... }
  unblockHotel(id: string): Observable<void> { ... }
}
```

### 9.3 BookingService

```typescript
@Injectable({ providedIn: 'root' })
export class BookingService {
  // Guest
  createReservation(dto): Observable<ReservationResponseDto> { ... }
  getMyReservationsHistory(page, pageSize, status?, search?): Observable<...> { ... }
  getReservationByCode(code): Observable<ReservationDetailsDto> { ... }
  cancelReservation(code, dto): Observable<void> { ... }
  getPaymentQr(reservationId): Observable<QrPaymentResponseDto> { ... }
  validatePromoCode(dto): Observable<PromoCodeValidationResultDto> { ... }

  // Admin
  getHotelReservations(page, pageSize, status?, search?, sortField?, sortDir?): Observable<...> { ... }
  confirmReservation(code): Observable<void> { ... }
  completeReservation(code): Observable<void> { ... }
}
```

### 9.4 ToastService

Shows notification messages using Angular Material Snackbar:

```typescript
@Injectable({ providedIn: 'root' })
export class ToastService {
  success(message: string): void {
    this.snackBar.open(message, '✕', {
      duration: 3500,
      panelClass: ['toast-success'],
      horizontalPosition: 'right',
      verticalPosition: 'top',
    });
  }
  error(message: string): void { ... }   // red, 5 seconds
  info(message: string): void { ... }    // blue, 3.5 seconds
  warning(message: string): void { ... } // orange, 4 seconds
}
```

Usage anywhere in the app:
```typescript
this.toast.success('Hotel updated successfully.');
this.toast.error('Cannot connect to server.');
```

### 9.5 LoadingService

Tracks how many HTTP requests are in-flight:

```typescript
@Injectable({ providedIn: 'root' })
export class LoadingService {
  private _count   = 0;
  private _loading = signal(false);
  readonly isLoading = this._loading.asReadonly();

  show(): void {
    this._count++;
    this._loading.set(true);
  }

  hide(): void {
    this._count = Math.max(0, this._count - 1);
    if (this._count === 0) this._loading.set(false);  // only hide when ALL requests done
  }
}
```

### 9.6 LocationService

Uses the `country-state-city` npm package to get Indian cities and states:

```typescript
@Injectable({ providedIn: 'root' })
export class LocationService {
  private readonly COUNTRY_CODE = 'IN';

  searchCities(query: string): ICity[] {
    if (!query || query.length < 2) return [];
    const all = City.getCitiesOfCountry('IN') || [];
    return all
      .filter(c => c.name.toLowerCase().startsWith(query.toLowerCase()))
      .slice(0, 20);  // max 20 results
  }

  getStateNameByCity(cityName: string): string {
    // finds the state for a given city name
  }
}
```

### 9.7 WalletService

```typescript
getWallet(page, pageSize): Observable<PagedWalletTransactionDto> {
  return this.http.post(`${base}/guest/wallet/list`, { page, pageSize });
}

topUp(dto: TopUpWalletDto): Observable<WalletResponseDto> {
  return this.http.post(`${base}/guest/wallet/topup`, dto);
}
```

### 9.8 ChatbotService

Calls the Groq API (free LLM API) with the conversation history:

```typescript
send(history: ChatMessage[], userMessage: string, systemPrompt: string): Observable<string> {
  const headers = new HttpHeaders({
    'Authorization': `Bearer ${environment.groqApiKey}`,
    'Content-Type': 'application/json'
  });

  const messages = [
    { role: 'system', content: systemPrompt },
    ...history.slice(-6).map(m => ({    // only last 6 messages (token limit)
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
    map(res => res.choices?.[0]?.message?.content ?? 'Sorry, try again.')
  );
}
```

### 9.9 api.services.ts — Multiple Services in One File

This file contains many services grouped together:

- `TransactionService` — create payment, get transaction history
- `ReviewService` — add/update/delete reviews, admin reply
- `UserService` — get/update user profile
- `DashboardService` — get dashboard stats for each role
- `AuditLogService` — get audit logs (admin & superadmin)
- `LogService` — get error logs
- `RoomTypeService` — room type CRUD + rates
- `RoomService` — room CRUD + occupancy
- `InventoryService` — inventory management
- `AmenityService` — public amenity list

---

## 10. Models (TypeScript Interfaces)

All data shapes are defined in `core/models/models.ts`. These match exactly what the backend API sends and receives.

### Auth Models

```typescript
export interface LoginDto {
  email: string;
  password: string;
}

export interface CurrentUser {
  userId: string;
  userName: string;
  role: 'Guest' | 'Admin' | 'SuperAdmin';
  hotelId?: string;
}

export interface JwtPayload {
  nameid: string;       // userId
  unique_name: string;  // userName
  role: string;
  HotelId?: string;
  exp: number;          // expiry timestamp
}
```

### Hotel Models

```typescript
export interface HotelListItemDto {
  hotelId: string;
  name: string;
  city: string;
  imageUrl: string;
  averageRating: number;
  reviewCount: number;
  startingPrice?: number;
}

export interface HotelDetailsDto {
  hotelId: string;
  name: string;
  address: string;
  city: string;
  state: string;
  description: string;
  imageUrl: string;
  contactNumber: string;
  upiId?: string;
  averageRating: number;
  gstPercent: number;
  amenities: string[];
  reviews: ReviewDto[];
  roomTypes: RoomTypePublicDto[];
}
```

### Reservation Models

```typescript
export interface ReservationDetailsDto {
  reservationCode: string;
  reservationId: string;
  hotelName: string;
  roomTypeName: string;
  checkInDate: string;
  checkOutDate: string;
  numberOfRooms: number;
  totalAmount: number;
  gstAmount: number;
  discountAmount: number;
  walletAmountUsed: number;
  finalAmount: number;
  status: string;  // 'Pending' | 'Confirmed' | 'Completed' | 'Cancelled' | 'NoShow'
  expiryTime?: string;  // 10-minute payment window
  cancellationFeePaid: boolean;
  cancellationPolicyText: string;
}
```

### Payment Enums

```typescript
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

### API Response Wrapper

Every API response is wrapped in this shape:

```typescript
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  statusCode?: number;
}
```

In services, we always unwrap with `.pipe(map(r => r.data!))`:

```typescript
getTopHotels(): Observable<HotelListItemDto[]> {
  return this.http.get<ApiResponse<HotelListItemDto[]>>(`${base}/public/hotels/top`)
    .pipe(map(r => r.data!));  // unwrap the data field
}
```

---

## 11. Shared Components

### 11.1 NavbarComponent

The top navigation bar. Shows different links based on user role.

```typescript
export class NavbarComponent implements OnInit {
  auth = inject(AuthService);
  mobileOpen = signal(false);
  isDarkMode = signal(false);

  ngOnInit() {
    // Load profile image for guest/superadmin
    if (this.auth.isAuthenticated() && (this.auth.isGuest() || this.auth.isSuperAdmin())) {
      this.userService.getProfile().subscribe({
        next: p => this.auth.updateProfileImage(p.profileImageUrl ?? null),
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
}
```

The navbar uses `auth.isGuest()`, `auth.isAdmin()`, `auth.isSuperAdmin()` to show the right menu items.

### 11.2 SpinnerComponent

Shows a full-page overlay when any API call is in progress:

```typescript
@Component({
  template: `
    @if (loading.isLoading()) {
      <div class="full-page-spinner">
        <mat-progress-spinner mode="indeterminate" diameter="40" />
        <span class="spinner-text">Loading...</span>
      </div>
    }
  `
})
export class SpinnerComponent {
  loading = inject(LoadingService);
}
```

### 11.3 ConfirmDialogComponent

A reusable dialog for "are you sure?" confirmations:

```typescript
// How to use it in any component:
async block(hotel: SuperAdminHotelListDto) {
  const { ConfirmDialogComponent } = await import('../../../shared/components/confirm-dialog/confirm-dialog.component');
  const ref = this.dialog.open(ConfirmDialogComponent, {
    data: {
      title: 'Block Hotel',
      message: `Block "${hotel.name}"?`,
      confirmLabel: 'Block',
      confirmColor: 'warn'
    }
  });
  ref.afterClosed().subscribe(ok => {
    if (!ok) return;
    this.hotelService.blockHotel(hotel.hotelId).subscribe(() => {
      this.toast.success(`${hotel.name} blocked.`);
      this.load();
    });
  });
}
```

Notice the **dynamic import** — `ConfirmDialogComponent` is only loaded when the dialog is actually opened. This is lazy loading for components.

### 11.4 InputDialogComponent

A reusable dialog with a text input field. Used by SuperAdmin to respond to support tickets:

```typescript
const ref = this.dialog.open(InputDialogComponent, {
  data: {
    title: 'Respond to: ' + r.subject,
    label: 'Your Response',
    placeholder: 'Type your response...',
    confirmLabel: 'Send Response',
    multiline: true,   // shows textarea instead of input
  },
  width: '520px',
});
ref.afterClosed().subscribe((response: string | null) => {
  if (!response) return;
  this.service.respond(r.supportRequestId, { response, status: 'Resolved' }).subscribe(...);
});
```

### 11.5 CityAutocompleteComponent

A Material autocomplete that searches Indian cities as you type:

```typescript
@Component({
  template: `
    <mat-form-field appearance="outline">
      <mat-label>📍 City</mat-label>
      <input matInput [formControl]="control" [matAutocomplete]="cityAuto" />
      <mat-autocomplete #cityAuto="matAutocomplete">
        @for (city of filteredCities; track city.name) {
          <mat-option [value]="city">{{ city.name }} — {{ city.stateCode }}</mat-option>
        }
      </mat-autocomplete>
    </mat-form-field>
  `
})
export class CityAutocompleteComponent implements OnInit {
  @Input() control!: FormControl;
  @Input() stateControl?: FormControl;  // optional: auto-fills state when city selected

  ngOnInit() {
    this.control.valueChanges.pipe(
      debounceTime(300),        // wait 300ms after typing stops
      distinctUntilChanged(),   // only search if value actually changed
    ).subscribe(value => {
      this.filteredCities = this.locationService.searchCities(value);
    });
  }
}
```

Usage in `HotelManagementComponent`:
```typescript
cityControl  = new FormControl('', [Validators.required]);
stateControl = new FormControl('');
```
```html
<app-city-autocomplete [control]="cityControl" [stateControl]="stateControl" />
```

### 11.6 InfiniteCarouselComponent

An auto-scrolling carousel for hotel cards on the home page:

```typescript
export class InfiniteCarouselComponent implements OnChanges, AfterViewInit, OnDestroy {
  @Input({ required: true }) hotels: HotelListItemDto[] = [];

  // Trick: triple the array so it looks infinite
  // [hotels, hotels, hotels] — start in the middle copy
  displayItems: HotelListItemDto[] = [];

  ngOnChanges() {
    this.displayItems = [...this.hotels, ...this.hotels, ...this.hotels];
    this.offset = -(this.hotels.length * this.CARD_WIDTH);  // start at middle
  }

  ngAfterViewInit() {
    this.autoTimer = setInterval(() => this.autoAdvance(), 3500);  // auto-scroll every 3.5s
  }

  private wrapIfNeeded() {
    // When we reach the end of the last copy, silently jump back to the middle copy
    // This creates the illusion of infinite scrolling
  }
}
```

### 11.7 ChatbotComponent

The AI assistant widget. Covered in detail in Section 17.

### 11.8 FooterComponent

Simple footer with links. Uses `RouterLink` for internal navigation and `href` for email/phone.

---

## 12. Feature Modules

### 12.1 Auth Features

**Login** — `features/auth/login/`
- Uses `ReactiveFormsModule` with email + password fields
- On success, calls `authService.login()` then navigates to `getRedirectUrl()`
- Checks `localStorage.getItem('returnUrl')` to redirect back to where the user was going

**Register** — `features/auth/register/`
- Two forms: Guest registration and Hotel Admin registration
- Hotel Admin form has extra fields: hotel name, address, city, state, description, contact

### 12.2 Hotel Features (Public)

**Hotel List** — `features/hotel/hotel-list/`
- Search form with city, check-in/out dates, filters (price, amenities, room type, sort)
- Uses `debounceTime` to avoid searching on every keystroke
- Paginated results with `MatPaginator`

**Hotel Detail** — `features/hotel/hotel-detail/`
- Shows hotel info, amenities, room types with availability
- "Book Now" button navigates to `/booking/create?hotelId=...&roomTypeId=...&checkIn=...`

### 12.3 Booking Features

**BookingCreateComponent** — the most complex component in the project.

It has a 3-step `MatStepper`:
1. **Step 1** — Select room type, dates, number of rooms, apply promo code, use wallet
2. **Step 2** — Review booking summary, create reservation
3. **Step 3** — Payment (Razorpay / UPI QR / Wallet)

Key features:
- All price calculations use `computed()` signals — they update automatically
- 10-minute countdown timer for pending reservations
- Resume mode: if user navigates away, they can come back and continue payment
- Razorpay integration for card/UPI/netbanking payments

```typescript
// Price calculation chain using computed signals
baseTotal = computed(() =>
  (this.selectedRoomType()?.pricePerNight ?? 0) * this.totalNights() * this.numberOfRooms()
);
gstAmount = computed(() =>
  Math.round(this.baseTotal() * this.gstPercent() / 100 * 100) / 100
);
finalTotal = computed(() =>
  Math.max(0, this.baseTotal() + this.gstAmount() - this.promoDiscount()
              - this.walletUsedAmount() + this.cancellationFeeAmount())
);
```

**BookingListComponent** — My Bookings page
- Status tabs: All, Pending, Confirmed, Completed, Cancelled, NoShow
- Live countdown timer for pending reservations (updates every second)
- Search by reservation code or hotel name

**BookingDetailComponent** — Single booking detail
- Shows full booking info, rooms assigned, price breakdown
- Pay Now button (if still pending)
- Cancel button with refund preview
- Download PDF button (generates booking confirmation using jsPDF)

### 12.4 Guest Features

| Component | What it does |
|-----------|-------------|
| `GuestDashboardComponent` | Stats: total bookings, active, completed, spent |
| `GuestProfileComponent` | Edit name, phone, address, city (with autocomplete), profile image |
| `GuestWalletComponent` | View balance, top up via Razorpay, transaction history |
| `GuestPromoCodesComponent` | View earned promo codes, copy to clipboard |
| `GuestReviewsComponent` | Write reviews for completed stays, edit/delete reviews |
| `GuestTransactionsComponent` | View all payment transactions |
| `GuestSupportRequestsComponent` | View submitted support tickets |

### 12.5 Admin Features

| Component | What it does |
|-----------|-------------|
| `AdminDashboardComponent` | Hotel stats, toggle hotel active/inactive, download report |
| `HotelManagementComponent` | Edit hotel details, UPI ID, GST percentage |
| `RoomManagementComponent` | Add/edit/toggle rooms, view room occupancy by date |
| `RoomTypeManagementComponent` | Add/edit room types, manage amenities, set date-based rates |
| `InventoryManagementComponent` | Set available rooms per date range |
| `ReservationManagementComponent` | View all reservations, confirm/complete them |
| `AdminReviewsComponent` | View guest reviews, reply to them |
| `AdminTransactionsComponent` | View all payment transactions |
| `AuditLogsComponent` | View history of all actions |
| `AmenityRequestsComponent` | Request new amenities from SuperAdmin |
| `AdminSupportRequestsComponent` | View submitted support tickets |

### 12.6 SuperAdmin Features

| Component | What it does |
|-----------|-------------|
| `SuperAdminDashboardComponent` | Platform-wide stats |
| `HotelControlComponent` | View all hotels, block/unblock them |
| `SuperadminRevenueComponent` | View 2% commission earned per reservation |
| `SuperadminAmenityManagementComponent` | Create/edit/delete global amenities |
| `SuperadminAmenityRequestsComponent` | Approve/reject admin amenity requests |
| `SuperadminSupportRequestsComponent` | Respond to all support tickets |
| `AuditLogsComponent` | View all actions across all hotels |
| `ErrorLogsComponent` | View application error logs |
| `SuperAdminProfileComponent` | Edit SuperAdmin profile |

### 12.7 Contact Component

Smart contact form that shows different fields based on who is logged in:

```typescript
// Public visitor → name, email, subject, category, message
// Logged-in Guest → subject, category, reservation code (optional), message
// Logged-in Admin → subject, category (Bug Report, Feature Request...), message
```

---

## 13. Angular Material

Angular Material is a UI component library. This project uses it heavily.

### How it's set up (`styles.scss`)

```scss
@use '@angular/material' as mat;
@include mat.core();

// Light theme
$light-theme: mat.m2-define-light-theme((
  color: (
    primary: mat.m2-define-palette(mat.$m2-indigo-palette, 700),
    accent:  mat.m2-define-palette(mat.$m2-amber-palette, 700),
    warn:    mat.m2-define-palette(mat.$m2-red-palette),
  ),
));

@include mat.all-component-themes($light-theme);

// Dark theme (applied when body has .dark-theme class)
.dark-theme {
  @include mat.all-component-colors($dark-theme);
}
```

### Material Components Used

| Component | Import | Used For |
|-----------|--------|---------|
| `MatTableModule` | `@angular/material/table` | Data tables everywhere |
| `MatPaginatorModule` | `@angular/material/paginator` | Page navigation for tables |
| `MatFormFieldModule` + `MatInputModule` | `@angular/material/form-field` | Form inputs |
| `MatButtonModule` | `@angular/material/button` | All buttons |
| `MatIconModule` | `@angular/material/icon` | Material icons |
| `MatDialogModule` | `@angular/material/dialog` | Confirm/input dialogs |
| `MatSnackBarModule` | `@angular/material/snack-bar` | Toast notifications |
| `MatTabsModule` | `@angular/material/tabs` | Status filter tabs |
| `MatChipsModule` | `@angular/material/chips` | Status badges |
| `MatProgressSpinnerModule` | `@angular/material/progress-spinner` | Loading spinners |
| `MatSelectModule` | `@angular/material/select` | Dropdown selects |
| `MatDatepickerModule` | `@angular/material/datepicker` | Date pickers |
| `MatStepperModule` | `@angular/material/stepper` | Multi-step booking form |
| `MatCardModule` | `@angular/material/card` | Content cards |
| `MatToolbarModule` | `@angular/material/toolbar` | Navbar toolbar |
| `MatMenuModule` | `@angular/material/menu` | Dropdown menus |
| `MatSlideToggleModule` | `@angular/material/slide-toggle` | Toggle switches |
| `MatTooltipModule` | `@angular/material/tooltip` | Hover tooltips |
| `MatExpansionModule` | `@angular/material/expansion` | Accordion panels |
| `MatAutocompleteModule` | `@angular/material/autocomplete` | City search autocomplete |
| `MatRadioModule` | `@angular/material/radio` | Radio buttons |
| `MatDividerModule` | `@angular/material/divider` | Horizontal dividers |
| `MatSortModule` | `@angular/material/sort` | Sortable table columns |

### Example: MatTable with Paginator

```typescript
// Component
@ViewChild(MatPaginator) paginator!: MatPaginator;

reservations = signal<ReservationDetailsDto[]>([]);
totalCount   = signal(0);
displayedColumns = ['reservationCode', 'hotelName', 'checkIn', 'status', 'actions'];

load() {
  this.bookingService.getHotelReservations(this.currentPage, this.pageSize, this.selectedStatus)
    .subscribe(res => {
      this.reservations.set(res.reservations);
      this.totalCount.set(res.totalCount);
    });
}

onPage(e: PageEvent) {
  this.currentPage = e.pageIndex + 1;
  this.pageSize    = e.pageSize;
  this.load();
}
```

```html
<!-- Template -->
<table mat-table [dataSource]="reservations()">
  <ng-container matColumnDef="reservationCode">
    <th mat-header-cell *matHeaderCellDef>Code</th>
    <td mat-cell *matCellDef="let r">{{ r.reservationCode }}</td>
  </ng-container>

  <!-- ... more columns ... -->

  <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
  <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
</table>

<mat-paginator
  [length]="totalCount()"
  [pageSize]="pageSize"
  [pageSizeOptions]="[10, 20, 50]"
  showFirstLastButtons
  (page)="onPage($event)"
/>
```

### Example: MatStepper (Booking Create)

```html
<mat-stepper #stepper linear>
  <mat-step label="Select Room">
    <!-- Step 1 content -->
    <button mat-flat-button matStepperNext [disabled]="!step1Valid()">Next</button>
  </mat-step>

  <mat-step label="Review">
    <!-- Step 2 content -->
    <button mat-flat-button matStepperPrevious>Back</button>
    <button mat-flat-button color="primary" (click)="createReservation()">Book Now</button>
  </mat-step>

  <mat-step label="Payment">
    <!-- Step 3 content -->
  </mat-step>
</mat-stepper>
```

### Example: MatDialog

```typescript
// Open a dialog
const ref = this.dialog.open(ConfirmDialogComponent, {
  data: { title: 'Delete', message: 'Are you sure?', confirmColor: 'warn' }
});

// React to the result
ref.afterClosed().subscribe(confirmed => {
  if (confirmed) {
    // user clicked confirm
  }
});
```

---

## 14. Bootstrap CSS

Bootstrap 5 is used for **layout utilities only** (grid, spacing, flex). Angular Material handles all the UI components.

Bootstrap classes used in templates:

```html
<!-- Grid system -->
<div class="row">
  <div class="col-md-4">...</div>
  <div class="col-md-8">...</div>
</div>

<!-- Spacing -->
<div class="mb-4">...</div>   <!-- margin-bottom: 1.5rem -->
<div class="py-4">...</div>   <!-- padding top+bottom: 1.5rem -->
<div class="mt-2">...</div>   <!-- margin-top: 0.5rem -->

<!-- Flex -->
<div class="d-flex gap-3 align-items-start">...</div>

<!-- Text -->
<div class="text-center">...</div>
<div class="w-100">...</div>
```

Bootstrap is loaded via CDN or npm — it provides the responsive grid and spacing utilities that complement Angular Material's components.

---

## 15. Global Styles & Theming

### CSS Variables (`styles.scss`)

The entire app uses CSS custom properties (variables) for consistent theming:

```scss
:root {
  --color-bg: #f8f7f4;
  --color-surface: #ffffff;
  --color-primary: #2d3a8c;        /* deep indigo */
  --color-accent: #c97d1b;         /* amber */
  --color-text-primary: #1a1a2e;
  --color-text-secondary: #5a6278;
  --color-success: #2e7d32;
  --color-error: #c62828;
  --shadow-sm: 0 1px 3px rgba(0,0,0,0.08);
  --radius-md: 10px;
  --radius-lg: 16px;
  --font-display: 'Playfair Display', Georgia, serif;
  --font-body: 'DM Sans', -apple-system, sans-serif;
}
```

### Utility Classes

The project defines its own utility classes (similar to Tailwind):

```scss
.badge { display: inline-flex; padding: 4px 10px; border-radius: 20px; }
.badge-success { background: #e8f5e9; color: #2e7d32; }
.badge-warning { background: #fff8e1; color: #c97d1b; }
.badge-error   { background: #ffebee; color: #c62828; }
.badge-primary { background: #e8eaf6; color: #2d3a8c; }
.badge-muted   { background: #f5f5f5; color: #616161; }

.stat-card { background: var(--color-surface); border-radius: var(--radius-lg); padding: 24px; }
.table-card { background: var(--color-surface); border-radius: var(--radius-lg); overflow: hidden; }
.empty-state { display: flex; flex-direction: column; align-items: center; padding: 64px 24px; }
```

### Toast Styles

```scss
.toast-success .mdc-snackbar__surface { background: #1b5e20 !important; }
.toast-error   .mdc-snackbar__surface { background: #b71c1c !important; }
.toast-info    .mdc-snackbar__surface { background: #0d47a1 !important; }
.toast-warning .mdc-snackbar__surface { background: #e65100 !important; }
```

### Animations

```scss
@keyframes fadeIn {
  from { opacity: 0; transform: translateY(12px); }
  to   { opacity: 1; transform: translateY(0); }
}

.animate-fade-in { animation: fadeIn 0.4s ease both; }
```

---

## 16. Dark Mode

Dark mode is toggled by adding/removing the `dark-theme` class on `<body>`.

### How it works

1. User clicks the moon/sun icon in the navbar
2. `toggleTheme()` adds/removes `dark-theme` class on `document.body`
3. Saves preference to `localStorage`
4. On app start, `AppComponent.ngOnInit()` reads localStorage and applies the class

```typescript
// NavbarComponent
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

### Dark Theme CSS

In `styles.scss`, the dark theme overrides all CSS variables and Material component colors:

```scss
body.dark-theme {
  --color-bg: #121212;
  --color-surface: #1e1e1e;
  --color-text-primary: #e0e0e0;
  --color-border: #333333;
  --color-primary: #90caf9;   /* lighter blue for dark bg */

  // Override Material Table
  .mat-mdc-table { background-color: #1e1e1e !important; }
  .mat-mdc-row:hover { background-color: #2a2a2a !important; }

  // Override Material Form Fields
  .mat-mdc-form-field .mdc-text-field { background-color: #2a2a2a !important; }

  // Override Material Dialog
  .mat-mdc-dialog-container .mdc-dialog__surface { background-color: #1e1e1e !important; }

  // ... and many more overrides
}
```

---

## 17. Chatbot (AI Assistant)

The chatbot is a floating widget in the bottom-right corner of every page (except auth pages).

### Architecture

```
ChatbotComponent (UI)
    ↓ calls
ChatbotService (API layer)
    ↓ calls
Groq API (llama-3.1-8b-instant model)
    ↑ uses
chatbot-prompts.ts (role-specific system prompts)
```

### chatbot-prompts.ts — Role-Based Context

The chatbot knows about the platform and behaves differently based on who is logged in:

```typescript
const BASE = `You are the official AI assistant for "Thanush StayHub"...
STRICT SCOPE RULE: You ONLY answer questions related to the Thanush StayHub platform.
If a user asks anything outside this scope, respond with:
"I'm exclusively here to assist with Thanush StayHub platform queries..."`;

export const GUEST_CONTEXT    = `${BASE}\nCURRENT USER ROLE: Guest\n...`;
export const ADMIN_CONTEXT    = `${BASE}\nCURRENT USER ROLE: Hotel Admin\n...`;
export const SUPERADMIN_CONTEXT = `${BASE}\nCURRENT USER ROLE: SuperAdmin\n...`;
export const PUBLIC_CONTEXT   = `${BASE}\nCURRENT USER: Not logged in\n...`;
```

Each context tells the AI what features the current user has access to.

### ChatbotComponent

```typescript
export class ChatbotComponent implements OnInit, AfterViewChecked, OnDestroy {
  isOpen   = signal(false);
  messages = signal<ChatMessage[]>([]);
  loading  = signal(false);

  // Pick the right system prompt based on role
  private get systemPrompt(): string {
    const role = this.role();
    if (role === 'Guest')      return GUEST_CONTEXT;
    if (role === 'Admin')      return ADMIN_CONTEXT;
    if (role === 'SuperAdmin') return SUPERADMIN_CONTEXT;
    return PUBLIC_CONTEXT;
  }

  send(): void {
    const text = this.userInput().trim();
    if (!text || this.loading()) return;

    // Add user message to chat
    this.messages.update(msgs => [...msgs, { role: 'user', text }]);
    this.loading.set(true);

    // Send to Groq API with last 6 messages as history
    const history = this.messages().slice(1, -1);  // skip greeting + current message
    this.chatbotService.send(history, text, this.systemPrompt).subscribe({
      next: (reply) => {
        this.messages.update(msgs => [...msgs, { role: 'model', text: reply }]);
        this.loading.set(false);
      }
    });
  }

  // Auto-close when user navigates to a different page
  ngOnInit(): void {
    this.routerSub = this.router.events.pipe(
      filter(event => event instanceof NavigationStart)
    ).subscribe(() => {
      if (this.isOpen()) this.isOpen.set(false);
    });
  }

  // Format markdown-like text to HTML
  formatText(text: string): string {
    return text
      .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
      .replace(/\*(.*?)\*/g, '<em>$1</em>')
      .replace(/`(.*?)`/g, '<code>$1</code>')
      .replace(/\n/g, '<br>');
  }
}
```

### ChatbotService

```typescript
send(history: ChatMessage[], userMessage: string, systemPrompt: string): Observable<string> {
  const messages = [
    { role: 'system', content: systemPrompt },
    ...history.slice(-6),   // only last 6 messages to stay within token limits
    { role: 'user', content: userMessage }
  ];

  return this.http.post<GroqResponse>(this.apiUrl, {
    model: 'llama-3.1-8b-instant',
    messages,
    max_tokens: 512,
    temperature: 0.7
  }, { headers }).pipe(
    map(res => res.choices?.[0]?.message?.content ?? 'Sorry, try again.')
  );
}
```

**Note:** The loading interceptor skips external URLs (Groq API), so the global spinner does NOT show during chatbot calls. This is intentional — the chatbot has its own loading indicator.

---

## 18. Payment Integration (Razorpay)

Razorpay is loaded dynamically from a CDN script:

```typescript
private loadRazorpay() {
  if (typeof Razorpay !== 'undefined') return;  // already loaded
  const script = document.createElement('script');
  script.src = 'https://checkout.razorpay.com/v1/checkout.js';
  script.async = true;
  document.head.appendChild(script);
}
```

### Payment Flow

```
1. Guest creates reservation → backend returns reservationId + finalAmount
2. Guest clicks "Pay with UPI/Card"
3. Razorpay popup opens
4. Guest completes payment in Razorpay
5. Razorpay calls handler() on success
6. Frontend calls backend: POST /transactions { reservationId, paymentMethod }
7. Backend records payment → reservation status becomes Confirmed
8. Frontend navigates to booking detail page
```

### Razorpay Options

```typescript
const options: any = {
  key: environment.razorpayKeyId,
  amount: Math.round(res.finalAmount * 100),  // Razorpay uses paise (1 rupee = 100 paise)
  currency: 'INR',
  name: '🏨 Thanush StayHub',
  description: `Booking: ${res.reservationCode}`,
  theme: { color: '#2d3a8c' },

  // Called when payment succeeds
  handler: (response: any) => {
    this.transactionService.createPayment({
      reservationId: res.reservationId,
      paymentMethod: paymentMethodId,  // 1=Credit, 2=Debit, 3=UPI, 4=NetBanking
    }).subscribe({
      next: () => this.router.navigate(['/booking', res.reservationCode])
    });
  },

  // Called when user closes the popup
  modal: {
    ondismiss: () => {
      this.bookingService.recordFailedPayment(res.reservationId).subscribe();
      this.toast.error('Payment cancelled. Retry from My Bookings.');
    }
  }
};

const rzp = new Razorpay(options);
rzp.open();
```

### Wallet Top-Up

The same Razorpay flow is used for wallet top-ups in `GuestWalletComponent` and `BookingCreateComponent`.

### Wallet-Only Payment

If the wallet balance covers the full amount, Razorpay is skipped entirely:

```typescript
if (this.walletCoversAll() || paymentMethodId === 5) {
  // Wallet already deducted at reservation creation
  // Just record the transaction
  this.transactionService.createPayment({
    reservationId: res.reservationId,
    paymentMethod: 5,  // Wallet
  }).subscribe(...);
  return;
}
```

---

## 19. PDF Generation (jsPDF)

`BookingDetailComponent` can generate a PDF booking confirmation using `jsPDF`.

The library is imported **dynamically** (lazy loaded) so it doesn't increase the initial bundle size:

```typescript
async downloadPdf() {
  const { default: jsPDF } = await import('jspdf');  // lazy import
  const doc = new jsPDF({ unit: 'mm', format: 'a4' });
  const W = 210, margin = 18;

  // Header band
  doc.setFillColor(45, 58, 140);
  doc.rect(0, 0, W, 36, 'F');
  doc.setTextColor(255, 255, 255);
  doc.setFontSize(22);
  doc.text('Thanush StayHub', margin, 16);

  // Status badge
  doc.setFillColor(46, 125, 50);  // green for Confirmed
  doc.roundedRect(W - margin - 32, 8, 32, 10, 2, 2, 'F');
  doc.text('CONFIRMED', W - margin - 16, 14.5, { align: 'center' });

  // Price breakdown
  doc.text('Base Amount', col1, y);
  doc.text(`Rs. ${res.totalAmount.toFixed(2)}`, W - margin, y, { align: 'right' });

  // Save the file
  doc.save(`ThanushStayHub-Booking-${res.reservationCode}.pdf`);
  this.toast.success('PDF downloaded!');
}
```

---

## 20. Frontend Testing — Karma & Jasmine

### What is Karma?

**Karma** is the test runner. It opens a real Chrome browser, runs your tests inside it, and reports the results. It's configured in `karma.conf.js`.

### What is Jasmine?

**Jasmine** is the testing framework. It provides the functions you use to write tests:

| Function | Purpose |
|----------|---------|
| `describe('name', () => {})` | Groups related tests together |
| `it('should ...', () => {})` | A single test case |
| `beforeEach(() => {})` | Runs before every test in the group |
| `expect(value)` | Makes an assertion |
| `jasmine.createSpyObj()` | Creates a fake (mock) service |

### How to Run Tests

```bash
# Run tests once (no watch mode)
ng test --watch=false

# Run with coverage report
ng test --code-coverage

# Run in watch mode (re-runs on file changes)
ng test
```

### Test Structure — The Pattern Used in This Project

Every spec file follows the same pattern:

```typescript
// 1. Import what you need
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MyComponent } from './my.component';
import { MyService } from '../services/my.service';

// 2. Create mock data
const MOCK_DATA = { id: '1', name: 'Test' };

describe('MyComponent', () => {
  let component: MyComponent;
  let fixture: ComponentFixture<MyComponent>;
  let serviceSpy: jasmine.SpyObj<MyService>;

  // 3. Set up before each test
  beforeEach(async () => {
    // Create a spy (fake) service
    serviceSpy = jasmine.createSpyObj('MyService', ['getData', 'saveData']);
    serviceSpy.getData.and.returnValue(of(MOCK_DATA));  // fake success response

    await TestBed.configureTestingModule({
      imports: [MyComponent],  // standalone component
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: MyService, useValue: serviceSpy },  // inject the spy
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();  // triggers ngOnInit
  });

  // 4. Write tests
  it('should create', () => expect(component).toBeTruthy());

  it('should load data on init', () => {
    expect(serviceSpy.getData).toHaveBeenCalled();
    expect(component.data()).toEqual(MOCK_DATA);
  });
});
```

### Real Example: AdminDashboardComponent Tests

```typescript
describe('AdminDashboardComponent', () => {
  // ── Creation ──────────────────────────────────────────────────────────────
  it('should create', () => expect(component).toBeTruthy());

  // ── ngOnInit ──────────────────────────────────────────────────────────────
  it('ngOnInit — should call getAdminDashboard', () => {
    expect(dashboardSpy.getAdminDashboard).toHaveBeenCalledOnceWith();
  });

  it('ngOnInit — should populate data signal', () => {
    expect(component.data()?.hotelName).toBe('Grand Palace');
    expect(component.data()?.totalRevenue).toBe(600000);
  });

  // ── toggleHotelStatus ─────────────────────────────────────────────────────
  it('toggleHotelStatus — should call toggleHotelStatus(false) when hotel is active', () => {
    hotelSpy.toggleHotelStatus.and.returnValue(of(undefined));
    component.toggleHotelStatus();
    expect(hotelSpy.toggleHotelStatus).toHaveBeenCalledOnceWith(false);
  });

  it('toggleHotelStatus — should show "Hotel deactivated." toast', () => {
    hotelSpy.toggleHotelStatus.and.returnValue(of(undefined));
    component.toggleHotelStatus();
    expect(toastSpy.success).toHaveBeenCalledOnceWith('Hotel deactivated.');
  });

  it('toggleHotelStatus — should reset isTogglingStatus to false on error', () => {
    hotelSpy.toggleHotelStatus.and.returnValue(throwError(() => new Error('fail')));
    component.toggleHotelStatus();
    expect(component.isTogglingStatus()).toBeFalse();
  });

  it('toggleHotelStatus — should do nothing when data is null', () => {
    component.data.set(null);
    component.toggleHotelStatus();
    expect(hotelSpy.toggleHotelStatus).not.toHaveBeenCalled();
  });

  // ── Template ──────────────────────────────────────────────────────────────
  it('should render hotel name in template', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Grand Palace');
  });
});
```

### Real Example: HotelManagementComponent Tests

```typescript
// Testing form validation
it('form — should be invalid when contactNumber is not 10 digits', () => {
  component.form.get('contactNumber')?.setValue('12345');
  expect(component.form.get('contactNumber')?.invalid).toBeTrue();
});

it('gstForm — should be invalid when gstPercent > 28', () => {
  component.gstForm.get('gstPercent')?.setValue(30);
  expect(component.gstForm.invalid).toBeTrue();
});

// Testing save() method
it('save — should call updateHotel with form values', () => {
  hotelSpy.updateHotel.and.returnValue(of(undefined));
  component.save();
  expect(hotelSpy.updateHotel).toHaveBeenCalledOnceWith(
    jasmine.objectContaining({ name: 'Grand Palace', contactNumber: '9840650390' })
  );
});

it('save — should NOT call updateHotel when form is invalid', () => {
  component.form.get('name')?.setValue('');
  component.save();
  expect(hotelSpy.updateHotel).not.toHaveBeenCalled();
});

it('save — should mark all fields touched when form is invalid', () => {
  component.form.get('name')?.setValue('');
  component.save();
  expect(component.form.get('name')?.touched).toBeTrue();
});
```

### Real Example: AdminReviewsComponent Tests

```typescript
// Testing reply functionality
it('startReply — should set replyingId', () => {
  component.startReply('rev-001');
  expect(component.replyingId()).toBe('rev-001');
});

it('startReply — should patch replyForm with existing reply', () => {
  component.startReply('rev-001', 'Thank you!');
  expect(component.replyForm.get('adminReply')?.value).toBe('Thank you!');
});

it('submitReply — should update reviews signal with new reply', () => {
  component.reviews.set(MOCK_REVIEWS as any);
  component.startReply('rev-001');
  component.replyForm.patchValue({ adminReply: 'Great!' });
  component.submitReply();
  const updated = component.reviews().find(r => r.reviewId === 'rev-001');
  expect(updated?.adminReply).toBe('Great!');
});

// Testing filter/sort
it('onRatingFilter — should toggle off when same rating clicked twice', () => {
  component.onRatingFilter(5);
  component.onRatingFilter(5);
  expect(component.ratingFilter).toBe(0);
});
```

### Real Example: RoomTypeManagementComponent Tests

```typescript
// Testing rate management
it('toggleRates — should load rates for room type', () => {
  component.toggleRates('rt-001');
  expect(roomTypeSpy.getRates).toHaveBeenCalledWith('rt-001');
});

it('toggleRates — should collapse when called twice on same id', () => {
  component.toggleRates('rt-001');
  component.toggleRates('rt-001');
  expect(component.expandedRateId()).toBeNull();
});

it('getRatesFor — should return empty array for unknown id', () => {
  expect(component.getRatesFor('unknown')).toEqual([]);
});

// Testing amenity request
it('submitAmenityRequest — should NOT call service when form is invalid', () => {
  component.submitAmenityRequest();
  expect(amenityReqSpy.create).not.toHaveBeenCalled();
});
```

### Key Testing Concepts

**jasmine.createSpyObj** — Creates a fake service so you don't need a real backend:

```typescript
// Creates a fake HotelService with these methods
hotelSpy = jasmine.createSpyObj('HotelService', ['getHotelDetails', 'updateHotel']);

// Make it return fake data
hotelSpy.getHotelDetails.and.returnValue(of(MOCK_HOTEL));

// Make it return an error
hotelSpy.updateHotel.and.returnValue(throwError(() => new Error('fail')));
```

**of()** — Creates an Observable that immediately emits a value (simulates a successful API call):

```typescript
import { of } from 'rxjs';
serviceSpy.getData.and.returnValue(of({ id: '1', name: 'Test' }));
```

**throwError()** — Creates an Observable that immediately throws an error (simulates a failed API call):

```typescript
import { throwError } from 'rxjs';
serviceSpy.getData.and.returnValue(throwError(() => new Error('Network error')));
```

**fixture.detectChanges()** — Triggers Angular's change detection (like pressing refresh):

```typescript
fixture.detectChanges();  // triggers ngOnInit and updates the template
```

**fixture.nativeElement** — The actual DOM element of the component:

```typescript
expect(fixture.nativeElement.textContent).toContain('Grand Palace');
```

**jasmine.objectContaining()** — Checks that an object has at least these properties:

```typescript
expect(spy.method).toHaveBeenCalledWith(
  jasmine.objectContaining({ name: 'Grand Palace' })
  // passes even if the object has more properties
);
```

### What Tests Are Written For

The project has spec files for these components:

| Spec File | Tests |
|-----------|-------|
| `admin-dashboard.component.spec.ts` | Dashboard load, toggle hotel status, download report |
| `hotel-management.component.spec.ts` | Form pre-fill, validation, save hotel, save GST |
| `admin-reviews.component.spec.ts` | Load reviews, start/cancel reply, submit reply, filter, sort |
| `roomtype-management.component.spec.ts` | CRUD room types, rate management, amenity requests |
| `room-management.component.spec.ts` | Add/edit/toggle rooms, occupancy |
| `inventory-management.component.spec.ts` | Add/view/edit inventory |
| `audit-logs.component.spec.ts` | Load logs, search, filter by date |

### Test Categories Used

Each spec file tests these categories in order:

1. **Creation** — `it('should create', ...)` — basic sanity check
2. **Initial state** — signals start with correct default values
3. **ngOnInit** — correct API calls made on startup, data loaded into signals
4. **Form validation** — invalid inputs are caught, valid inputs pass
5. **Success paths** — correct API called, toast shown, signal updated
6. **Error paths** — loading/saving signals reset to false on error
7. **Guard conditions** — nothing happens when data is null or form is invalid
8. **Template** — correct data rendered in the HTML

---

## Summary

This Angular 18 project uses:

- **Standalone components** — no NgModules, self-contained
- **Signals** — reactive state that auto-updates the UI
- **Lazy loading** — routes and dialogs loaded on demand
- **Role-based guards** — protect routes by user role
- **HTTP interceptors** — auto-attach JWT, show spinner, handle errors
- **Angular Material** — tables, forms, dialogs, tabs, steppers
- **Bootstrap** — grid and spacing utilities
- **Razorpay** — payment gateway for bookings and wallet top-ups
- **Groq AI** — chatbot with role-specific context
- **jsPDF** — booking confirmation PDF generation
- **Karma + Jasmine** — unit tests with spy objects and mock data
- **Dark mode** — CSS variable overrides with localStorage persistence
- **RxJS** — debounceTime, distinctUntilChanged, computed observables
- **JWT decode** — read user info from token without extra API call
