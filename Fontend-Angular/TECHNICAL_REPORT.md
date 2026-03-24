# ThanushStayHub Hotel Booking — Angular Frontend Technical Report

## 1. Project Overview

ThanushStayHub is a production-grade Angular 18 single-page application (SPA) for a hotel booking platform.
It consumes a .NET Web API backend and supports three user roles: Guest, Hotel Admin, and SuperAdmin.

---

## 2. Angular Features Used & Why

### 2.1 Standalone Components
**What:** Components without NgModules — each component declares its own imports.
**Why:** Angular 18's recommended approach. Smaller bundle size, simpler dependency graph, no shared module boilerplate.
**Where:** Every single component in this project uses `standalone: true`.

### 2.2 Signals (`signal`, `computed`, `effect`)
**What:** Angular's new reactive primitive — a value that notifies consumers when it changes.
**Why:** Replaces BehaviorSubject/async pipe pattern. Simpler, more explicit, no subscription leaks.
**Example:**
```typescript
// Instead of: private subject = new BehaviorSubject(null); data$ = subject.asObservable();
data = signal<HotelListItemDto[]>([]);               // write
readonly safeData = data.asReadonly();                // expose read-only
computed = computed(() => data().filter(h => h.isActive));  // derived value
```
**Where used:** Every component uses signals for local state (`isLoading`, `data`, `page`, etc.)

### 2.3 New Control Flow (`@if`, `@for`, `@else`)
**What:** Angular 18's built-in control flow syntax replacing `*ngIf` and `*ngFor` directives.
**Why:** Better performance (compiler-optimized), no import of `NgIf`/`NgFor`, cleaner templates.
**Example:**
```html
<!-- Old: <div *ngIf="data; else loading"> -->
@if (data(); as d) {
  <div>{{ d.name }}</div>
} @else {
  <div>Loading…</div>
}
@for (item of items(); track item.id) {
  <app-card [hotel]="item" />
}
```
**Where:** All HTML templates — no `*ngIf` or `*ngFor` used anywhere.

### 2.4 `inject()` Function
**What:** Inject dependencies inside class body without constructor parameters.
**Why:** Cleaner than constructor injection, works in any injection context.
**Example:**
```typescript
export class HotelListComponent {
  private hotelService = inject(HotelService);  // no constructor needed
  private router       = inject(Router);
}
```
**Where:** Every component and service in this project.

### 2.5 Lazy Loading with `loadComponent` / `loadChildren`
**What:** Routes load components only when navigated to — not at startup.
**Why:** Dramatically reduces initial bundle size. Users only download code for pages they visit.
**Example:**
```typescript
{
  path: 'admin',
  canActivate: [adminGuard],
  loadChildren: () => import('./features/admin/admin.routes').then(m => m.ADMIN_ROUTES),
}
```
**Where:** `app.routes.ts` — every feature is lazy-loaded.

### 2.6 Functional Guards (`CanActivateFn`)
**What:** Route guards as plain functions instead of injectable classes.
**Why:** Simpler, no class boilerplate, still supports `inject()`.
**Example:**
```typescript
export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  if (auth.isAdmin()) return true;
  inject(Router).navigate(['/auth/login']);
  return false;
};
```
**Where:** `core/guards/auth.guard.ts` — 5 guards: authGuard, guestGuard, adminGuard, superAdminGuard, publicGuard.

### 2.7 Functional HTTP Interceptors (`HttpInterceptorFn`)
**What:** HTTP middleware as functions instead of classes.
**Why:** Can use `inject()` directly, no class needed.
**Where:**
- `auth.interceptor.ts` — Attaches `Authorization: Bearer <token>` to every request, handles 401/403 globally.
- `loading.interceptor.ts` — Shows/hides global spinner using a request counter.

### 2.8 Reactive Forms (`ReactiveFormsModule`)
**What:** Forms managed programmatically via `FormGroup` and `FormControl`.
**Why:** Type-safe, testable, works with complex validation, enables dynamic forms.
**Example:**
```typescript
form = this.fb.group({
  email:    ['', [Validators.required, Validators.email]],
  password: ['', [Validators.required, Validators.minLength(6)]],
});
```
**Where:** All forms — login, register, booking, hotel management, inventory, etc.

