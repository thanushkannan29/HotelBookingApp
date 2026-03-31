# Project Backend Documentation

> **Project:** Hotel Booking App Web API
> **Stack:** ASP.NET Core 8, Entity Framework Core, SQL Server, JWT Authentication
> **Architecture:** Clean Layered Architecture — Controllers → Services → Repositories → DbContext → SQL Server

---

## 1. Project Overview

### What This Backend Does

This is a full-featured **Hotel Booking REST API** built with ASP.NET Core 8. It powers a hotel reservation platform where three types of users interact with the system:

- **Guests** — browse hotels, make reservations, pay via UPI/wallet, write reviews
- **Hotel Admins** — manage their hotel, rooms, inventory, pricing, and reservations
- **SuperAdmins** — oversee all hotels, approve amenities, track platform revenue

### Core Features

- User registration and JWT-based login for all three roles
- Hotel search with filters (city, state, amenities, price range)
- Room type management with date-based inventory and pricing
- Full reservation lifecycle: create → pay → confirm → check-in → complete
- Wallet system with top-up, deductions, and automatic refunds
- Promo code generation and validation
- Review system (one per completed reservation, with admin replies)
- Audit logging for all critical admin actions
- Global error logging to database
- Three background services for automated tasks
- IP-based rate limiting

### How It Connects to the Angular Frontend

The Angular app (running at `http://localhost:4200`) communicates with this API via HTTP. CORS is configured to allow requests from that origin. The Angular app sends a `Bearer {JWT}` token in the `Authorization` header for all protected routes. The API returns consistent JSON envelopes:

```json
{ "success": true, "data": { ... } }
{ "success": false, "statusCode": 404, "message": "Hotel not found.", "traceId": "..." }
```

---

## 2. Solution Structure

```
SolHotelBookingAppWebApi/
│
├── HotelBookingAppWebApi/              ← Main API project
│   ├── Controllers/
│   │   ├── Admin/                      ← Hotel admin endpoints
│   │   ├── Guest/                      ← Guest-only endpoints
│   │   ├── Public/                     ← No auth required
│   │   ├── SuperAdmin/                 ← SuperAdmin-only endpoints
│   │   ├── AuthenticationController.cs ← Login & Register
│   │   ├── DashboardController.cs
│   │   ├── ReviewController.cs
│   │   ├── TransactionController.cs
│   │   ├── UserProfileController.cs
│   │   └── LogController.cs
│   │
│   ├── Services/
│   │   ├── BackgroundServices/         ← Hosted background workers
│   │   ├── AuthService.cs
│   │   ├── HotelService.cs
│   │   ├── ReservationService.cs
│   │   ├── TokenService.cs
│   │   ├── WalletService.cs
│   │   └── ... (22 services total)
│   │
│   ├── Interfaces/                     ← Service + Repository contracts
│   │   ├── RepositoryInterface/
│   │   └── UnitOfWorkInterface/
│   │
│   ├── Repository/
│   │   └── Repository.cs               ← Generic EF Core repository
│   │
│   ├── Models/
│   │   ├── DTOs/                       ← Data Transfer Objects
│   │   ├── Hotel.cs
│   │   ├── Reservation.cs
│   │   ├── User.cs
│   │   └── ... (20 entity files)
│   │
│   ├── Contexts/
│   │   └── HotelBookingContext.cs      ← EF Core DbContext
│   │
│   ├── Exceptions/
│   │   ├── AppExceptions.cs            ← Custom exception classes
│   │   └── Middleware/
│   │       └── GlobalExceptionMiddleware.cs
│   │
│   └── Program.cs                      ← App entry point, DI, pipeline
│
└── HotelBookingAppWebApi.Tests/        ← Unit test project
    └── Services/
```

### Folder Responsibilities

| Folder | Responsibility |
|---|---|
| `Controllers/` | Receive HTTP requests, extract user identity, call services, return responses |
| `Services/` | All business logic — validation, calculations, orchestration |
| `Interfaces/` | Contracts (interfaces) that decouple layers |
| `Repository/` | Generic data access — wraps EF Core DbContext |
| `Models/` | EF Core entity classes mapped to database tables |
| `Models/DTOs/` | Input/output shapes for API — never expose raw entities |
| `Contexts/` | EF Core DbContext — defines tables and relationships |
| `Exceptions/` | Custom exception types and global error handling middleware |
| `Services/BackgroundServices/` | Automated tasks that run on a timer |


---

## 3. Database Layer

### DbContext

`HotelBookingContext` is the EF Core class that represents your database. It inherits from `DbContext` and acts as the bridge between your C# code and SQL Server.

```csharp
// Contexts/HotelBookingContext.cs
public class HotelBookingContext : DbContext
{
    public HotelBookingContext(DbContextOptions<HotelBookingContext> options)
        : base(options) { }

    // Each DbSet = one SQL table
    public DbSet<User> Users { get; set; }
    public DbSet<Hotel> Hotels { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    // ... 28 DbSets total
}
```

**How it connects to SQL Server** — in `Program.cs`:

```csharp
services.AddDbContext<HotelBookingContext>(options =>
    options.UseSqlServer(
        config.GetConnectionString("Developer"),
        sqlOptions => sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
    ));
```

`SplitQuery` means EF Core splits complex queries with multiple `.Include()` calls into separate SQL queries instead of one giant JOIN — this prevents the "cartesian explosion" problem.

### All DbSets (Tables)

| DbSet | Table | Purpose |
|---|---|---|
| `Users` | Users | All user accounts (Guest, Admin, SuperAdmin) |
| `UserProfileDetails` | UserProfileDetails | Extended profile info |
| `Hotels` | Hotels | Hotel records |
| `RoomTypes` | RoomTypes | Room categories per hotel |
| `Rooms` | Rooms | Physical rooms |
| `RoomTypeRates` | RoomTypeRates | Pricing by date range |
| `RoomTypeInventories` | RoomTypeInventories | Availability per date |
| `Reservations` | Reservations | Booking records |
| `ReservationRooms` | ReservationRooms | Which rooms are in a reservation |
| `Transactions` | Transactions | Payment records |
| `Reviews` | Reviews | Guest reviews |
| `Amenities` | Amenities | Hotel amenities (WiFi, Pool, etc.) |
| `RoomTypeAmenities` | RoomTypeAmenities | Many-to-many join |
| `Wallets` | Wallets | Guest wallet balances |
| `WalletTransactions` | WalletTransactions | Wallet credit/debit history |
| `PromoCodes` | PromoCodes | Discount codes |
| `AmenityRequests` | AmenityRequests | Admin requests for new amenities |
| `SuperAdminRevenues` | SuperAdminRevenues | 2% commission records |
| `SupportRequests` | SupportRequests | Support tickets |
| `AuditLogs` | AuditLogs | Admin action audit trail |
| `Logs` | Logs | Error/exception logs |

### Table Relationships

Relationships are configured in `OnModelCreating()`. Here are the key ones:

**One-to-Many: Hotel → RoomTypes**
```csharp
// A hotel has many room types
modelBuilder.Entity<RoomType>()
    .HasIndex(rt => rt.HotelId);
// Navigation: hotel.RoomTypes, roomType.Hotel
```

**One-to-Many: RoomType → Rooms**
```csharp
modelBuilder.Entity<RoomType>()
    .HasMany(rt => rt.Rooms)
    .WithOne(r => r.RoomType)
    .HasForeignKey(r => r.RoomTypeId)
    .OnDelete(DeleteBehavior.Restrict); // can't delete room type if rooms exist
```

**One-to-Many: User → Reservations**
```csharp
modelBuilder.Entity<User>()
    .HasMany(u => u.Reservations)
    .WithOne(r => r.User)
    .HasForeignKey(r => r.UserId)
    .OnDelete(DeleteBehavior.Restrict);
```

