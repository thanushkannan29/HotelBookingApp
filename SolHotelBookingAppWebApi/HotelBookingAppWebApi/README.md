# 🏨 Hotel Booking System — ASP.NET Core Web API

A production-ready, clean-architecture REST API for a complete hotel booking platform supporting **Guests**, **Hotel Admins**, and **SuperAdmins**.

---

## 🏗️ Architecture

```
Controller → Service → Repository (IRepository<K,C>) → EF Core DbContext
                     ↓
               UnitOfWork (transactions)
                     ↓
              AuditLogService (audit trail)
```

**Layers:**
| Layer | Responsibility |
|---|---|
| Controllers | HTTP routing, auth, DTO binding |
| Services | All business logic |
| Repository | Generic data access (`IRepository<K,C>`) |
| UnitOfWork | DB transaction management |
| Models | EF Core entities |
| DTOs | Request/response contracts (entities never exposed) |
| Exceptions | Custom typed exceptions + global middleware |
| BackgroundServices | Automated cleanup, refund, no-show handling |

---

## ⚙️ Setup

### Prerequisites
- .NET 10 SDK
- SQL Server (LocalDB works fine)

### Steps

```bash
# 1. Clone / copy project
cd HotelBookingAppWebApi

# 2. Restore packages
dotnet restore

# 3. Set connection string in appsettings.json
# "Developer": "Server=...;Database=dbHotelBookingAppV2;..."

# 4. Run migrations
dotnet ef migrations add InitialCreate
dotnet ef database update

# 5. Run
dotnet run
```

Swagger UI: `https://localhost:{port}/swagger`

---

## 🗄️ Database Migration (after schema changes)

```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

**New tables added in this version:**
- `RefundRequests` — guest-initiated refund requests with admin approval flow
- `AuditLogs` — tracks Hotel/Room/RoomType changes and refund decisions

**Updated columns:**
- `Hotels.IsBlockedBySuperAdmin` (bool)
- `Reservations.IsCheckedIn` (bool)
- `Reservations.Status` — added `NoShow = 5`
- `Reviews.ImageUrl` (nullable string)
- `UserProfileDetails.ProfileImageUrl` (nullable string)

---

## 👥 Roles

| Role | Description |
|---|---|
| `Guest` | Search hotels, make/cancel bookings, pay, review |
| `Admin` | Manage own hotel, rooms, inventory, rates, approve refunds |
| `SuperAdmin` | Block/unblock hotels, view all logs and audit trails |

---

## 📡 API Endpoints

### 🔓 Public (no auth required)

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/auth/register-guest` | Register new guest |
| POST | `/api/auth/register-hotel-admin` | Register hotel + admin |
| POST | `/api/auth/login` | Login (all roles) |
| GET | `/api/public/hotels/top` | Top 10 hotels |
| GET | `/api/public/hotels/cities` | All available cities |
| GET | `/api/public/hotels/by-city?city=` | Hotels in a city |
| POST | `/api/public/hotels/search` | Search by city + dates |
| GET | `/api/public/hotels/{id}` | Hotel details |
| GET | `/api/public/hotels/{id}/full-details` | Full details with room types + reviews |
| GET | `/api/public/hotels/{id}/roomtypes` | Room types |
| GET | `/api/public/hotels/{id}/availability` | Room availability |
| POST | `/api/reviews/hotel` | Reviews for a hotel |

---