### 2.9 `toSignal` (from `@angular/core/rxjs-interop`)
**What:** Converts an Observable to a Signal.
**Why:** Bridges the RxJS Observable world with Angular's new Signal system.
**Where:** `app.component.ts` — converts Router NavigationEnd events to a signal for tracking current URL.

### 2.10 View Transitions API (`withViewTransitions`)
**What:** Smooth CSS transitions between route changes.
**Why:** Better UX with zero extra code — built into Angular router.
**Where:** `app.config.ts` → `provideRouter(routes, withViewTransitions())`.

### 2.11 Angular Material (`@angular/material`)
**What:** Google's Material Design component library.
**Why:** Production-quality, accessible, consistent UI components out of the box.
**Components used:**
| Component | Used For |
|---|---|
| `MatButtonModule` | All buttons |
| `MatFormFieldModule` + `MatInputModule` | All form inputs |
| `MatSelectModule` | Dropdown selects |
| `MatDatepickerModule` | Date selection (booking, inventory, rates) |
| `MatNativeDateModule` | Native date adapter for datepicker |
| `MatStepperModule` | Multi-step booking and register flows |
| `MatRadioModule` | Payment method selection |
| `MatTabsModule` | Hotel details tabs (Overview/Rooms/Reviews) |
| `MatMenuModule` | User dropdown in navbar |
| `MatIconModule` | All icons (Material Icons font) |
| `MatSnackBarModule` | Toast notifications |
| `MatDividerModule` | Visual separators |
| `MatTooltipModule` | Hover tooltips on icon buttons |
| `MatProgressSpinnerModule` | Loading spinners |
| `MatExpansionModule` | Expandable error log entries |

### 2.12 `provideAnimationsAsync`
**What:** Loads Angular animations lazily.
**Why:** Reduces initial bundle; animations load only when first needed.
**Where:** `app.config.ts`.

---

## 3. Architecture Patterns

### 3.1 Feature-Based Folder Structure
```
src/app/
├── core/        → Shared infrastructure (services, guards, interceptors, models)
├── shared/      → Reusable UI components (navbar, footer, spinner)
└── features/    → One folder per domain (auth, hotel, booking, guest, admin, superadmin, contact)
```
**Why:** Each feature is self-contained. Adding/removing a feature doesn't touch other features.

### 3.2 Smart vs Dumb Components
- **Smart (Container) components** — inject services, manage state signals, pass data down.
  Example: `HotelListComponent` fetches hotels, passes each to `HotelCardComponent`.
- **Dumb (Presentational) components** — receive `@Input()`, emit `@Output()`, no service injection.
  Example: `HotelCardComponent` receives a `HotelListItemDto` and renders it.

### 3.3 Service Layer
Every API domain has a dedicated service:
| Service | File | Responsibility |
|---|---|---|
| `AuthService` | `auth.service.ts` | Login, register, JWT decode, token storage, role signals |
| `HotelService` | `hotel.service.ts` | Public search, details, admin hotel edit, SA block/unblock |
| `BookingService` | `booking.service.ts` | Create/cancel/list reservations, admin complete |
| `TransactionService` | `api.services.ts` | Payment, direct refund, transaction history |
| `ReviewService` | `api.services.ts` | Add/edit/delete reviews, hotel reviews |
| `RefundService` | `api.services.ts` | Guest refunds, admin approve/reject |
| `UserService` | `api.services.ts` | Profile get/update, booking history |
| `DashboardService` | `api.services.ts` | Role-specific dashboard stats |
| `AuditLogService` | `api.services.ts` | Admin and SuperAdmin audit logs |
| `LogService` | `api.services.ts` | SuperAdmin system error logs |
| `RoomTypeService` | `api.services.ts` | Room type CRUD, rates |
| `RoomService` | `api.services.ts` | Physical room CRUD |
| `InventoryService` | `api.services.ts` | Per-day inventory management |
| `LoadingService` | `loading.service.ts` | Global spinner (request counter) |
| `ToastService` | `toast.service.ts` | Snackbar notifications |

---

## 4. All Routes — Complete Map