**One-to-Many: Reservation → Transactions**
```csharp
modelBuilder.Entity<Reservation>()
    .HasMany(r => r.Transactions)
    .WithOne(t => t.Reservation)
    .HasForeignKey(t => t.ReservationId)
    .OnDelete(DeleteBehavior.Cascade); // deleting reservation deletes its transactions
```

**Many-to-Many: RoomType ↔ Amenity (via RoomTypeAmenity)**
```csharp
// Composite primary key on the join table
modelBuilder.Entity<RoomTypeAmenity>()
    .HasKey(rta => new { rta.RoomTypeId, rta.AmenityId });

modelBuilder.Entity<RoomTypeAmenity>()
    .HasOne(rta => rta.RoomType)
    .WithMany(rt => rt.RoomTypeAmenities)
    .HasForeignKey(rta => rta.RoomTypeId)
    .OnDelete(DeleteBehavior.Cascade);
```

**One-to-One: User → UserProfileDetails**
```csharp
modelBuilder.Entity<User>()
    .HasOne(u => u.UserDetails)
    .WithOne(d => d.User)
    .HasForeignKey<UserProfileDetails>(d => d.UserId)
    .OnDelete(DeleteBehavior.Cascade);
```

**Navigation Properties** — these are the C# properties that let you traverse relationships:

```csharp
// From a Reservation, you can access:
reservation.Hotel.Name          // the hotel name
reservation.User.Email          // the guest's email
reservation.ReservationRooms    // list of rooms booked
reservation.Transactions        // payment records
```

EF Core loads these via `.Include()` in queries:
```csharp
_reservationRepo.GetQueryable()
    .Include(r => r.Hotel)
    .Include(r => r.ReservationRooms!).ThenInclude(rr => rr.Room)
    .Include(r => r.Transactions)
```


---

## 4. Models / Entities

### User

```csharp
public class User
{
    public Guid UserId { get; set; }          // Primary key (GUID)
    public string Name { get; set; }           // Max 150 chars
    public string Email { get; set; }          // Unique index
    public byte[] Password { get; set; }       // Hashed password bytes
    public byte[] PasswordSaltValue { get; set; } // Salt for hashing
    public bool IsActive { get; set; } = true;
    public UserRole Role { get; set; }         // Guest=1, Admin=2, SuperAdmin=3
    public DateTime CreatedAt { get; set; }
    public Guid? HotelId { get; set; }         // Only set for Admin role

    // Navigation
    public UserProfileDetails? UserDetails { get; set; }
    public Hotel? Hotel { get; set; }
    public ICollection<Reservation>? Reservations { get; set; }
    public ICollection<Review>? Reviews { get; set; }
}
```

### Hotel

```csharp
public class Hotel
{
    public Guid HotelId { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public string City { get; set; }           // Indexed for search
    public string State { get; set; }          // Indexed for search
    public string Description { get; set; }
    public string ImageUrl { get; set; }
    public string ContactNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsBlockedBySuperAdmin { get; set; } = false;
    public string? UpiId { get; set; }         // For simulated UPI payments
    public decimal GstPercent { get; set; } = 0; // GST set by admin

    // Navigation
    public ICollection<RoomType>? RoomTypes { get; set; }
    public ICollection<Room>? Rooms { get; set; }
    public ICollection<Review>? Reviews { get; set; }
    public ICollection<Reservation>? Reservations { get; set; }
}
```

### Reservation

```csharp
public class Reservation
{
    public Guid ReservationId { get; set; }
    public string ReservationCode { get; set; }  // e.g. "RES-A1B2C3D4" — unique
    public Guid UserId { get; set; }
    public Guid HotelId { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public decimal TotalAmount { get; set; }     // Base price before GST/discount
    public decimal GstPercent { get; set; }
    public decimal GstAmount { get; set; }
    public decimal DiscountPercent { get; set; } // From promo code
    public decimal DiscountAmount { get; set; }
    public decimal WalletAmountUsed { get; set; }
    public string? PromoCodeUsed { get; set; }
    public decimal FinalAmount { get; set; }     // What guest actually pays
    public ReservationStatus Status { get; set; } // Pending/Confirmed/Cancelled/Completed/NoShow
    public bool IsCheckedIn { get; set; } = false;
    public bool CancellationFeePaid { get; set; } = false; // 10% protection fee
    public decimal CancellationFeeAmount { get; set; }
    public DateTime? ExpiryTime { get; set; }    // 10 min payment window
    public DateTime CreatedDate { get; set; }
}
```

### Transaction

```csharp
public class Transaction
{
    public Guid TransactionId { get; set; }
    public Guid ReservationId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } // CreditCard/DebitCard/UPI/NetBanking/Wallet
    public PaymentStatus Status { get; set; }        // Pending/Success/Failed/Refunded
    public DateTime TransactionDate { get; set; }
    public bool WalletUsed { get; set; }
    public decimal WalletAmountUsed { get; set; }
}
```

### Review

```csharp
public class Review
{
    public Guid ReviewId { get; set; }
    public Guid UserId { get; set; }
    public Guid HotelId { get; set; }
    public Guid ReservationId { get; set; }  // One review per completed reservation
    public decimal Rating { get; set; }      // 1–5 stars
    public string Comment { get; set; }
    public string? ImageUrl { get; set; }
    public string? AdminReply { get; set; }  // Hotel admin can reply
    public DateTime CreatedDate { get; set; }
}
```

### Wallet & WalletTransaction

```csharp
public class Wallet
{
    public Guid WalletId { get; set; }
    public Guid UserId { get; set; }
    public decimal Balance { get; set; } = 0;
    public DateTime UpdatedAt { get; set; }
    public ICollection<WalletTransaction>? WalletTransactions { get; set; }
}

public class WalletTransaction
{
    public Guid WalletTransactionId { get; set; }
    public Guid WalletId { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; }        // "Credit" or "Debit"
    public string Description { get; set; } // Human-readable reason
    public DateTime CreatedAt { get; set; }
}
```

### PromoCode

```csharp
public class PromoCode
{
    public Guid PromoCodeId { get; set; }
    public string Code { get; set; }         // e.g. "PROMO-A1B2C3D4" — unique
    public Guid UserId { get; set; }         // Belongs to a specific guest
    public Guid HotelId { get; set; }        // Valid only for this hotel
    public Guid ReservationId { get; set; }  // Generated from this completed reservation
    public decimal DiscountPercent { get; set; } // 5–25% based on booking amount
    public DateTime ExpiryDate { get; set; } // 90 days from generation
    public bool IsUsed { get; set; } = false;
}
```


---

## 5. DTOs (Data Transfer Objects)

### Why DTOs?

DTOs are separate classes used to send/receive data through the API. They exist for three reasons:

1. **Security** — never expose raw entity fields like `Password`, `PasswordSaltValue`, or internal flags
2. **Shape control** — the API input/output shape can differ from the database shape
3. **Validation** — DTOs carry `[Required]`, `[EmailAddress]`, `[Range]` annotations for input validation

### Example: Login Flow

```csharp
// Input DTO — only what the client needs to send
public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

// Output DTO — only the JWT token, nothing else
public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
}
```

The `User` entity has `Password` (byte[]), `PasswordSaltValue` (byte[]), `Role`, etc. — none of that is exposed to the client.

### Example: Create Reservation DTO

```csharp
public class CreateReservationDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    public Guid RoomTypeId { get; set; }

    [Required]
    public DateOnly CheckInDate { get; set; }

    [Required]
    public DateOnly CheckOutDate { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int NumberOfRooms { get; set; }

    public List<Guid>? SelectedRoomIds { get; set; }  // Optional: guest picks rooms
    public string? PromoCodeUsed { get; set; }
    public decimal WalletAmountToUse { get; set; } = 0;
    public bool PayCancellationFee { get; set; } = false;
}
```

### DTO → Entity Mapping

Mapping is done manually inside services (no AutoMapper). Example from `AuthService`:

