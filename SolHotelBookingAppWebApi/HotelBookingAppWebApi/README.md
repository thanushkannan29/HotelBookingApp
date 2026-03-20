# 🏨 Hotel Booking System — ASP.NET Core Web API

A production-ready, clean-architecture REST API for a complete hotel booking platform.
Supports three roles: **Guest**, **Hotel Admin**, and **SuperAdmin**.

---

## 🏗️ Architecture

```
HTTP Request
    ↓
GlobalExceptionMiddleware  (catches all exceptions — runs first)
    ↓
JWT Authentication         (validates Bearer token, populates User claims)
    ↓
Role Authorization         ([Authorize(Roles = "...")] attribute check)
    ↓
Controller                 (extracts userId from claims, calls service)
    ↓
Service                    (all business logic, validation, orchestration)
    ↓
Repository<K,C>            (generic EF Core data access — no logic here)
    ↓
UnitOfWork                 (DB transaction: Begin → Commit or Rollback)
    ↓
HotelBookingContext        (EF Core DbContext → SQL Server)
```

### Layer Responsibilities

| Layer | Files | Does |
|---|---|---|
| **Controllers** | `*Controller.cs` | HTTP routing, auth, DTO binding, calls service, returns `{ success, data }` |
| **Services** | `*Service.cs` | All business rules, validation, orchestration |
| **Repository** | `Repository.cs` | Generic CRUD + `GetQueryable()` for complex LINQ |
| **Unit of Work** | `UnitOfWork.cs` | Wraps DB transactions — Begin/Commit/Rollback |
| **DbContext** | `HotelBookingContext.cs` | EF Core tables, relationships, Fluent API config |
| **Models** | `*.cs` (Models folder) | EF Core entity classes — map to SQL tables |
| **DTOs** | `*Dtos.cs` | Request/response contracts — entities never exposed |
| **Interfaces** | `I*.cs` | Contracts for DI — enables testing and loose coupling |
| **Exceptions** | `AppExceptions.cs` + middleware | Typed exceptions (404/409/400/401) + global handler |
| **Background Services** | `*Service.cs` (BackgroundServices) | Automated cleanup, refunds, no-show handling |

---

## ⚙️ Setup

### Prerequisites
- .NET 10 SDK
- SQL Server (LocalDB is fine for development)

### Steps

```bash
# 1. Clone the project
cd HotelBookingAppWebApi

# 2. Restore NuGet packages
dotnet restore

# 3. Set connection string in appsettings.json
#    "Developer": "Server=(localdb)\MSSQLLocalDB;TrustServerCertificate=True;
#                   Integrated Security=True;Database=dbHotelBookingAppV2;"

# 4. Create and apply database migration
dotnet ef migrations add InitialCreate
dotnet ef database update

# 5. Run
dotnet run
```

Swagger UI: `https://localhost:{port}/swagger`