### Public (No Auth)
| Route | Component | API Used |
|---|---|---|
| `/` | → `/hotels` | — |
| `/hotels` | `HotelListComponent` | `GET /public/hotels/top`, `GET /public/hotels/cities`, `POST /public/hotels/search` |
| `/hotels/:id` | `HotelDetailsComponent` | `GET /public/hotels/{id}/full-details`, `GET /public/hotels/{id}/availability` |
| `/auth/login` | `LoginComponent` | `POST /auth/login` |
| `/auth/register` | `RegisterComponent` | `POST /auth/register-guest` |
| `/auth/register-admin` | `RegisterAdminComponent` | `POST /auth/register-hotel-admin` |
| `/contact` | `ContactComponent` | (static page) |

### Guest (Role: Guest)
| Route | Component | API Used |
|---|---|---|
| `/guest/dashboard` | `GuestDashboardComponent` | `GET /dashboard/guest`, `GET /user-profile` |
| `/guest/bookings` | `BookingListComponent` | `GET /guest/reservations` |
| `/guest/profile` | `GuestProfileComponent` | `GET /user-profile`, `PUT /user-profile` |
| `/guest/reviews` | `GuestReviewsComponent` | `GET /reviews/my-reviews`, `POST /reviews`, `PUT /reviews/{id}`, `DELETE /reviews/{id}`, `GET /guest/reservations` |
| `/guest/refunds` | `GuestRefundsComponent` | `GET /guest/refund-requests` |
| `/guest/transactions` | `GuestTransactionsComponent` | `GET /transactions`, `POST /transactions/{id}/refund` |
| `/booking/create` | `BookingCreateComponent` | `GET /public/hotels/{id}/availability`, `GET /guest/reservations/available-rooms`, `POST /guest/reservations`, `POST /transactions` |
| `/booking/:code` | `BookingDetailComponent` | `GET /guest/reservations/{code}`, `PATCH /guest/reservations/{code}/cancel` |
| `/booking/list` | `BookingListComponent` | `GET /guest/reservations` |

### Admin (Role: Admin)
| Route | Component | API Used |
|---|---|---|
| `/admin/dashboard` | `AdminDashboardComponent` | `GET /dashboard/admin`, `GET /user-profile`, `PATCH /admin/hotels/status` |
| `/admin/hotel` | `HotelManagementComponent` | `GET /public/hotels/{id}/full-details`, `PUT /admin/hotels` |
| `/admin/rooms` | `RoomManagementComponent` | `GET /admin/rooms`, `POST /admin/rooms`, `PUT /admin/rooms`, `PATCH /admin/rooms/{id}/status` |
| `/admin/roomtypes` | `RoomTypeManagementComponent` | `GET /admin/roomtypes`, `POST /admin/roomtypes`, `PUT /admin/roomtypes`, `PATCH .../status`, `POST /admin/roomtypes/rate` |
| `/admin/inventory` | `InventoryManagementComponent` | `GET /admin/inventory`, `POST /admin/inventory`, `PUT /admin/inventory` |
| `/admin/reservations` | `ReservationManagementComponent` | `GET /admin/reservations`, `PATCH /admin/reservations/{code}/complete` |
| `/admin/refunds` | `RefundManagementComponent` | `GET /admin/refund-requests`, `POST .../approve`, `POST .../reject` |
| `/admin/audit-logs` | `AuditLogsComponent` (mode=admin) | `GET /admin/audit-logs` |

### SuperAdmin (Role: SuperAdmin)
| Route | Component | API Used |
|---|---|---|
| `/superadmin/dashboard` | `SuperAdminDashboardComponent` | `GET /dashboard/superadmin` |
| `/superadmin/hotels` | `HotelControlComponent` | `GET /superadmin/hotels`, `PATCH .../block`, `PATCH .../unblock` |
| `/superadmin/audit-logs` | `AuditLogsComponent` (mode=superadmin) | `GET /superadmin/audit-logs` |
| `/superadmin/error-logs` | `ErrorLogsComponent` | `GET /logs` |

---

## 5. API Coverage — All 60 Backend Endpoints