```csharp
// DTO comes in from controller
var user = new User
{
    UserId = Guid.NewGuid(),
    Name = dto.Name,          // from RegisterUserDto
    Email = dto.Email,
    Password = hashedPassword,
    PasswordSaltValue = salt!,
    Role = UserRole.Guest,
    CreatedAt = DateTime.UtcNow
};
```

### Entity → Response DTO Mapping

```csharp
// From ReservationService — maps entity to response DTO
private static ReservationResponseDto MapToResponseDto(Reservation r, List<Room> rooms, PricingResult pricing) => new()
{
    ReservationId = r.ReservationId,
    ReservationCode = r.ReservationCode,
    TotalAmount = pricing.TotalAmount,
    FinalAmount = pricing.FinalAmount,
    Status = r.Status.ToString(),
    Rooms = rooms.Select(rm => new RoomSummaryDto
    {
        RoomId = rm.RoomId,
        RoomNumber = rm.RoomNumber,
        Floor = rm.Floor
    }).ToList()
};
```

---

## 6. Repository Pattern

### Why Repository Pattern?

The repository pattern wraps all database access behind an interface. This means:
- Services don't directly use `DbContext` — they use `IRepository<TKey, TEntity>`
- You can swap the data source without changing service code
- Unit testing is easier — you can mock `IRepository`

### The Generic Repository

```csharp
// Interfaces/RepositoryInterface/IRepository.cs
public interface IRepository<TKey, TEntity> where TEntity : class
{
    Task<TEntity?> AddAsync(TEntity entity);
    Task<TEntity?> GetAsync(TKey key);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<TEntity?> UpdateAsync(TKey key, TEntity entity);
    Task<TEntity?> DeleteAsync(TKey key);
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);
    IQueryable<TEntity> GetQueryable();
    Task<IEnumerable<TEntity>> GetAllByForeignKeyAsync(
        Expression<Func<TEntity, bool>> predicate, int limit, int pageNumber);
}
```

### Implementation

```csharp
// Repository/Repository.cs
public class Repository<TKey, TEntity> : IRepository<TKey, TEntity> where TEntity : class
{
    protected readonly HotelBookingContext _context;

    public Repository(HotelBookingContext context) => _context = context;

    public async Task<TEntity?> AddAsync(TEntity entity)
    {
        await _context.Set<TEntity>().AddAsync(entity);
        return entity;
        // NOTE: Does NOT call SaveChanges — that's UnitOfWork's job
    }

    public async Task<TEntity?> GetAsync(TKey key)
        => await _context.FindAsync<TEntity>(key);

    public IQueryable<TEntity> GetQueryable()
        => _context.Set<TEntity>(); // Returns IQueryable for LINQ chaining
}
```

### How Services Use It

```csharp
// In ReservationService constructor — one repo per entity type
private readonly IRepository<Guid, Reservation> _reservationRepo;
private readonly IRepository<Guid, Room> _roomRepo;
private readonly IRepository<Guid, Hotel> _hotelRepo;

// Usage — get a hotel by ID
var hotel = await _hotelRepo.GetAsync(dto.HotelId)
    ?? throw new NotFoundException("Hotel not found.");

// Usage — complex query with LINQ
var reservations = await _reservationRepo.GetQueryable()
    .Include(r => r.Hotel)
    .Where(r => r.UserId == userId)
    .OrderByDescending(r => r.CreatedDate)
    .ToListAsync();
```

### Registration in Program.cs

```csharp
// One line registers the generic repo for ALL entity types
services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
```

---

## 7. Unit of Work Pattern

### What Is Unit of Work?

The Unit of Work pattern coordinates multiple repository operations into a single database transaction. Without it, each `AddAsync` or `UpdateAsync` call would need its own `SaveChanges`, making it impossible to roll back a group of operations if one fails.

### The Interface

```csharp
public interface IUnitOfWork
{
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
    Task SaveChangesAsync();
}
```

### The Implementation

```csharp
// Services/UnitOfWork.cs
public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly HotelBookingContext _context;
    private IDbContextTransaction? _transaction;

    public async Task BeginTransactionAsync()
    {
        if (_transaction is not null) return; // prevents nested transactions
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        if (_transaction is null)
        {
            await _context.SaveChangesAsync(); // fallback if no explicit transaction
            return;
        }
        try
        {
            await _context.SaveChangesAsync(); // write to DB
            await _transaction.CommitAsync();  // commit the transaction
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackAsync()
    {
        if (_transaction is null) return;
        try { await _transaction.RollbackAsync(); }
        finally { await DisposeTransactionAsync(); }
    }
}
```

### How It's Used in Services

Every write operation that touches multiple tables uses this pattern:

```csharp
// From ReservationService.CreateReservationAsync
await _unitOfWork.BeginTransactionAsync();
try
{
    // Step 1: validate dates
    // Step 2: check inventory
    // Step 3: calculate pricing
    // Step 4: assign rooms
    // Step 5: save reservation + reservation rooms + update inventory
    await _reservationRepo.AddAsync(reservation);
    await _reservationRoomRepo.AddAsync(reservationRoom);
    // ... update inventory records

    await ProcessWalletDeductionAsync(userId, pricing, reservation.ReservationCode);

    await _unitOfWork.CommitAsync(); // all or nothing
    return MapToResponseDto(reservation, assignedRooms, pricing);
}
catch
{
    await _unitOfWork.RollbackAsync(); // undo everything if any step fails
    throw;
}
```


---

## 8. Services Layer

The services layer contains all business logic. Controllers are thin — they just call services. Services handle validation, calculations, and orchestration.

### AuthService

Handles registration and login for all roles.

```csharp
// Register a guest — creates User + UserProfileDetails + Wallet in one transaction
public async Task<AuthResponseDto> RegisterGuestAsync(RegisterUserDto dto)
{
    await EnsureEmailIsUniqueAsync(dto.Email); // throws ConflictException if duplicate

    await _unitOfWork.BeginTransactionAsync();
    try
    {
        var user = await CreateGuestUserAsync(dto);       // hash password, create User
        await CreateUserProfileAsync(user.UserId, ...);   // create profile record
        await _unitOfWork.CommitAsync();
        await _walletService.EnsureWalletExistsAsync(user.UserId); // create wallet
        return BuildAuthResponse(user);                   // generate JWT
    }
    catch { await _unitOfWork.RollbackAsync(); throw; }
}
```

### HotelService

Manages hotel data for three audiences: public browsing, admin self-management, SuperAdmin oversight.

```csharp
// Public search with filters
public async Task<SearchHotelResponseDto> SearchHotelsAsync(SearchHotelRequestDto request)
{
    var query = BuildPublicHotelQuery(); // only active, non-blocked hotels
    query = ApplySearchFilters(query, request); // city, state, amenities, price range
    var sorted = ApplySorting(query, request.SortBy);
    var hotels = await sorted
        .Skip((request.PageNumber - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(h => new HotelListItemDto { ... })
        .ToListAsync();
    return new SearchHotelResponseDto { Hotels = hotels, TotalCount = totalRecords };
}
```

### ReservationService

The most complex service — handles the full booking lifecycle.