### After Schema Changes (no migration needed for current changes)
```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

---

## 👥 Roles & Permissions

| Role | Who | Can Do |
|---|---|---|
| `Guest` | Registered travellers | Search hotels, book rooms, pay, cancel, review (after stay), view own data |
| `Admin` | Hotel owner/manager | Manage their hotel, rooms, rates, inventory, approve/reject refunds |
| `SuperAdmin` | Platform owner | Block/unblock hotels, view all system data, audit logs |

---

## 🗄️ Database Tables

| Table | Key Fields | Purpose |
|---|---|---|
| `Users` | UserId, Email (unique), Role (enum int), HotelId (Admin FK) | All users across all roles |
| `UserProfileDetails` | UserId (1:1 FK) | Extended profile — phone, address, image URL |
| `Hotels` | IsActive, IsBlockedBySuperAdmin | Hotel entity. Two flags control visibility |
| `RoomTypes` | HotelId, IsActive, Amenities (CSV string) | Categories: Standard, Deluxe, Suite etc. |
| `Rooms` | RoomNumber, Floor, RoomTypeId, IsActive. Unique: (HotelId, RoomNumber) | Physical rooms |
| `RoomTypeRates` | RoomTypeId, StartDate, EndDate, Rate | Date-range pricing. No overlaps allowed |
| `RoomTypeInventories` | RoomTypeId, Date (unique per type), TotalInventory, ReservedInventory | Per-day availability count |
| `Reservations` | ReservationCode (unique), Status (enum), IsCheckedIn, ExpiryTime | Guest booking. 10-min payment window |
| `ReservationRooms` | ReservationId, RoomTypeId, RoomId, PricePerNight | Junction — links reservation to specific rooms |
| `Transactions` | ReservationId, Amount, PaymentMethod (enum), Status (enum) | Payment records |
| `Reviews` | UserId, HotelId, Rating (1–5), Comment | Hotel reviews. One per guest per hotel. Requires completed stay. |
| `RefundRequests` | ReservationId, UserId, Status (Pending/Approved/Rejected) | Admin-approval refund flow |
| `AuditLogs` | UserId, Action, EntityName, Changes (JSON) | Immutable trail of admin actions |
| `Logs` | ExceptionType, StackTrace, StatusCode, Controller, Action | Auto-written by exception middleware |

### Enums (stored as int in DB)

| Enum | Values |
|---|---|
| `UserRole` | Guest=1, Admin=2, SuperAdmin=3 |
| `ReservationStatus` | Pending=1, Confirmed=2, Cancelled=3, Completed=4, NoShow=5 |
| `PaymentMethod` | CreditCard=1, DebitCard=2, UPI=3, NetBanking=4, Wallet=5 |
| `PaymentStatus` | Pending=1, Success=2, Failed=3 (reserved for real gateway), Refunded=4 |
| `RefundRequestStatus` | Pending=1, Approved=2, Rejected=3 |

---

## 📡 API Endpoints

### 🔓 Public (No Auth)

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/auth/register-guest` | Register guest. Returns JWT. |
| POST | `/api/auth/register-hotel-admin` | Register hotel + admin in one step. Returns JWT. |
| POST | `/api/auth/login` | Login all roles. Returns JWT. |
| GET | `/api/public/hotels/top` | Top 10 hotels by average rating |
| GET | `/api/public/hotels/cities` | All cities with active hotels |
| GET | `/api/public/hotels/by-city?city=` | All active hotels in a city |
| POST | `/api/public/hotels/search` | Search by city + date range. Paginated. |
| GET | `/api/public/hotels/{id}` | Basic hotel details |
| GET | `/api/public/hotels/{id}/full-details` | Full details with active room types + 10 recent reviews |
| GET | `/api/public/hotels/{id}/roomtypes` | Active room types for a hotel |
| GET | `/api/public/hotels/{id}/availability?checkIn=&checkOut=` | Available rooms per type for dates |
| POST | `/api/reviews/hotel` | Paginated reviews for a hotel |