| # | Method | Endpoint | Service Method | UI Location |
|---|---|---|---|---|
| 1 | POST | `/api/auth/register-guest` | `registerGuest()` | `/auth/register` |
| 2 | POST | `/api/auth/register-hotel-admin` | `registerHotelAdmin()` | `/auth/register-admin` |
| 3 | POST | `/api/auth/login` | `login()` | `/auth/login` |
| 4 | GET | `/api/public/hotels/top` | `getTopHotels()` | `/hotels` (default) |
| 5 | GET | `/api/public/hotels/cities` | `getCities()` | `/hotels` search dropdown |
| 6 | GET | `/api/public/hotels/by-city` | `getHotelsByCity()` | Available in service |
| 7 | POST | `/api/public/hotels/search` | `searchHotels()` | `/hotels` search form |
| 8 | GET | `/api/public/hotels/{id}` | `getHotelDetails()` | `/hotels/:id` |
| 9 | GET | `/api/public/hotels/{id}/full-details` | `getHotelDetails()` | `/hotels/:id`, `/admin/hotel` |
| 10 | GET | `/api/public/hotels/{id}/roomtypes` | `getRoomTypes()` | Available in service |
| 11 | GET | `/api/public/hotels/{id}/availability` | `getAvailability()` | `/hotels/:id` sidebar, `/booking/create` |
| 12 | GET | `/api/dashboard/admin` | `getAdminDashboard()` | `/admin/dashboard` |
| 13 | GET | `/api/dashboard/guest` | `getGuestDashboard()` | `/guest/dashboard` |
| 14 | GET | `/api/dashboard/superadmin` | `getSuperAdminDashboard()` | `/superadmin/dashboard` |
| 15 | POST | `/api/guest/reservations` | `createReservation()` | `/booking/create` Step 1 |
| 16 | GET | `/api/guest/reservations` | `getMyReservations()` | `/guest/bookings`, `/guest/reviews` |
| 17 | GET | `/api/guest/reservations/history` | `getMyReservationsHistory()` | Available in service |
| 18 | GET | `/api/guest/reservations/{code}` | `getReservationByCode()` | `/booking/:code` |
| 19 | PATCH | `/api/guest/reservations/{code}/cancel` | `cancelReservation()` | `/booking/:code` cancel form |
| 20 | GET | `/api/guest/reservations/available-rooms` | `getAvailableRooms()` | `/booking/create` |
| 21 | GET | `/api/guest/refund-requests` | `getGuestRefundRequests()` | `/guest/refunds` |
| 22 | POST | `/api/transactions` | `createPayment()` | `/booking/create` Step 2 |
| 23 | POST | `/api/transactions/{id}/refund` | `directRefund()` | `/guest/transactions` |
| 24 | GET | `/api/transactions` | `getTransactions()` | `/guest/transactions`, admin, superadmin |
| 25 | POST | `/api/reviews` | `addReview()` | `/guest/reviews` add form |
| 26 | PUT | `/api/reviews/{id}` | `updateReview()` | `/guest/reviews` edit form |
| 27 | DELETE | `/api/reviews/{id}` | `deleteReview()` | `/guest/reviews` delete button |
| 28 | POST | `/api/reviews/hotel` | `getHotelReviews()` | `/hotels/:id` Reviews tab |
| 29 | GET | `/api/reviews/my-reviews` | `getMyReviews()` | `/guest/reviews` |
| 30 | GET | `/api/user-profile` | `getProfile()` | `/guest/profile`, dashboards |
| 31 | PUT | `/api/user-profile` | `updateProfile()` | `/guest/profile` edit form |
| 32 | POST | `/api/user-profile/booking-history` | `getBookingHistory()` | Available in service |
| 33 | PUT | `/api/admin/hotels` | `updateHotel()` | `/admin/hotel` |
| 34 | PATCH | `/api/admin/hotels/status` | `toggleHotelStatus()` | `/admin/dashboard` |
| 35 | GET | `/api/admin/roomtypes` | `getRoomTypes()` | `/admin/roomtypes` |
| 36 | POST | `/api/admin/roomtypes` | `addRoomType()` | `/admin/roomtypes` add form |
| 37 | PUT | `/api/admin/roomtypes` | `updateRoomType()` | `/admin/roomtypes` edit inline |
| 38 | PATCH | `/api/admin/roomtypes/{id}/status` | `toggleRoomTypeStatus()` | `/admin/roomtypes` toggle |
| 39 | POST | `/api/admin/roomtypes/rate` | `addRate()` | `/admin/roomtypes` set pricing |
| 40 | PUT | `/api/admin/roomtypes/rate` | `updateRate()` | Available in service |
| 41 | POST | `/api/admin/roomtypes/rate-by-date` | `getRateByDate()` | Available in service |
| 42 | GET | `/api/admin/rooms` | `getRooms()` | `/admin/rooms` |
| 43 | POST | `/api/admin/rooms` | `addRoom()` | `/admin/rooms` add form |
| 44 | PUT | `/api/admin/rooms` | `updateRoom()` | `/admin/rooms` edit inline |
| 45 | PATCH | `/api/admin/rooms/{id}/status` | `toggleRoomStatus()` | `/admin/rooms` toggle |
| 46 | GET | `/api/admin/inventory` | `getInventory()` | `/admin/inventory` |
| 47 | POST | `/api/admin/inventory` | `addInventory()` | `/admin/inventory` set form |
| 48 | PUT | `/api/admin/inventory` | `updateInventory()` | `/admin/inventory` edit inline |
| 49 | GET | `/api/admin/reservations` | `getHotelReservations()` | `/admin/reservations` |
| 50 | PATCH | `/api/admin/reservations/{code}/complete` | `completeReservation()` | `/admin/reservations` Complete btn |
| 51 | GET | `/api/admin/refund-requests` | `getHotelRefundRequests()` | `/admin/refunds` |
| 52 | POST | `/api/admin/refund-requests/{id}/approve` | `approveRefund()` | `/admin/refunds` Approve btn |
| 53 | POST | `/api/admin/refund-requests/{id}/reject` | `rejectRefund()` | `/admin/refunds` Reject btn |
| 54 | GET | `/api/admin/audit-logs` | `getAdminAuditLogs()` | `/admin/audit-logs` |
| 55 | GET | `/api/superadmin/hotels` | `getAllHotelsForSuperAdmin()` | `/superadmin/hotels` |
| 56 | PATCH | `/api/superadmin/hotels/{id}/block` | `blockHotel()` | `/superadmin/hotels` Block btn |
| 57 | PATCH | `/api/superadmin/hotels/{id}/unblock` | `unblockHotel()` | `/superadmin/hotels` Unblock btn |
| 58 | GET | `/api/superadmin/audit-logs` | `getAllAuditLogs()` | `/superadmin/audit-logs` |
| 59 | GET | `/api/logs/my-logs` | `getMyLogs()` | Available in service |
| 60 | GET | `/api/logs` | `getAllLogs()` | `/superadmin/error-logs` |