**Create Reservation flow:**
1. Validate check-in/check-out dates (must be tomorrow or later)
2. Verify hotel exists and is active
3. Verify room type belongs to hotel and is active
4. Build date range (list of `DateOnly` for each night)
5. Check inventory for each date — throw if insufficient
6. Calculate base amount (rate × rooms × nights)
7. Apply GST, promo code discount, wallet deduction, cancellation fee
8. Assign rooms (auto-assign or use guest's selection)
9. Save reservation + reservation rooms + update inventory counts
10. Deduct wallet if used
11. Mark promo code as used
12. Commit transaction

**Cancellation refund policy:**
```csharp
// Without cancellation protection:
if (daysUntilCheckIn >= 7)      refundPercent = 100; // full refund
else if (daysUntilCheckIn >= 3) refundPercent = 50;  // 50% refund
else if (daysUntilCheckIn >= 1) refundPercent = 25;  // 25% refund
else                            refundPercent = 0;   // no refund on check-in day

// With cancellation protection (10% fee paid at booking):
if (daysUntilCheckIn > 0)  refundPercent = 100; // full refund before check-in day
else                       refundPercent = 50;  // 50% on check-in day
```

### WalletService

Manages guest wallet balance with full transaction history.

```csharp
// Credit (add money) — used for refunds and review rewards
public async Task CreditAsync(Guid userId, decimal amount, string description)
{
    var wallet = await GetOrCreateWalletAsync(userId);
    wallet.Balance += amount;
    await RecordWalletTransactionAsync(wallet.WalletId, amount, "Credit", description);
    await _unitOfWork.SaveChangesAsync();
}

// Deduct (remove money) — used at booking time
public async Task<bool> DeductAsync(Guid userId, decimal amount, string description)
{
    var wallet = await GetOrCreateWalletAsync(userId);
    if (wallet.Balance < amount) return false; // insufficient balance
    wallet.Balance -= amount;
    await RecordWalletTransactionAsync(wallet.WalletId, amount, "Debit", description);
    await _unitOfWork.SaveChangesAsync();
    return true;
}
```

### ReviewService

One review per completed reservation. Rewards ₹100 to wallet on submission.

```csharp
public async Task<ReviewResponseDto> AddReviewAsync(Guid userId, CreateReviewDto dto)
{
    await EnsureHotelExistsAsync(dto.HotelId);
    var reservation = await GetCompletedReservationOrThrowAsync(userId, dto);
    await EnsureNotAlreadyReviewedAsync(dto.ReservationId); // one review per reservation

    var review = BuildReview(userId, dto);
    await _reviewRepo.AddAsync(review);
    await _walletService.CreditAsync(userId, 100m, "Review contribution reward"); // ₹100 reward
    await _unitOfWork.CommitAsync();
    return MapToDto(review, reservation.ReservationCode);
}
```

### PromoCodeService

Generates discount codes after completed reservations. Discount tier is based on booking amount:

```csharp
private static decimal CalculateDiscountPercent(decimal totalAmount) => totalAmount switch
{
    <= 500  => 5,   // 5% discount
    <= 1000 => 10,  // 10% discount
    <= 2000 => 15,  // 15% discount
    <= 5000 => 20,  // 20% discount
    _       => 25   // 25% discount for large bookings
};
```

Codes expire in 90 days and are hotel-specific (can only be used at the same hotel).

### InventoryService

Manages date-based room availability. Idempotent — skips dates that already have inventory:

```csharp
private async Task InsertMissingDatesAsync(CreateInventoryDto dto, HashSet<DateOnly> existingDates)
{
    for (var date = dto.StartDate; date <= dto.EndDate; date = date.AddDays(1))
    {
        if (existingDates.Contains(date)) continue; // skip if already exists
        await _inventoryRepo.AddAsync(new RoomTypeInventory
        {
            RoomTypeInventoryId = Guid.NewGuid(),
            RoomTypeId = dto.RoomTypeId,
            Date = date,
            TotalInventory = dto.TotalInventory,
            ReservedInventory = 0
        });
    }
}
```

### DashboardService

Aggregates statistics for each role's dashboard:

```csharp
// Admin dashboard — stats for their hotel
public async Task<AdminDashboardDto> GetAdminDashboardAsync(Guid userId)
{
    var hotelId = await GetAdminHotelIdOrThrowAsync(userId);
    var roomStats = await GetRoomStatsAsync(hotelId);
    var reservationStats = await GetReservationStatsAsync(hotelId);
    var totalRevenue = await GetHotelRevenueAsync(hotelId);
    var reviewStats = await GetReviewStatsAsync(hotelId);
    return new AdminDashboardDto { TotalRooms = roomStats.Total, TotalRevenue = totalRevenue, ... };
}
```

### AuditLogService

Records every critical admin action with before/after data:

```csharp
// Called from HotelService after updating hotel
await _auditLogService.LogAsync(
    userId,
    "HotelUpdated",
    "Hotel",
    hotel.HotelId,
    JsonSerializer.Serialize(new { Before = before, After = dto })
);
```


---

## 9. Controllers

Controllers are thin — they receive HTTP requests, extract the user's identity from the JWT, call the appropriate service, and return a response.

### Pattern Used in Every Controller

```csharp
[Route("api/guest/reservations")]
[ApiController]
[Authorize(Roles = "Guest")]  // only Guests can access
public class GuestReservationController : ControllerBase
{
    private readonly IReservationService _reservationService;

    public GuestReservationController(IReservationService reservationService)
        => _reservationService = reservationService; // injected by DI

    // Extract the logged-in user's ID from JWT claims
    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReservationDto dto)
    {
        var result = await _reservationService.CreateReservationAsync(GetUserId(), dto);
        return Ok(new { success = true, data = result });
    }
}
```

### Controller Groups

**AuthenticationController** — `api/auth` — No auth required
- `POST /register-guest` — Register new guest
- `POST /register-hotel-admin` — Register admin + hotel
- `POST /login` — Login for all roles

**GuestReservationController** — `api/guest/reservations` — Guest only
- `POST /` — Create reservation
- `GET /{code}` — Get reservation by code
- `GET /` — Get all my reservations
- `POST /history` — Paged reservation history with filters
- `PATCH /{code}/cancel` — Cancel reservation
- `GET /available-rooms` — Check available rooms for dates

**AdminInventoryController** — `api/admin/inventory` — Admin only
- `POST /` — Add inventory for a date range
- `PUT /` — Update total inventory for a date
- `GET /` — Get inventory between two dates

**SuperAdminHotelController** — `api/superadmin/hotels` — SuperAdmin only
- `POST /list` — Paged list of all hotels with stats
- `PATCH /{id}/block` — Block a hotel
- `PATCH /{id}/unblock` — Unblock a hotel

### Request and Response Flow

```
Angular sends:
POST /api/guest/reservations
Authorization: Bearer eyJhbGci...
Body: { "hotelId": "...", "checkInDate": "2026-04-10", ... }

↓ Controller receives request
↓ [Authorize(Roles = "Guest")] — JWT middleware validates token
↓ GetUserId() extracts UserId from claims
↓ Calls _reservationService.CreateReservationAsync(userId, dto)

↓ Service validates, calculates, saves
↓ Returns ReservationResponseDto

↓ Controller wraps in envelope:
{ "success": true, "data": { "reservationCode": "RES-A1B2C3D4", ... } }
```

---

## 10. Authentication & Token System

### JWT Authentication Flow

1. Guest/Admin calls `POST /api/auth/login` with email + password
2. `AuthService` finds the user, verifies the password hash
3. `TokenService` creates a signed JWT with user claims
4. Client stores the token and sends it in every request: `Authorization: Bearer {token}`
5. ASP.NET Core JWT middleware validates the token on every request
6. `[Authorize(Roles = "Admin")]` checks the Role claim

### Password Hashing

Passwords are never stored as plain text. A random salt is generated per user:

```csharp
// PasswordService hashes with HMACSHA512
var hashedPassword = _passwordService.HashPassword(dto.Password, null, out var salt);
// salt is stored in User.PasswordSaltValue
// hash is stored in User.Password
```

On login, the same salt is used to hash the input and compared byte-by-byte:
```csharp
var hashed = _passwordService.HashPassword(dto.Password, user.PasswordSaltValue, out _);
if (!hashed.SequenceEqual(user.Password))
    throw new UnAuthorizedException("Invalid credentials.");
```

### Token Generation

```csharp
// Services/TokenService.cs
public string CreateToken(TokenPayloadDto payload)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, payload.UserId.ToString()), // user ID
        new(ClaimTypes.Name,           payload.UserName),
        new(ClaimTypes.Role,           payload.Role)               // "Guest", "Admin", "SuperAdmin"
    };

    if (payload.HotelId.HasValue)
        claims.Add(new Claim("HotelId", payload.HotelId.ToString()!)); // only for Admins

    var descriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddDays(1),  // 24-hour token
        SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256)
    };

    var handler = new JwtSecurityTokenHandler();
    return handler.WriteToken(handler.CreateToken(descriptor));
}
```

### Token Validation (Program.cs)

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,           // no issuer check
            ValidateAudience = false,         // no audience check
            ValidateLifetime = true,          // token must not be expired
            ValidateIssuerSigningKey = true,  // signature must match
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        });
```

### Extracting Claims in Controllers

```csharp
// Every protected controller uses this helper
private Guid GetUserId()
    => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