### 👤 Guest (JWT required, Role: Guest)

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/dashboard/guest` | Guest dashboard |
| GET | `/api/guest/reservations` | All my reservations |
| GET | `/api/guest/reservations/history?page=&pageSize=` | Paginated reservation history |
| POST | `/api/guest/reservations` | Create reservation (supports room selection) |
| GET | `/api/guest/reservations/{code}` | Get reservation by code |
| PATCH | `/api/guest/reservations/{code}/cancel` | Cancel reservation |
| GET | `/api/guest/reservations/available-rooms` | Available rooms for a hotel/type/dates |
| GET | `/api/guest/refund-requests` | My refund requests |
| POST | `/api/transactions` | Pay for a reservation |
| GET | `/api/transactions` | My transactions |
| POST | `/api/reviews` | Post a review |
| PUT | `/api/reviews/{id}` | Update a review |
| DELETE | `/api/reviews/{id}` | Delete a review |
| GET | `/api/reviews/my-reviews` | My reviews |
| GET | `/api/user-profile` | Get profile |
| PUT | `/api/user-profile` | Update profile (incl. image URL) |
| POST | `/api/user-profile/booking-history` | Paginated booking history |
| GET | `/api/logs/my-logs` | My error logs |

---

### 🏢 Admin (JWT required, Role: Admin)

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/dashboard/admin` | Admin dashboard |
| PUT | `/api/admin/hotels` | Update hotel info |
| PATCH | `/api/admin/hotels/status?isActive=` | Toggle hotel active/inactive |
| GET | `/api/admin/roomtypes` | List all room types |
| POST | `/api/admin/roomtypes` | Add room type |
| PUT | `/api/admin/roomtypes` | Update room type |
| PATCH | `/api/admin/roomtypes/{id}/status` | Toggle room type status |
| POST | `/api/admin/roomtypes/rate` | Add rate |
| PUT | `/api/admin/roomtypes/rate` | Update rate |
| POST | `/api/admin/roomtypes/rate-by-date` | Get rate for a date |
| GET | `/api/admin/rooms?pageNumber=&pageSize=` | List rooms |
| POST | `/api/admin/rooms` | Add room |
| PUT | `/api/admin/rooms` | Update room |
| PATCH | `/api/admin/rooms/{id}/status` | Toggle room status |
| GET | `/api/admin/inventory` | Get inventory |
| POST | `/api/admin/inventory` | Add inventory |
| PUT | `/api/admin/inventory` | Update inventory |
| GET | `/api/admin/reservations` | List hotel reservations |
| PATCH | `/api/admin/reservations/{code}/complete` | Mark as completed |
| GET | `/api/admin/refund-requests` | All refund requests |
| POST | `/api/admin/refund-requests/{id}/approve` | Approve refund |
| POST | `/api/admin/refund-requests/{id}/reject` | Reject refund |
| GET | `/api/admin/audit-logs` | Audit trail for this hotel |
| GET | `/api/transactions` | Hotel transactions |
| GET | `/api/logs/my-logs` | Admin's own error logs |

---

### 👑 SuperAdmin (JWT required, Role: SuperAdmin)

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/dashboard/superadmin` | SuperAdmin dashboard |
| GET | `/api/superadmin/hotels` | All hotels with stats |
| PATCH | `/api/superadmin/hotels/{id}/block` | Block hotel |
| PATCH | `/api/superadmin/hotels/{id}/unblock` | Unblock hotel |
| GET | `/api/superadmin/audit-logs` | All system audit logs |
| GET | `/api/logs` | All system error logs |
| GET | `/api/transactions` | All transactions |

---

## ⚙️ Background Services

| Service | Interval | Description |
|---|---|---|
| `ReservationCleanupService` | 5 min | Cancels Pending reservations past their 10-min payment window; restores inventory |
| `HotelDeactivationRefundService` | 5 min | When hotel is deactivated, auto-cancels all Confirmed reservations and issues refunds |
| `NoShowAutoCancelService` | 5 min | Marks Confirmed reservations as `NoShow` if guest never checked in past checkout date (no refund) |

---

## 🔄 Refund Flow

```
Guest cancels reservation
        ↓
Inventory restored immediately
        ↓
If payment was made → RefundRequest created (Pending)
        ↓
Admin reviews → Approve or Reject
        ↓
On Approve → Transaction.Status = Refunded (actual financial refund)
On Reject  → RefundRequest.Status = Rejected (no refund)
```

---

## 🔒 Security

- JWT Bearer authentication (1-day expiry)
- Role-based authorization (`[Authorize(Roles = "...")]`)
- IP-based rate limiting (60 req/min default)
- Passwords hashed with HMACSHA256 + random salt per user
- All exceptions caught by `GlobalExceptionMiddleware`
- All exceptions persisted to `Logs` table in DB

---

## 📊 Audit Trail

The following actions are tracked in `AuditLogs`:
- Hotel created / updated / activated / deactivated / blocked / unblocked
- RoomType added / updated
- Room added / updated
- Refund approved / rejected

Admin can query `/api/admin/audit-logs`.  
SuperAdmin can query `/api/superadmin/audit-logs` for all.

---

## 📦 NuGet Packages

```xml
<PackageReference Include="AspNetCoreRateLimit" Version="5.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.x" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.x" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.x" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
```
