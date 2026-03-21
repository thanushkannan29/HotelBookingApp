# 🏨 Thanush — Hotel Booking Frontend

Angular 18 frontend for the Hotel Booking System REST API.

---

## ⚡ Quick Setup (3 steps)

### Step 1 — Install dependencies
```bash
npm install
```
> The deprecation warnings during install (rimraf, glob, tar) are from transitive
> dependencies and do NOT affect your app. They are safe to ignore.

### Step 2 — Configure API URL
Open `src/environments/environment.ts` and set your backend URL:
```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7001/api'   // ← your .NET API URL here
};
```

### Step 3 — Run
```bash
ng serve
# App runs at http://localhost:4200
```

---

## 🔧 If you get "Cannot find module '@angular/forms'" error

This means Angular packages aren't installed yet. Run:
```bash
npm install
```
That error is purely a missing `node_modules` issue — the code is correct.

---

## 📡 CORS Setup (Backend)

Make sure your .NET API allows requests from `http://localhost:4200`.

In `Program.cs` the backend already has:
```csharp
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
```
This is already configured correctly.

---

## 🏗️ Project Structure

```
src/app/
├── core/
│   ├── guards/       → authGuard, guestGuard, adminGuard, superAdminGuard
│   ├── interceptors/ → JWT auth header + loading spinner + error handler
│   ├── models/       → All TypeScript interfaces matching backend DTOs
│   └── services/     → All API service calls
├── shared/
│   └── components/   → Navbar, Footer, Spinner
└── features/
    ├── auth/         → Login, Register Guest, Register Hotel Admin
    ├── hotel/        → Public hotel listing + details + search
    ├── booking/      → Create booking, booking list, booking detail + cancel
    ├── guest/        → Guest dashboard, profile, reviews, refund requests
    ├── admin/        → Hotel management suite (rooms, inventory, rates, refunds)
    ├── superadmin/   → Platform control (block/unblock hotels, audit logs)
    └── not-found/    → 404 page
```

---

## 👥 Roles & Default Redirects After Login

| Role       | Redirects To             |
|------------|--------------------------|
| Guest      | `/guest/dashboard`       |
| Admin      | `/admin/dashboard`       |
| SuperAdmin | `/superadmin/dashboard`  |

---

## 📋 All Routes

### Public (no login required)
| Route | Page |
|---|---|
| `/` | → `/hotels` |
| `/hotels` | Hotel search & listing |
| `/hotels/:id` | Hotel details, rooms, reviews |
| `/auth/login` | Sign in |
| `/auth/register` | Register as guest |
| `/auth/register-admin` | Register hotel + admin |

### Guest (login required, Role: Guest)
| Route | Page |
|---|---|
| `/guest/dashboard` | Stats overview |
| `/guest/bookings` | All my bookings |
| `/guest/profile` | View/edit profile |
| `/guest/reviews` | My hotel reviews |
| `/guest/refunds` | Refund request tracker |
| `/booking/create` | New reservation + payment |
| `/booking/:code` | Reservation detail + cancel |

### Admin (login required, Role: Admin)
| Route | Page |
|---|---|
| `/admin/dashboard` | Hotel KPIs + quick nav |
| `/admin/hotel` | Edit hotel info |
| `/admin/rooms` | Manage physical rooms |
| `/admin/roomtypes` | Room categories + pricing |
| `/admin/inventory` | Per-day room availability |
| `/admin/reservations` | All reservations + complete |
| `/admin/refunds` | Approve/reject refunds |
| `/admin/audit-logs` | Hotel action history |

### SuperAdmin (login required, Role: SuperAdmin)
| Route | Page |
|---|---|
| `/superadmin/dashboard` | Platform metrics |
| `/superadmin/hotels` | Block/unblock hotels |
| `/superadmin/audit-logs` | Full system audit trail |

---

## 🎨 Design Tokens

| Variable | Value |
|---|---|
| `--color-primary` | `#2d3a8c` (deep indigo) |
| `--color-accent` | `#c97d1b` (warm amber) |
| `--font-display` | Playfair Display |
| `--font-body` | DM Sans |