```

---

## 11. Exception Handling

### Custom Exception Hierarchy

All custom exceptions inherit from `AppException`, which carries an HTTP status code:

```csharp
// Base class
public class AppException : Exception
{
    public int StatusCode { get; }
    public AppException(string message, int statusCode) : base(message)
        => StatusCode = statusCode;
}

// Specific exceptions
public class NotFoundException : AppException
    { public NotFoundException(string message) : base(message, 404) { } }

public class ConflictException : AppException
    { public ConflictException(string message) : base(message, 409) { } }

public class ValidationException : AppException
    { public ValidationException(string message) : base(message, 400) { } }

public class UnAuthorizedException : AppException
    { public UnAuthorizedException(string message = "Unauthorized") : base(message, 401) { } }

public class PaymentException : AppException
    { public PaymentException(string message) : base(message, 400) { } }

public class InsufficientInventoryException : AppException
    { public InsufficientInventoryException(string message) : base($"{message} — Inventory insufficient.", 409) { } }

public class ReservationFailedException : AppException
    { public ReservationFailedException(string message) : base($"{message} — Reservation failed.", 400) { } }
```

### Global Exception Middleware

This middleware wraps the entire request pipeline. Any unhandled exception is caught here:

```csharp
// Exceptions/Middleware/GlobalExceptionMiddleware.cs
public async Task InvokeAsync(HttpContext context)
{
    try
    {
        await _next(context); // run the rest of the pipeline
    }
    catch (Exception ex)
    {
        await HandleExceptionAsync(context, ex);
    }
}

private async Task HandleExceptionAsync(HttpContext context, Exception ex)
{
    // 1. Determine status code (AppException carries it; others = 500)
    var statusCode = ex is AppException appEx ? appEx.StatusCode : 500;
    var message = ex is AppException ? ex.Message : "An unexpected error occurred.";

    // 2. Log to ILogger (console/file)
    _logger.LogError(ex, "Exception | Status:{StatusCode} | ...", statusCode, ...);

    // 3. Persist to Logs table in database
    await db.Logs.AddAsync(BuildLogEntry(ex, statusCode, message, info));
    await db.SaveChangesAsync();

    // 4. Return consistent JSON to client
    context.Response.StatusCode = statusCode;
    await context.Response.WriteAsJsonAsync(new
    {
        success = false,
        statusCode,
        message,
        traceId = context.TraceIdentifier
    });
}
```

**Example error response the Angular app receives:**
```json
{
  "success": false,
  "statusCode": 404,
  "message": "Hotel not found.",
  "traceId": "0HN8ABCDEF:00000001"
}
```


---

## 12. Middleware

Middleware is code that runs on every HTTP request in a defined order (the "pipeline"). Each piece of middleware can process the request, pass it to the next, and process the response on the way back.

### Middleware Pipeline (in order)

```csharp
// Program.cs — ConfigurePipeline
app.UseSwagger();                              // 1. Swagger UI (dev only)
app.UseSwaggerUI();

app.UseMiddleware<GlobalExceptionMiddleware>(); // 2. Catch ALL exceptions
app.UseCors("AngularClient");                  // 3. Allow Angular origin
app.UseIpRateLimiting();                       // 4. Rate limit by IP
app.UseRouting();                              // 5. Match URL to controller
app.UseAuthentication();                       // 6. Validate JWT token
app.UseAuthorization();                        // 7. Check [Authorize] roles
app.MapControllers();                          // 8. Execute controller action
```

**Why GlobalExceptionMiddleware is first:** It must wrap everything including authentication. If the JWT is invalid, ASP.NET throws an exception — the middleware catches it and returns a clean JSON error instead of a raw 500.

### GlobalExceptionMiddleware

Described in detail in Section 11. Key behaviors:
- Catches all unhandled exceptions
- Logs to both `ILogger` and the `Logs` database table
- Extracts user context (UserId, Role, Controller, Action, HTTP method)
- Returns `{ success: false, statusCode, message, traceId }`

### CORS Middleware

```csharp
services.AddCors(options =>
{
    options.AddPolicy("AngularClient", policy =>
        policy.WithOrigins("http://localhost:4200") // Angular dev server
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});
```

This allows the Angular frontend to call the API from a different port (4200 vs the API port).

### Rate Limiting Middleware

```csharp
// Uses AspNetCoreRateLimit package
services.Configure<IpRateLimitOptions>(config.GetSection("IpRateLimiting"));
services.AddInMemoryRateLimiting();
```

Configured in `appsettings.json` under `IpRateLimiting`. Limits requests per IP address to prevent abuse.

---

## 13. Program.cs

`Program.cs` is the entry point of the application. It does two things: registers services (dependency injection) and configures the middleware pipeline.

```csharp
var builder = WebApplication.CreateBuilder(args);

// ── REGISTER SERVICES ─────────────────────────────────────────────────────────
RegisterControllers(builder.Services);       // AddControllers, AddEndpointsApiExplorer
RegisterRateLimiting(builder.Services, ...); // IP rate limiting
RegisterSwagger(builder.Services);           // Swagger with JWT security
RegisterDatabase(builder.Services, ...);     // EF Core + SQL Server
RegisterCors(builder.Services);              // CORS for Angular
RegisterRepositories(builder.Services);      // Generic repo + UnitOfWork
RegisterApplicationServices(builder.Services); // All 22 business services
RegisterBackgroundServices(builder.Services);  // 3 hosted background workers
RegisterAuthentication(builder.Services, ...); // JWT Bearer auth

var app = builder.Build();

// ── CONFIGURE PIPELINE ────────────────────────────────────────────────────────
ConfigurePipeline(app);
app.Run();
```

### Service Registration Details

```csharp
// Database — SQL Server with split query behavior
services.AddDbContext<HotelBookingContext>(options =>
    options.UseSqlServer(config.GetConnectionString("Developer"),
        sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

// Generic repository — one registration covers ALL entity types
services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
services.AddScoped<IUnitOfWork, UnitOfWork>();

// All services registered as Scoped (one instance per HTTP request)
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IHotelService, HotelService>();
services.AddScoped<IReservationService, ReservationService>();
services.AddScoped<IWalletService, WalletService>();
// ... 22 services total

// Background services — Singleton lifetime (run for app lifetime)
services.AddHostedService<ReservationCleanupService>();
services.AddHostedService<HotelDeactivationRefundService>();
services.AddHostedService<NoShowAutoCancelService>();
```

### Swagger Configuration

```csharp
services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hotel Booking API",
        Version = "v1",
        Description = "Complete Hotel Booking System — Guest, Admin, SuperAdmin roles"
    });

    // Adds "Authorize" button to Swagger UI for JWT testing
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter: Bearer {your JWT token}"
    });
});
```

---

## 14. Pagination

### Why Pagination?

Without pagination, a query like "get all reservations" could return thousands of records, causing slow responses and high memory usage. Pagination returns a small page of results at a time.

### How It Works

The pattern used throughout the project is **page number + page size**:

```csharp
// Skip = (page - 1) * pageSize
// Take = pageSize
var items = await query
    .OrderByDescending(r => r.CreatedDate)
    .Skip((page - 1) * pageSize)  // skip previous pages
    .Take(pageSize)                // take only this page
    .ToListAsync();