### 👤 Guest (JWT · Role: Guest)

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/dashboard/guest` | Stats: bookings, spending, pending refunds |
| POST | `/api/guest/reservations` | Create reservation (optional room selection) |
| GET | `/api/guest/reservations` | All my reservations |
| GET | `/api/guest/reservations/history?page=&pageSize=` | Paginated history |
| GET | `/api/guest/reservations/{code}` | Single reservation by code |
| PATCH | `/api/guest/reservations/{code}/cancel` | Cancel. Restores inventory. Creates refund request if paid. |
| GET | `/api/guest/reservations/available-rooms?hotelId=&roomTypeId=&checkIn=&checkOut=` | Specific rooms available to select |
| GET | `/api/guest/refund-requests` | My refund requests |
| POST | `/api/transactions` | Pay for a pending reservation → status becomes Confirmed |
| POST | `/api/transactions/{id}/refund` | **Direct refund within 30 min of payment only.** Backend enforces window. |
| GET | `/api/transactions?page=&pageSize=` | My transactions |
| POST | `/api/reviews` | Post review. Requires completed stay at hotel. |
| PUT | `/api/reviews/{id}` | Update own review |
| DELETE | `/api/reviews/{id}` | Delete own review |
| GET | `/api/reviews/my-reviews` | All my reviews |
| GET | `/api/user-profile` | My profile |
| PUT | `/api/user-profile` | Update profile |
| POST | `/api/user-profile/booking-history` | Paginated booking history |
| GET | `/api/logs/my-logs?page=&pageSize=` | My error logs |

### 🏢 Admin (JWT · Role: Admin)

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/dashboard/admin` | Hotel stats: rooms, reservations, revenue, rating |
| PUT | `/api/admin/hotels` | Update hotel info |
| PATCH | `/api/admin/hotels/status?isActive=` | Activate/deactivate. Cannot activate if SuperAdmin blocked. |
| GET | `/api/admin/roomtypes` | All room types for my hotel |
| POST | `/api/admin/roomtypes` | Add room type |
| PUT | `/api/admin/roomtypes` | Update room type |
| PATCH | `/api/admin/roomtypes/{id}/status?isActive=` | Toggle room type active |
| POST | `/api/admin/roomtypes/rate` | Add rate for date range |
| PUT | `/api/admin/roomtypes/rate` | Update rate |
| POST | `/api/admin/roomtypes/rate-by-date` | Get rate for a specific date |
| GET | `/api/admin/rooms?pageNumber=&pageSize=` | List rooms (paginated) |
| POST | `/api/admin/rooms` | Add physical room |
| PUT | `/api/admin/rooms` | Update room |
| PATCH | `/api/admin/rooms/{id}/status?isActive=` | Toggle room active |
| GET | `/api/admin/inventory?roomTypeId=&start=&end=` | View inventory |
| POST | `/api/admin/inventory` | Add inventory for date range (idempotent — skips existing dates) |
| PUT | `/api/admin/inventory` | Update inventory count (cannot go below reserved) |
| GET | `/api/admin/reservations?page=&pageSize=` | All hotel reservations |
| PATCH | `/api/admin/reservations/{code}/complete` | Mark Confirmed → Completed (also sets IsCheckedIn=true) |
| GET | `/api/admin/refund-requests` | All refund requests for hotel |
| POST | `/api/admin/refund-requests/{id}/approve` | Approve → Transaction becomes Refunded |
| POST | `/api/admin/refund-requests/{id}/reject` | Reject refund |
| GET | `/api/admin/audit-logs?page=&pageSize=` | Hotel audit trail |
| GET | `/api/transactions?page=&pageSize=` | Hotel transactions |
| GET | `/api/logs/my-logs?page=&pageSize=` | Admin's own error logs |

### 👑 SuperAdmin (JWT · Role: SuperAdmin)

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/dashboard/superadmin` | System-wide stats |
| GET | `/api/superadmin/hotels` | All hotels with revenue + reservation counts (optimised — 3 queries total) |
| PATCH | `/api/superadmin/hotels/{id}/block` | Block hotel (forces inactive, admin cannot re-activate) |
| PATCH | `/api/superadmin/hotels/{id}/unblock` | Unblock hotel (admin can now re-activate) |
| GET | `/api/superadmin/audit-logs?page=&pageSize=` | All system audit logs |
| GET | `/api/logs?page=&pageSize=` | All system error logs |
| GET | `/api/transactions?page=&pageSize=` | All transactions |

---

## ⚙️ Background Services

All three run every 5 minutes. They create their own DI scope, use transactions, and log failures without stopping the service.

| Service | Trigger | Actions |
|---|---|---|
| `ReservationCleanupService` | `Status=Pending` AND `ExpiryTime < now` | Cancels reservation, restores inventory. No refund (never paid). |
| `HotelDeactivationRefundService` | `Status=Confirmed` AND `Hotel.IsActive=false` | Cancels reservation, restores inventory, marks transaction Refunded, creates auto-approved RefundRequest. |
| `NoShowAutoCancelService` | `Status=Confirmed` AND `IsCheckedIn=false` AND `CheckOutDate < today` | Sets status to NoShow, restores inventory. No refund. |

---

## 🔄 Key Business Flows

### Complete Booking Flow

```
1. Guest: POST /api/guest/reservations
   → Reservation created (Status=Pending, ExpiryTime=now+10min)
   → Inventory decremented
   → Returns ReservationCode

2. Guest: POST /api/transactions  [within 10 minutes]
   → Transaction created (Status=Success)
   → Reservation → Confirmed