**Coverage: 60/60 (100%)**

---

## 6. Security Implementation

### JWT Flow
1. User logs in → API returns `{ token: "eyJ…" }`
2. `AuthService.setToken()` decodes it with `jwtDecode<JwtPayload>(token)`
3. Claims extracted: `nameid` (UserId), `unique_name` (UserName), `role`, `HotelId`
4. Stored in `localStorage` under key `hotel_token`
5. On every HTTP request, `authInterceptor` attaches `Authorization: Bearer <token>`
6. On app startup, token is read from storage, checked for expiry (`payload.exp * 1000 > Date.now()`)

### Role-Based Guards
```
/auth/*         → publicGuard   (redirects logged-in users to their dashboard)
/guest/*        → guestGuard    (Role must be 'Guest')
/admin/*        → adminGuard    (Role must be 'Admin')
/superadmin/*   → superAdminGuard (Role must be 'SuperAdmin')
```

---

## 7. Component Quick Reference

| Component | File | Purpose |
|---|---|---|
| `AppComponent` | `app.component.ts` | Root shell; hides navbar on auth pages |
| `NavbarComponent` | `shared/components/navbar/` | Responsive nav; role-aware links; user dropdown |
| `FooterComponent` | `shared/components/footer/` | Site footer with links |
| `SpinnerComponent` | `shared/components/spinner/` | Full-page loading overlay |
| `LoginComponent` | `features/auth/login/` | Email + password login form |
| `RegisterComponent` | `features/auth/register/` | Guest registration form |
| `RegisterAdminComponent` | `features/auth/register/` | Hotel + admin 2-step stepper registration |
| `HotelListComponent` | `features/hotel/hotel-list/` | Public hotel search with hero and grid |
| `HotelCardComponent` | `features/hotel/hotel-card/` | Reusable hotel card (dumb component) |
| `HotelDetailsComponent` | `features/hotel/hotel-details/` | Full hotel page with tabs, availability sidebar |
| `BookingCreateComponent` | `features/booking/booking-create/` | 2-step booking flow (room → payment) |
| `BookingListComponent` | `features/booking/booking-list/` | All bookings with status filter |
| `BookingDetailComponent` | `features/booking/booking-detail/` | Single booking with cancel form |
| `GuestDashboardComponent` | `features/guest/dashboard/` | Guest stats + quick actions |
| `GuestProfileComponent` | `features/guest/profile/` | View and edit user profile |
| `GuestReviewsComponent` | `features/guest/reviews/` | Add/edit/delete hotel reviews |
| `GuestRefundsComponent` | `features/guest/refund-requests/` | View refund request status |
| `GuestTransactionsComponent` | `features/guest/transactions/` | Payment history + 30-min direct refund |
| `AdminDashboardComponent` | `features/admin/dashboard/` | Hotel KPIs, reservation status bars |
| `HotelManagementComponent` | `features/admin/hotel-management/` | Edit hotel info (pre-filled) |
| `RoomManagementComponent` | `features/admin/room-management/` | CRUD physical rooms |
| `RoomTypeManagementComponent` | `features/admin/room-management/` | Room categories + datepicker rate form |
| `InventoryManagementComponent` | `features/admin/inventory-management/` | Per-day inventory with datepicker |
| `ReservationManagementComponent` | `features/admin/reservation-management/` | Hotel reservation list + complete action |
| `RefundManagementComponent` | `features/admin/refund-management/` | Approve/reject guest refund requests |
| `AuditLogsComponent` | `features/admin/audit-logs/` | Reused for both Admin and SuperAdmin audit logs |
| `SuperAdminDashboardComponent` | `features/superadmin/dashboard/` | Platform-wide stats |
| `HotelControlComponent` | `features/superadmin/hotel-control/` | Block/unblock hotels |
| `ErrorLogsComponent` | `features/superadmin/error-logs/` | System error log viewer with stack traces |
| `ContactComponent` | `features/contact/` | Support page with email and phone |
| `NotFoundComponent` | `features/not-found/` | 404 page |