```

**Example:** Page 2, size 10 → Skip 10, Take 10 → rows 11–20.

### Paged Response DTOs

Every paged endpoint returns a total count alongside the items:

```csharp
// From ReservationService
return new PagedReservationResponseDto
{
    TotalCount = total,          // total matching records (for frontend pagination UI)
    Reservations = items.Select(MapToDetailsDto)
};
```

### Query Parameters

Paged endpoints accept filters via request body DTOs:

```csharp
// Guest reservation history
public class ReservationHistoryQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Status { get; set; }  // "Pending", "Confirmed", "Cancelled", etc.
    public string? Search { get; set; }  // search by code or hotel name
}
```

### Pagination with Sorting

Admin reservation list supports dynamic sorting:

```csharp
bool desc = string.IsNullOrWhiteSpace(sortDir) || sortDir.ToLower() == "desc";
query = sortField?.ToLower() switch
{
    "guestname" => desc ? query.OrderByDescending(r => r.Hotel!.Name) : query.OrderBy(r => r.Hotel!.Name),
    "amount"    => desc ? query.OrderByDescending(r => r.FinalAmount) : query.OrderBy(r => r.FinalAmount),
    _           => query.OrderByDescending(r => r.CreatedDate) // default
};
```


---

## 15. SQL & Database Operations

### Tables Created by EF Core Migrations

EF Core generates SQL `CREATE TABLE` statements from your entity classes. Key tables:

| Table | Key Columns | Notes |
|---|---|---|
| Users | UserId (PK), Email (Unique), Role (int) | Password stored as byte[] |
| Hotels | HotelId (PK), City (Index), State (Index) | IsBlockedBySuperAdmin flag |
| RoomTypes | RoomTypeId (PK), HotelId (FK, Index) | |
| Rooms | RoomId (PK), HotelId+RoomNumber (Unique) | |
| RoomTypeRates | RoomTypeRateId (PK), RoomTypeId+StartDate+EndDate (Index) | |
| RoomTypeInventories | RoomTypeInventoryId (PK), RoomTypeId+Date (Unique) | |
| Reservations | ReservationId (PK), ReservationCode (Unique) | Status stored as int |
| ReservationRooms | ReservationRoomId (PK), ReservationId+RoomId | Join table |
| Transactions | TransactionId (PK), ReservationId (FK) | PaymentMethod/Status as int |
| Reviews | ReviewId (PK), UserId+ReservationId (Unique) | One review per reservation |
| Wallets | WalletId (PK), UserId (FK) | Balance with precision(18,2) |
| PromoCodes | PromoCodeId (PK), Code (Unique) | |
| AuditLogs | AuditLogId (PK) | JSON changes column |
| Logs | LogId (PK) | Exception logs |

### Decimal Precision

All monetary fields use `HasPrecision(18, 2)` to avoid floating-point errors:

```csharp
modelBuilder.Entity<Reservation>()
    .Property(r => r.TotalAmount)
    .HasPrecision(18, 2); // up to 9,999,999,999,999,999.99
```

### Enum Storage

Enums are stored as integers in the database:

```csharp
modelBuilder.Entity<User>()
    .Property(u => u.Role)
    .HasConversion<int>(); // Guest=1, Admin=2, SuperAdmin=3
```

### Default Values

```csharp
modelBuilder.Entity<User>()
    .Property(u => u.CreatedAt)
    .HasDefaultValueSql("GETUTCDATE()"); // SQL Server sets this automatically
```

### Queries Generated by EF Core

**Simple lookup:**
```csharp
await _hotelRepo.GetAsync(hotelId);
// SQL: SELECT * FROM Hotels WHERE HotelId = @hotelId
```

**Complex query with includes:**
```csharp
await _reservationRepo.GetQueryable()
    .Include(r => r.ReservationRooms!).ThenInclude(rr => rr.Room)
    .Include(r => r.Hotel)
    .Where(r => r.UserId == userId)
    .OrderByDescending(r => r.CreatedDate)
    .Skip(0).Take(10)
    .ToListAsync();
// SQL: Multiple SELECT statements (split query behavior)
```

**Aggregate query:**
```csharp
await _transactionRepo.GetQueryable()
    .Where(t => t.Status == PaymentStatus.Success && t.Reservation!.HotelId == hotelId)
    .SumAsync(t => (decimal?)t.Amount) ?? 0;
// SQL: SELECT SUM(Amount) FROM Transactions WHERE Status = 2 AND ...
```

### Migrations

Run these commands to manage the database schema:

```bash
# Create a new migration after changing entities
dotnet ef migrations add MigrationName --project HotelBookingAppWebApi

# Apply migrations to the database
dotnet ef database update --project HotelBookingAppWebApi
```

---

## 16. HTTP Concepts Used

### HTTP Methods

| Method | Usage in This API | Example |
|---|---|---|
| `GET` | Read data, no side effects | `GET /api/guest/reservations` |
| `POST` | Create new resource or complex queries | `POST /api/guest/reservations` |
| `PUT` | Full update of a resource | `PUT /api/admin/inventory` |
| `PATCH` | Partial update | `PATCH /api/guest/reservations/{code}/cancel` |
| `DELETE` | Remove a resource | `DELETE /api/admin/rooms/{id}` |

### Status Codes Used

| Code | Meaning | When Used |
|---|---|---|
| `200 OK` | Success | All successful responses |
| `400 Bad Request` | Invalid input or business rule violation | ValidationException, PaymentException |
| `401 Unauthorized` | Not authenticated or wrong credentials | UnAuthorizedException |
| `403 Forbidden` | Authenticated but wrong role | [Authorize(Roles = "Admin")] fails |
| `404 Not Found` | Resource doesn't exist | NotFoundException |
| `409 Conflict` | Duplicate or conflicting state | ConflictException, InsufficientInventoryException |
| `500 Internal Server Error` | Unexpected server error | Unhandled exceptions |

### Request/Response Format

All requests with a body use JSON (`Content-Type: application/json`).

All responses follow this envelope:
```json
// Success
{ "success": true, "data": { ... } }
{ "success": true, "message": "Operation completed." }

// Error (from GlobalExceptionMiddleware)
{ "success": false, "statusCode": 404, "message": "Hotel not found.", "traceId": "..." }
```

---

## 17. OOP Concepts Used

### Encapsulation

Private helpers inside services hide implementation details. Only the public interface is exposed:

```csharp
public class ReservationService : IReservationService
{
    // Public — what the controller sees
    public async Task<ReservationResponseDto> CreateReservationAsync(Guid userId, CreateReservationDto dto) { ... }

    // Private — internal steps hidden from callers
    private async Task ValidateDatesAsync(CreateReservationDto dto) { ... }
    private async Task<List<RoomTypeInventory>> GetInventoriesAsync(...) { ... }
    private async Task<PricingResult> CalculatePricingAsync(...) { ... }
}
```

### Abstraction

Interfaces define contracts without exposing implementation:

```csharp
// Controller only knows about the interface — not the concrete class
public class GuestReservationController : ControllerBase
{
    private readonly IReservationService _reservationService; // interface, not ReservationService

    public GuestReservationController(IReservationService reservationService)
        => _reservationService = reservationService;
}
```

### Inheritance

Custom exceptions inherit from `AppException`:

```csharp
public class AppException : Exception
{
    public int StatusCode { get; }
    public AppException(string message, int statusCode) : base(message)
        => StatusCode = statusCode;
}

// NotFoundException inherits AppException, which inherits Exception
public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message, 404) { }
}
```

The middleware uses `is` to check the type:
```csharp
var statusCode = ex is AppException appEx ? appEx.StatusCode : 500;
```

### Polymorphism

The generic repository works with any entity type:

```csharp
// Same interface, different entity types — polymorphism via generics
IRepository<Guid, Hotel> _hotelRepo;
IRepository<Guid, Reservation> _reservationRepo;
IRepository<Guid, User> _userRepo;

