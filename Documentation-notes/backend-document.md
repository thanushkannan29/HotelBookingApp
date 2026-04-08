# Hotel Booking System — Complete Backend Documentation

> **Project:** `HotelBookingAppWebApi`
> **Framework:** ASP.NET Core Web API (.NET 10)
> **Database:** MS SQL Server (via Entity Framework Core 10)
> **Auth:** JWT Bearer Tokens (HS256)
> **Architecture:** Generic Repository + Unit of Work + Service Layer + DTOs

---

## Table of Contents

1. [Project Overview & Architecture](#1-project-overview--architecture)
2. [Tech Stack & NuGet Packages](#2-tech-stack--nuget-packages)
3. [Solution Folder Structure](#3-solution-folder-structure)
4. [Configuration — appsettings.json](#4-configuration--appsettingsjson)
5. [Program.cs — Application Startup](#5-programcs--application-startup)
6. [Domain Models (C# Classes + SQL Tables)](#6-domain-models-c-classes--sql-tables)
7. [DbContext — EF Core + Fluent API + Seed Data](#7-dbcontext--ef-core--fluent-api--seed-data)
8. [Generic Repository Pattern](#8-generic-repository-pattern)
9. [Unit of Work Pattern](#9-unit-of-work-pattern)
10. [Interfaces (Service Contracts)](#10-interfaces-service-contracts)
11. [Services — Business Logic Layer](#11-services--business-logic-layer)
12. [Background Services](#12-background-services)
13. [Controllers (API Endpoints)](#13-controllers-api-endpoints)
14. [DTOs (Data Transfer Objects)](#14-dtos-data-transfer-objects)
15. [Exception Handling — Custom Exceptions + Global Middleware](#15-exception-handling--custom-exceptions--global-middleware)
16. [JWT Authentication Deep Dive](#16-jwt-authentication-deep-dive)
17. [Password Hashing — HMAC-SHA256](#17-password-hashing--hmac-sha256)
18. [SQL Concepts Used in This Project](#18-sql-concepts-used-in-this-project)
19. [EF Core LINQ Patterns & Query Splitting](#19-ef-core-linq-patterns--query-splitting)
20. [Pagination Pattern](#20-pagination-pattern)
21. [Dependency Injection — Complete Registration Map](#21-dependency-injection--complete-registration-map)
22. [Role-Based Authorization](#22-role-based-authorization)
23. [Rate Limiting](#23-rate-limiting)
24. [Key Business Flows End-to-End](#24-key-business-flows-end-to-end)
25. [C# Concepts Mastery Guide](#25-c-concepts-mastery-guide)
26. [SQL Server Concepts Mastery Guide](#26-sql-server-concepts-mastery-guide)

---

## 1. Project Overview & Architecture

The Hotel Booking System backend is a layered ASP.NET Core Web API. Each layer has a single responsibility:

```
HTTP Request
    ↓
[Controller]         ← Receives HTTP, extracts claims, calls service, returns IActionResult
    ↓
[Service Layer]      ← All business logic lives here; uses repository + unit of work
    ↓
[Repository Layer]   ← Generic CRUD operations; no business logic; works on DbSet<T>
    ↓
[DbContext (EF Core)]← Tracks entities, maps to SQL Server, translates LINQ → SQL
    ↓
[SQL Server Database]← Stores all data
```

**Three user roles** drive the entire API surface:

| Role | What they can do |
|------|-----------------|
| `Guest` | Search hotels, make reservations, pay, review, cancel, wallet |
| `Admin` | Manage own hotel, rooms, room types, pricing, inventory, reservations |
| `SuperAdmin` | Manage all hotels, approve amenity requests, view platform revenue |

**Design patterns applied:**
- **Generic Repository** — one `IRepository<TKey, TEntity>` for all entities
- **Unit of Work** — wraps EF Core transactions so multi-step writes either all succeed or all roll back
- **Service Layer** — all business rules isolated in services, never in controllers
- **DTO pattern** — models never cross the API boundary; DTOs do
- **Dependency Injection** — ASP.NET Core's built-in DI wires everything together

---

## 2. Tech Stack & NuGet Packages

```xml
<!-- HotelBookingAppWebApi.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>           <!-- nullable reference types ON -->
    <ImplicitUsings>enable</ImplicitUsings><!-- common using directives auto-included -->
  </PropertyGroup>

  <ItemGroup>
    <!-- Rate limiting by IP address -->
    <PackageReference Include="AspNetCoreRateLimit" Version="5.0.0" />

    <!-- JWT Bearer token authentication -->
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.3" />

    <!-- EF Core with SQL Server provider -->
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.3" />

    <!-- EF Core CLI tools (Add-Migration, Update-Database) -->
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.3" />

    <!-- QR code generation for payment flow -->
    <PackageReference Include="QRCoder" Version="1.6.0" />

    <!-- Swagger / OpenAPI UI -->
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
  </ItemGroup>
</Project>
```

**What each package does:**

- **AspNetCoreRateLimit** — Protects the API from abuse by limiting each IP to 60 requests/minute.
- **JwtBearer** — Validates `Authorization: Bearer <token>` headers on every protected endpoint. Decodes the JWT and populates `HttpContext.User` with claims.
- **EF Core SqlServer** — Maps C# classes to SQL Server tables. Lets you write LINQ instead of raw SQL.
- **EF Core Tools** — Provides `dotnet ef migrations add` and `dotnet ef database update` commands.
- **QRCoder** — Generates QR codes for the simulated UPI payment flow.
- **Swashbuckle** — Auto-generates Swagger documentation at `/swagger` from your controller XML comments and route attributes.

---

## 3. Solution Folder Structure

```
HotelBookingAppWebApi/
│
├── appsettings.json                    # Connection string, JWT key, rate limit config
├── Program.cs                          # App entry point, DI registration, middleware pipeline
├── HotelBookingAppWebApi.csproj        # Project/package references
│
├── Contexts/
│   └── HotelBookingContext.cs          # EF Core DbContext — all DbSets + Fluent API mappings
│
├── Models/                             # Domain entity classes (map to DB tables)
│   ├── User.cs
│   ├── Hotel.cs
│   ├── Room.cs
│   ├── RoomType.cs
│   ├── RoomTypeRate.cs
│   ├── RoomTypeInventory.cs
│   ├── RoomTypeAmenity.cs
│   ├── Reservation.cs
│   ├── ReservationRoom.cs
│   ├── Transaction.cs
│   ├── Review.cs
│   ├── Wallet.cs  (+ WalletTransaction)
│   ├── PromoCode.cs
│   ├── Amenity.cs
│   ├── AmenityRequest.cs
│   ├── AuditLog.cs
│   ├── Log.cs
│   ├── SupportRequest.cs
│   ├── SuperAdminRevenue.cs
│   └── UserProfileDetails.cs
│   └── DTOs/                           # Data Transfer Objects (one per feature area)
│       ├── Auth/
│       ├── Hotel/
│       ├── Room/
│       ├── RoomType/
│       ├── Reservation/
│       ├── Transaction/
│       ├── Review/
│       ├── Wallet/
│       ├── PromoCode/
│       ├── Amenity/
│       ├── AmenityRequest/
│       ├── AuditLog/
│       ├── Dashboard/
│       ├── Inventory/
│       ├── Log/
│       ├── Revenue/
│       ├── SupportRequest/
│       └── UserDetails/
│
├── Interfaces/                         # Service contracts (C# interfaces)
│   ├── IAuthService.cs
│   ├── IHotelService.cs
│   ├── IReservationService.cs
│   ├── ITransactionService.cs
│   ├── IReviewService.cs
│   ├── IWalletService.cs
│   ├── IPromoCodeService.cs
│   ├── IRoomService.cs
│   ├── IRoomTypeService.cs
│   ├── IInventoryService.cs
│   ├── IAmenityService.cs
│   ├── IAmenityRequestService.cs
│   ├── IAuditLogService.cs
│   ├── IDashboardService.cs
│   ├── ILogService.cs
│   ├── IPasswordService.cs
│   ├── ITokenService.cs
│   ├── ISuperAdminRevenueService.cs
│   ├── ISupportRequestService.cs
│   ├── IUserService.cs
│   ├── RepositoryInterface/
│   │   └── IRepository.cs             # Generic repository interface
│   └── UnitOfWorkInterface/
│       └── IUnitOfWork.cs             # Unit of work interface
│
├── Repository/
│   └── Repository.cs                  # Generic repository implementation
│
├── Services/                          # Business logic
│   ├── AuthService.cs
│   ├── HotelService.cs
│   ├── ReservationService.cs
│   ├── TransactionService.cs
│   ├── ReviewService.cs
│   ├── WalletService.cs
│   ├── PromoCodeService.cs
│   ├── RoomService.cs
│   ├── RoomTypeService.cs
│   ├── InventoryService.cs
│   ├── AmenityService.cs
│   ├── AmenityRequestService.cs
│   ├── AuditLogService.cs
│   ├── DashboardService.cs
│   ├── LogService.cs
│   ├── PasswordService.cs
│   ├── TokenService.cs
│   ├── SuperAdminRevenueService.cs
│   ├── SupportRequestService.cs
│   ├── UserService.cs
│   ├── QrCodeHelper.cs
│   ├── UnitOfWork.cs
│   └── BackgroundServices/
│       ├── ReservationCleanupService.cs      # Cancels expired pending reservations
│       ├── NoShowAutoCancelService.cs        # Marks no-shows
│       ├── HotelDeactivationRefundService.cs # Refunds when hotel deactivated
│       └── InventoryRestoreHelper.cs
│
├── Controllers/                       # API endpoint handlers
│   ├── AuthenticationController.cs
│   ├── ReviewController.cs
│   ├── DashboardController.cs
│   ├── LogController.cs
│   ├── TransactionController.cs
│   ├── UserProfileController.cs
│   ├── QueryDtos.cs                   # Shared pagination/filter DTOs for controllers
│   ├── Admin/
│   │   ├── AdminHotelController.cs
│   │   ├── AdminRoomController.cs
│   │   ├── AdminRoomTypeController.cs
│   │   ├── AdminInventoryController.cs
│   │   ├── AdminReservationController.cs
│   │   ├── AdminAmenityRequestController.cs
│   │   ├── AdminAuditLogController.cs
│   │   ├── AdminReviewController.cs
│   │   ├── AdminSupportController.cs
│   │   ├── AdminTransactionController.cs
│   │   └── AdminWalletController.cs
│   ├── Guest/
│   │   ├── GuestReservationController.cs
│   │   ├── GuestPaymentController.cs
│   │   ├── GuestPromoCodeController.cs
│   │   ├── GuestSupportController.cs
│   │   └── GuestWalletController.cs
│   ├── Public/
│   │   ├── PublicHotelController.cs
│   │   ├── PublicAmenityController.cs
│   │   └── PublicSupportController.cs
│   └── SuperAdmin/
│       ├── SuperAdminHotelController.cs
│       ├── SuperAdminAmenityController.cs
│       ├── SuperAdminAmenityRequestController.cs
│       ├── SuperAdminAuditLogController.cs
│       ├── SuperAdminRevenueController.cs
│       └── SuperAdminSupportController.cs
│
└── Exceptions/
    ├── AppExceptions.cs               # Custom exception hierarchy
    └── Middleware/
        └── GlobalExceptionMiddleware.cs  # Catches all unhandled exceptions
```

---

## 4. Configuration — appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ReviewSettings": {
    "RewardPoints": 10
  },
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 60
      }
    ]
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Developer": "Server=(localdb)\\MSSQLLocalDB;TrustServerCertificate=True;Integrated Security=True;Database=dbHotelBookingAppV8;"
  },
  "Keys": {
    "Jwt": "Lets have a very long key for jwt token generation, at least 32 characters long."
  }
}
```

**Key fields explained:**

| Key | Purpose |
|-----|---------|
| `ConnectionStrings:Developer` | SQL Server connection string. `(localdb)\MSSQLLocalDB` is the local development SQL Server instance. `Integrated Security=True` uses Windows login. `TrustServerCertificate=True` bypasses SSL certificate check locally. |
| `Keys:Jwt` | Secret key used to sign and verify JWT tokens. Must be at least 32 characters. **Never commit this to git in production — use environment variables or Azure Key Vault.** |
| `IpRateLimiting` | Limits each IP to 60 requests per minute across all endpoints. Returns HTTP 429 when exceeded. |
| `ReviewSettings:RewardPoints` | Business config (not hard-coded). When a guest submits a review, they earn 10 wallet reward points. Read via `IConfiguration`. |

**How to read config in C#:**
```csharp
// In a service constructor:
public TokenService(IConfiguration configuration)
{
    string secret = configuration["Keys:Jwt"]
        ?? throw new InvalidOperationException("JWT Key not configured.");
}

// Nested access uses colon notation:
// "ReviewSettings:RewardPoints" => configuration["ReviewSettings:RewardPoints"]
```

---

## 5. Program.cs — Application Startup

`Program.cs` is the application entry point. It builds the DI container, configures the middleware pipeline, and starts the server. Here is the full annotated version:

```csharp
var builder = WebApplication.CreateBuilder(args);
// builder is a WebApplicationBuilder. It gives access to:
// builder.Services  → the DI container (IServiceCollection)
// builder.Configuration → appsettings.json values
// builder.Environment  → Development / Production

// ── 1. CONTROLLERS ──────────────────────────────────────────
builder.Services.AddControllers();
// Registers all classes annotated with [ApiController] [Route(...)]
// as HTTP request handlers.

builder.Services.AddEndpointsApiExplorer();
// Required for Swagger to discover minimal API endpoints.

// ── 2. RATE LIMITING ────────────────────────────────────────
builder.Services.AddMemoryCache();
// AspNetCoreRateLimit stores its counters in an in-memory cache.

builder.Services.Configure<IpRateLimitOptions>(
    builder.Configuration.GetSection("IpRateLimiting"));
// Binds the "IpRateLimiting" JSON section to the IpRateLimitOptions object.

builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// ── 3. SWAGGER ──────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Hotel Booking API", Version = "v1" });

    // Adds a "Bearer" button to Swagger UI so you can paste your JWT.
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { ... });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { ... });
});

// ── 4. DATABASE ─────────────────────────────────────────────
builder.Services.AddDbContext<HotelBookingContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Developer"),
        sqlOptions => sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
    ));
// Registers HotelBookingContext as a Scoped service.
// QuerySplittingBehavior.SplitQuery avoids Cartesian product explosions
// when loading multiple collection navigations (see Section 19).

// ── 5. CORS ─────────────────────────────────────────────────
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
// Allows any frontend origin during development.
// In production, restrict to your domain.

// ── 6. GENERIC REPOSITORY ───────────────────────────────────
builder.Services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
// This one line registers the GENERIC REPOSITORY for ALL entity types.
// typeof(IRepository<,>) is an open generic type — the <,> means
// "I have two type parameters but I'm not specifying them yet."
// When something asks for IRepository<Guid, Hotel>,
// ASP.NET Core resolves it to Repository<Guid, Hotel>.

// ── 7. UNIT OF WORK ─────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ── 8. ALL APPLICATION SERVICES ─────────────────────────────
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
// ... (all 20+ services registered here)

// ── 9. BACKGROUND SERVICES ──────────────────────────────────
builder.Services.AddHostedService<ReservationCleanupService>();
builder.Services.AddHostedService<HotelDeactivationRefundService>();
builder.Services.AddHostedService<NoShowAutoCancelService>();
// IHostedService runs in the background alongside the web server.

// ── 10. JWT AUTHENTICATION ──────────────────────────────────
string jwtKey = builder.Configuration["Keys:Jwt"]
    ?? throw new InvalidOperationException("JWT Key not found.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        // Without this, ASP.NET renames standard JWT claims.
        // MapInboundClaims=false keeps our custom claim names ("role", "nameid").

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,          // Don't check the "iss" claim
            ValidateAudience = false,        // Don't check the "aud" claim
            ValidateLifetime = true,         // Check the "exp" claim (expiry)
            ValidateIssuerSigningKey = true, // Verify the signature
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = "role",          // Which claim stores the role
            NameClaimType = "unique_name"    // Which claim stores the username
        };
    });

builder.Services.AddAuthorization();

// ── BUILD ───────────────────────────────────────────────────
var app = builder.Build();
// After Build(), the DI container is locked. No more registrations.

// ── MIDDLEWARE PIPELINE ─────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();       // Serves swagger.json at /swagger/v1/swagger.json
    app.UseSwaggerUI();     // Serves the Swagger UI at /swagger
}

app.UseCors();              // Must be BEFORE routing
app.UseIpRateLimiting();    // Checks IP counters before the request reaches any controller
app.UseRouting();           // Determines which controller/action to invoke

app.UseMiddleware<GlobalExceptionMiddleware>(); // Catches ALL exceptions from here on
app.UseAuthentication();    // Reads the JWT from the Authorization header
app.UseAuthorization();     // Checks [Authorize] attributes using the authenticated identity

app.MapControllers();       // Wires controllers to routes
app.Run();                  // Starts the HTTP server
```

**Middleware order matters.** If you put `UseAuthentication` before `GlobalExceptionMiddleware`, authentication errors won't be caught by your middleware. The order above ensures exceptions from anywhere (including auth) are caught and logged.

---

## 6. Domain Models (C# Classes + SQL Tables)

Each C# class in the `Models/` folder becomes a SQL Server table via EF Core. Here is every model explained.

### 6.1 User

```csharp
public class User
{
    [Key]
    public Guid UserId { get; set; }         // PK — uniqueidentifier in SQL

    [Required, MaxLength(150)]
    public string Name { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; }        // Unique index added in DbContext

    [Required]
    public byte[] Password { get; set; }     // HMAC-SHA256 hash — varbinary(max)

    [Required]
    public byte[] PasswordSaltValue { get; set; }  // Random salt — varbinary(max)

    public bool IsActive { get; set; } = true;

    [Required]
    public UserRole Role { get; set; }       // Stored as int (1,2,3) in SQL

    public DateTime CreatedAt { get; set; }

    // Navigation properties (EF Core knows to JOIN on these)
    public UserProfileDetails? UserDetails { get; set; }  // 1-to-1
    public Guid? HotelId { get; set; }       // FK — only set for Admin role
    public Hotel? Hotel { get; set; }
    public ICollection<Reservation>? Reservations { get; set; }  // 1-to-many
    public ICollection<Review>? Reviews { get; set; }
    public ICollection<Log>? Logs { get; set; }
    public ICollection<AuditLog>? AuditLogs { get; set; }
}

public enum UserRole
{
    Guest = 1,
    Admin = 2,
    SuperAdmin = 3
}
```

**SQL Table generated:**
```sql
CREATE TABLE Users (
    UserId               UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Name                 NVARCHAR(150)    NOT NULL,
    Email                NVARCHAR(450)    NOT NULL,   -- auto-sized for index
    Password             VARBINARY(MAX)   NOT NULL,
    PasswordSaltValue    VARBINARY(MAX)   NOT NULL,
    IsActive             BIT              NOT NULL DEFAULT 1,
    Role                 INT              NOT NULL,   -- enum stored as int
    CreatedAt            DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    HotelId              UNIQUEIDENTIFIER NULL
);
CREATE UNIQUE INDEX IX_Users_Email ON Users(Email);
```

**Key C# concept — `Guid` as primary key:**
Using `Guid` (UUID) instead of `int` is common in distributed systems. The database generates the value at insert time (or you set it in C# before adding). It prevents ID guessing attacks and works in distributed/replica scenarios.

**Key C# concept — `enum` stored as `int`:**
The `UserRole` enum has integer values (1, 2, 3). EF Core stores them as `INT` in the database. When you read from the database, EF Core converts the integer back to the enum. The Fluent API line `.HasConversion<int>()` makes this explicit.

---

### 6.2 Hotel

```csharp
public class Hotel
{
    [Key] public Guid HotelId { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; }
    [Required, MaxLength(500)] public string Address { get; set; }
    [Required, MaxLength(100)] public string City { get; set; }   // Indexed
    [MaxLength(100)]           public string State { get; set; }  // Indexed
    [MaxLength(1000)]          public string Description { get; set; }
    public string ImageUrl { get; set; }
    [Required, MaxLength(15)]  public string ContactNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsBlockedBySuperAdmin { get; set; } = false;
    [MaxLength(50)] public string? UpiId { get; set; }  // For payment simulation
    public decimal GstPercent { get; set; } = 0;        // Precision(5,2) in SQL
    public DateTime CreatedAt { get; set; }

    // Navigation
    public ICollection<RoomType>? RoomTypes { get; set; }
    public ICollection<Room>? Rooms { get; set; }
    public ICollection<Review>? Reviews { get; set; }
    public ICollection<Reservation>? Reservations { get; set; }
}
```

Two indexed columns (`City`, `State`) allow fast hotel searches by location without full-table scans.

---

### 6.3 RoomType

```csharp
public class RoomType
{
    [Key] public Guid RoomTypeId { get; set; }
    [Required] public Guid HotelId { get; set; }   // FK — Indexed
    [Required] public string Name { get; set; }     // e.g. "Deluxe Suite"
    public string Description { get; set; }
    [Required] public int MaxOccupancy { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;

    // Many-to-many: a RoomType has many Amenities via join table
    public ICollection<RoomTypeAmenity>? RoomTypeAmenities { get; set; }
    public Hotel? Hotel { get; set; }
    public ICollection<Room>? Rooms { get; set; }
    public ICollection<RoomTypeRate>? Rates { get; set; }
    public ICollection<RoomTypeInventory>? Inventories { get; set; }
}
```

---

### 6.4 RoomTypeRate (Date-based Pricing)

```csharp
public class RoomTypeRate
{
    [Key] public Guid RoomTypeRateId { get; set; }
    [Required] public Guid RoomTypeId { get; set; }
    [Required] public DateOnly StartDate { get; set; }
    [Required] public DateOnly EndDate { get; set; }
    [Required] public decimal Rate { get; set; }  // Precision(18,2)
    public RoomType? RoomType { get; set; }
}
```

A Composite Index on `(RoomTypeId, StartDate, EndDate)` makes date-range queries efficient. To get the rate for a booking, the service queries: `WHERE RoomTypeId = ? AND StartDate <= checkIn AND EndDate >= checkOut`.

---

### 6.5 RoomTypeInventory (Per-Date Availability)

```csharp
public class RoomTypeInventory
{
    [Key] public Guid RoomTypeInventoryId { get; set; }
    [Required] public Guid RoomTypeId { get; set; }
    [Required] public DateOnly Date { get; set; }
    [Required] public int TotalInventory { get; set; }  // Total rooms of this type
    [Required] public int ReservedInventory { get; set; }

    [NotMapped]  // NOT a database column — computed in memory
    public int AvailableInventory => TotalInventory - ReservedInventory;

    public RoomType? RoomType { get; set; }
}
```

A **unique index** on `(RoomTypeId, Date)` ensures there is at most one inventory record per room type per day. When a reservation is made, the service increments `ReservedInventory` for each date in the stay. When cancelled, it decrements.

**`[NotMapped]` attribute:** Tells EF Core to ignore this property when creating/querying the table. It is a computed C# property only.

---

### 6.6 Room

```csharp
public class Room
{
    [Key] public Guid RoomId { get; set; }
    [Required] public string RoomNumber { get; set; }  // e.g. "101"
    [Required] public int Floor { get; set; }
    [Required] public Guid HotelId { get; set; }
    [Required] public Guid RoomTypeId { get; set; }
    public bool IsActive { get; set; } = true;

    public Hotel? Hotel { get; set; }
    public RoomType? RoomType { get; set; }
    public ICollection<ReservationRoom>? ReservationRooms { get; set; }
}
```

Unique index on `(HotelId, RoomNumber)` prevents two rooms in the same hotel from having the same number.

---

### 6.7 Reservation

```csharp
public class Reservation
{
    [Key] public Guid ReservationId { get; set; }
    [Required] public string ReservationCode { get; set; }  // Unique, human-readable e.g. "RES-ABC123"
    [Required] public Guid UserId { get; set; }
    [Required] public Guid HotelId { get; set; }
    [Required] public DateOnly CheckInDate { get; set; }
    [Required] public DateOnly CheckOutDate { get; set; }
    [Required] public decimal TotalAmount { get; set; }  // Base amount before discounts
    [Required] public ReservationStatus Status { get; set; }
    public bool IsCheckedIn { get; set; } = false;
    public DateTime? CancelledDate { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? ExpiryTime { get; set; }  // Pending reservations expire after 15 min

    // Financial breakdown
    public decimal GstPercent { get; set; } = 0;      // e.g. 18.00
    public decimal GstAmount { get; set; } = 0;       // Computed: TotalAmount * GstPercent / 100
    public decimal DiscountPercent { get; set; } = 0; // From promo code
    public decimal DiscountAmount { get; set; } = 0;
    public decimal WalletAmountUsed { get; set; } = 0;
    public string? PromoCodeUsed { get; set; }
    public decimal FinalAmount { get; set; } = 0;     // What guest actually pays
    public bool CancellationFeePaid { get; set; } = false;
    public decimal CancellationFeeAmount { get; set; } = 0;  // 10% of TotalAmount

    public DateTime CreatedDate { get; set; }

    public User? User { get; set; }
    public Hotel? Hotel { get; set; }
    public ICollection<ReservationRoom>? ReservationRooms { get; set; }
    public ICollection<Transaction>? Transactions { get; set; }
}

public enum ReservationStatus
{
    Pending = 1,     // Created but not yet paid
    Confirmed = 2,   // Payment received
    Cancelled = 3,   // Cancelled by guest or system
    Completed = 4,   // Stay finished — marked by admin
    NoShow = 5       // Guest never checked in
}
```

---

### 6.8 ReservationRoom (Booking Detail)

```csharp
public class ReservationRoom
{
    [Key] public Guid ReservationRoomId { get; set; }
    [Required] public Guid ReservationId { get; set; }
    [Required] public Guid RoomTypeId { get; set; }
    [Required] public Guid RoomId { get; set; }
    [Required] public decimal PricePerNight { get; set; }

    public Reservation? Reservation { get; set; }
    public RoomType? RoomType { get; set; }
    public Room? Room { get; set; }

    [NotMapped]  // Computed in C# only — not stored in DB
    public bool IsCurrentlyOccupied =>
        Reservation != null &&
        Reservation.Status == ReservationStatus.Confirmed &&
        Reservation.CheckInDate <= DateOnly.FromDateTime(DateTime.UtcNow) &&
        Reservation.CheckOutDate > DateOnly.FromDateTime(DateTime.UtcNow);
}
```

This is the **junction/bridge table** between Reservations and Rooms. One reservation can have multiple rooms.

---

### 6.9 Transaction

```csharp
public class Transaction
{
    [Key] public Guid TransactionId { get; set; }
    [Required] public Guid ReservationId { get; set; }
    [Required] public decimal Amount { get; set; }
    [Required] public PaymentMethod PaymentMethod { get; set; }
    [Required] public PaymentStatus Status { get; set; }
    [Required] public DateTime TransactionDate { get; set; }
    public bool WalletUsed { get; set; } = false;
    public decimal WalletAmountUsed { get; set; } = 0;
    public Reservation? Reservation { get; set; }
}

public enum PaymentMethod { CreditCard=1, DebitCard=2, UPI=3, NetBanking=4, Wallet=5 }
public enum PaymentStatus  { Pending=1, Success=2, Failed=3, Refunded=4 }
```

---

### 6.10 Review

```csharp
public class Review
{
    [Key] public Guid ReviewId { get; set; }
    [Required] public Guid UserId { get; set; }
    [Required] public Guid HotelId { get; set; }
    [Required] public Guid ReservationId { get; set; }  // One review per completed reservation
    [Range(1,5)] public decimal Rating { get; set; }
    [Required] public string Comment { get; set; }
    public string? ImageUrl { get; set; }
    public string? AdminReply { get; set; }  // Hotel admin can reply
    public DateTime CreatedDate { get; set; }
    // Navigation...
}
```

A unique index on `(UserId, ReservationId)` enforces the **business rule**: a guest can only write one review per reservation.

---

### 6.11 Wallet + WalletTransaction

```csharp
public class Wallet
{
    [Key] public Guid WalletId { get; set; }
    [Required] public Guid UserId { get; set; }
    public decimal Balance { get; set; } = 0;   // Precision(18,2)
    public DateTime UpdatedAt { get; set; }
    public User? User { get; set; }
    public ICollection<WalletTransaction>? WalletTransactions { get; set; }
}

public class WalletTransaction
{
    [Key] public Guid WalletTransactionId { get; set; }
    [Required] public Guid WalletId { get; set; }
    [Required] public decimal Amount { get; set; }
    [Required, MaxLength(10)] public string Type { get; set; } // "Credit" | "Debit"
    [MaxLength(500)] public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public Wallet? Wallet { get; set; }
}
```

Every balance change is recorded as a `WalletTransaction`. This gives a full audit trail.

---

### 6.12 PromoCode

```csharp
public class PromoCode
{
    [Key] public Guid PromoCodeId { get; set; }
    [Required, MaxLength(20)] public string Code { get; set; }  // Unique
    [Required] public Guid UserId { get; set; }
    [Required] public Guid HotelId { get; set; }
    [Required] public Guid ReservationId { get; set; } // Which stay earned this code
    public decimal DiscountPercent { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime CreatedAt { get; set; }
}
```

Promo codes are **system-generated** when a guest completes a stay. They are user-specific and hotel-specific — a code earned at Hotel A cannot be used at Hotel B.

---

### 6.13 AuditLog

```csharp
public class AuditLog
{
    [Key] public Guid AuditLogId { get; set; }
    public Guid? UserId { get; set; }
    [Required, MaxLength(100)] public string Action { get; set; }      // e.g. "HotelUpdated"
    [Required, MaxLength(100)] public string EntityName { get; set; }  // e.g. "Hotel"
    public Guid? EntityId { get; set; }
    public string Changes { get; set; }   // JSON string: { "OldName": "...", "NewName": "..." }
    public DateTime CreatedAt { get; set; }
    public User? User { get; set; }
}
```

Stores a **JSON diff** of what changed. Used by services like `HotelService` when an admin updates hotel details — the old and new values are serialized to JSON and saved here.

---

### 6.14 Log (Error/Exception Log)

```csharp
public class Log
{
    [Key] public Guid LogId { get; set; }
    public string Message { get; set; }
    public string ExceptionType { get; set; }
    public string StackTrace { get; set; }
    public int StatusCode { get; set; }
    public string UserName { get; set; } = "Anonymous";
    public string Role { get; set; } = "Anonymous";
    public Guid? UserId { get; set; }
    public string Controller { get; set; }
    public string Action { get; set; }
    public string HttpMethod { get; set; }  // GET, POST, etc.
    public string RequestPath { get; set; }
    public DateTime CreatedAt { get; set; }
    public User? User { get; set; }
}
```

Written by `GlobalExceptionMiddleware` every time an unhandled exception occurs. Admins and SuperAdmins can view these through the API.

---

### 6.15 SupportRequest

```csharp
public class SupportRequest
{
    [Key] public Guid SupportRequestId { get; set; }
    public Guid? UserId { get; set; }          // Null for anonymous submissions
    [MaxLength(20)] public string? SubmitterRole { get; set; }  // "Guest", "Admin", null
    [MaxLength(150)] public string? GuestName { get; set; }     // Anonymous user's name
    [MaxLength(200)] public string? GuestEmail { get; set; }    // Anonymous user's email
    [Required, MaxLength(100)] public string Subject { get; set; }
    [Required, MaxLength(2000)] public string Message { get; set; }
    [Required, MaxLength(50)] public string Category { get; set; }
    [MaxLength(50)] public string? ReservationCode { get; set; }
    public Guid? HotelId { get; set; }
    public SupportRequestStatus Status { get; set; } = SupportRequestStatus.Open;
    [MaxLength(2000)] public string? AdminResponse { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public User? User { get; set; }
    public Hotel? Hotel { get; set; }
}

public enum SupportRequestStatus { Open=1, InProgress=2, Resolved=3 }
```

Supports three submitter types: unauthenticated (no UserId), authenticated Guest, and Admin.

---

### 6.16 SuperAdminRevenue

```csharp
public class SuperAdminRevenue
{
    [Key] public Guid SuperAdminRevenueId { get; set; }
    [Required] public Guid ReservationId { get; set; }
    [Required] public Guid HotelId { get; set; }
    public decimal ReservationAmount { get; set; }
    public decimal CommissionAmount { get; set; }  // Always 2% of ReservationAmount
    [MaxLength(100)] public string SuperAdminUpiId { get; set; } = "thanushstayhubsuperadmin@okaxis";
    public DateTime CreatedAt { get; set; }
    public Reservation? Reservation { get; set; }
    public Hotel? Hotel { get; set; }
}
```

Created automatically when `CompleteReservationAsync` is called. The platform charges 2% commission on every completed booking.

---

## 7. DbContext — EF Core + Fluent API + Seed Data

`HotelBookingContext` is the bridge between your C# code and the SQL Server database.

```csharp
public class HotelBookingContext : DbContext
{
    public HotelBookingContext(DbContextOptions<HotelBookingContext> options)
        : base(options) { }

    // Each DbSet<T> = one table in SQL
    public DbSet<User> Users { get; set; }
    public DbSet<Hotel> Hotels { get; set; }
    public DbSet<RoomType> RoomTypes { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    // ... all other entities
}
```

**Fluent API in `OnModelCreating`:**

The Fluent API gives more control than Data Annotations. It runs once when EF Core builds the model.

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ── UNIQUE EMAIL INDEX ───────────────────────────────────
    modelBuilder.Entity<User>()
        .HasIndex(u => u.Email)
        .IsUnique();
    // → SQL: CREATE UNIQUE INDEX IX_Users_Email ON Users(Email)

    // ── ENUM STORED AS INT ───────────────────────────────────
    modelBuilder.Entity<User>()
        .Property(u => u.Role)
        .HasConversion<int>();
    // → Stores enum as INT in SQL, converts back when reading

    // ── DEFAULT SQL VALUE ────────────────────────────────────
    modelBuilder.Entity<User>()
        .Property(u => u.CreatedAt)
        .HasDefaultValueSql("GETUTCDATE()");
    // → SQL: CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()

    // ── ONE-TO-ONE RELATIONSHIP ──────────────────────────────
    modelBuilder.Entity<User>()
        .HasOne(u => u.UserDetails)           // User has one UserDetails
        .WithOne(d => d.User)                 // UserDetails belongs to one User
        .HasForeignKey<UserProfileDetails>(d => d.UserId) // FK is on UserProfileDetails
        .OnDelete(DeleteBehavior.Cascade);    // Delete profile when user deleted

    // ── ONE-TO-MANY WITH RESTRICT DELETE ────────────────────
    modelBuilder.Entity<User>()
        .HasMany(u => u.Reservations)
        .WithOne(r => r.User)
        .HasForeignKey(r => r.UserId)
        .OnDelete(DeleteBehavior.Restrict);
    // → Restrict = can't delete user if they have reservations

    // ── DECIMAL PRECISION ────────────────────────────────────
    modelBuilder.Entity<RoomTypeRate>()
        .Property(r => r.Rate)
        .HasPrecision(18, 2);
    // → SQL: Rate DECIMAL(18,2) — 18 digits total, 2 after decimal point

    // ── COMPOSITE INDEX ──────────────────────────────────────
    modelBuilder.Entity<RoomTypeInventory>()
        .HasIndex(i => new { i.RoomTypeId, i.Date })
        .IsUnique();
    // → SQL: CREATE UNIQUE INDEX on (RoomTypeId, Date)

    // ── MANY-TO-MANY JOIN TABLE ──────────────────────────────
    modelBuilder.Entity<RoomTypeAmenity>()
        .HasKey(rta => new { rta.RoomTypeId, rta.AmenityId }); // COMPOSITE PK
    // A RoomTypeAmenity row is identified by BOTH columns together.
    // No surrogate key needed.

    modelBuilder.Entity<RoomTypeAmenity>()
        .HasOne(rta => rta.RoomType)
        .WithMany(rt => rt.RoomTypeAmenities)
        .HasForeignKey(rta => rta.RoomTypeId)
        .OnDelete(DeleteBehavior.Cascade); // Delete links when RoomType deleted

    // ── SEED DATA ────────────────────────────────────────────
    modelBuilder.Entity<Amenity>().HasData(
        new Amenity {
            AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Name = "WiFi", Category = "Tech", IconName = "wifi", IsActive = true
        },
        // 30 amenities pre-seeded...
    );
    // HasData() = run as part of migrations.
    // SQL: INSERT INTO Amenities VALUES (...) if not exists
}
```

**EF Core Migrations workflow:**
```bash
# Create a new migration (records what changed in your model)
dotnet ef migrations add InitialCreate

# Apply all pending migrations to SQL Server
dotnet ef database update

# View the SQL that will be executed (dry run)
dotnet ef migrations script
```

Each migration creates a C# class with `Up()` (apply changes) and `Down()` (undo changes) methods.

---

## 8. Generic Repository Pattern

The repository pattern separates data access from business logic.

### Interface

```csharp
public interface IRepository<TKey, TEntity> where TEntity : class
{
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<TEntity?> GetAsync(TKey key);
    Task<TEntity?> AddAsync(TEntity entity);
    Task<TEntity?> DeleteAsync(TKey key);
    Task<TEntity?> UpdateAsync(TKey key, TEntity entity);
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);
    IQueryable<TEntity> GetQueryable();
    Task<IEnumerable<TEntity>> GetAllByForeignKeyAsync(
        Expression<Func<TEntity, bool>> predicate, int limit, int pageNumber);
}
```

**Generic type parameters:**
- `TKey` — the type of the primary key (usually `Guid`)
- `TEntity` — the entity class (`User`, `Hotel`, etc.)

**`where TEntity : class`** — a generic constraint. It means `TEntity` must be a reference type (a class), not a value type like `int`. EF Core's `Set<T>()` requires this.

### Implementation

```csharp
public class Repository<TKey, TEntity> : IRepository<TKey, TEntity>
    where TEntity : class
{
    protected readonly HotelBookingContext _context;

    public Repository(HotelBookingContext context) => _context = context;

    public async Task<TEntity?> AddAsync(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await _context.Set<TEntity>().AddAsync(entity);
        return entity;
        // ⚠ Does NOT save to DB yet. Call unitOfWork.CommitAsync() to save.
    }

    public async Task<TEntity?> GetAsync(TKey key)
        => await _context.FindAsync<TEntity>(key);
    // FindAsync looks in the change tracker first (memory),
    // then hits the database if not found. Efficient for PK lookups.

    public async Task<TEntity?> UpdateAsync(TKey key, TEntity entity)
    {
        var existing = await GetAsync(key);
        if (existing is null) return null;
        _context.Entry(existing).CurrentValues.SetValues(entity);
        // SetValues copies all scalar properties from 'entity' to 'existing'.
        // Navigation properties are NOT copied.
        return existing;
    }

    public async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate)
        => await _context.Set<TEntity>().FirstOrDefaultAsync(predicate);
    // Expression<Func<TEntity, bool>> = a LINQ lambda that EF Core
    // can translate to SQL. e.g. u => u.Email == "test@test.com"

    public IQueryable<TEntity> GetQueryable()
        => _context.Set<TEntity>();
    // Returns an IQueryable — you can chain .Where().Include().OrderBy()
    // on it in the service layer. The SQL is not executed until
    // .ToListAsync() or similar is called.

    public async Task<IEnumerable<TEntity>> GetAllByForeignKeyAsync(
        Expression<Func<TEntity, bool>> predicate, int limit, int pageNumber)
        => await _context.Set<TEntity>()
            .Where(predicate)
            .Skip((pageNumber - 1) * limit)
            .Take(limit)
            .ToListAsync();
    // Pagination: Skip = how many to skip, Take = how many to return.
    // SQL: SELECT ... OFFSET ? ROWS FETCH NEXT ? ROWS ONLY
}
```

**How services use it:**
```csharp
// In a service constructor, inject the generic repo for each entity:
public AuthService(
    IRepository<Guid, User> userRepository,
    IRepository<Guid, Hotel> hotelRepository,
    ...)

// Usage in a method:
var user = await _userRepository.GetAsync(userId);
var users = await _userRepository.GetQueryable()
    .Where(u => u.IsActive)
    .Include(u => u.UserDetails)
    .ToListAsync();
```

**Why generic repository?** Without it, you'd write a separate `UserRepository`, `HotelRepository`, etc. each with the same 5 CRUD methods. The generic version eliminates all that duplication. Services that need custom queries use `GetQueryable()` to build them.

---

## 9. Unit of Work Pattern

The Unit of Work (UoW) pattern wraps database transactions. When you need to do multiple writes that must all succeed or all fail together, you use UoW.

```csharp
public interface IUnitOfWork
{
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
    Task SaveChangesAsync();
}
```

### Implementation

```csharp
public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly HotelBookingContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(HotelBookingContext context) => _context = context;

    public async Task BeginTransactionAsync()
    {
        if (_transaction is not null) return; // Guard: no nested transactions
        _transaction = await _context.Database.BeginTransactionAsync();
        // → SQL: BEGIN TRANSACTION
    }

    public async Task CommitAsync()
    {
        if (_transaction is null)
        {
            await _context.SaveChangesAsync(); // No explicit transaction — just save
            return;
        }
        try
        {
            await _context.SaveChangesAsync(); // Writes all tracked changes
            await _transaction.CommitAsync();  // → SQL: COMMIT
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackAsync()
    {
        if (_transaction is null) return;
        try
        {
            await _transaction.RollbackAsync(); // → SQL: ROLLBACK
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }
}
```

### Pattern Used in Services

```csharp
public async Task<AuthResponseDto> RegisterGuestAsync(RegisterUserDto dto)
{
    await EnsureEmailIsUniqueAsync(dto.Email);  // Throws if duplicate

    await _unitOfWork.BeginTransactionAsync();  // BEGIN TRANSACTION
    try
    {
        var user = await CreateGuestUserAsync(dto);   // AddAsync — tracked
        await CreateUserProfileAsync(user.UserId, dto.Name, dto.Email);  // AddAsync
        await _unitOfWork.CommitAsync();  // SaveChanges + COMMIT
        // Both rows inserted atomically
        await _walletService.EnsureWalletExistsAsync(user.UserId);
        return BuildAuthResponse(user);
    }
    catch
    {
        await _unitOfWork.RollbackAsync();  // ROLLBACK — neither row saved
        throw;
    }
}
```

**Without UoW:** If `CreateUserProfileAsync` fails after `CreateGuestUserAsync` succeeded, you'd have a User row with no profile — a corrupted state. With UoW and `ROLLBACK`, neither row is saved.

---

## 10. Interfaces (Service Contracts)

Every service has a corresponding interface. This is the **Dependency Inversion Principle** — high-level modules depend on abstractions, not concrete classes.

### IRepository (Generic)
Already shown in Section 8.

### IUnitOfWork
Already shown in Section 9.

### IAuthService
```csharp
public interface IAuthService
{
    Task<AuthResponseDto> RegisterGuestAsync(RegisterUserDto dto);
    Task<AuthResponseDto> RegisterHotelAdminAsync(RegisterHotelAdminDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
}
```

### IReservationService (most complex)
```csharp
public interface IReservationService
{
    Task<ReservationResponseDto> CreateReservationAsync(Guid userId, CreateReservationDto dto);
    Task<ReservationDetailsDto> GetReservationByCodeAsync(Guid userId, string reservationCode);
    Task<IEnumerable<ReservationDetailsDto>> GetMyReservationsAsync(Guid userId);
    Task<PagedReservationResponseDto> GetMyReservationsPagedAsync(
        Guid userId, int page, int pageSize, string? status = null, string? search = null);
    Task<bool> CancelReservationAsync(Guid userId, string reservationCode, string reason);
    Task<bool> CompleteReservationAsync(string reservationCode);
    Task<bool> ConfirmReservationAsync(string reservationCode);
    Task<PagedReservationResponseDto> GetAdminReservationsAsync(
        Guid adminUserId, string? status, string? search,
        int page, int pageSize, string? sortField = null, string? sortDir = null);
    Task<IEnumerable<AvailableRoomDto>> GetAvailableRoomsAsync(
        Guid hotelId, Guid roomTypeId, DateOnly checkIn, DateOnly checkOut);
    Task<IEnumerable<RoomOccupancyDto>> GetRoomOccupancyAsync(Guid adminUserId, DateOnly date);
}
```

### ITransactionService
```csharp
public interface ITransactionService
{
    Task<TransactionResponseDto> CreatePaymentAsync(CreatePaymentDto dto);
    Task<TransactionResponseDto> DirectGuestRefundAsync(
        Guid transactionId, Guid userId, RefundRequestDto dto);
    Task<PagedTransactionResponseDto> GetAllTransactionsAsync(
        Guid userId, string role, int page, int pageSize,
        string? sortField = null, string? sortDir = null);
    Task<PaymentIntentDto> GetPaymentIntentAsync(Guid reservationId, Guid userId);
    Task MarkTransactionFailedAsync(Guid transactionId, Guid adminUserId);
    Task RecordFailedPaymentAsync(Guid reservationId, Guid userId);
}
```

### IHotelService
```csharp
public interface IHotelService
{
    // Public (no auth)
    Task<IEnumerable<HotelListItemDto>> GetTopHotelsAsync();
    Task<SearchHotelResponseDto> SearchHotelsAsync(SearchHotelRequestDto request);
    Task<HotelDetailsDto> GetHotelDetailsAsync(Guid hotelId);
    Task<IEnumerable<RoomTypePublicDto>> GetRoomTypesAsync(Guid hotelId);
    Task<IEnumerable<RoomAvailabilityDto>> GetAvailabilityAsync(
        Guid hotelId, DateOnly checkIn, DateOnly checkOut);
    Task<IEnumerable<string>> GetCitiesAsync();
    Task<IEnumerable<HotelListItemDto>> GetHotelsByCityAsync(string city);
    Task<IEnumerable<string>> GetActiveStatesAsync();
    Task<IEnumerable<HotelListItemDto>> GetHotelsByStateAsync(string stateName);

    // Admin
    Task UpdateHotelAsync(Guid userId, UpdateHotelDto dto);
    Task ToggleHotelStatusAsync(Guid userId, bool isActive);
    Task UpdateHotelGstAsync(Guid adminUserId, decimal gstPercent);

    // SuperAdmin
    Task<IEnumerable<SuperAdminHotelListDto>> GetAllHotelsForSuperAdminAsync();
    Task<PagedSuperAdminHotelResponseDto> GetAllHotelsForSuperAdminPagedAsync(
        int page, int pageSize, string? search = null, string? status = null);
    Task BlockHotelAsync(Guid hotelId);
    Task UnblockHotelAsync(Guid hotelId);
}
```

**Why interfaces?**
1. **Testability** — you can swap the real implementation with a mock in unit tests.
2. **DI** — ASP.NET Core's DI container maps `IHotelService` → `HotelService`. The controller only knows the interface.
3. **Clear contracts** — the interface tells you exactly what a service can do without reading the implementation.

---

## 11. Services — Business Logic Layer

Services are where all the domain logic lives. They receive DTOs from controllers, execute business rules, use repositories for data access, and return DTOs.

### 11.1 AuthService

```csharp
public class AuthService(
    IRepository<Guid, User> userRepository,
    IRepository<Guid, Hotel> hotelRepository,
    IRepository<Guid, UserProfileDetails> userProfileRepository,
    IPasswordService passwordService,
    ITokenService tokenService,
    IWalletService walletService,
    IUnitOfWork unitOfWork) : IAuthService
{
    // Guest Registration Flow:
    // 1. Check email uniqueness
    // 2. Begin DB transaction
    // 3. Hash password with new salt
    // 4. Create User entity (role = Guest)
    // 5. Create UserProfileDetails entity
    // 6. Commit transaction (both rows saved atomically)
    // 7. Create wallet for the new user
    // 8. Generate JWT token and return
    public async Task<AuthResponseDto> RegisterGuestAsync(RegisterUserDto dto)
    {
        await EnsureEmailIsUniqueAsync(dto.Email);
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var user = await CreateGuestUserAsync(dto);
            await CreateUserProfileAsync(user.UserId, dto.Name, dto.Email);
            await _unitOfWork.CommitAsync();
            await _walletService.EnsureWalletExistsAsync(user.UserId);
            return BuildAuthResponse(user);
        }
        catch { await _unitOfWork.RollbackAsync(); throw; }
    }

    // Login Flow:
    // 1. Find user by email
    // 2. Re-hash submitted password with stored salt
    // 3. Compare hash with stored hash
    // 4. If match, generate JWT
    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.FirstOrDefaultAsync(u => u.Email == dto.Email)
            ?? throw new UnAuthorizedException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnAuthorizedException("Account is deactivated.");

        var computedHash = _passwordService.HashPassword(dto.Password, user.PasswordSaltValue, out _);
        if (!computedHash.SequenceEqual(user.Password))
            throw new UnAuthorizedException("Invalid email or password.");

        return BuildAuthResponse(user);
    }
}
```

### 11.2 PasswordService

```csharp
public class PasswordService : IPasswordService
{
    // On REGISTRATION: existingSalt = null → generates a new salt
    // On LOGIN: existingSalt = stored salt → reproduces same hash
    public byte[] HashPassword(string password, byte[]? existingSalt, out byte[]? newSalt)
    {
        using var hmac = CreateHmac(existingSalt, out newSalt);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    private static HMACSHA256 CreateHmac(byte[]? existingSalt, out byte[]? newSalt)
    {
        if (existingSalt is null)
        {
            var hmac = new HMACSHA256();    // Generates random 64-byte key as salt
            newSalt = hmac.Key;
            return hmac;
        }
        newSalt = null;
        return new HMACSHA256(existingSalt); // Uses the stored salt as the HMAC key
    }
}
```

**How HMAC-SHA256 password hashing works:**
- `HMACSHA256` uses a key (the salt) to hash data (the password).
- Same password + same salt → always produces the same hash.
- Different salts → completely different hashes, even for the same password.
- This prevents rainbow table attacks.

### 11.3 TokenService

```csharp
public class TokenService : ITokenService
{
    private readonly SymmetricSecurityKey _key;

    public TokenService(IConfiguration configuration)
    {
        string secret = configuration["Keys:Jwt"]!;
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    }

    public string CreateToken(TokenPayloadDto payload)
    {
        var claims = new List<Claim>
        {
            new("nameid",      payload.UserId.ToString()),  // User's GUID
            new("unique_name", payload.UserName),           // User's email or name
            new("role",        payload.Role)                // "Guest", "Admin", "SuperAdmin"
        };

        if (payload.HotelId.HasValue)
            claims.Add(new Claim("HotelId", payload.HotelId.ToString()!));
        // Admin's hotel ID embedded in token — extracted in controllers
        // without needing a DB query.

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(1),   // Token valid for 1 day
            SigningCredentials = new SigningCredentials(
                _key, SecurityAlgorithms.HmacSha256) // Signed with HS256
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }
}
```

### 11.4 ReservationService (Most Complex)

```csharp
public async Task<ReservationResponseDto> CreateReservationAsync(
    Guid userId, CreateReservationDto dto)
{
    await _unitOfWork.BeginTransactionAsync();
    try
    {
        // Step 1: Validate dates (checkIn >= today, checkOut > checkIn)
        await ValidateDatesAsync(dto);

        // Step 2: Load hotel and verify it's active
        var hotel = await GetHotelAsync(dto.HotelId);

        // Step 3: Load room type and verify it belongs to the hotel
        var roomType = await GetRoomTypeAsync(dto.RoomTypeId, dto.HotelId);

        // Step 4: Calculate number of nights
        int nights = dto.CheckOutDate.DayNumber - dto.CheckInDate.DayNumber;

        // Step 5: Get price per night from RoomTypeRate table
        // Finds a rate whose date range covers the entire stay
        decimal pricePerNight = await GetRoomRateAsync(
            dto.RoomTypeId, dto.CheckInDate, dto.CheckOutDate);

        // Step 6: Check inventory for each date in the stay
        await ValidateAndDeductInventoryAsync(
            dto.RoomTypeId, dto.CheckInDate, dto.CheckOutDate, dto.NumberOfRooms);
        // Increments ReservedInventory for each date

        // Step 7: Select or auto-assign rooms
        var rooms = await SelectRoomsAsync(dto, roomType);

        // Step 8: Calculate totals
        decimal totalAmount = pricePerNight * nights * dto.NumberOfRooms;
        decimal gstAmount = totalAmount * hotel.GstPercent / 100;

        // Step 9: Apply promo code if provided
        decimal discountAmount = 0;
        decimal discountPercent = 0;
        if (!string.IsNullOrEmpty(dto.PromoCodeUsed))
        {
            var promoResult = await _promoCodeService.ValidateAsync(
                dto.PromoCodeUsed, userId, dto.HotelId);
            discountPercent = promoResult.DiscountPercent;
            discountAmount = totalAmount * discountPercent / 100;
        }

        // Step 10: Deduct wallet if requested
        decimal walletUsed = 0;
        if (dto.WalletAmountToUse > 0)
        {
            walletUsed = await _walletService.DeductAsync(userId, dto.WalletAmountToUse);
        }

        // Step 11: Final amount
        decimal finalAmount = totalAmount + gstAmount - discountAmount - walletUsed;
        if (dto.PayCancellationFee)
            finalAmount += totalAmount * 0.10m;  // 10% cancellation protection

        // Step 12: Create Reservation entity
        var reservation = new Reservation {
            ReservationId = Guid.NewGuid(),
            ReservationCode = GenerateReservationCode(),
            UserId = userId,
            HotelId = dto.HotelId,
            CheckInDate = dto.CheckInDate,
            CheckOutDate = dto.CheckOutDate,
            TotalAmount = totalAmount,
            GstPercent = hotel.GstPercent,
            GstAmount = gstAmount,
            DiscountPercent = discountPercent,
            DiscountAmount = discountAmount,
            WalletAmountUsed = walletUsed,
            FinalAmount = finalAmount,
            Status = ReservationStatus.Pending,
            ExpiryTime = DateTime.UtcNow.AddMinutes(15), // 15 min to pay
            CreatedDate = DateTime.UtcNow,
        };
        await _reservationRepo.AddAsync(reservation);

        // Step 13: Create ReservationRoom rows (one per room)
        foreach (var room in rooms)
        {
            await _reservationRoomRepo.AddAsync(new ReservationRoom {
                ReservationId = reservation.ReservationId,
                RoomId = room.RoomId,
                RoomTypeId = dto.RoomTypeId,
                PricePerNight = pricePerNight
            });
        }

        await _unitOfWork.CommitAsync();
        return MapToDto(reservation);
    }
    catch
    {
        await _unitOfWork.RollbackAsync();
        throw;
    }
}
```

### 11.5 WalletService

```csharp
public class WalletService : IWalletService
{
    public async Task EnsureWalletExistsAsync(Guid userId)
    {
        var exists = await _walletRepo.GetQueryable()
            .AnyAsync(w => w.UserId == userId);
        if (!exists) await CreateWalletAsync(userId);
    }

    public async Task<decimal> DeductAsync(Guid userId, decimal amount)
    {
        var wallet = await GetOrCreateWalletAsync(userId);
        if (wallet.Balance < amount)
            throw new ValidationException("Insufficient wallet balance.");

        wallet.Balance -= amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        await _walletTransactionRepo.AddAsync(new WalletTransaction {
            WalletId = wallet.WalletId,
            Amount = amount,
            Type = "Debit",
            Description = "Reservation payment",
            CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.SaveChangesAsync();
        return amount;
    }

    public async Task CreditAsync(Guid userId, decimal amount, string description)
    {
        var wallet = await GetOrCreateWalletAsync(userId);
        wallet.Balance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        await _walletTransactionRepo.AddAsync(new WalletTransaction {
            WalletId = wallet.WalletId,
            Amount = amount,
            Type = "Credit",
            Description = description,
            CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.SaveChangesAsync();
    }
}
```

---

## 12. Background Services

Background services run as long-running tasks alongside the web server. In .NET, they inherit from `BackgroundService` and override `ExecuteAsync`.

### 12.1 ReservationCleanupService

```csharp
/// <summary>
/// Runs every 5 minutes. Finds Pending reservations past their ExpiryTime
/// and cancels them, restoring inventory and refunding wallet.
/// </summary>
public class ReservationCleanupService : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMinutes(5);
    private readonly IServiceScopeFactory _scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessExpiredReservationsAsync(stoppingToken);
            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessExpiredReservationsAsync(CancellationToken ct)
    {
        // ⚠ Background services are SINGLETONS in .NET's DI.
        // DbContext is SCOPED. You CANNOT inject it directly.
        // Use IServiceScopeFactory to create a new scope per iteration.
        using var scope = _scopeFactory.CreateScope();
        var reservationRepo = scope.ServiceProvider
            .GetRequiredService<IRepository<Guid, Reservation>>();

        var expired = await reservationRepo.GetQueryable()
            .Where(r => r.Status == ReservationStatus.Pending
                     && r.ExpiryTime < DateTime.UtcNow)
            .Include(r => r.ReservationRooms)
            .ToListAsync(ct);

        foreach (var res in expired)
        {
            res.Status = ReservationStatus.Cancelled;
            // Restore inventory for each date in the stay
            await RestoreInventoryAsync(res, inventoryRepo);
            // Refund wallet if any was deducted
            if (res.WalletAmountUsed > 0)
                await walletService.CreditAsync(res.UserId,
                    res.WalletAmountUsed, "Expired reservation refund");
        }
        await unitOfWork.SaveChangesAsync();
    }
}
```

**Why `IServiceScopeFactory`?**
Background services are registered as Singletons (live the entire app lifetime). `DbContext` and other services are Scoped (live per request). If a Singleton directly holds a Scoped service, it creates a **captive dependency** — the scoped service lives as long as the singleton and can cause bugs. The solution is to create a new DI scope manually per background iteration.

### 12.2 NoShowAutoCancelService

```csharp
/// Marks Confirmed reservations as NoShow when:
/// - Today is past CheckOutDate
/// - IsCheckedIn == false
/// Runs every 5 minutes. No refund issued for no-shows.
```

### 12.3 HotelDeactivationRefundService

```csharp
/// When a hotel is deactivated (IsActive = false), finds all upcoming
/// Confirmed reservations and issues full refunds to guest wallets,
/// then cancels the reservations.
```

---

## 13. Controllers (API Endpoints)

Controllers receive HTTP requests, extract identity claims, call service methods, and return `IActionResult`.

### Common Patterns

```csharp
// All admin controllers follow this pattern:
[Route("api/admin/hotels")]
[ApiController]
[Authorize(Roles = "Admin")]  // Only Admins can access this controller
public class AdminHotelController : ControllerBase
{
    private readonly IHotelService _hotelService;

    // Constructor injection — ASP.NET Core injects IHotelService
    public AdminHotelController(IHotelService hotelService)
        => _hotelService = hotelService;

    // Extract the user's ID from their JWT token claims
    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue("nameid")!);

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateHotelDto dto)
    {
        await _hotelService.UpdateHotelAsync(GetUserId(), dto);
        return Ok(new { success = true, message = "Hotel updated successfully." });
    }

    [HttpPatch("status")]
    public async Task<IActionResult> ToggleStatus([FromQuery] bool isActive)
    {
        await _hotelService.ToggleHotelStatusAsync(GetUserId(), isActive);
        return Ok(new { success = true, message = "Hotel status updated." });
    }
}
```

### Authentication Controller (Public)

```csharp
[Route("api/auth")]
[ApiController]
public class AuthenticationController : ControllerBase
{
    [HttpPost("register-guest")]
    [AllowAnonymous]   // Overrides any global auth policy — no JWT needed
    public async Task<IActionResult> RegisterGuest([FromBody] RegisterUserDto dto)
    {
        var result = await _authService.RegisterGuestAsync(dto);
        return Ok(new { success = true, data = result });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return Ok(new { success = true, data = result });
    }
}
```

### Guest Reservation Controller

```csharp
[Route("api/guest/reservations")]
[ApiController]
[Authorize(Roles = "Guest")]
public class GuestReservationController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReservationDto dto)
    {
        var result = await _reservationService.CreateReservationAsync(GetUserId(), dto);
        return Ok(new { success = true, data = result });
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var result = await _reservationService.GetReservationByCodeAsync(GetUserId(), code);
        return Ok(new { success = true, data = result });
    }

    // Pagination with filters via POST body
    [HttpPost("history")]
    public async Task<IActionResult> GetHistory([FromBody] ReservationHistoryQueryDto dto)
    {
        var result = await _reservationService.GetMyReservationsPagedAsync(
            GetUserId(), dto.Page, dto.PageSize, dto.Status, dto.Search);
        return Ok(new { success = true, data = result });
    }
}
```

### Full Route Map

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/auth/register-guest` | Public | Register guest |
| POST | `/api/auth/register-hotel-admin` | Public | Register hotel + admin |
| POST | `/api/auth/login` | Public | Login |
| GET | `/api/hotels/top` | Public | Top hotels |
| POST | `/api/hotels/search` | Public | Search with filters |
| GET | `/api/hotels/{id}` | Public | Hotel details |
| POST | `/api/guest/reservations` | Guest | Create reservation |
| GET | `/api/guest/reservations/{code}` | Guest | Get reservation |
| POST | `/api/guest/reservations/cancel` | Guest | Cancel reservation |
| POST | `/api/guest/payments` | Guest | Make payment |
| GET | `/api/guest/wallet` | Guest | Wallet balance + history |
| POST | `/api/guest/wallet/topup` | Guest | Top up wallet |
| POST | `/api/reviews` | Guest | Submit review |
| PUT | `/api/admin/hotels` | Admin | Update hotel |
| PATCH | `/api/admin/hotels/status` | Admin | Toggle active |
| POST | `/api/admin/roomtypes` | Admin | Create room type |
| POST | `/api/admin/rooms` | Admin | Add room |
| POST | `/api/admin/inventory` | Admin | Add inventory dates |
| POST | `/api/admin/roomtypes/rate` | Admin | Add pricing rate |
| PATCH | `/api/admin/reservations/{code}/confirm` | Admin | Confirm reservation |
| PATCH | `/api/admin/reservations/{code}/complete` | Admin | Complete stay |
| GET | `/api/superadmin/hotels` | SuperAdmin | All hotels |
| PATCH | `/api/superadmin/hotels/{id}/block` | SuperAdmin | Block hotel |
| GET | `/api/superadmin/revenue` | SuperAdmin | Platform revenue |
| GET | `/api/superadmin/amenities` | SuperAdmin | Manage amenities |

---

## 14. DTOs (Data Transfer Objects)

DTOs are simple classes that carry data between the API and the client. They prevent exposing internal model details (like password hashes).

### Why DTOs?

```csharp
// BAD — exposes password hash and internal fields:
[HttpGet("me")]
public User GetProfile() => user;

// GOOD — returns only what the client needs:
[HttpGet("me")]
public UserProfileResponseDto GetProfile() => MapToDto(user);
```

### Examples

```csharp
// Input DTO — what the client sends
public class RegisterUserDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;
}

// Output DTO — what the API returns
public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    // Only the JWT — never expose UserId, password, role separately
}

// Complex input DTO with optional fields
public class CreateReservationDto
{
    [Required] public Guid HotelId { get; set; }
    [Required] public Guid RoomTypeId { get; set; }
    [Required] public DateOnly CheckInDate { get; set; }
    [Required] public DateOnly CheckOutDate { get; set; }
    [Required, Range(1, int.MaxValue)]
    public int NumberOfRooms { get; set; }

    public List<Guid>? SelectedRoomIds { get; set; }  // Optional
    public string? PromoCodeUsed { get; set; }         // Optional
    public decimal WalletAmountToUse { get; set; } = 0;
    public bool PayCancellationFee { get; set; } = false;
}

// Paged response DTO
public class PagedReservationResponseDto
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public IEnumerable<ReservationResponseDto> Items { get; set; }
}
```

### Pagination Query DTOs (QueryDtos.cs)

```csharp
public class PageQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class ReservationQueryDto : PageQueryDto
{
    public string? Status { get; set; } = "All";
    public string? Search { get; set; }
    public string? SortField { get; set; }
    public string? SortDir { get; set; }
}
```

---

## 15. Exception Handling — Custom Exceptions + Global Middleware

### Custom Exception Hierarchy

```csharp
// Base class — all custom exceptions inherit from this
public class AppException : Exception
{
    public int StatusCode { get; }
    public AppException(string message, int statusCode) : base(message)
        => StatusCode = statusCode;
}

// Specific exceptions with HTTP codes baked in
public class NotFoundException    : AppException  // 404
public class ConflictException    : AppException  // 409
public class ValidationException  : AppException  // 400
public class UnAuthorizedException: AppException  // 401
public class PaymentException     : AppException  // 400
public class ReservationFailedException : AppException  // 400
public class InsufficientInventoryException : AppException  // 409
public class RateNotFoundException : AppException  // 404
public class ReviewException      : AppException  // 400
```

**Usage in services:**
```csharp
var user = await _userRepo.GetAsync(userId)
    ?? throw new NotFoundException("User not found.");

if (hotel.IsBlockedBySuperAdmin)
    throw new ValidationException("Hotel is blocked by SuperAdmin.");

if (wallet.Balance < amount)
    throw new ValidationException("Insufficient wallet balance.");
```

### Global Exception Middleware

```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context); // Run the rest of the pipeline
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // Determine HTTP status code
        var statusCode = ex is AppException appEx ? appEx.StatusCode : 500;
        var message = ex is AppException ? ex.Message : "An unexpected error occurred.";

        // Extract user info from JWT claims for logging
        var userId = GetUserId(context);
        var controller = context.Request.RouteValues["controller"]?.ToString();
        var action = context.Request.RouteValues["action"]?.ToString();

        // Log structured error
        _logger.LogError(ex,
            "Status:{StatusCode} | User:{User} | {Controller}/{Action} | {Message}",
            statusCode, userName, controller, action, message);

        // Persist to Logs table in DB
        using var scope = context.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingContext>();
        await db.Logs.AddAsync(new Log {
            LogId = Guid.NewGuid(),
            Message = message,
            ExceptionType = ex.GetType().Name,
            StackTrace = ex.StackTrace ?? "",
            StatusCode = statusCode,
            UserId = userId,
            // ... all other fields
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Write JSON error response to client
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new {
            success = false,
            message = message,
            statusCode = statusCode
        });
    }
}
```

**Key insight:** Without this middleware, unhandled exceptions would return a 500 with an HTML error page (or nothing). This middleware: (1) sets the correct HTTP status code, (2) returns structured JSON, (3) logs to the database for later review by admins.

---

## 16. JWT Authentication Deep Dive

### What is a JWT?

A JSON Web Token has three parts separated by dots: `header.payload.signature`

```
eyJhbGciOiJIUzI1NiJ9.eyJuYW1laWQiOiJhYmMiLCJyb2xlIjoiR3Vlc3QifQ.XYZ
     ↑ Header (Base64)      ↑ Payload/Claims (Base64)         ↑ Signature
```

**Header** — algorithm info:
```json
{ "alg": "HS256", "typ": "JWT" }
```

**Payload** — claims (data):
```json
{
  "nameid": "550e8400-e29b-41d4-a716-446655440000",
  "unique_name": "john@example.com",
  "role": "Guest",
  "exp": 1714000000
}
```

**Signature** — HMAC-SHA256(base64Header + "." + base64Payload, secretKey)

No one can forge a JWT without the secret key. The server verifies the signature on every request.

### Token Creation (TokenService)

```csharp
var claims = new List<Claim>
{
    new("nameid",      userId.ToString()),
    new("unique_name", userEmail),
    new("role",        role),         // "Guest", "Admin", or "SuperAdmin"
};
if (hotelId.HasValue)
    claims.Add(new("HotelId", hotelId.ToString()!));
// Admin's hotelId embedded — no DB query needed in controllers

var descriptor = new SecurityTokenDescriptor
{
    Subject = new ClaimsIdentity(claims),
    Expires = DateTime.UtcNow.AddDays(1),
    SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
};
```

### Reading Claims in Controllers

```csharp
// Extract user ID from JWT
private Guid GetUserId()
    => Guid.Parse(User.FindFirstValue("nameid")!);

// Extract hotel ID (Admin only)
private Guid GetHotelId()
    => Guid.Parse(User.FindFirstValue("HotelId")!);

// Extract role
private string GetRole()
    => User.FindFirstValue("role")!;
```

`User` is a property on `ControllerBase` — it's a `ClaimsPrincipal` populated by the JWT middleware from the token in the `Authorization: Bearer <token>` header.

### Role-Based Authorization

```csharp
[Authorize(Roles = "Admin")]           // Only Admin
[Authorize(Roles = "SuperAdmin")]      // Only SuperAdmin
[Authorize(Roles = "Guest")]           // Only Guest
[Authorize(Roles = "Admin,SuperAdmin")]// Admin OR SuperAdmin
[AllowAnonymous]                       // No auth required
```

---

## 17. Password Hashing — HMAC-SHA256

```
Registration:
password "MyPass123" + random 64-byte salt
                ↓
        HMACSHA256(salt).ComputeHash(password)
                ↓
     hash = [0xAB, 0xCD, 0xEF, ...]   ← stored in Users.Password
     salt = [0x12, 0x34, ...]          ← stored in Users.PasswordSaltValue

Login:
password "MyPass123" + stored salt
                ↓
        HMACSHA256(stored_salt).ComputeHash(password)
                ↓
     computed_hash = [0xAB, 0xCD, 0xEF, ...] == stored hash? → LOGIN OK
```

**`SequenceEqual` for comparison:**
```csharp
if (!computedHash.SequenceEqual(user.Password))
    throw new UnAuthorizedException("Invalid email or password.");
```

Never use `==` to compare byte arrays in C# — it compares references, not values. `SequenceEqual` is from `System.Linq` and compares element-by-element.

---

## 18. SQL Concepts Used in This Project

### 18.1 Tables and Primary Keys

Every EF Core entity becomes a table. `[Key]` marks the primary key column.

```sql
-- Generated for User model:
CREATE TABLE Users (
    UserId    UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    Email     NVARCHAR(450)    NOT NULL,
    ...
);
```

### 18.2 Unique Indexes

Enforce uniqueness on columns that aren't the primary key.

```sql
-- Fluent API: .HasIndex(u => u.Email).IsUnique()
CREATE UNIQUE INDEX IX_Users_Email ON Users (Email);

-- Fluent API: .HasIndex(r => r.ReservationCode).IsUnique()
CREATE UNIQUE INDEX IX_Reservations_ReservationCode ON Reservations (ReservationCode);

-- Fluent API: .HasIndex(i => new { i.RoomTypeId, i.Date }).IsUnique()
CREATE UNIQUE INDEX IX_RoomTypeInventories_RoomTypeId_Date
    ON RoomTypeInventories (RoomTypeId, Date);
```

### 18.3 Foreign Keys

```sql
-- Fluent API:
-- .HasMany(u => u.Reservations)
-- .WithOne(r => r.User)
-- .HasForeignKey(r => r.UserId)
-- .OnDelete(DeleteBehavior.Restrict)

ALTER TABLE Reservations ADD CONSTRAINT FK_Reservations_Users_UserId
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
    ON DELETE RESTRICT;  -- Cannot delete a user who has reservations
```

**Delete behaviors:**
- `Cascade` — deleting parent also deletes children (e.g., delete User → delete UserProfile)
- `Restrict` — cannot delete parent if children exist (e.g., cannot delete user with reservations)
- `SetNull` — sets the FK column to NULL when parent deleted

### 18.4 Composite Primary Key

```sql
-- Fluent API: .HasKey(rta => new { rta.RoomTypeId, rta.AmenityId })
CREATE TABLE RoomTypeAmenities (
    RoomTypeId  UNIQUEIDENTIFIER NOT NULL,
    AmenityId   UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_RoomTypeAmenities PRIMARY KEY (RoomTypeId, AmenityId)
);
```

No separate `Id` column — the combination of both columns identifies each row uniquely.

### 18.5 Decimal Precision

```sql
-- Fluent API: .HasPrecision(18, 2)
TotalAmount  DECIMAL(18, 2)  -- up to 16 digits before decimal, 2 after
GstPercent   DECIMAL(5,  2)  -- e.g. 18.00

-- Fluent API: .HasPrecision(3, 2)
Rating       DECIMAL(3,  2)  -- e.g. 4.75
```

Always specify precision for `decimal` in SQL to avoid implicit rounding.

### 18.6 Default Values

```sql
-- Fluent API: .HasDefaultValueSql("GETUTCDATE()")
CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
-- If you don't set CreatedAt in C#, SQL Server uses the current UTC time.
```

### 18.7 JOIN Queries (via EF Core Include)

EF Core generates JOINs from `.Include()` calls:

```csharp
// C# LINQ:
var reservation = await _context.Reservations
    .Include(r => r.Hotel)
    .Include(r => r.ReservationRooms)
        .ThenInclude(rr => rr.Room)
    .FirstOrDefaultAsync(r => r.ReservationCode == code);
```

Generated SQL:
```sql
SELECT r.*, h.*, rr.*, ro.*
FROM Reservations r
INNER JOIN Hotels h ON r.HotelId = h.HotelId
LEFT JOIN ReservationRooms rr ON rr.ReservationId = r.ReservationId
LEFT JOIN Rooms ro ON rr.RoomId = ro.RoomId
WHERE r.ReservationCode = 'RES-ABC123'
```

### 18.8 WHERE Clauses

```csharp
// C# → SQL WHERE:
.Where(r => r.Status == ReservationStatus.Pending
         && r.ExpiryTime < DateTime.UtcNow)
// → WHERE Status = 1 AND ExpiryTime < '2026-04-08T15:00:00'
```

### 18.9 OFFSET / FETCH NEXT (Pagination)

```csharp
// C# pagination:
.Skip((pageNumber - 1) * limit)
.Take(limit)

// → SQL Server:
ORDER BY CreatedDate DESC
OFFSET 20 ROWS         -- skip first 20 (page 3, pageSize 10 → skip 20)
FETCH NEXT 10 ROWS ONLY
```

### 18.10 Aggregate Functions

```csharp
// Count:
var total = await query.CountAsync();
// → SELECT COUNT(*) FROM ...

// Sum:
var totalRevenue = await query.SumAsync(t => t.Amount);
// → SELECT SUM(Amount) FROM ...

// Average:
var avgRating = await query.AverageAsync(r => r.Rating);
// → SELECT AVG(Rating) FROM ...
```

### 18.11 Stored Procedures vs EF Core

This project does **not use stored procedures directly**. Instead, EF Core's LINQ-to-SQL provides equivalent power. However, if needed, EF Core can execute stored procedures:

```csharp
// How you COULD call a stored procedure in EF Core:
var results = await _context.Reservations
    .FromSqlRaw("EXEC sp_GetActiveReservations @HotelId", new SqlParameter("@HotelId", hotelId))
    .ToListAsync();
```

**Stored Procedure concept (SQL):**
```sql
-- What a stored procedure looks like in SQL Server:
CREATE PROCEDURE sp_GetHotelRevenue
    @HotelId UNIQUEIDENTIFIER,
    @FromDate DATE,
    @ToDate DATE
AS
BEGIN
    SELECT
        SUM(t.Amount) AS TotalRevenue,
        COUNT(r.ReservationId) AS TotalBookings
    FROM Transactions t
    INNER JOIN Reservations r ON t.ReservationId = r.ReservationId
    WHERE r.HotelId = @HotelId
      AND t.TransactionDate BETWEEN @FromDate AND @ToDate
      AND t.Status = 2  -- Success
END
```

EF Core's equivalent (no stored procedure needed):
```csharp
var revenue = await _context.Transactions
    .Where(t => t.Reservation!.HotelId == hotelId
             && t.TransactionDate >= fromDate
             && t.TransactionDate <= toDate
             && t.Status == PaymentStatus.Success)
    .SumAsync(t => t.Amount);
```

### 18.12 SQL JOINs Theory

```sql
-- INNER JOIN: Returns rows where both sides match
SELECT u.Name, r.ReservationCode
FROM Users u
INNER JOIN Reservations r ON u.UserId = r.UserId

-- LEFT JOIN: Returns ALL rows from left, matched or NULL from right
SELECT h.Name, AVG(rv.Rating) as AvgRating
FROM Hotels h
LEFT JOIN Reviews rv ON h.HotelId = rv.HotelId
GROUP BY h.Name

-- Multi-table JOIN (used in reservation details):
SELECT res.ReservationCode, h.Name as HotelName,
       rr.PricePerNight, rm.RoomNumber, rt.Name as RoomTypeName
FROM Reservations res
INNER JOIN Hotels h ON res.HotelId = h.HotelId
INNER JOIN ReservationRooms rr ON res.ReservationId = rr.ReservationId
INNER JOIN Rooms rm ON rr.RoomId = rm.RoomId
INNER JOIN RoomTypes rt ON rr.RoomTypeId = rt.RoomTypeId
WHERE res.UserId = '550e8400-...'
```

### 18.13 Indexes for Performance

```sql
-- Without index: Full table scan O(n)
SELECT * FROM Hotels WHERE City = 'Mumbai'

-- With index: B-tree lookup O(log n)
CREATE INDEX IX_Hotels_City ON Hotels(City)
SELECT * FROM Hotels WHERE City = 'Mumbai'  -- Uses index!
```

Indexes are created in the DbContext for frequently queried columns (`City`, `State`, `HotelId`, `RoomTypeId`).

---

## 19. EF Core LINQ Patterns & Query Splitting

### AsNoTracking — Read-Only Queries

```csharp
// Default (tracking): EF Core watches the entity for changes
var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.HotelId == id);
hotel.Name = "New Name";
await _context.SaveChangesAsync(); // Detects change, runs UPDATE

// With AsNoTracking: No change detection — faster for read-only queries
var hotel = await _context.Hotels
    .AsNoTracking()
    .FirstOrDefaultAsync(h => h.HotelId == id);
// Do NOT modify and save this — changes won't be tracked
```

Public GET endpoints use `AsNoTracking()` for better performance.

### Query Splitting

```csharp
// Problem: Loading multiple collections in one query causes a Cartesian product
// e.g., Hotel with 10 RoomTypes and 5 Reviews = 10×5 = 50 rows
var hotel = await _context.Hotels
    .Include(h => h.RoomTypes)   // 10 rows
    .Include(h => h.Reviews)     // 5 rows
    .FirstOrDefaultAsync();
// Without split: 50 rows returned (duplicated data)

// Solution — configured globally in Program.cs:
.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
// EF Core runs separate SELECT queries for each Include:
// SELECT * FROM Hotels WHERE ...
// SELECT * FROM RoomTypes WHERE HotelId IN (...)
// SELECT * FROM Reviews WHERE HotelId IN (...)
// 3 queries, no duplication
```

### AnyAsync, CountAsync, SumAsync

```csharp
// Check existence without loading the entity:
bool exists = await _walletRepo.GetQueryable()
    .AnyAsync(w => w.UserId == userId);
// → SELECT CASE WHEN EXISTS(SELECT 1 FROM Wallets WHERE UserId=?) THEN 1 ELSE 0 END

// Count without loading:
int total = await query.CountAsync();

// Sum without loading:
decimal revenue = await query.SumAsync(t => t.Amount);
```

### Dynamic Filtering

```csharp
// Build query conditionally
IQueryable<Reservation> query = _context.Reservations
    .Where(r => r.HotelId == hotelId)
    .AsNoTracking();

if (status != null && status != "All")
    query = query.Where(r => r.Status.ToString() == status);

if (!string.IsNullOrEmpty(search))
    query = query.Where(r => r.ReservationCode.Contains(search)
                           || r.User!.Name.Contains(search));

// Dynamic sorting
query = sortField switch
{
    "date"   => sortDir == "asc"
                    ? query.OrderBy(r => r.CreatedDate)
                    : query.OrderByDescending(r => r.CreatedDate),
    "amount" => sortDir == "asc"
                    ? query.OrderBy(r => r.FinalAmount)
                    : query.OrderByDescending(r => r.FinalAmount),
    _        => query.OrderByDescending(r => r.CreatedDate)
};

// Execute pagination
int total = await query.CountAsync();
var items = await query.Skip((page-1) * pageSize).Take(pageSize).ToListAsync();
```

---

## 20. Pagination Pattern

Consistent pagination across all list endpoints:

```csharp
// Query DTO (input):
public class PageQueryDto
{
    public int Page { get; set; } = 1;      // Default: page 1
    public int PageSize { get; set; } = 10; // Default: 10 items per page
}

// Response DTO (output):
public class PagedReservationResponseDto
{
    public int TotalCount { get; set; }    // Total matching rows (for UI to show total pages)
    public int Page { get; set; }
    public int PageSize { get; set; }
    public IEnumerable<ReservationResponseDto> Items { get; set; }
}

// Service implementation:
int totalCount = await query.CountAsync();

var items = await query
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

return new PagedReservationResponseDto
{
    TotalCount = totalCount,
    Page = page,
    PageSize = pageSize,
    Items = items.Select(MapToDto)
};
```

**Total pages formula (for frontend):**
```
totalPages = Math.Ceiling(totalCount / pageSize)
```

---

## 21. Dependency Injection — Complete Registration Map

DI is how services, repositories, and other components are wired together. You register them once in `Program.cs`, then the framework injects them automatically.

### Lifetimes

| Lifetime | `AddScoped` | `AddSingleton` | `AddTransient` |
|----------|-------------|----------------|----------------|
| Created | Once per HTTP request | Once per app lifetime | Every time injected |
| Disposed | End of request | App shutdown | After use |
| Use for | DbContext, services | Rate limiter, JWT config | Lightweight, stateless |

### All Registrations in This Project

```csharp
// ── SCOPED (per request) ─────────────────────────────────────────────────────
builder.Services.AddDbContext<HotelBookingContext>(...);  // EF Core context
builder.Services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>)); // Generic repo
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IHotelService, HotelService>();
builder.Services.AddScoped<IRoomTypeService, RoomTypeService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAmenityService, AmenityService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IPromoCodeService, PromoCodeService>();
builder.Services.AddScoped<IAmenityRequestService, AmenityRequestService>();
builder.Services.AddScoped<ISuperAdminRevenueService, SuperAdminRevenueService>();
builder.Services.AddScoped<ISupportRequestService, SupportRequestService>();

// ── SINGLETON (once for app) ──────────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// ── HOSTED SERVICES (background) ─────────────────────────────────────────────
builder.Services.AddHostedService<ReservationCleanupService>();
builder.Services.AddHostedService<HotelDeactivationRefundService>();
builder.Services.AddHostedService<NoShowAutoCancelService>();
```

**How DI resolves a chain:**
When `AuthenticationController` is created, ASP.NET Core sees it needs `IAuthService`. It looks up the registration (`IAuthService → AuthService`), creates an `AuthService`, and injects it. But `AuthService` needs `IRepository<Guid, User>`, `IPasswordService`, etc. The container resolves those recursively, creating the entire dependency tree automatically.

---

## 22. Role-Based Authorization

### The Three Roles

```csharp
public enum UserRole
{
    Guest = 1,      // Registered hotel guests
    Admin = 2,      // Hotel admin (manages one hotel)
    SuperAdmin = 3  // Platform admin (manages everything)
}
```

### How It Works

1. User logs in → `LoginAsync` returns a JWT with `"role": "Admin"` claim.
2. Client sends `Authorization: Bearer <token>` on every request.
3. JWT middleware validates the signature and populates `HttpContext.User`.
4. `[Authorize(Roles = "Admin")]` checks if `User.IsInRole("Admin")` is true.
5. If not → returns `403 Forbidden`.

```csharp
// These map directly to JWT claim: "role": "Admin"
[Authorize(Roles = "Admin")]
public class AdminHotelController ...

[Authorize(Roles = "SuperAdmin")]
public class SuperAdminHotelController ...

[Authorize(Roles = "Guest")]
public class GuestReservationController ...

[AllowAnonymous]  // No JWT required
public class PublicHotelController ...
```

### Extracting Claims in Controllers

```csharp
// User ID (all authenticated roles)
Guid userId = Guid.Parse(User.FindFirstValue("nameid")!);

// Hotel ID (Admin only — embedded in token at login)
Guid hotelId = Guid.Parse(User.FindFirstValue("HotelId")!);

// Role check in service (not controller)
string role = User.FindFirstValue("role")!;
// services use role to filter data differently per role
```

---

## 23. Rate Limiting

```json
"IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "GeneralRules": [
        { "Endpoint": "*", "Period": "1m", "Limit": 60 }
    ]
}
```

- Each IP can make **60 requests per minute** across all endpoints.
- Exceeding this returns HTTP **429 Too Many Requests**.
- Counters stored in-memory (`AddMemoryCache`).
- Configured via `Configure<IpRateLimitOptions>` — no code changes needed to adjust limits.

The rate limit middleware is applied at the start of the pipeline (`app.UseIpRateLimiting()`) so it blocks requests before they reach the controllers.

---

## 24. Key Business Flows End-to-End

### Flow 1: Guest Books a Hotel Room

```
1. Guest calls POST /api/guest/reservations
   Body: { HotelId, RoomTypeId, CheckInDate, CheckOutDate, NumberOfRooms, PromoCodeUsed? }

2. JWT Middleware validates token → populates UserId from "nameid" claim

3. GuestReservationController.Create() calls:
   ReservationService.CreateReservationAsync(userId, dto)

4. ReservationService:
   a. BeginTransaction
   b. Validate dates (checkIn >= today, checkOut > checkIn)
   c. Load Hotel — check IsActive, not blocked
   d. Load RoomType — check belongs to hotel, IsActive
   e. Get PricePerNight from RoomTypeRate (date range overlap check)
   f. For each date in stay: check RoomTypeInventory.AvailableInventory >= NumberOfRooms
   g. Increment ReservedInventory for each date
   h. Auto-assign or validate selected rooms
   i. Calculate: TotalAmount = price × nights × rooms
   j. Apply GST: GstAmount = TotalAmount × hotel.GstPercent / 100
   k. Apply PromoCode if provided (validate, mark as used)
   l. Deduct wallet if requested
   m. Calculate FinalAmount
   n. Create Reservation entity (Status = Pending, ExpiryTime = now + 15min)
   o. Create ReservationRoom entities (one per room)
   p. Commit transaction

5. Return ReservationResponseDto with ReservationCode

6. Guest has 15 minutes to pay. If they don't:
   ReservationCleanupService (background) detects ExpiryTime < now,
   cancels reservation, restores inventory, refunds wallet.
```

### Flow 2: Guest Makes Payment

```
1. Guest calls POST /api/guest/payments
   Body: { ReservationId, PaymentMethod }

2. TransactionService.CreatePaymentAsync():
   a. Load reservation, verify it's Pending and belongs to user
   b. Check ExpiryTime not exceeded
   c. Create Transaction entity (Status = Success)
   d. Update Reservation.Status = Confirmed
   e. Commit

3. Return TransactionResponseDto
```

### Flow 3: Admin Completes a Reservation

```
1. Admin calls PATCH /api/admin/reservations/{code}/complete

2. ReservationService.CompleteReservationAsync(code):
   a. Load reservation for admin's hotel
   b. Verify Status == Confirmed
   c. Set Status = Completed
   d. Record SuperAdminRevenue (2% of FinalAmount)
   e. Generate PromoCode for the guest (for their next booking)
   f. Commit

3. Guest can now write a review for this reservation
```

### Flow 4: Guest Cancels a Reservation

```
Cancellation policy:
- If CancellationFeePaid = true: full refund always
- If < 24 hours before checkIn: 50% refund
- If >= 24 hours before checkIn: full refund

1. Guest calls POST /api/guest/reservations/cancel
   Body: { ReservationCode, Reason }

2. ReservationService.CancelReservationAsync():
   a. Load reservation, verify belongs to user
   b. Verify Status == Confirmed or Pending
   c. Calculate refund amount per policy
   d. Set Status = Cancelled
   e. Restore inventory for each date
   f. Credit refund to wallet
   g. Commit
```

---

## 25. C# Concepts Mastery Guide

### Async/Await

```csharp
// Every database or I/O operation is async to avoid blocking threads.
public async Task<User?> GetUserAsync(Guid id)
{
    // 'await' releases the thread while waiting for the DB response.
    // When DB responds, the thread resumes.
    return await _context.Users.FindAsync(id);
}
// Return type is Task<T> for async methods.
// Task = a promise that something will complete in the future.
```

### Nullable Reference Types (`?`)

```csharp
// Nullable reference type — may or may not have a value
public string? AdminReply { get; set; }   // Can be null
public Guid? HotelId { get; set; }        // Can be null

// Non-nullable — guaranteed to have a value
public string Name { get; set; }          // Cannot be null (with <Nullable>enable</Nullable>)

// Null-forgiving operator (!)
Guid userId = Guid.Parse(User.FindFirstValue("nameid")!);
// The ! tells the compiler "I know this isn't null, trust me."
// Use only when you're certain the value exists.

// Null-conditional operator (?.)
string? name = user?.Name;   // Returns null if user is null, otherwise user.Name

// Null-coalescing operator (??)
string name = user?.Name ?? "Anonymous";  // Uses "Anonymous" if null
```

### Expression Trees

```csharp
// Expression<Func<TEntity, bool>> allows EF Core to translate to SQL.
// Regular Func<T, bool> cannot be translated — it runs in C# memory.
public async Task<TEntity?> FirstOrDefaultAsync(
    Expression<Func<TEntity, bool>> predicate)
    => await _context.Set<TEntity>().FirstOrDefaultAsync(predicate);

// Usage:
await _userRepo.FirstOrDefaultAsync(u => u.Email == dto.Email);
// EF Core sees the lambda as an expression tree, translates to:
// SELECT TOP 1 * FROM Users WHERE Email = 'test@example.com'
```

### Primary Constructors (C# 12)

```csharp
// Old constructor syntax (C# 9 and below):
public class AuthService : IAuthService
{
    private readonly IRepository<Guid, User> _userRepository;
    public AuthService(IRepository<Guid, User> userRepository)
    {
        _userRepository = userRepository;
    }
}

// New primary constructor syntax (C# 12, used in TransactionService):
public class TransactionService(
    IRepository<Guid, Transaction> transactionRepo,
    IRepository<Guid, Reservation> reservationRepo,
    IUnitOfWork unitOfWork) : ITransactionService
{
    private readonly IRepository<Guid, Transaction> _transactionRepo = transactionRepo;
    // ... same result, less boilerplate
}
```

### IQueryable vs IEnumerable

```csharp
// IQueryable — query is built but NOT executed yet (deferred)
IQueryable<Hotel> query = _context.Hotels.Where(h => h.IsActive);
// No SQL runs here!

query = query.Where(h => h.City == "Mumbai");
// Still no SQL — just adds to the WHERE clause

var results = await query.ToListAsync();
// NOW the SQL executes: SELECT * FROM Hotels WHERE IsActive=1 AND City='Mumbai'

// IEnumerable — already loaded into memory; filtering happens in C#
IEnumerable<Hotel> hotels = await _context.Hotels.ToListAsync(); // ALL hotels loaded
var filtered = hotels.Where(h => h.City == "Mumbai"); // Filters in C# memory
// BAD: loads ALL hotels, then filters. Very inefficient!
```

Always filter, sort, and page with `IQueryable` before calling `ToListAsync()`.

### Generic Constraints

```csharp
public class Repository<TKey, TEntity> : IRepository<TKey, TEntity>
    where TEntity : class  // TEntity must be a reference type
{
    // _context.Set<TEntity>() requires TEntity to be a class
}
```

### `using` Statement (Resource Cleanup)

```csharp
// IDisposable pattern — ensures cleanup even if exception thrown
using var scope = _scopeFactory.CreateScope();
// When the using block exits (or throws), scope.Dispose() is called automatically.
// This releases the DI scope and all services created within it.

using var hmac = new HMACSHA256();
// HMACSHA256 implements IDisposable. The using ensures it's disposed after use.
```

### Pattern Matching

```csharp
// is null / is not null
if (entity is null) return null;

// Type pattern (switch expression)
var statusCode = ex switch
{
    AppException appEx  => appEx.StatusCode,  // If ex is AppException, use its code
    _                   => 500                // Default case
};

// is AppException (type check + cast)
var message = ex is AppException ? ex.Message : "Unexpected error";
```

### LINQ Methods

```csharp
// Filtering
.Where(r => r.Status == ReservationStatus.Pending)

// Projection (transform shape)
.Select(h => new HotelListItemDto { Name = h.Name, City = h.City })

// Ordering
.OrderByDescending(r => r.CreatedDate)
.OrderBy(r => r.Name)

// Pagination
.Skip((page - 1) * pageSize).Take(pageSize)

// Existence check
.AnyAsync(u => u.Email == email)

// First or null
.FirstOrDefaultAsync(h => h.HotelId == id)

// Aggregate
.CountAsync()
.SumAsync(t => t.Amount)
.AverageAsync(r => r.Rating)

// Include (JOIN)
.Include(r => r.Hotel)
.ThenInclude(h => h.RoomTypes)

// AsNoTracking (read-only performance)
.AsNoTracking()
```

---

## 26. SQL Server Concepts Mastery Guide

### Concepts Used in This Project

#### UNIQUEIDENTIFIER (GUID)
```sql
UserId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()
-- A 128-bit globally unique identifier.
-- EF Core sets it in C# code before inserting.
```

#### NVARCHAR vs VARCHAR
```sql
Name NVARCHAR(150)  -- Unicode — supports any language (Tamil, Arabic, Chinese, etc.)
Code VARCHAR(20)    -- ASCII only — fine for codes and identifiers
```

#### DECIMAL Precision
```sql
TotalAmount DECIMAL(18, 2)  -- 18 total digits, 2 decimal places
-- Can store up to 9999999999999999.99
-- Never use FLOAT or REAL for money — they have rounding errors
```

#### DATETIME2 vs DATETIME
```sql
CreatedAt DATETIME2  -- More precise (100ns), wider date range — preferred in EF Core
-- vs
CreatedAt DATETIME   -- Legacy, less precise
```

#### DateOnly (C#) → DATE (SQL)
```sql
CheckInDate DATE   -- Stores only year/month/day, no time component
```

#### Indexes
```sql
-- Single column index
CREATE INDEX IX_Hotels_City ON Hotels(City)

-- Composite index (both columns together)
CREATE INDEX IX_RoomTypeRates_RoomTypeId_StartDate_EndDate
    ON RoomTypeRates (RoomTypeId, StartDate, EndDate)

-- Unique index (enforces uniqueness)
CREATE UNIQUE INDEX IX_Users_Email ON Users(Email)
```

#### DELETE Behaviors
```sql
-- CASCADE: deleting parent deletes children
FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE

-- RESTRICT / NO ACTION: prevents deleting parent if children exist
FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE NO ACTION

-- SET NULL: sets FK to NULL when parent deleted
FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE SET NULL
```

#### OFFSET / FETCH NEXT (SQL Server Pagination)
```sql
SELECT ReservationCode, TotalAmount, Status
FROM Reservations
WHERE UserId = '...'
ORDER BY CreatedDate DESC
OFFSET 10 ROWS          -- Skip first 10 (page 2, size 10)
FETCH NEXT 10 ROWS ONLY -- Take next 10
```

#### Data Integrity Rules Applied
1. **Unique email** — `UNIQUE INDEX` on `Users.Email`
2. **Unique room number per hotel** — `UNIQUE INDEX` on `(HotelId, RoomNumber)`
3. **One inventory row per room type per day** — `UNIQUE INDEX` on `(RoomTypeId, Date)`
4. **One review per completed reservation** — `UNIQUE INDEX` on `(UserId, ReservationId)`
5. **Unique reservation code** — `UNIQUE INDEX` on `Reservations.ReservationCode`
6. **Financial precision** — `DECIMAL(18,2)` on all money columns

#### How EF Migrations Work

```
C# Model Change → Add-Migration → Migration File → Update-Database → SQL Server

1. You add a new property to a model (e.g., Hotel.GstPercent)
2. Run: dotnet ef migrations add AddGstToHotel
3. EF generates a migration file:
   public partial class AddGstToHotel : Migration
   {
       protected override void Up(MigrationBuilder migrationBuilder)
       {
           migrationBuilder.AddColumn<decimal>(
               name: "GstPercent",
               table: "Hotels",
               precision: 5, scale: 2,
               defaultValue: 0m);
       }
       protected override void Down(MigrationBuilder migrationBuilder)
       {
           migrationBuilder.DropColumn(name: "GstPercent", table: "Hotels");
       }
   }
4. Run: dotnet ef database update
5. EF executes: ALTER TABLE Hotels ADD GstPercent DECIMAL(5,2) NOT NULL DEFAULT 0
```

---

## Summary: Architecture at a Glance

```
┌─────────────────────────────────────────────────────────────┐
│                    HTTP Client (Frontend)                    │
│         Authorization: Bearer <JWT Token>                   │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                    ASP.NET Core Pipeline                     │
│  CORS → RateLimit → Routing → ExceptionMiddleware            │
│  → Authentication (JWT) → Authorization → Controllers        │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                      Controllers                             │
│  [Authorize(Roles="Guest/Admin/SuperAdmin")]                 │
│  Extract claims → Call IService → Return Ok(dto)            │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                    Service Layer                             │
│  All business logic, validation, domain rules               │
│  Uses IRepository<Guid,T> for data access                   │
│  Uses IUnitOfWork for atomic transactions                   │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│           Generic Repository (IRepository<TKey,T>)          │
│  GetAsync, AddAsync, UpdateAsync, DeleteAsync                │
│  FirstOrDefaultAsync, GetQueryable, GetAllByForeignKeyAsync  │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                 HotelBookingContext (EF Core)                │
│  DbSet<User>, DbSet<Hotel>, DbSet<Reservation>...            │
│  OnModelCreating: Fluent API, indexes, seed data             │
│  Translates LINQ → SQL → MS SQL Server                      │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│            MS SQL Server — dbHotelBookingAppV8               │
│  Users, Hotels, Rooms, RoomTypes, Reservations               │
│  Transactions, Reviews, Wallets, PromoCode, Amenities        │
│  AuditLogs, Logs, SupportRequests, SuperAdminRevenue         │
└─────────────────────────────────────────────────────────────┘
```

---

*This document was generated from the actual source code of `HotelBookingAppWebApi` running on .NET 10 with MS SQL Server. Every code snippet is taken from or directly represents the actual implementation.*