---

## 8. Key Design Decisions

### Why `computed()` for derived values?
`computed()` creates a value that automatically recalculates when its signal dependencies change. Used for `estimatedTotal`, `totalNights`, `isAuthenticated`, `isAdmin`, etc. — no manual subscriptions needed.

### Why `finalize()` in loading interceptor?
`finalize()` runs whether the Observable completes OR errors. This guarantees the spinner always hides, even on API failures. Combined with a request counter (not just a boolean), concurrent requests work correctly.

### Why deduplication of availability results?
The backend returns one inventory record **per day** for each room type. A 3-night stay returns 3 records per room type. The frontend deduplicates by `roomTypeId`, keeping the minimum available rooms across all days — giving the most conservative (accurate) count.

### Why `@Input()` with `ActivatedRoute` fallback in `AuditLogsComponent`?
The same component is used for both Admin (`/admin/audit-logs`) and SuperAdmin (`/superadmin/audit-logs`). It reads `mode` from route data (`data: { mode: 'superadmin' }`), defaulting to `'admin'`.

---

## 9. npm Dependencies

| Package | Version | Purpose |
|---|---|---|
| `@angular/core` | ^18.2.0 | Core Angular framework |
| `@angular/material` | ^18.2.0 | UI component library |
| `@angular/cdk` | ^18.2.0 | Material component dev kit |
| `@angular/forms` | ^18.2.0 | Reactive and template-driven forms |
| `@angular/router` | ^18.2.0 | Client-side routing |
| `@angular/animations` | ^18.2.0 | Component animations |
| `jwt-decode` | ^4.0.0 | Decode JWT tokens without library overhead |
| `rxjs` | ~7.8.0 | Reactive extensions (Observables) |
| `zone.js` | ~0.14.10 | Angular change detection |

---

*ThanushStayHub Hotel Booking System — Angular 18 Frontend*