// All use the same Repository<TKey, TEntity> implementation
```

### Interfaces

Every service has an interface. This enables:
- Dependency injection (DI container resolves the concrete class)
- Unit testing (mock the interface)
- Loose coupling between layers

```csharp
public interface IHotelService
{
    Task<IEnumerable<HotelListItemDto>> GetTopHotelsAsync();
    Task<SearchHotelResponseDto> SearchHotelsAsync(SearchHotelRequestDto request);
    Task UpdateHotelAsync(Guid userId, UpdateHotelDto dto);
    Task BlockHotelAsync(Guid hotelId);
    // ...
}
```

### Dependency Injection

ASP.NET Core's built-in DI container injects dependencies automatically:

```csharp
// Registered in Program.cs
services.AddScoped<IHotelService, HotelService>();

// Injected into controller constructor — DI resolves it
public class AdminHotelController : ControllerBase
{
    private readonly IHotelService _hotelService;

    public AdminHotelController(IHotelService hotelService)
        => _hotelService = hotelService; // DI provides HotelService instance
}
```

`Scoped` lifetime means one instance per HTTP request — all services in the same request share the same `DbContext` instance, which is essential for transactions to work correctly.


---

## 18. Coding Standards

### Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Classes | PascalCase | `ReservationService`, `HotelBookingContext` |
| Interfaces | `I` prefix + PascalCase | `IReservationService`, `IRepository` |
| Methods | PascalCase | `CreateReservationAsync`, `GetTopHotelsAsync` |
| Private fields | `_camelCase` | `_reservationRepo`, `_unitOfWork` |
| Parameters/locals | camelCase | `userId`, `dto`, `reservationCode` |
| DTOs | Suffix with `Dto` | `CreateReservationDto`, `AuthResponseDto` |
| Enums | PascalCase values | `ReservationStatus.Confirmed` |

### Async/Await Usage

Every database operation is async. No blocking calls:

```csharp
// Always async — never .Result or .Wait()
public async Task<Hotel> GetHotelAsync(Guid hotelId)
{
    return await _hotelRepo.GetAsync(hotelId)
        ?? throw new NotFoundException("Hotel not found.");
}
```

### Null Safety

Null-coalescing and null-conditional operators are used throughout:

```csharp
var revenue = await query.SumAsync(t => (decimal?)t.Amount) ?? 0;
var hotelName = reservation.Hotel?.Name ?? "Hotel";
var hotelId = admin.HotelId ?? throw new UnAuthorizedException("No hotel.");
```

### Clean Architecture Principles

- **Controllers** never contain business logic — only call services
- **Services** never directly use `DbContext` — only use repositories
- **Repositories** never contain business logic — only data access
- **DTOs** are never used as entities — entities are never returned directly
- **Interfaces** separate every layer — nothing depends on concrete classes

### Folder Organization

Code is organized by feature area, not by type:
- `Controllers/Admin/` — all admin controllers together
- `Controllers/Guest/` — all guest controllers together
- `Models/DTOs/Reservation/` — all reservation DTOs together

### Guard Clauses

Early returns and throws keep methods readable:

```csharp
// Instead of deeply nested if-else:
if (user.HotelId is null) throw new UnAuthorizedException("No hotel associated.");
if (!hotel.IsActive) throw new ValidationException("Hotel is not active.");
if (inventory.AvailableInventory < numberOfRooms)
    throw new InsufficientInventoryException($"Insufficient inventory on {inv.Date}.");
```

---

## 19. API Endpoints Summary

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register-guest` | None | Register new guest |
| POST | `/api/auth/register-hotel-admin` | None | Register admin + hotel |
| POST | `/api/auth/login` | None | Login (all roles) |
| GET | `/api/hotels/top` | None | Top 10 hotels by rating |
| POST | `/api/hotels/search` | None | Search hotels with filters |
| GET | `/api/hotels/{id}` | None | Hotel details |
| GET | `/api/hotels/{id}/room-types` | None | Room types for a hotel |
| GET | `/api/hotels/{id}/availability` | None | Room availability for dates |
| GET | `/api/hotels/cities` | None | List of active cities |
| GET | `/api/hotels/states` | None | List of active states |
| POST | `/api/guest/reservations` | Guest | Create reservation |
| GET | `/api/guest/reservations` | Guest | Get all my reservations |
| GET | `/api/guest/reservations/{code}` | Guest | Get reservation by code |
| POST | `/api/guest/reservations/history` | Guest | Paged reservation history |
| PATCH | `/api/guest/reservations/{code}/cancel` | Guest | Cancel reservation |
| GET | `/api/guest/reservations/available-rooms` | Guest | Available rooms for dates |
| POST | `/api/guest/payments` | Guest | Create payment |
| GET | `/api/guest/wallet` | Guest | Get wallet + transactions |
| POST | `/api/guest/wallet/topup` | Guest | Top up wallet |
| GET | `/api/guest/promo-codes` | Guest | My promo codes |
| POST | `/api/guest/promo-codes/validate` | Guest | Validate promo code |
| POST | `/api/guest/reviews` | Guest | Submit review |
| GET | `/api/guest/reviews` | Guest | My reviews |
| POST | `/api/admin/hotel` | Admin | Update hotel info |
| PATCH | `/api/admin/hotel/status` | Admin | Toggle hotel active/inactive |
| POST | `/api/admin/room-types` | Admin | Create room type |
| PUT | `/api/admin/room-types/{id}` | Admin | Update room type |
| POST | `/api/admin/rooms` | Admin | Add room |
| PUT | `/api/admin/rooms/{id}` | Admin | Update room |
| POST | `/api/admin/inventory` | Admin | Add inventory for date range |
| PUT | `/api/admin/inventory` | Admin | Update inventory for date |
| GET | `/api/admin/inventory` | Admin | Get inventory between dates |
| POST | `/api/admin/reservations/list` | Admin | Paged reservations for hotel |
| PATCH | `/api/admin/reservations/{code}/confirm` | Admin | Confirm reservation |
| PATCH | `/api/admin/reservations/{code}/complete` | Admin | Complete reservation |
| GET | `/api/admin/reviews` | Admin | Hotel reviews (paged) |
| POST | `/api/admin/reviews/{id}/reply` | Admin | Reply to review |
| GET | `/api/admin/transactions` | Admin | Transaction history |
| GET | `/api/admin/audit-logs` | Admin | Audit log (paged) |
| POST | `/api/admin/amenity-requests` | Admin | Request new amenity |
| GET | `/api/dashboard` | Any auth | Role-specific dashboard |
| POST | `/api/superadmin/hotels/list` | SuperAdmin | All hotels (paged) |
| PATCH | `/api/superadmin/hotels/{id}/block` | SuperAdmin | Block hotel |
| PATCH | `/api/superadmin/hotels/{id}/unblock` | SuperAdmin | Unblock hotel |
| GET | `/api/superadmin/amenities` | SuperAdmin | All amenities |
| POST | `/api/superadmin/amenities` | SuperAdmin | Create amenity |
| GET | `/api/superadmin/amenity-requests` | SuperAdmin | Pending requests |
| PATCH | `/api/superadmin/amenity-requests/{id}/approve` | SuperAdmin | Approve request |
| PATCH | `/api/superadmin/amenity-requests/{id}/reject` | SuperAdmin | Reject request |
| GET | `/api/superadmin/revenue` | SuperAdmin | Commission revenue |
| GET | `/api/logs` | SuperAdmin | Error logs |


---

## 20. Complete Request Flow

Let's trace a real example: **Guest creates a reservation**.

### The Request