2a. [If guest doesn't pay in 10 min]
   → ReservationCleanupService cancels it, restores inventory

3. Admin: PATCH /api/admin/reservations/{code}/complete
   → Status → Completed
   → IsCheckedIn → true  ✅ (prevents no-show trigger)
```

### Cancellation & Refund Flow

```
Option A — Guest within 30 min of payment:
   POST /api/transactions/{id}/refund
   → Transaction → Refunded
   → Reservation → Cancelled
   → Inventory restored
   → Immediate. No admin needed.

Option B — Guest after 30 min:
   PATCH /api/guest/reservations/{code}/cancel
   → Reservation → Cancelled
   → Inventory restored
   → RefundRequest created (Status=Pending)
   → Admin reviews in /api/admin/refund-requests
   → Approve → Transaction Refunded
   → Reject  → No refund, RefundRequest Rejected

Option C — Hotel deactivated while guest has Confirmed booking:
   → HotelDeactivationRefundService auto-handles within 5 min
   → Auto-approved RefundRequest created (no admin action needed)
```

### Review Eligibility

```
Guest tries to POST /api/reviews
→ Backend checks: does this guest have a Completed reservation at this hotel?
→ No  → 400 "You can only review a hotel after completing a stay there."
→ Yes → checks: already reviewed?
         → Yes → 400 "You have already reviewed this hotel."
         → No  → Review created ✅
```

### Admin Setup Order (First Time)

```
1. POST /api/auth/register-hotel-admin    → creates hotel + admin
2. POST /api/admin/roomtypes              → add room categories
3. POST /api/admin/inventory              → set room counts per date range
4. POST /api/admin/roomtypes/rate         → set price per night per date range
5. POST /api/admin/rooms                  → add physical rooms (≤ inventory cap)
6. PATCH /api/admin/hotels/status?isActive=true  → go live
```

---

## 🔒 Security

### Password Hashing
- **Algorithm:** HMACSHA256 with a unique random salt per user
- **Storage:** `Password` (hash bytes) + `PasswordSaltValue` (salt bytes) in `Users` table
- **Verification:** Re-hash with stored salt, compare byte-by-byte with `SequenceEqual`

### JWT Tokens
- **Signing:** HMACSHA256 with key from `appsettings.json → Keys:Jwt`
- **Expiry:** 1 day
- **Claims:** `NameIdentifier` (UserId), `Name` (UserName), `Role`, `HotelId` (Admin only)
- **Usage in controllers:** `GetUserId()` = `Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))`

### Rate Limiting
- **Package:** `AspNetCoreRateLimit`
- **Rule:** 60 requests per IP per minute across all endpoints
- **Response on breach:** HTTP 429 Too Many Requests

### Global Exception Middleware
Every unhandled exception:
1. Is caught before it reaches the client
2. Logged to `ILogger` with structured data
3. Persisted to `Logs` table in DB (controller, action, user, role, stacktrace)
4. Returns consistent JSON: `{ success: false, statusCode, message, traceId }`

---

## 📦 OOP Concepts Used & Why

### Interface Segregation + Dependency Injection
Every service has its own `I*Service` interface. Controllers and other services depend only on the interface, never the concrete class. This means:
- Easy to swap implementations (e.g. mock in tests)
- Clear contract of what each service does
- Registered in `Program.cs` as `AddScoped<IHotelService, HotelService>()`

### Generic Repository Pattern (`IRepository<K, C>`)
A single generic repository handles all 14 entities. Instead of writing `UserRepository`, `HotelRepository`, etc., one implementation works for all:
```csharp
IRepository<Guid, User>
IRepository<Guid, Hotel>
IRepository<Guid, Reservation>
// etc.
```
The key method is `GetQueryable()` which returns `IQueryable<C>` — services chain `.Where()`, `.Include()`, `.Select()`, `.OrderBy()`, `.Skip()`, `.Take()` before execution. No N+1 queries.

### Unit of Work Pattern
Groups multiple repository operations into one atomic database transaction:
```csharp
await _unitOfWork.BeginTransactionAsync();
// multiple repo operations in memory
await _unitOfWork.CommitAsync();  // SaveChanges + transaction commit
// or
await _unitOfWork.RollbackAsync(); // discard everything on error
```
Ensures data integrity — if creating a reservation and decrementing inventory fails halfway, the entire operation rolls back.

### Custom Exception Hierarchy (Polymorphism)
```
AppException (base, has StatusCode)
  ├── NotFoundException        (404)
  ├── ConflictException        (409)
  ├── ValidationException      (400)
  ├── UnAuthorizedException    (401)
  ├── PaymentException         (400)
  ├── ReservationFailedException (400)
  ├── InsufficientInventoryException (409)
  ├── RateNotFoundException    (404)
  ├── ReviewException          (400)
  └── UserProfileException     (404)
