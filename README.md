# 🏨 Thanush StayHub — Hotel Booking & Management Platform

A full-stack hotel booking application built with **Angular 18** on the frontend and **ASP.NET Core (.NET 10)** on the backend. The platform supports three user roles — Guest, Hotel Admin, and SuperAdmin — each with a dedicated dashboard and feature set.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Angular 18, Angular Material, TypeScript, SCSS |
| Backend | ASP.NET Core Web API, .NET 10 |
| Database | SQL Server (EF Core 10) |
| Auth | JWT Bearer Tokens |
| Payments | Razorpay (UPI, Card, Net Banking, Wallet) |
| AI Chatbot | Groq API (LLaMA 3.1) |
| Testing (BE) | xUnit, Moq, FluentAssertions, EF Core InMemory |
| Testing (FE) | Jasmine, Karma, Angular Testing Utilities |

---

## Project Structure

```
HotelBookingApp/
├── Fontend-Angular/              # Angular 18 SPA
│   └── src/app/
│       ├── core/                 # Services, models, guards, interceptors
│       ├── features/             # Feature modules (auth, booking, admin, guest, superadmin)
│       └── shared/               # Reusable components (navbar, footer, chatbot, dialogs)
│
├── SolHotelBookingAppWebApi/
│   ├── HotelBookingAppWebApi/    # ASP.NET Core Web API
│   │   ├── Controllers/          # REST endpoints (Admin, Guest, SuperAdmin, Public)
│   │   ├── Services/             # Business logic + background services
│   │   ├── Models/               # EF Core entities
│   │   ├── Exceptions/           # Custom exceptions + global middleware
│   │   └── Interfaces/           # Service contracts
│   └── HotelBookingAppWebApi.Tests/  # xUnit test project
│
└── Documentation-notes/          # Project docs and presentation materials
```

---

## Features

### Guest
- Browse and search hotels by city, state, dates, price range, and amenities
- Book rooms with real-time availability, promo code discounts, and wallet payments
- Pay via Razorpay (UPI, Credit/Debit Card, Net Banking) or wallet balance
- View booking history, cancel reservations, and track refunds
- Write and manage hotel reviews
- Wallet top-up and transaction history
- AI chatbot assistant (Groq / LLaMA 3.1)
- Support request submission

### Hotel Admin
- Dashboard with revenue, occupancy, and reservation stats
- Hotel profile and GST management
- Room type management with amenity assignment and pricing rates
- Room management with occupancy calendar
- Inventory management (date-range bulk set)
- Reservation management with status tabs, search, and sort
- Transaction history and refund tracking
- Guest review management with admin replies
- Audit log viewer
- Amenity request submission to SuperAdmin
- PDF report download

### SuperAdmin
- Platform-wide dashboard (hotels, users, revenue, reviews)
- Hotel control — activate, deactivate, block/unblock hotels
- Amenity management (CRUD + toggle status)
- Amenity request approval/rejection
- Support request management across all roles
- Commission revenue tracking (2% per completed booking)
- Error log viewer
- Audit log viewer (all hotels)

---

## Background Services

| Service | Purpose |
|---|---|
| `ReservationCleanupService` | Auto-cancels unpaid reservations after 10 minutes |
| `NoShowAutoCancelService` | Marks reservations as NoShow if not checked in by checkout |
| `HotelDeactivationRefundService` | Refunds guests when a hotel is deactivated with active bookings |
| `InventoryRestoreHelper` | Restores room inventory on cancellation/no-show |

---

## Getting Started

### Prerequisites
- Node.js 20+, Angular CLI 18
- .NET 10 SDK
- SQL Server (local or Azure)

### Backend Setup

```bash
cd SolHotelBookingAppWebApi/HotelBookingAppWebApi

# Update connection string in appsettings.json
# "DefaultConnection": "Server=...;Database=HotelBookingDb;..."

dotnet ef database update
dotnet run
# API runs at https://localhost:7208
```

### Frontend Setup

```bash
cd Fontend-Angular
npm install
ng serve
# App runs at http://localhost:4200
```

Update `src/environments/environment.ts` with your API URL, Razorpay key, and Groq API key.

---

## Running Tests

### Backend (xUnit)

```bash
cd SolHotelBookingAppWebApi
dotnet test --verbosity normal
```

Test coverage includes:
- All service classes (AuthService, ReservationService, HotelService, etc.)
- Background services
- Repository layer
- Global exception middleware
- DTO model validation

### Frontend (Jasmine / Karma)

```bash
cd Fontend-Angular
ng test --watch=false --browsers=ChromeHeadless
```

Test coverage includes:
- All Angular services (HTTP contract tests via `HttpClientTestingModule`)
- All feature components (component creation, signal state, form validation, service interactions)
- Shared components (Chatbot, Dialogs, Carousel, City Autocomplete)
- Guards and interceptors

---

## API Overview

Base URL: `https://localhost:7208/api`

| Prefix | Role |
|---|---|
| `/auth` | Public — login, register |
| `/public` | Public — hotel search, availability |
| `/guest` | Authenticated Guest |
| `/admin` | Authenticated Hotel Admin |
| `/superadmin` | Authenticated SuperAdmin |

Swagger UI available at `https://localhost:7208/swagger`

---

## Key Design Decisions

- **JWT auth** with role-based route guards (`AuthGuard`) on the frontend
- **Repository pattern** with `UnitOfWork` on the backend
- **Signal-based state** throughout Angular components (no NgRx)
- **Global exception middleware** maps custom `AppException` subclasses to HTTP status codes
- **Rate limiting** via `AspNetCoreRateLimit` on sensitive endpoints
- **Razorpay** handles all real payment flows; wallet is a platform credit system

---

## Author

**Thanush K** — Software Engineer Trainee · NAF
Contact: thanush.k@nafinc.com | +91 9840650939