```
Angular sends:
POST http://localhost:5000/api/guest/reservations
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "hotelId": "a1b2c3d4-...",
  "roomTypeId": "e5f6g7h8-...",
  "checkInDate": "2026-04-15",
  "checkOutDate": "2026-04-17",
  "numberOfRooms": 1,
  "walletAmountToUse": 500,
  "promoCodeUsed": "PROMO-A1B2C3D4",
  "payCancellationFee": true
}
```

### Step 1: Middleware Pipeline

```
Request arrives at ASP.NET Core
→ GlobalExceptionMiddleware wraps everything in try/catch
→ CORS middleware checks origin (localhost:4200 ✓)
→ Rate limiting checks IP (not exceeded ✓)
→ Authentication middleware reads "Authorization: Bearer ..."
  → Validates JWT signature ✓
  → Validates expiry ✓
  → Extracts claims: UserId, Role="Guest", UserName
→ Authorization middleware checks [Authorize(Roles = "Guest")] ✓
→ Routing matches POST /api/guest/reservations → GuestReservationController.Create
```

### Step 2: Controller

```csharp
// GuestReservationController.cs
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateReservationDto dto)
{
    // Extract UserId from JWT claims
    var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    // e.g. userId = "user-guid-here"

    // Delegate ALL logic to service
    var result = await _reservationService.CreateReservationAsync(userId, dto);

    return Ok(new { success = true, data = result });
}
```

### Step 3: Service — ReservationService.CreateReservationAsync

```csharp
await _unitOfWork.BeginTransactionAsync(); // START DB TRANSACTION
try
{
    // 1. Validate dates
    //    CheckInDate must be tomorrow or later → OK
    //    CheckOutDate must be after CheckInDate → OK

    // 2. Get hotel from DB
    //    SELECT * FROM Hotels WHERE HotelId = 'a1b2c3d4-...'
    //    → hotel found, IsActive = true ✓

    // 3. Get room type
    //    SELECT * FROM RoomTypes WHERE RoomTypeId = 'e5f6g7h8-...' AND HotelId = 'a1b2c3d4-...' AND IsActive = 1
    //    → roomType found ✓

    // 4. Build date range: [2026-04-15, 2026-04-16] (2 nights)

    // 5. Check inventory for each date
    //    SELECT * FROM RoomTypeInventories WHERE RoomTypeId = '...' AND Date IN ('2026-04-15', '2026-04-16')
    //    → 2026-04-15: TotalInventory=5, ReservedInventory=2, Available=3 ✓
    //    → 2026-04-16: TotalInventory=5, ReservedInventory=1, Available=4 ✓

    // 6. Calculate base amount
    //    SELECT * FROM RoomTypeRates WHERE RoomTypeId = '...' AND StartDate <= '2026-04-17' AND EndDate >= '2026-04-15'
    //    → Rate = ₹2000/night
    //    → TotalAmount = 2000 × 1 room × 2 nights = ₹4000

    // 7. Calculate pricing
    //    GST = 4000 × 18% = ₹720
    //    Promo code "PROMO-A1B2C3D4" → 15% discount → ₹600
    //    Cancellation fee = 4000 × 10% = ₹400
    //    Wallet deduction = min(500, 4000+720-600+400) = ₹500
    //    FinalAmount = 4000 + 720 - 600 - 500 + 400 = ₹4020

    // 8. Assign rooms
    //    Find rooms not already booked for these dates
    //    SELECT RoomId FROM ReservationRooms WHERE ... (overlapping dates, active reservations)
    //    → Room "101" is available → assigned

    // 9. Save to database
    //    INSERT INTO Reservations (ReservationId, ReservationCode='RES-A1B2C3D4', UserId, HotelId, ...)
    //    INSERT INTO ReservationRooms (ReservationRoomId, ReservationId, RoomId='101', ...)
    //    UPDATE RoomTypeInventories SET ReservedInventory = ReservedInventory + 1 WHERE Date IN (...)

    // 10. Deduct wallet
    //     UPDATE Wallets SET Balance = Balance - 500 WHERE UserId = '...'
    //     INSERT INTO WalletTransactions (Type='Debit', Amount=500, Description='Wallet payment for RES-A1B2C3D4')

    // 11. Mark promo code used
    //     UPDATE PromoCodes SET IsUsed = 1 WHERE Code = 'PROMO-A1B2C3D4'

    await _unitOfWork.CommitAsync(); // COMMIT ALL CHANGES TO DB
    return MapToResponseDto(reservation, assignedRooms, pricing);
}
catch
{
    await _unitOfWork.RollbackAsync(); // UNDO EVERYTHING if any step fails
    throw;
}
```

### Step 4: Repository → DbContext → SQL Server

```
_reservationRepo.AddAsync(reservation)
→ _context.Set<Reservation>().AddAsync(reservation)
→ EF Core tracks the entity as "Added"

_unitOfWork.CommitAsync()
→ _context.SaveChangesAsync()
→ EF Core generates SQL INSERT/UPDATE statements
→ SQL Server executes them within the transaction
→ _transaction.CommitAsync() — transaction committed
```

### Step 5: Response

```csharp
// Service returns ReservationResponseDto
// Controller wraps it:
return Ok(new { success = true, data = result });
```

```json
HTTP 200 OK
{
  "success": true,
  "data": {
    "reservationId": "...",
    "reservationCode": "RES-A1B2C3D4",
    "totalAmount": 4000.00,
    "gstPercent": 18,
    "gstAmount": 720.00,
    "discountPercent": 15,
    "discountAmount": 600.00,
    "walletAmountUsed": 500.00,
    "finalAmount": 4020.00,
    "status": "Pending",
    "totalRooms": 1,
    "rooms": [{ "roomId": "...", "roomNumber": "101", "floor": 1 }]
  }
}
```

### Step 6: Angular Receives Response

The Angular `ReservationService` receives this response, stores the `reservationCode`, and navigates the user to the payment page where they complete the UPI payment.

---

## Background Services

Three hosted services run automatically in the background every 5 minutes:

### ReservationCleanupService

Cancels `Pending` reservations whose 10-minute payment window has expired:

```
Every 5 minutes:
→ SELECT * FROM Reservations WHERE Status = Pending AND ExpiryTime < NOW()
→ For each expired reservation:
   → UPDATE Reservations SET Status = Cancelled, CancellationReason = 'Payment timeout'
   → UPDATE RoomTypeInventories SET ReservedInventory = ReservedInventory - roomCount
   → If WalletAmountUsed > 0: credit wallet back (refund pre-deducted amount)
→ COMMIT
```

### HotelDeactivationRefundService

When a hotel is deactivated, all its confirmed reservations are auto-cancelled with full refunds:

```
Every 5 minutes:
→ SELECT * FROM Reservations WHERE Status = Confirmed AND Hotel.IsActive = false
→ For each affected reservation:
   → UPDATE Reservations SET Status = Cancelled, CancellationReason = 'Hotel deactivated'
   → UPDATE Transactions SET Status = Refunded (for the successful payment)
   → Restore inventory
   → Credit full refund to guest wallet
→ COMMIT
```

### NoShowAutoCancelService

Marks confirmed reservations as `NoShow` when the guest never checked in and checkout date has passed:

```
Every 5 minutes:
→ SELECT * FROM Reservations WHERE Status = Confirmed AND IsCheckedIn = false AND CheckOutDate < TODAY
→ For each no-show:
   → UPDATE Reservations SET Status = NoShow, CancellationReason = 'No-show: guest did not check in'
   → Restore inventory
   → NO refund issued for no-shows
→ COMMIT
```

All three services use `IServiceScopeFactory` to create a new DI scope per run (because they are Singleton but need Scoped services like repositories):

```csharp
using var scope = _scopeFactory.CreateScope();
var reservationRepo = scope.ServiceProvider.GetRequiredService<IRepository<Guid, Reservation>>();
var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
```

---

*Documentation generated from actual project source code.*
*Project: HotelBookingAppWebApi | Architecture: ASP.NET Core 8 Clean Layered Architecture*