```
`GlobalExceptionMiddleware` catches any `Exception`. If it's an `AppException`, it reads `StatusCode` (polymorphism). Otherwise returns 500. Services throw the right typed exception — the middleware handles the HTTP response. Controllers have zero error-handling code.

### DTOs (Data Transfer Object Pattern)
Entities are never returned from the API. DTOs:
- Decouple the API contract from the database schema
- Allow selective field exposure (e.g. never expose `Password` or `PasswordSaltValue`)
- Use `DataAnnotations` for automatic model validation (`[Required]`, `[Range]`, `[MaxLength]`)

### Background Services (Template Method Pattern)
All three background services extend `BackgroundService` (abstract class from ASP.NET Core). They override `ExecuteAsync()` and loop forever with a 5-minute delay. Each uses `IServiceScopeFactory` to create a fresh DI scope per run (required because repositories are Scoped, not Singleton).

### Audit Trail (Observer-like)
After any critical write operation (hotel update, room add, refund approve/reject), services call `IAuditLogService.LogAsync()` with structured before/after JSON. This is a clean post-commit call — intentionally outside the main transaction so a failed audit write never rolls back legitimate business data.

---

## 🔧 What Was Changed & Why (vs Original)

### ✅ `ReservationService.cs` — Check-in on Complete
**What:** `CompleteReservationAsync` now sets `IsCheckedIn = true` alongside `Status = Completed`.

**Why:** When admin marks a reservation complete, the guest definitively stayed. Setting `IsCheckedIn` at that point:
- Prevents `NoShowAutoCancelService` from ever flagging an already-completed stay
- Gives frontend a clean boolean to show "Checked In" badge on booking history
- No separate endpoint needed — completion implies check-in

### ✅ `HotelService.cs` — SuperAdmin N+1 Fix
**What:** `GetAllHotelsForSuperAdminAsync` was a `foreach` loop making 2 DB queries per hotel (N×2 queries). Rewritten as 3 total queries — hotels, reservation counts grouped, revenue grouped — then merged in memory with dictionary lookups.

**Why:** With 100 hotels, old code = 201 queries. New code = 3 queries always. Critical for SuperAdmin dashboard performance.

### ✅ `ReviewService.cs` — Completed Stay Required
**What:** `AddReviewAsync` now checks that the guest has at least one `Completed` reservation at the hotel before allowing a review. Added `IRepository<Guid, Reservation>` dependency.

**Why:** Prevents fake reviews from guests who never stayed. Makes the rating system trustworthy and realistic. Aligns with how real booking platforms (Booking.com, Airbnb) work.

---

## 📦 NuGet Packages

```xml
<PackageReference Include="AspNetCoreRateLimit" Version="5.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.x" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.x" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.x" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
```

---

## 🔮 Deferred for Later (Not In Scope Now)

| Feature | When |
|---|---|
| Payment Gateway (Stripe/Razorpay) | After deployment — needs live API keys |
| Email/SMS Notifications (SendGrid/Twilio) | After deployment — needs accounts |
| Image Uploads (Azure Blob / S3) | During Azure cloud migration |
| JWT Refresh Tokens | Post-MVP security hardening |
| Multi-hotel Admin | Only if business model requires it |

---

## ✅ Spec Coverage Summary

| Spec | Status |
|---|---|
| 1. Auth (register + JWT login) | ✅ Complete |
| 2. Hotel search by city + dates | ✅ Complete |
| 3. Room selection + multi-room booking | ✅ Complete |
| 4. Pricing + payment methods | ✅ Complete (simulated gateway) |
| 5. Booking confirmation (reservation code returned) | ✅ Partial — no email, code in API response |
| 6. User profiles | ✅ Complete |
| 7. Booking history | ✅ Complete |
| 8. Hotel info + reviews + amenities | ✅ Complete |
| 9. Cancellations + refunds with timeframe | ✅ Complete (30-min direct + admin-approval flow) |
| 10. Additional services | — Optional, not in scope |
| 11. Responsive design | — Frontend concern |
| 12. Security (JWT + hashing + rate limit) | ✅ Complete |
| 13. MS SQL Server | ✅ Complete |
| 14. Real-time availability | ✅ Complete (inventory system) |
| 15. Testing | — To be done in frontend dev phase |
| 16. Deployment | — Azure (planned) |

**Backend is ready for frontend development in Angular. ✅**
