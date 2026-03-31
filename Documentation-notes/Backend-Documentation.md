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


---

# PART 2 — DEEP DIVE: Every Service, Every Function, Every .NET Concept

---

## A. Every Service File — Every Function Explained Simply

---

### RoomTypeService — Every Function

This service manages room categories (Deluxe, Suite, Standard) inside a hotel. Each room type has a name, max occupancy, amenities, pricing rates, and inventory.

---

**`AddRoomTypeAsync(Guid userId, CreateRoomTypeDto dto)`**

What it does: Creates a new room type for the admin's hotel.

Step by step:
1. Starts a database transaction
2. Gets the admin's hotel ID from their user record
3. Builds a new `RoomType` object from the DTO
4. Saves it to the database
5. Saves amenity associations (links amenity IDs to this room type in the join table)
6. Commits the transaction
7. Writes an audit log entry

```csharp
var roomType = new RoomType
{
    RoomTypeId = Guid.NewGuid(),
    HotelId = hotelId,          // from admin's user record
    Name = dto.Name,            // e.g. "Deluxe Room"
    Description = dto.Description,
    MaxOccupancy = dto.MaxOccupancy, // e.g. 2
    ImageUrl = dto.ImageUrl,
    IsActive = true
};
await _roomTypeRepo.AddAsync(roomType);
// Then link amenities: WiFi, AC, TV etc.
await SaveAmenityAssociationsAsync(roomType.RoomTypeId, dto.AmenityIds);
await _unitOfWork.CommitAsync();
```

---

**`UpdateRoomTypeAsync(Guid userId, UpdateRoomTypeDto dto)`**

What it does: Updates name, description, occupancy, image, and amenities.

Key detail — amenity update uses "remove all, re-insert" strategy:
```csharp
private async Task ReplaceAmenityAssociationsAsync(Guid roomTypeId, List<Guid> amenityIds)
{
    // Step 1: Delete all existing amenity links for this room type
    var existing = await _context.RoomTypeAmenities
        .Where(rta => rta.RoomTypeId == roomTypeId)
        .ToListAsync();
    _context.RoomTypeAmenities.RemoveRange(existing);

    // Step 2: Insert the new list
    await SaveAmenityAssociationsAsync(roomTypeId, amenityIds);
}
```
Why? It's simpler than calculating which ones to add/remove individually.

---

**`ToggleRoomTypeStatusAsync(Guid userId, Guid roomTypeId, bool isActive)`**

What it does: Activates or deactivates a room type. Deactivated room types don't appear in public search.

```csharp
roomType.IsActive = isActive; // true = visible, false = hidden
await _unitOfWork.SaveChangesAsync();
```

---

**`AddRateAsync(Guid userId, CreateRoomTypeRateDto dto)`**

What it does: Sets a price for a date range. Example: ₹2000/night from April 1 to April 30.

Validation steps:
1. Verifies the room type belongs to the admin's hotel
2. Checks start date is before end date
3. Checks no existing rate overlaps this date range (prevents double pricing)

```csharp
// Overlap check — if any existing rate's range intersects the new range, throw
var overlapping = await _rateRepo.GetQueryable()
    .AnyAsync(r => r.RoomTypeId == roomTypeId &&
                   start <= r.EndDate && end >= r.StartDate);
if (overlapping) throw new ConflictException("Rate already exists for this date range.");
```

---

**`GetRateByDateAsync(Guid userId, GetRateByDateRequestDto dto)`**

What it does: Returns the price for a specific date. Used by the frontend to show "₹2000/night" on the booking page.

```csharp
var rate = await _rateRepo.GetQueryable()
    .FirstOrDefaultAsync(r =>
        r.RoomTypeId == dto.RoomTypeId &&
        dto.Date >= r.StartDate &&   // date falls within the rate's range
        dto.Date <= r.EndDate)
    ?? throw new NotFoundException("Rate not found for the given date.");
return rate.Rate;
```

---

**`GetRoomTypesByHotelPagedAsync(Guid userId, int page, int pageSize)`**

What it does: Returns a paged list of room types with their amenities and room count.

```csharp
// EF Core projection — builds the DTO directly in SQL, no extra round trips
.Select(rt => new RoomTypeListDto
{
    RoomTypeId = rt.RoomTypeId,
    Name = rt.Name,
    RoomCount = rt.Rooms!.Count,  // counts physical rooms of this type
    AmenityList = rt.RoomTypeAmenities!.Select(rta => new AmenityItemDto
    {
        Name = rta.Amenity!.Name,
        IconName = rta.Amenity.IconName
    }).ToList()
})
```

---

### RoomService — Every Function

Manages physical rooms (Room 101, Room 202, etc.) inside a hotel.

---

**`AddRoomAsync(Guid userId, CreateRoomDto dto)`**

What it does: Creates a physical room and links it to a room type.

Validation chain:
1. Gets admin's hotel ID
2. Verifies the room type belongs to this hotel
3. Checks room number is unique within the hotel
4. Checks room count doesn't exceed the inventory maximum

```csharp
// Cap check — can't add more rooms than inventory allows
private async Task EnsureRoomCapacityNotExceededAsync(Guid roomTypeId, Guid hotelId)
{
    var currentCount = await _roomRepo.GetQueryable()
        .CountAsync(r => r.RoomTypeId == roomTypeId && r.HotelId == hotelId);

    var maxInventory = await _inventoryRepo.GetQueryable()
        .Where(i => i.RoomTypeId == roomTypeId)
        .MaxAsync(i => (int?)i.TotalInventory);

    if (currentCount >= maxInventory)
        throw new ConflictException($"Maximum rooms allowed: {maxInventory}.");
}
```

---

**`UpdateRoomAsync(Guid userId, UpdateRoomDto dto)`**

What it does: Changes room number, floor, or room type. Logs before/after values to audit log.

```csharp
var before = new { room.RoomNumber, room.Floor, room.RoomTypeId };
room.RoomNumber = dto.RoomNumber;
room.Floor = dto.Floor;
room.RoomTypeId = dto.RoomTypeId;
await _unitOfWork.CommitAsync();
// Audit log stores JSON: { "Before": {...}, "After": {...} }
await _auditLogService.LogAsync(userId, "RoomUpdated", "Room", room.RoomId,
    JsonSerializer.Serialize(new { Before = before, After = dto }));
```

---

**`ToggleRoomStatusAsync(Guid userId, Guid roomId, bool isActive)`**

What it does: Marks a room as active or inactive. Inactive rooms are excluded from booking assignment.

---

**`GetRoomsByHotelAsync(Guid userId, int pageNumber, int pageSize)`**

What it does: Returns paged list of rooms with their room type name. Used in admin room management table.

---

**`GetRoomCountByHotelAsync(Guid userId)`**

What it does: Returns total room count for the admin's hotel. Used in dashboard stats.

---

### AmenityService — Every Function

Manages the global amenity catalogue (WiFi, Pool, Gym, etc.). SuperAdmin controls this.

---

**`GetAllActiveAsync()`**

What it does: Returns all active amenities sorted by category then name. Used in public hotel details and admin room type creation.

```csharp
return await _amenityRepo.GetQueryable()
    .Where(a => a.IsActive)
    .OrderBy(a => a.Category)   // "Bathroom", "Food", "Room", "Services", "Tech"
    .ThenBy(a => a.Name)
    .Select(a => MapToDto(a))
    .ToListAsync();
```

---

**`SearchAsync(string query)`**

What it does: Returns up to 20 amenities matching the search term. Used in admin's amenity picker when creating room types.

```csharp
return await _amenityRepo.GetQueryable()
    .Where(a => a.IsActive && a.Name.ToLower().Contains(query.ToLower()))
    .Take(20)  // limit results
    .Select(a => MapToDto(a))
    .ToListAsync();
```

---

**`CreateAmenityAsync(CreateAmenityDto dto)`**

What it does: SuperAdmin creates a new amenity. Checks name is unique first.

```csharp
await EnsureNameIsUniqueAsync(dto.Name); // throws ConflictException if duplicate
var amenity = new Amenity
{
    AmenityId = Guid.NewGuid(),
    Name = dto.Name,       // e.g. "Rooftop Pool"
    Category = dto.Category, // e.g. "Services"
    IconName = dto.IconName, // e.g. "pool" (Material icon name for Angular)
    IsActive = true
};
```

---

**`DeleteAmenityAsync(Guid amenityId)`**

What it does: Deletes an amenity — but only if no room type is currently using it.

```csharp
private async Task EnsureNotInUseAsync(Guid amenityId)
{
    var inUse = await _context.RoomTypeAmenities
        .AnyAsync(rta => rta.AmenityId == amenityId);
    if (inUse) throw new ConflictException("Amenity is in use by one or more room types.");
}
```

---

**`ToggleAmenityStatusAsync(Guid amenityId)`**

What it does: Flips active/inactive. Returns the new status. Used by SuperAdmin to hide/show amenities.

```csharp
amenity.IsActive = !amenity.IsActive; // toggle
await _unitOfWork.SaveChangesAsync();
return amenity.IsActive; // returns true or false
```

---

### UserService — Every Function

Manages user profile data and booking history.

---

**`GetProfileAsync(Guid userId)`**

What it does: Returns the user's profile. If profile details don't exist yet (edge case for seeded accounts), auto-creates them.

```csharp
private async Task EnsureProfileDetailsExistAsync(User user)
{
    if (user.UserDetails is not null) return; // already exists, skip

    // Auto-create empty profile for accounts without one
    user.UserDetails = new UserProfileDetails
    {
        UserDetailsId = Guid.NewGuid(),
        UserId = user.UserId,
        Name = user.Name,
        Email = user.Email,
        PhoneNumber = string.Empty,
        // ...
    };
    await _unitOfWork.SaveChangesAsync();
}
```

Also calculates `TotalReviewPoints = reviewCount * 100` (each review = 100 points).

---

**`UpdateProfileAsync(Guid userId, UpdateUserProfileDto dto)`**

What it does: Updates profile fields. Only updates fields that are not null/empty — partial update pattern.

```csharp
private static void ApplyProfileUpdates(User user, UpdateUserProfileDto dto)
{
    var details = user.UserDetails!;
    if (!string.IsNullOrWhiteSpace(dto.Name))
    {
        details.Name = dto.Name;
        user.Name = dto.Name; // IMPORTANT: keeps User.Name in sync so reviews show updated name
    }
    if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)) details.PhoneNumber = dto.PhoneNumber;
    // ... only update fields that were provided
}
```

---

**`GetBookingHistoryAsync(Guid userId, int page, int pageSize)`**

What it does: Returns paged booking history for the user profile page. Simpler than the full reservation list — just shows hotel name, dates, amount, status.

---

### LogService — Every Function

Provides access to the error log table. SuperAdmin uses this to see all errors; users see only their own.

---

**`GetAllLogsAsync(int page, int pageSize, string? search)`**

What it does: Returns all error logs with optional search. SuperAdmin only.

Search matches against: `RequestPath`, `ExceptionType`, `UserName`, `Message`.

```csharp
private IQueryable<Log> BuildSearchQuery(string? search)
{
    var query = _logRepo.GetQueryable().AsQueryable();
    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(l =>
            l.RequestPath.Contains(search) ||
            l.ExceptionType.Contains(search) ||
            l.UserName.Contains(search) ||
            l.Message.Contains(search));
    return query.OrderByDescending(l => l.CreatedAt);
}
```

---

**`GetUserLogsAsync(Guid userId, int page, int pageSize)`**

What it does: Returns only the logs for a specific user. Used when a user wants to see their own error history.

---

**Compiled Expression in LogService**

```csharp
// This is a compiled LINQ expression — defined once, reused every query
// Avoids re-compiling the lambda on every call
private static readonly Expression<Func<Log, LogResponseDto>> ProjectToDto =
    l => new LogResponseDto
    {
        LogId = l.LogId,
        Message = l.Message,
        ExceptionType = l.ExceptionType,
        // ...
    };
```

This is a performance optimization. EF Core translates this expression to SQL once and caches it.

---

### SuperAdminRevenueService — Every Function

Records and retrieves the 2% platform commission on every completed reservation.

---

**`RecordCommissionAsync(Guid reservationId)`**

What it does: Creates a commission record when a reservation is completed. Called from `ReservationService.CompleteReservationAsync`.

Idempotent — safe to call twice, won't create duplicate records:

```csharp
public async Task RecordCommissionAsync(Guid reservationId)
{
    // Check if already recorded — if yes, skip
    if (await CommissionAlreadyRecordedAsync(reservationId)) return;

    var reservation = await _reservationRepo.GetAsync(reservationId);

    await _revenueRepo.AddAsync(new SuperAdminRevenue
    {
        ReservationId = reservationId,
        HotelId = reservation.HotelId,
        ReservationAmount = reservation.TotalAmount,
        CommissionAmount = Math.Round(reservation.TotalAmount * 0.02M, 2), // 2%
        SuperAdminUpiId = "thanushstayhubsuperadmin@okaxis",
        CreatedAt = DateTime.UtcNow
    });
    await _unitOfWork.SaveChangesAsync();
}
```

Example: Reservation total = ₹4000 → Commission = ₹80.

---

**`GetSummaryAsync()`**

What it does: Returns total commission earned across all time.

```csharp
var total = await _revenueRepo.GetQueryable()
    .SumAsync(r => (decimal?)r.CommissionAmount) ?? 0;
// (decimal?) cast handles the case where table is empty — SumAsync returns null
```

---

### AmenityRequestService — Every Function

Manages the workflow: Admin requests a new amenity → SuperAdmin approves/rejects.

---

**`CreateRequestAsync(Guid adminUserId, CreateAmenityRequestDto dto)`**

What it does: Admin submits a request for a new amenity (e.g. "Rooftop Jacuzzi").

```csharp
var request = new AmenityRequest
{
    AmenityRequestId = Guid.NewGuid(),
    RequestedByAdminId = adminUserId,
    AdminHotelId = admin.HotelId!.Value,
    AmenityName = dto.AmenityName,   // e.g. "Rooftop Jacuzzi"
    Category = dto.Category,          // e.g. "Services"
    IconName = dto.IconName,          // e.g. "hot_tub"
    Status = AmenityRequestStatus.Pending,
    CreatedAt = DateTime.UtcNow
};
```

---

**`ApproveRequestAsync(Guid requestId, Guid superAdminUserId)`**

What it does: SuperAdmin approves the request. This automatically creates the actual `Amenity` record in the global catalogue.

```csharp
public async Task<AmenityRequestResponseDto> ApproveRequestAsync(Guid requestId, ...)
{
    var request = await GetPendingRequestOrThrowAsync(requestId);

    // Create the actual amenity in the global catalogue
    await _amenityRepo.AddAsync(new Amenity
    {
        AmenityId = Guid.NewGuid(),
        Name = request.AmenityName,
        Category = request.Category,
        IconName = request.IconName,
        IsActive = true
    });

    // Mark request as approved
    request.Status = AmenityRequestStatus.Approved;
    request.ProcessedAt = DateTime.UtcNow;
    await _unitOfWork.SaveChangesAsync();
}
```

---

**`RejectRequestAsync(Guid requestId, Guid superAdminUserId, string note)`**

What it does: SuperAdmin rejects with a note explaining why.

```csharp
request.Status = AmenityRequestStatus.Rejected;
request.SuperAdminNote = note;  // e.g. "Already exists as 'Hot Tub'"
request.ProcessedAt = DateTime.UtcNow;
await _unitOfWork.SaveChangesAsync();
```

---

### SupportRequestService — Every Function

Handles support tickets from three types of submitters: public visitors, guests, and admins.

---

**`CreatePublicRequestAsync(PublicSupportRequestDto dto)`**

What it does: Anyone (not logged in) can submit a contact form. No UserId stored.

```csharp
var request = new SupportRequest
{
    GuestName = dto.Name,    // stored as plain text since no account
    GuestEmail = dto.Email,
    Subject = dto.Subject,
    Message = dto.Message,
    Category = dto.Category,
    SubmitterRole = "Public",
    Status = SupportRequestStatus.Open
};
```

---

**`CreateGuestRequestAsync(Guid userId, GuestSupportRequestDto dto)`**

What it does: Logged-in guest submits a complaint, optionally referencing a reservation and hotel.

```csharp
var request = new SupportRequest
{
    UserId = userId,
    SubmitterRole = "Guest",
    ReservationCode = dto.ReservationCode, // e.g. "RES-A1B2C3D4"
    HotelId = dto.HotelId,
    // ...
};
```

---

**`RespondAsync(Guid requestId, RespondSupportRequestDto dto)`**

What it does: SuperAdmin responds to a ticket and changes its status.

```csharp
private static void ApplyResponse(SupportRequest request, RespondSupportRequestDto dto)
{
    // Parse status from string — default to Resolved if invalid
    var newStatus = Enum.TryParse<SupportRequestStatus>(dto.Status, out var parsed)
        ? parsed
        : SupportRequestStatus.Resolved;

    request.Status = newStatus;
    request.RespondedAt = DateTime.UtcNow;
    request.AdminResponse = dto.Response; // the reply text
}
```

---

**`GetAllRequestsAsync(string? status, string? role, string? search, int page, int pageSize)`**

What it does: SuperAdmin sees all tickets with filters. Uses a `Func<SupportRequest, SupportRequestResponseDto>` delegate as a mapper parameter — explained in the .NET concepts section below.

```csharp
// The mapper is passed as a delegate — different callers pass different mapping logic
private static async Task<PagedSupportRequestResponseDto> BuildPagedResponseAsync(
    IQueryable<SupportRequest> query, int page, int pageSize,
    Func<SupportRequest, SupportRequestResponseDto> mapper) // ← delegate parameter
{
    var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    return new PagedSupportRequestResponseDto
    {
        Requests = items.Select(mapper) // applies the delegate to each item
    };
}
```

---

### QrCodeHelper — Static Helper

A static utility class (not a service, no DI) that generates QR code images.

---

**`GenerateQrCodeBase64(string content)`**

What it does: Takes a UPI payment string and returns a Base64-encoded PNG image that the Angular frontend displays as an `<img>` tag.

```csharp
public static string GenerateQrCodeBase64(string content)
{
    var pngBytes = RenderQrCodePng(content);
    return Convert.ToBase64String(pngBytes); // converts byte[] to base64 string
}

private static byte[] RenderQrCodePng(string content)
{
    using var generator = new QRCodeGenerator();
    using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
    using var code = new PngByteQRCode(data);
    return code.GetGraphic(10); // 10 pixels per module
}
```

The UPI string looks like:
```
upi://pay?pa=hotel@upi&pn=Grand%20Hotel&am=4020&cu=INR
```

Angular receives the base64 string and shows it as:
```html
<img [src]="'data:image/png;base64,' + qrCodeBase64" />
```

---


---

## B. Program.cs — Line by Line Explanation

`Program.cs` is the startup file. It runs once when the app starts. It does two jobs:
1. **Register services** — tell the DI container what classes exist
2. **Configure the pipeline** — tell ASP.NET Core how to handle requests

```csharp
var builder = WebApplication.CreateBuilder(args);
```
Creates the app builder. `args` are command-line arguments. The builder reads `appsettings.json` automatically.

---

```csharp
RegisterControllers(builder.Services);
```
Calls this helper:
```csharp
static void RegisterControllers(IServiceCollection services)
{
    services.AddControllers();           // enables MVC controllers
    services.AddEndpointsApiExplorer();  // enables Swagger endpoint discovery
}
```
`AddControllers()` scans all classes with `[ApiController]` and registers them.

---

```csharp
RegisterRateLimiting(builder.Services, builder.Configuration);
```
```csharp
static void RegisterRateLimiting(IServiceCollection services, IConfiguration config)
{
    services.AddMemoryCache();  // in-memory cache for rate limit counters
    services.Configure<IpRateLimitOptions>(config.GetSection("IpRateLimiting")); // reads appsettings.json
    services.AddInMemoryRateLimiting();
    services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
}
```
Reads rate limit rules from `appsettings.json` section `IpRateLimiting`. Example rule: max 100 requests per minute per IP.

---

```csharp
RegisterSwagger(builder.Services);
```
Registers Swagger UI with JWT support. In development, you can open `/swagger` in the browser to test all endpoints with a "Authorize" button to paste your JWT token.

---

```csharp
RegisterDatabase(builder.Services, builder.Configuration);
```
```csharp
static void RegisterDatabase(IServiceCollection services, IConfiguration config)
{
    services.AddDbContext<HotelBookingContext>(options =>
        options.UseSqlServer(
            config.GetConnectionString("Developer"), // reads from appsettings.json
            sqlOptions => sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
        ));
}
```
`AddDbContext` registers `HotelBookingContext` as a **Scoped** service — one instance per HTTP request. All repositories in the same request share the same context, which is why transactions work.

`SplitQuery` — when you do `.Include(r => r.ReservationRooms).Include(r => r.Hotel)`, EF Core runs separate SQL queries instead of one giant JOIN. Prevents "cartesian explosion" where rows multiply.

---

```csharp
RegisterCors(builder.Services);
```
```csharp
static void RegisterCors(IServiceCollection services)
{
    services.AddCors(options =>
    {
        options.AddPolicy("AngularClient", policy =>
            policy.WithOrigins("http://localhost:4200") // Angular dev server
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials());
    });
}
```
Without this, the browser blocks requests from Angular (port 4200) to the API (different port). CORS tells the browser "this origin is allowed."

---

```csharp
RegisterRepositories(builder.Services);
```
```csharp
static void RegisterRepositories(IServiceCollection services)
{
    services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
    services.AddScoped<IUnitOfWork, UnitOfWork>();
}
```
`typeof(IRepository<,>)` — the `<,>` means "open generic type." One registration covers ALL entity types:
- `IRepository<Guid, Hotel>` → `Repository<Guid, Hotel>`
- `IRepository<Guid, Reservation>` → `Repository<Guid, Reservation>`
- etc.

---

```csharp
RegisterApplicationServices(builder.Services);
```
Registers all 22 business services as **Scoped**:
```csharp
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IHotelService, HotelService>();
// ... 20 more
```
**Scoped** = one instance per HTTP request. When the request ends, the instance is disposed.

---

```csharp
RegisterBackgroundServices(builder.Services);
```
```csharp
services.AddHostedService<ReservationCleanupService>();
services.AddHostedService<HotelDeactivationRefundService>();
services.AddHostedService<NoShowAutoCancelService>();
```
`AddHostedService` registers as **Singleton** — starts when the app starts, runs forever. These are `BackgroundService` subclasses that loop every 5 minutes.

---

```csharp
RegisterAuthentication(builder.Services, builder.Configuration);
```
```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,           // don't check who issued the token
            ValidateAudience = false,         // don't check who the token is for
            ValidateLifetime = true,          // DO check expiry
            ValidateIssuerSigningKey = true,  // DO verify the signature
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        });
services.AddAuthorization();
```
This tells ASP.NET Core: "When you see `Authorization: Bearer ...`, validate it using HMAC-SHA256 with this key."

---

```csharp
var app = builder.Build();
ConfigurePipeline(app);
app.Run();
```
`builder.Build()` creates the app from all registrations. `app.Run()` starts listening for HTTP requests.

---

```csharp
static void ConfigurePipeline(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseMiddleware<GlobalExceptionMiddleware>(); // 1st — catches all errors
    app.UseCors("AngularClient");                  // 2nd — CORS headers
    app.UseIpRateLimiting();                       // 3rd — rate limit check
    app.UseRouting();                              // 4th — match URL to controller
    app.UseAuthentication();                       // 5th — validate JWT
    app.UseAuthorization();                        // 6th — check [Authorize] roles
    app.MapControllers();                          // 7th — execute controller
}
```

**Order matters.** If you put `UseAuthentication` before `UseMiddleware<GlobalExceptionMiddleware>`, auth errors won't be caught by the middleware. The exception middleware must be first.



---

# PART 3 — .NET Core Competency Guide (With Your Project Code)

---

## C. .NET Type System — Classes, Structs, Enums, Interfaces, Delegates

### What is the .NET Type System?

Every piece of data in C# has a type. .NET organizes types into categories. Your project uses all of them.

---

### 1. Classes (Reference Types)

A class is a blueprint. When you create an object from a class, it lives on the **heap** and is accessed by reference.

Every entity in your project is a class:

```csharp
// Models/Hotel.cs
public class Hotel
{
    public Guid HotelId { get; set; }      // value type stored inside reference type
    public string Name { get; set; }        // string is a reference type
    public bool IsActive { get; set; }      // bool is a value type
    public decimal GstPercent { get; set; } // decimal is a value type
    public ICollection<RoomType>? RoomTypes { get; set; } // reference to another class
}
```

When you do `var hotel = new Hotel()`, the object is created on the heap. The variable `hotel` holds a reference (memory address) to it.

---

### 2. Structures (Value Types)

Structs live on the **stack** and are copied when assigned. `Guid`, `DateOnly`, `DateTime`, `decimal`, `int`, `bool` are all value types (structs).

Your project uses them everywhere:

```csharp
// Models/Reservation.cs
public Guid ReservationId { get; set; }    // Guid is a struct — 16 bytes
public DateOnly CheckInDate { get; set; }  // DateOnly is a struct — date without time
public decimal TotalAmount { get; set; }   // decimal is a struct — precise money math
public bool IsCheckedIn { get; set; }      // bool is a struct — true/false
```

Why `decimal` for money and not `double`?
- `double` has floating-point errors: `0.1 + 0.2 = 0.30000000000000004`
- `decimal` is exact: `0.1 + 0.2 = 0.3`
- That's why all monetary fields use `HasPrecision(18, 2)` in DbContext.

---

### 3. Enumerations (Enums)

Enums are named integer constants. They make code readable and prevent magic numbers.

Your project has 6 enums:

```csharp
// Models/User.cs
public enum UserRole
{
    Guest = 1,
    Admin = 2,
    SuperAdmin = 3
}

// Models/Reservation.cs
public enum ReservationStatus
{
    Pending = 1,
    Confirmed = 2,
    Cancelled = 3,
    Completed = 4,
    NoShow = 5
}

// Models/Transaction.cs
public enum PaymentMethod
{
    CreditCard = 1,
    DebitCard = 2,
    UPI = 3,
    NetBanking = 4,
    Wallet = 5
}

public enum PaymentStatus
{
    Pending = 1,
    Success = 2,
    Failed = 3,
    Refunded = 4
}

// Models/AmenityRequest.cs
public enum AmenityRequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

// Models/SupportRequest.cs
public enum SupportRequestStatus
{
    Open = 1,
    InProgress = 2,
    Resolved = 3
}
```

How enums are stored in DB — as integers:
```csharp
// HotelBookingContext.cs
modelBuilder.Entity<Reservation>()
    .Property(r => r.Status)
    .HasConversion<int>(); // Pending=1, Confirmed=2, etc. stored as numbers
```

How enums are used in code:
```csharp
// ReservationService.cs — comparing enum values
if (res.Status == ReservationStatus.Cancelled)
    throw new ReservationFailedException("Already cancelled.");

// Parsing enum from string (from query parameter)
if (Enum.TryParse<ReservationStatus>(status, out var statusEnum))
    query = query.Where(r => r.Status == statusEnum);

// Converting enum to string for response DTO
Status = r.Status.ToString() // "Confirmed", "Pending", etc.
```

---

### 4. Interfaces

An interface is a contract — it says "any class that implements me MUST have these methods." It has no implementation, only signatures.

Every service in your project has an interface:

```csharp
// Interfaces/IHotelService.cs
public interface IHotelService
{
    Task<IEnumerable<HotelListItemDto>> GetTopHotelsAsync();
    Task<SearchHotelResponseDto> SearchHotelsAsync(SearchHotelRequestDto request);
    Task UpdateHotelAsync(Guid userId, UpdateHotelDto dto);
    Task BlockHotelAsync(Guid hotelId);
    Task UnblockHotelAsync(Guid hotelId);
    // ... more methods
}
```

The concrete class `HotelService` implements it:
```csharp
public class HotelService : IHotelService  // "implements IHotelService"
{
    public async Task<IEnumerable<HotelListItemDto>> GetTopHotelsAsync()
    {
        // actual implementation here
    }
}
```

The controller only knows the interface — not the concrete class:
```csharp
public class SuperAdminHotelController : ControllerBase
{
    private readonly IHotelService _hotelService; // interface type

    public SuperAdminHotelController(IHotelService hotelService)
        => _hotelService = hotelService; // DI injects HotelService at runtime
}
```

Why? If you ever want to swap `HotelService` for a different implementation (e.g. for testing), you only change the DI registration — not the controller.

---

### 5. Delegates and Func/Action

A **delegate** is a type that holds a reference to a method. Think of it as a "method variable" — you can pass a method as a parameter.

`Func<T, TResult>` is a built-in delegate type that takes input and returns output.
`Action<T>` is a built-in delegate type that takes input and returns nothing.

**Real example from your project — SupportRequestService:**

```csharp
// BuildPagedResponseAsync accepts a Func delegate as a parameter
private static async Task<PagedSupportRequestResponseDto> BuildPagedResponseAsync(
    IQueryable<SupportRequest> query,
    int page,
    int pageSize,
    Func<SupportRequest, SupportRequestResponseDto> mapper) // ← delegate parameter
{
    var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    return new PagedSupportRequestResponseDto
    {
        Requests = items.Select(mapper) // applies the delegate to each item
    };
}
```

The caller passes a different lambda (anonymous method) each time:

```csharp
// Guest call — maps with guest's name and email
return await BuildPagedResponseAsync(query, page, pageSize,
    r => MapToDto(r, user.Name, user.Email, r.Hotel?.Name));

// SuperAdmin call — maps with user name from navigation property
return await BuildPagedResponseAsync(query, page, pageSize,
    r => {
        var name = r.User?.Name ?? r.GuestName ?? string.Empty;
        return MapToDto(r, name, email, r.Hotel?.Name);
    });
```

Same method, different behavior — that's the power of delegates.

**Another example — compiled Expression delegate in LogService and AuditLogService:**

```csharp
// LogService.cs — Expression<Func<>> is a delegate stored as a syntax tree
// EF Core can translate it to SQL
private static readonly Expression<Func<Log, LogResponseDto>> ProjectToDto =
    l => new LogResponseDto
    {
        LogId = l.LogId,
        Message = l.Message,
        ExceptionType = l.ExceptionType,
        // ...
    };

// Used in query — EF Core translates this to a SQL SELECT projection
var logs = await query.Select(ProjectToDto).ToListAsync();
```

`Expression<Func<>>` vs `Func<>`:
- `Func<Log, LogResponseDto>` — a compiled method, runs in memory
- `Expression<Func<Log, LogResponseDto>>` — a syntax tree, EF Core reads it and converts to SQL

**Lambda expressions are anonymous delegates:**
```csharp
// This lambda:
r => r.Status == ReservationStatus.Confirmed

// Is the same as writing a named delegate:
bool IsConfirmed(Reservation r) => r.Status == ReservationStatus.Confirmed;
```


---

## D. Input / Output in .NET

### What is I/O?

I/O means reading from or writing to something outside your program — files, network, database, HTTP.

---

### 1. Reading Configuration (File I/O via IConfiguration)

Your app reads `appsettings.json` at startup. This is file-based I/O abstracted by `IConfiguration`:

```csharp
// Program.cs — reads JWT key from appsettings.json
var jwtKey = config["Keys:Jwt"]
    ?? throw new InvalidOperationException("JWT Key not found.");

// TokenService.cs — reads the same key
public TokenService(IConfiguration configuration)
{
    var secret = configuration["Keys:Jwt"]
        ?? throw new InvalidOperationException("JWT Key not configured.");
    _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
}
```

`appsettings.json` looks like:
```json
{
  "ConnectionStrings": {
    "Developer": "Server=...;Database=HotelBookingDb;..."
  },
  "Keys": {
    "Jwt": "your-super-secret-key-here"
  },
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "GeneralRules": [
      { "Endpoint": "*", "Period": "1m", "Limit": 100 }
    ]
  }
}
```

---

### 2. Network I/O — HTTP Requests (Receiving)

Your API receives HTTP requests from Angular. ASP.NET Core handles the low-level socket reading. You just write controllers:

```csharp
// GuestReservationController.cs
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateReservationDto dto)
{
    // ASP.NET Core already:
    // 1. Read the HTTP request body from the network socket
    // 2. Deserialized the JSON into CreateReservationDto
    // 3. Validated [Required] and [Range] annotations
    // You just use dto directly
    var result = await _reservationService.CreateReservationAsync(GetUserId(), dto);
    return Ok(new { success = true, data = result });
    // ASP.NET Core then:
    // 4. Serializes the response object to JSON
    // 5. Writes it back to the network socket
}
```

---

### 3. Database I/O (via EF Core)

All database reads/writes are async I/O operations:

```csharp
// Reading from DB — async I/O
var hotel = await _hotelRepo.GetAsync(hotelId);
// EF Core sends SQL to SQL Server over a network connection
// Awaits the response without blocking the thread

// Writing to DB — async I/O
await _reservationRepo.AddAsync(reservation);
await _unitOfWork.CommitAsync();
// EF Core sends INSERT SQL to SQL Server
```

---

### 4. Binary I/O — QR Code Generation

`QrCodeHelper` generates a PNG image as bytes:

```csharp
// QrCodeHelper.cs
private static byte[] RenderQrCodePng(string content)
{
    using var generator = new QRCodeGenerator();
    using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
    using var code = new PngByteQRCode(data);
    return code.GetGraphic(10); // returns raw PNG bytes
}

// Then converts bytes to Base64 string for JSON transport
return Convert.ToBase64String(pngBytes);
```

`byte[]` is raw binary data. `Convert.ToBase64String` encodes it as text so it can be sent in JSON.

---

### 5. Password Hashing — Byte Array I/O

Passwords are stored as `byte[]` (binary), not strings:

```csharp
// User.cs
public byte[] Password { get; set; } = Array.Empty<byte>();
public byte[] PasswordSaltValue { get; set; } = Array.Empty<byte>();
```

On login, the input password is hashed and compared byte-by-byte:
```csharp
// AuthService.cs
var hashed = _passwordService.HashPassword(dto.Password, user.PasswordSaltValue, out _);
if (!hashed.SequenceEqual(user.Password)) // byte-by-byte comparison
    throw new UnAuthorizedException("Invalid credentials.");
```

`SequenceEqual` is a LINQ extension that compares two sequences element by element.

---

## E. Transforming Data — String, DateTime, JSON

### 1. String Operations

```csharp
// Checking if a string is null or whitespace
if (string.IsNullOrWhiteSpace(dto.PromoCodeUsed)) { ... }

// String interpolation — building messages
$"Wallet payment for reservation {reservationCode}"
$"Refund ({refundNote}) for {res.ReservationCode}"
$"2% commission sent to SuperAdmin for reservation {commission.Reservation?.ReservationCode}"

// String comparison (case-insensitive)
h.City.ToLower() == request.City.ToLower()
a.Name.ToLower().Contains(query.ToLower())

// String formatting for codes
$"RES-{Guid.NewGuid().ToString("N")[..8].ToUpper()}"
// "N" format = no dashes: "a1b2c3d4e5f6..."
// [..8] = take first 8 characters
// .ToUpper() = "A1B2C3D4"
// Result: "RES-A1B2C3D4"

// Same pattern for promo codes
$"PROMO-{Guid.NewGuid().ToString("N")[..8].ToUpper()}"
// Result: "PROMO-A1B2C3D4"
```

---

### 2. DateTime Operations

```csharp
// Getting current UTC time
DateTime.UtcNow

// Getting current local time (used for IST timezone in India)
DateTime.Now  // used in ReservationService for check-in date validation

// DateOnly — date without time component (new in .NET 6)
DateOnly.FromDateTime(DateTime.Now)  // converts DateTime to DateOnly
DateOnly.FromDateTime(DateTime.UtcNow)

// Date arithmetic
var today = DateOnly.FromDateTime(DateTime.Now);
var daysUntilCheckIn = res.CheckInDate.DayNumber - today.DayNumber;
// DayNumber is an integer — easy subtraction

// Adding days to DateOnly
date.AddDays(1)  // next day

// Expiry time — 10 minutes from now
ExpiryTime = DateTime.UtcNow.AddMinutes(10)

// Promo code expiry — 90 days from now
ExpiryDate = DateTime.UtcNow.AddDays(90)

// Token expiry — 1 day from now
Expires = DateTime.UtcNow.Add(TokenLifetime) // TimeSpan.FromDays(1)

// Comparing dates
if (reservation.ExpiryTime < DateTime.UtcNow) // expired?
if (promo.ExpiryDate < DateTime.UtcNow)        // promo expired?
```

---

### 3. JSON Serialization / Deserialization

Your project uses `System.Text.Json` (built into .NET):

```csharp
// Serializing to JSON string — for audit logs
using System.Text.Json;

var changes = new
{
    Before = new { hotel.Name, hotel.Address, hotel.City },
    After = new { dto.Name, dto.Address, dto.City }
};
await _auditLogService.LogAsync(userId, "HotelUpdated", "Hotel",
    hotel.HotelId, JsonSerializer.Serialize(changes));
// Stores: {"Before":{"Name":"Old Name",...},"After":{"Name":"New Name",...}}
```

ASP.NET Core automatically deserializes incoming JSON:
```csharp
// When Angular sends: { "hotelId": "...", "checkInDate": "2026-04-15" }
// ASP.NET Core reads the body and fills this DTO automatically
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateReservationDto dto)
// dto.HotelId, dto.CheckInDate are already populated
```

And serializes outgoing responses:
```csharp
return Ok(new { success = true, data = result });
// Automatically becomes: {"success":true,"data":{...}}
```

---

### 4. Enum to String / String to Enum

```csharp
// Enum → string (for response DTOs)
Status = r.Status.ToString()  // ReservationStatus.Confirmed → "Confirmed"

// String → Enum (for filter parameters from Angular)
if (Enum.TryParse<ReservationStatus>(status, out var statusEnum))
    query = query.Where(r => r.Status == statusEnum);
// "Confirmed" → ReservationStatus.Confirmed

// String → Enum with default fallback
var newStatus = Enum.TryParse<SupportRequestStatus>(dto.Status, out var parsed)
    ? parsed
    : SupportRequestStatus.Resolved; // default if parse fails
```

---

### 5. Decimal Formatting and Rounding

```csharp
// Round to 2 decimal places (for money)
var gstAmount = Math.Round(totalAmount * gstPercent / 100, 2);
var discountAmount = Math.Round(dto.TotalAmount * promo.DiscountPercent / 100, 2);
var commissionAmount = Math.Round(reservation.TotalAmount * 0.02M, 2);

// Math.Max — ensure value never goes below 0
inv.ReservedInventory = Math.Max(0, inv.ReservedInventory - roomCount);
var finalAmount = Math.Max(0, totalAmount + gstAmount - discountAmount - walletUsed);

// Math.Min — cap wallet usage at the maximum allowed
walletUsed = Math.Min(dto.WalletAmountToUse, maxWallet);
```

---

## F. Collections — List, Dictionary, HashSet, IEnumerable, IQueryable

### 1. List\<T\>

An ordered, resizable collection. Most common collection in your project.

```csharp
// ReservationService.cs — building a list of dates
var dates = Enumerable.Range(0, totalDays)
    .Select(d => checkIn.AddDays(d))
    .ToList(); // IEnumerable → List<DateOnly>
// Result: [2026-04-15, 2026-04-16] for a 2-night stay

// List of assigned rooms
var assignedRooms = await _roomRepo.GetQueryable()
    .Where(r => r.RoomTypeId == dto.RoomTypeId && r.IsActive)
    .Take(dto.NumberOfRooms)
    .ToListAsync(); // executes SQL and returns List<Room>

// List of amenity IDs from DTO
public List<Guid>? AmenityIds { get; set; }
public List<Guid>? SelectedRoomIds { get; set; }
```

---

### 2. Dictionary\<TKey, TValue\>

Key-value lookup. O(1) access time. Used for fast lookups in your project.

```csharp
// HotelService.cs — build a dictionary of reservation counts per hotel
var reservationCounts = await _reservationRepo.GetQueryable()
    .GroupBy(r => r.HotelId)
    .Select(g => new { HotelId = g.Key, Count = g.Count() })
    .ToDictionaryAsync(x => x.HotelId, x => x.Count);
// { "hotel-guid-1": 45, "hotel-guid-2": 12, ... }

// Then look up count for each hotel in O(1)
TotalReservations = reservationCounts.TryGetValue(hotel.HotelId, out var rc) ? rc : 0;

// TransactionService.cs — wallet ID to user ID mapping
var walletIds = walletEntries.ToDictionary(w => w.WalletId, w => w.UserId);
// { walletGuid: userGuid, ... }

// NoShowAutoCancelService.cs — room occupancy map
var occupancyMap = occupiedRoomIds.ToDictionary(x => x.RoomId, x => x.ReservationCode);
// { roomGuid: "RES-A1B2C3D4", ... }
```

---

### 3. HashSet\<T\>

A set — no duplicates, O(1) lookup. Used for fast "does this exist?" checks.

```csharp
// InventoryService.cs — get existing dates as a HashSet for fast lookup
var existingDates = await _inventoryRepo.GetQueryable()
    .Where(i => i.RoomTypeId == roomTypeId && i.Date >= start && i.Date <= end)
    .Select(i => i.Date)
    .ToListAsync();
var existingDatesSet = dates.ToHashSet(); // HashSet<DateOnly>

// Then check in O(1) instead of O(n)
if (existingDatesSet.Contains(date)) continue; // skip if already exists
```

---

### 4. IEnumerable\<T\> vs IQueryable\<T\>

This is critical to understand for EF Core performance.

```csharp
// IQueryable<T> — the query is NOT executed yet
// It builds up a SQL query expression tree
IQueryable<Reservation> query = _reservationRepo.GetQueryable()
    .Where(r => r.UserId == userId);  // adds WHERE clause to SQL

query = query.Where(r => r.Status == statusEnum); // adds another WHERE
query = query.OrderByDescending(r => r.CreatedDate); // adds ORDER BY

// Only when you call ToListAsync() does the SQL actually execute
var items = await query.Skip(0).Take(10).ToListAsync();
// SQL: SELECT * FROM Reservations WHERE UserId=? AND Status=? ORDER BY CreatedDate DESC OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY

// IEnumerable<T> — data is already in memory
// Operations happen in C#, not SQL
IEnumerable<Reservation> inMemory = items; // already loaded
var confirmed = inMemory.Where(r => r.Status == ReservationStatus.Confirmed); // C# filter
```

Rule: Use `IQueryable` for database queries (filters happen in SQL). Use `IEnumerable` for in-memory operations.

---

### 5. AsNoTracking() — Performance Optimization

```csharp
// HotelService.cs — public read-only queries use AsNoTracking
var hotels = await _hotelRepo.GetQueryable()
    .AsNoTracking()  // EF Core won't track changes to these objects
    .Where(h => h.IsActive)
    .ToListAsync();
```

Without `AsNoTracking`, EF Core tracks every loaded entity in memory (change tracking). For read-only queries, this wastes memory. `AsNoTracking` skips tracking and is faster.

---

## G. LINQ — Language Integrated Query

LINQ lets you query collections and databases using C# syntax. Your project uses LINQ extensively.

### Basic LINQ Operations

```csharp
// WHERE — filter
query.Where(r => r.UserId == userId)
query.Where(h => h.IsActive && !h.IsBlockedBySuperAdmin)

// SELECT — project/transform
query.Select(h => new HotelListItemDto { Name = h.Name, City = h.City })

// ORDER BY
query.OrderByDescending(r => r.CreatedDate)
query.OrderBy(a => a.Category).ThenBy(a => a.Name)

// SKIP + TAKE — pagination
query.Skip((page - 1) * pageSize).Take(pageSize)

// COUNT
await query.CountAsync()

// ANY — does at least one match?
await _amenityRepo.GetQueryable().AnyAsync(a => a.Name == dto.Name)

// FIRST OR DEFAULT — get first match or null
await _userRepo.FirstOrDefaultAsync(u => u.Email == dto.Email)

// SUM — aggregate
await query.SumAsync(t => (decimal?)t.Amount) ?? 0

// MIN — find minimum
h.RoomTypes!.SelectMany(rt => rt.Rates!).Min(r => (decimal?)r.Rate)

// AVERAGE
h.Reviews!.Average(r => (decimal?)r.Rating)
```

---

### Intermediate LINQ — GroupBy, SelectMany, Joins

```csharp
// GROUP BY — group reservations by hotel, count each group
var reservationCounts = await _reservationRepo.GetQueryable()
    .GroupBy(r => r.HotelId)
    .Select(g => new { HotelId = g.Key, Count = g.Count() })
    .ToDictionaryAsync(x => x.HotelId, x => x.Count);

// SELECT MANY — flatten nested collections
// Get all rates from all room types of a hotel
h.RoomTypes!.SelectMany(rt => rt.Rates!)
// RoomTypes is List<RoomType>, each has List<RoomTypeRate>
// SelectMany flattens: [[rate1,rate2],[rate3]] → [rate1,rate2,rate3]

// Then find minimum price
.SelectMany(rt => rt.Rates!).Min(r => (decimal?)r.Rate)

// DISTINCT — remove duplicates
var bookedRoomIds = await _reservationRoomRepo.GetQueryable()
    .Where(rr => rr.Reservation!.Status == ReservationStatus.Confirmed)
    .Select(rr => rr.RoomId)
    .Distinct()  // remove duplicate room IDs
    .ToListAsync();

// CONTAINS — SQL IN clause
var rooms = await _roomRepo.GetQueryable()
    .Where(r => dto.SelectedRoomIds.Contains(r.RoomId))
    .ToListAsync();
// SQL: WHERE RoomId IN ('guid1', 'guid2', ...)

// INTERSECT — find common elements between two lists
var conflicting = dto.SelectedRoomIds.Intersect(bookedRoomIds).ToList();

// INCLUDE + THEN INCLUDE — eager loading (SQL JOINs)
await _reservationRepo.GetQueryable()
    .Include(r => r.ReservationRooms!)      // JOIN ReservationRooms
        .ThenInclude(rr => rr.Room)          // JOIN Rooms
    .Include(r => r.ReservationRooms!)
        .ThenInclude(rr => rr.RoomType)      // JOIN RoomTypes
    .Include(r => r.Hotel)                   // JOIN Hotels
    .ToListAsync();

// AsSplitQuery — runs as separate SQL queries instead of one big JOIN
.AsSplitQuery()
```

---

### Switch Expression (Pattern Matching LINQ-style)

```csharp
// PromoCodeService.cs — discount tier based on amount
private static decimal CalculateDiscountPercent(decimal totalAmount) => totalAmount switch
{
    <= 500  => 5,
    <= 1000 => 10,
    <= 2000 => 15,
    <= 5000 => 20,
    _       => 25   // default case
};

// HotelService.cs — dynamic sorting
query = sortField?.ToLower() switch
{
    "price_asc"  => query.OrderBy(h => h.RoomTypes!.SelectMany(rt => rt.Rates!).Min(r => (decimal?)r.Rate)),
    "price_desc" => query.OrderByDescending(h => h.RoomTypes!.SelectMany(rt => rt.Rates!).Min(r => (decimal?)r.Rate)),
    _            => query.OrderBy(h => h.Name)
};
```


---

## H. Routing — How URLs Map to Controllers

### What is Routing?

Routing is the process of matching an incoming HTTP request URL to a specific controller action method.

### Attribute Routing (used in your project)

Every controller and action has route attributes:

```csharp
// The controller sets the base route
[Route("api/guest/reservations")]
[ApiController]
[Authorize(Roles = "Guest")]
public class GuestReservationController : ControllerBase
{
    // GET api/guest/reservations
    [HttpGet]
    public async Task<IActionResult> GetAll() { ... }

    // GET api/guest/reservations/RES-A1B2C3D4
    [HttpGet("{code}")]
    public async Task<IActionResult> GetByCode(string code) { ... }
    // {code} is a route parameter — extracted from the URL

    // POST api/guest/reservations
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReservationDto dto) { ... }

    // PATCH api/guest/reservations/RES-A1B2C3D4/cancel
    [HttpPatch("{code}/cancel")]
    public async Task<IActionResult> Cancel(string code, [FromBody] CancelReservationDto dto) { ... }

    // GET api/guest/reservations/available-rooms?hotelId=...&checkIn=...
    [HttpGet("available-rooms")]
    public async Task<IActionResult> GetAvailableRooms(
        [FromQuery] Guid hotelId,      // from URL query string
        [FromQuery] DateOnly checkIn)  { ... }
}
```

### Parameter Sources

| Attribute | Where it reads from | Example |
|---|---|---|
| `[FromBody]` | Request body (JSON) | `POST` with JSON payload |
| `[FromQuery]` | URL query string | `?hotelId=...&page=1` |
| `[FromRoute]` | URL path segment | `/{id}` in the route |
| `[FromHeader]` | HTTP header | `Authorization: Bearer ...` |

### Route Examples from Your Project

```
POST   /api/auth/login                          → AuthenticationController.Login
GET    /api/hotels/top                          → PublicHotelController.GetTop
POST   /api/hotels/search                       → PublicHotelController.Search
GET    /api/hotels/{id}                         → PublicHotelController.GetDetails
GET    /api/guest/reservations                  → GuestReservationController.GetAll
POST   /api/guest/reservations                  → GuestReservationController.Create
GET    /api/guest/reservations/{code}           → GuestReservationController.GetByCode
PATCH  /api/guest/reservations/{code}/cancel    → GuestReservationController.Cancel
POST   /api/admin/inventory                     → AdminInventoryController.Add
GET    /api/admin/inventory?roomTypeId=...      → AdminInventoryController.Get
PATCH  /api/superadmin/hotels/{id}/block        → SuperAdminHotelController.Block
```

### How MapControllers() Works

```csharp
// Program.cs
app.MapControllers();
```

This scans all classes with `[ApiController]` and registers their routes. When a request comes in, ASP.NET Core matches the URL + HTTP method to the correct action method.

---

## I. Memory Cache

### What is Memory Cache?

Memory cache stores data in RAM for fast access. Instead of hitting the database every time, you cache the result and return it from memory.

### How Your Project Uses It

Your project uses memory cache for **IP rate limiting**:

```csharp
// Program.cs
services.AddMemoryCache(); // registers IMemoryCache in DI

services.Configure<IpRateLimitOptions>(config.GetSection("IpRateLimiting"));
services.AddInMemoryRateLimiting(); // uses IMemoryCache internally
```

The `AspNetCoreRateLimit` library uses `IMemoryCache` to track how many requests each IP has made within the time window. When the limit is exceeded, it returns `429 Too Many Requests`.

### IMemoryCache Basics (how it works internally)

```csharp
// Injecting and using IMemoryCache
public class SomeService
{
    private readonly IMemoryCache _cache;

    public SomeService(IMemoryCache cache) => _cache = cache;

    public string GetData(string key)
    {
        // Try to get from cache first
        if (_cache.TryGetValue(key, out string? cached))
            return cached!; // cache hit — fast

        // Cache miss — fetch from DB
        var data = FetchFromDatabase(key);

        // Store in cache with expiry
        _cache.Set(key, data, TimeSpan.FromMinutes(5));
        return data;
    }
}
```

### Cache Limits

- Default memory cache has no size limit — it uses available RAM
- You can set size limits with `MemoryCacheOptions`
- Entries expire based on `AbsoluteExpiration` or `SlidingExpiration`
- `SlidingExpiration` resets the timer on each access
- `AbsoluteExpiration` expires regardless of access

---

## J. Building a REST API Service — How Your Project Does It

### What Makes a REST API?

REST (Representational State Transfer) uses HTTP methods to perform operations on resources.

Your project follows REST conventions:

```
Resource: Hotels
GET    /api/hotels          → list hotels
GET    /api/hotels/{id}     → get one hotel
POST   /api/hotels/search   → search hotels (POST because of complex body)
PUT    /api/admin/hotel     → update hotel
PATCH  /api/admin/hotel/status → partial update (just status)

Resource: Reservations
POST   /api/guest/reservations          → create
GET    /api/guest/reservations          → list mine
GET    /api/guest/reservations/{code}   → get one
PATCH  /api/guest/reservations/{code}/cancel → cancel
```

### The Full Stack of a REST Endpoint

```csharp
// 1. Model — the database entity
public class Reservation { ... }

// 2. DTO — the API shape
public class CreateReservationDto
{
    [Required] public Guid HotelId { get; set; }
    [Required] public DateOnly CheckInDate { get; set; }
    // ...
}

// 3. Interface — the contract
public interface IReservationService
{
    Task<ReservationResponseDto> CreateReservationAsync(Guid userId, CreateReservationDto dto);
}

// 4. Service — the business logic
public class ReservationService : IReservationService
{
    public async Task<ReservationResponseDto> CreateReservationAsync(Guid userId, CreateReservationDto dto)
    {
        // validate, calculate, save, return
    }
}

// 5. Controller — the HTTP endpoint
[Route("api/guest/reservations")]
[ApiController]
[Authorize(Roles = "Guest")]
public class GuestReservationController : ControllerBase
{
    private readonly IReservationService _reservationService;

    public GuestReservationController(IReservationService reservationService)
        => _reservationService = reservationService;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReservationDto dto)
    {
        var result = await _reservationService.CreateReservationAsync(GetUserId(), dto);
        return Ok(new { success = true, data = result });
    }
}

// 6. Registration — Program.cs
services.AddScoped<IReservationService, ReservationService>();
```

---

## K. Data Access — ADO.NET vs Entity Framework Core

### ADO.NET (Low Level — not used directly in your project)

ADO.NET is the raw database API. You write SQL manually:

```csharp
// ADO.NET style (your project does NOT use this directly)
using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();
var command = new SqlCommand("SELECT * FROM Hotels WHERE IsActive = 1", connection);
var reader = await command.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    var hotel = new Hotel { Name = reader["Name"].ToString() };
}
```

### Entity Framework Core (what your project uses)

EF Core is an ORM (Object-Relational Mapper). You write C# and it generates SQL:

```csharp
// EF Core style — C# code, SQL generated automatically
var hotels = await _context.Hotels
    .Where(h => h.IsActive)
    .OrderBy(h => h.Name)
    .ToListAsync();
// EF Core generates: SELECT * FROM Hotels WHERE IsActive = 1 ORDER BY Name
```

### How EF Core Works in Your Project

```
C# Code → EF Core → SQL → SQL Server → Results → C# Objects
```

1. You call `_hotelRepo.GetQueryable().Where(...).ToListAsync()`
2. EF Core builds a SQL query from the LINQ expression
3. Sends it to SQL Server via ADO.NET internally
4. SQL Server returns rows
5. EF Core maps rows back to `Hotel` objects
6. You get `List<Hotel>`

### DbContext is the Heart of EF Core

```csharp
// HotelBookingContext.cs
public class HotelBookingContext : DbContext
{
    // Each DbSet = one table
    public DbSet<Hotel> Hotels { get; set; }
    public DbSet<Reservation> Reservations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure relationships, indexes, constraints
        modelBuilder.Entity<Hotel>()
            .HasIndex(h => h.City); // creates SQL index on City column

        modelBuilder.Entity<Reservation>()
            .HasIndex(r => r.ReservationCode)
            .IsUnique(); // UNIQUE constraint
    }
}
```

### Change Tracking

EF Core tracks every entity you load. When you call `SaveChanges`, it detects what changed and generates UPDATE SQL:

```csharp
// Load hotel — EF Core starts tracking it
var hotel = await _hotelRepo.GetAsync(hotelId);

// Modify it
hotel.IsActive = false; // EF Core detects this change

// Save — EF Core generates: UPDATE Hotels SET IsActive = 0 WHERE HotelId = ?
await _unitOfWork.SaveChangesAsync();
```

`AsNoTracking()` disables this for read-only queries — faster because EF Core doesn't need to track the objects.


---

## L. .NET Ecosystem — .NET Core vs .NET Framework vs .NET Standard

### .NET Framework (Old — Windows only)
- Released 2002, Windows-only
- Your project does NOT use this
- Example: old ASP.NET WebForms, WCF

### .NET Core (What your project uses)
- Cross-platform: Windows, Linux, macOS
- Open source, fast, modern
- Your project uses **.NET 8** (latest LTS as of 2024)
- `dotnet run`, `dotnet build`, `dotnet test` all work on any OS

### .NET Standard
- A specification (not an implementation)
- Libraries targeting .NET Standard work on both .NET Framework AND .NET Core
- Example: a NuGet package targeting `netstandard2.0` works everywhere

### Your Project's Target Framework

```xml
<!-- HotelBookingAppWebApi.csproj -->
<TargetFramework>net8.0</TargetFramework>
```

This means it runs on .NET 8 runtime. The runtime is what actually executes your compiled code.

### NuGet Packages Used in Your Project

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
<PackageReference Include="AspNetCoreRateLimit" />
<PackageReference Include="QRCoder" />
<PackageReference Include="Moq" />           <!-- for unit tests -->
<PackageReference Include="xunit" />         <!-- test framework -->
```

NuGet is the .NET package manager — like npm for Node.js.

---

## M. Common Language Infrastructure (CLI)

### What is CLI?

The Common Language Infrastructure is the specification that allows multiple languages (C#, F#, VB.NET) to run on the same runtime.

### How It Works

```
Your C# Code (.cs files)
        ↓
Roslyn Compiler (csc / dotnet build)
        ↓
IL (Intermediate Language) — .dll files
        ↓
CLR (Common Language Runtime) — JIT compiles IL to native machine code
        ↓
CPU executes native code
```

### IL (Intermediate Language)

When you build your project, C# is compiled to IL (also called MSIL or CIL), not directly to machine code. IL is stored in `.dll` files.

```
dotnet build
→ Creates: HotelBookingAppWebApi.dll (contains IL)
→ Creates: HotelBookingAppWebApi.exe (entry point)
```

### JIT (Just-In-Time) Compilation

When your app runs, the CLR's JIT compiler converts IL to native machine code on the fly. The first call to a method is slower (JIT compiles it), subsequent calls are fast (cached native code).

### Roslyn Compiler

Roslyn is the C# compiler. It:
- Parses your `.cs` files into syntax trees
- Performs type checking and semantic analysis
- Generates IL code
- Reports errors and warnings

When you see a red squiggle in VS Code, that's Roslyn telling you about a compile error.

---

## N. Assemblies

### What is an Assembly?

An assembly is a compiled unit of code — a `.dll` or `.exe` file. It contains:
- IL code (your compiled C# methods)
- Metadata (type information, method signatures)
- Manifest (assembly name, version, dependencies)

### Your Project's Assemblies

```
SolHotelBookingAppWebApi/
├── HotelBookingAppWebApi/
│   └── bin/Debug/net8.0/
│       ├── HotelBookingAppWebApi.dll      ← your main assembly
│       ├── HotelBookingAppWebApi.exe      ← entry point
│       ├── Microsoft.EntityFrameworkCore.dll  ← NuGet dependency
│       ├── Microsoft.AspNetCore.Authentication.JwtBearer.dll
│       └── ... (all referenced assemblies)
│
└── HotelBookingAppWebApi.Tests/
    └── bin/Debug/net8.0/
        └── HotelBookingAppWebApi.Tests.dll  ← test assembly
```

### How Assemblies Are Referenced

```xml
<!-- HotelBookingAppWebApi.Tests.csproj -->
<ProjectReference Include="..\HotelBookingAppWebApi\HotelBookingAppWebApi.csproj" />
```

This tells the compiler: "my test project depends on the main project's assembly." The test project can then use all public classes from the main project.

### Reflection — Reading Assembly Metadata at Runtime

ASP.NET Core uses reflection to discover your controllers:

```csharp
// Internally, AddControllers() does something like:
var assembly = Assembly.GetExecutingAssembly();
var controllerTypes = assembly.GetTypes()
    .Where(t => t.GetCustomAttribute<ApiControllerAttribute>() != null);
// Finds: GuestReservationController, AdminInventoryController, etc.
```

Your `[ApiController]`, `[Route]`, `[HttpGet]` attributes are metadata stored in the assembly. ASP.NET Core reads them at startup via reflection.

---

## O. Asynchronous Programming — async/await and Tasks

### Why Async?

Without async, a thread waits (blocks) while the database responds. With async, the thread is released to handle other requests while waiting.

```
Without async:
Thread 1: [Request A] → [waiting for DB...........] → [respond]
Thread 2: [Request B] → [waiting for DB...........] → [respond]
(2 threads blocked, can't handle more requests)

With async:
Thread 1: [Request A] → [send DB query] → [free] → [Request C] → [free] → [Request A response]
Thread 1: [Request B] → [send DB query] → [free] → [Request D] → [free] → [Request B response]
(1 thread handles 4 requests)
```

### async/await in Your Project

Every service method that touches the database is async:

```csharp
// The method signature uses async Task<T>
public async Task<ReservationResponseDto> CreateReservationAsync(Guid userId, CreateReservationDto dto)
{
    // await suspends this method and releases the thread
    // when the DB responds, the method resumes
    var hotel = await _hotelRepo.GetAsync(dto.HotelId);
    var inventories = await _inventoryRepo.GetQueryable()
        .Where(i => i.RoomTypeId == dto.RoomTypeId)
        .ToListAsync(); // async DB call

    await _reservationRepo.AddAsync(reservation);
    await _unitOfWork.CommitAsync(); // async commit

    return MapToResponseDto(reservation, assignedRooms, pricing);
}
```

### Task\<T\> — The Return Type

`Task<T>` represents an ongoing async operation that will eventually return `T`.

```csharp
Task<Hotel>          // will return a Hotel
Task<bool>           // will return true/false
Task<IActionResult>  // will return an HTTP response
Task                 // will return nothing (void equivalent)
```

### CancellationToken — Stopping Async Operations

Background services use `CancellationToken` to know when to stop:

```csharp
// NoShowAutoCancelService.cs
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested) // keep running until app stops
    {
        await RunSafeAsync(stoppingToken);
        await Task.Delay(PollingInterval, stoppingToken); // wait 5 minutes
        // If app is shutting down, stoppingToken is cancelled
        // Task.Delay throws OperationCanceledException → loop exits
    }
}
```

When you stop the API (`Ctrl+C`), ASP.NET Core cancels the token, and all background services stop gracefully.

### Task.Delay — Non-Blocking Wait

```csharp
// Background services wait 5 minutes between runs
private static readonly TimeSpan PollingInterval = TimeSpan.FromMinutes(5);

await Task.Delay(PollingInterval, stoppingToken);
// This does NOT block a thread — it schedules a callback after 5 minutes
// The thread is free to do other work during the wait
```

### Parallel Async Operations

When you need multiple independent async operations, run them in parallel:

```csharp
// DashboardService.cs — these queries are independent, run them together
var roomStats = await GetRoomStatsAsync(hotelId);
var reservationStats = await GetReservationStatsAsync(hotelId);
var totalRevenue = await GetHotelRevenueAsync(hotelId);
// Currently sequential — could be parallelized with Task.WhenAll:

var (roomStats, reservationStats, totalRevenue) = await (
    GetRoomStatsAsync(hotelId),
    GetReservationStatsAsync(hotelId),
    GetHotelRevenueAsync(hotelId)
).WhenAll(); // runs all 3 simultaneously
```

---

## P. Attributes — Cross-Cutting Concerns

### What are Attributes?

Attributes are metadata you attach to classes, methods, or properties using `[AttributeName]`. They don't change the code logic — they add information that other code (like ASP.NET Core) reads at runtime.

### Attributes in Your Project

**Routing attributes:**
```csharp
[Route("api/guest/reservations")]  // sets base URL
[HttpGet]                          // this method handles GET
[HttpPost]                         // this method handles POST
[HttpPatch("{code}/cancel")]       // PATCH with route parameter
```

**Authorization attributes:**
```csharp
[Authorize(Roles = "Guest")]       // only Guests can access
[Authorize(Roles = "Admin")]       // only Admins can access
[Authorize(Roles = "SuperAdmin")]  // only SuperAdmins can access
[AllowAnonymous]                   // no auth required (login, register)
```

**Validation attributes on DTOs:**
```csharp
public class RegisterUserDto
{
    [Required]                    // field must be present
    public string Name { get; set; }

    [Required, EmailAddress]      // must be valid email format
    public string Email { get; set; }

    [Required, MinLength(6)]      // minimum 6 characters
    public string Password { get; set; }
}

public class CreateReservationDto
{
    [Required, Range(1, int.MaxValue, ErrorMessage = "Number of rooms must be at least 1")]
    public int NumberOfRooms { get; set; }
}
```

**EF Core attributes on models:**
```csharp
public class User
{
    [Key]                         // primary key
    public Guid UserId { get; set; }

    [Required, MaxLength(150)]    // NOT NULL, VARCHAR(150)
    public string Name { get; set; }

    [Required, EmailAddress]      // NOT NULL, email format validation
    public string Email { get; set; }
}

public class RoomTypeInventory
{
    [NotMapped]                   // this property is NOT a database column
    public int AvailableInventory => TotalInventory - ReservedInventory;
}

public class ReservationRoom
{
    [NotMapped]                   // computed property, not stored in DB
    public bool IsCurrentlyOccupied => Reservation != null && ...;
}
```

**ApiController attribute:**
```csharp
[ApiController]  // enables: automatic model validation, [FromBody] inference, problem details
public class GuestReservationController : ControllerBase { }
```

When `[ApiController]` is present and model validation fails (e.g. `[Required]` field missing), ASP.NET Core automatically returns `400 Bad Request` without you writing any validation code.

---

## Q. Security — JWT, Role-Based Auth, Password Hashing

### Role-Based Security

Your project has three roles. Each role can only access its own endpoints:

```csharp
// Guest can only access guest endpoints
[Authorize(Roles = "Guest")]
public class GuestReservationController : ControllerBase { }

// Admin can only access admin endpoints
[Authorize(Roles = "Admin")]
public class AdminInventoryController : ControllerBase { }

// SuperAdmin can only access superadmin endpoints
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminHotelController : ControllerBase { }
```

If a Guest tries to call an Admin endpoint, ASP.NET Core returns `403 Forbidden` automatically.

### JWT Token Security

```csharp
// TokenService.cs — token is signed with HMAC-SHA256
var descriptor = new SecurityTokenDescriptor
{
    Subject = new ClaimsIdentity(claims),
    Expires = DateTime.UtcNow.AddDays(1),
    SigningCredentials = new SigningCredentials(
        _signingKey,
        SecurityAlgorithms.HmacSha256) // signing algorithm
};
```

The token has 3 parts separated by dots:
```
eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJ1c2VyLWd1aWQiLCJyb2xlIjoiR3Vlc3QifQ.SIGNATURE
     HEADER                          PAYLOAD                              SIGNATURE
```

- Header: algorithm used (HS256)
- Payload: claims (UserId, Role, UserName, HotelId)
- Signature: HMAC-SHA256(header + payload + secret key)

If anyone tampers with the payload, the signature won't match → token rejected.

### Password Security

```csharp
// Passwords are NEVER stored as plain text
// HMACSHA512 with a random salt per user

// Registration:
var salt = new byte[64];
RandomNumberGenerator.Fill(salt); // cryptographically random salt
var hmac = new HMACSHA512(salt);
var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
// Store hash + salt in DB

// Login verification:
var hmac = new HMACSHA512(user.PasswordSaltValue); // use stored salt
var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(inputPassword));
if (!hash.SequenceEqual(user.Password)) // compare hashes
    throw new UnAuthorizedException("Invalid credentials.");
```

Why salt? Without salt, two users with the same password would have the same hash. An attacker with a rainbow table could crack it. Salt makes every hash unique.

### Secure Coding Practices in Your Project

```csharp
// 1. Never expose raw entities — always use DTOs
return MapToResponseDto(reservation, ...); // not return reservation;

// 2. Always validate ownership before modifying
var room = await _roomRepo.GetQueryable()
    .FirstOrDefaultAsync(r => r.RoomId == roomId && r.HotelId == admin.HotelId)
    ?? throw new NotFoundException("Room not found.");
// Can't modify rooms from other hotels

// 3. Null-coalescing to prevent null reference exceptions
var hotelName = reservation.Hotel?.Name ?? "Hotel";

// 4. Input validation via attributes
[Required, EmailAddress] public string Email { get; set; }
[Range(1, int.MaxValue)] public int NumberOfRooms { get; set; }

// 5. Rate limiting prevents brute force attacks
services.AddInMemoryRateLimiting(); // max 100 requests/minute per IP
```

---

## R. Memory Management — Garbage Collection and IDisposable

### Garbage Collection (GC)

.NET manages memory automatically. You don't call `free()` like in C. The GC runs periodically and frees objects that are no longer referenced.

```csharp
// When this method returns, 'hotel' goes out of scope
// GC will eventually free the memory
public async Task<Hotel> GetHotelAsync(Guid id)
{
    var hotel = await _hotelRepo.GetAsync(id); // allocated on heap
    return hotel; // caller now holds the reference
} // hotel local variable goes out of scope here
```

### IDisposable and `using` — Deterministic Cleanup

Some objects hold unmanaged resources (database connections, file handles, network sockets). These must be released immediately, not waiting for GC.

`IDisposable` provides a `Dispose()` method. The `using` statement calls it automatically:

```csharp
// QrCodeHelper.cs — using ensures Dispose() is called even if exception occurs
private static byte[] RenderQrCodePng(string content)
{
    using var generator = new QRCodeGenerator();  // Dispose() called at end of scope
    using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
    using var code = new PngByteQRCode(data);
    return code.GetGraphic(10);
} // generator.Dispose(), data.Dispose(), code.Dispose() all called here automatically
```

```csharp
// UnitOfWork.cs — implements IDisposable to clean up the transaction
public class UnitOfWork : IUnitOfWork, IDisposable
{
    private IDbContextTransaction? _transaction;

    public void Dispose()
    {
        _transaction?.Dispose(); // release the DB transaction object
        _transaction = null;
    }
}
```

```csharp
// Background services create a scope per run and dispose it
private async Task ProcessNoShowsAsync(CancellationToken ct)
{
    using var scope = _scopeFactory.CreateScope(); // creates DI scope
    var reservationRepo = scope.ServiceProvider.GetRequiredService<IRepository<Guid, Reservation>>();
    // ... do work
} // scope.Dispose() called here — releases DbContext and all scoped services
```

### Why Background Services Use IServiceScopeFactory

Background services are **Singleton** (one instance for the app lifetime). But `DbContext` is **Scoped** (one per request). You can't inject a Scoped service into a Singleton directly — it would be shared across all runs.

Solution: create a new scope per run:

```csharp
// NoShowAutoCancelService.cs
private readonly IServiceScopeFactory _scopeFactory; // Singleton — safe to inject

private async Task ProcessNoShowsAsync(CancellationToken ct)
{
    using var scope = _scopeFactory.CreateScope(); // new scope = new DbContext
    var reservationRepo = scope.ServiceProvider
        .GetRequiredService<IRepository<Guid, Reservation>>(); // fresh instance
    var unitOfWork = scope.ServiceProvider
        .GetRequiredService<IUnitOfWork>(); // fresh instance
    // ... use them
} // scope disposed — DbContext disposed — connection returned to pool
```

---

## S. Parallel Programming

### Task Parallel Library (TPL)

Your background services run in parallel with the main API — they are separate tasks running concurrently.

```csharp
// Three background services run simultaneously
services.AddHostedService<ReservationCleanupService>();      // Task 1
services.AddHostedService<HotelDeactivationRefundService>(); // Task 2
services.AddHostedService<NoShowAutoCancelService>();         // Task 3
// All three run their loops at the same time, independently
```

### Task.WhenAll — Running Multiple Tasks in Parallel

```csharp
// Run multiple independent async operations simultaneously
var task1 = GetRoomStatsAsync(hotelId);
var task2 = GetReservationStatsAsync(hotelId);
var task3 = GetHotelRevenueAsync(hotelId);

await Task.WhenAll(task1, task2, task3); // all 3 run at the same time

var roomStats = task1.Result;
var reservationStats = task2.Result;
var totalRevenue = task3.Result;
```

### Thread Safety in Background Services

Each background service run creates its own scope and its own `DbContext`. They don't share state, so there are no race conditions between services.

---

## T. Unsafe Code and Native Interop

Your project does not use unsafe code directly. However, the `QRCoder` library internally uses unsafe code for performance when generating PNG bytes. You call it safely through its public API:

```csharp
// Safe managed code calling a library that may use unsafe internally
using var code = new PngByteQRCode(data);
return code.GetGraphic(10); // returns byte[] — safe managed array
```

The `byte[]` password hashing also works with raw bytes but through safe managed APIs:

```csharp
// HMACSHA512 works with byte arrays — managed, not unsafe
var hmac = new HMACSHA512(salt);
var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
// byte[] is a managed type — GC handles its memory
```

---

## U. Globalization and Localization

### Timezone Handling in Your Project

Your project explicitly handles timezone differences (India uses IST = UTC+5:30):

```csharp
// ReservationService.cs — uses local time for date validation
// because guests are in India (IST), not UTC
var today = DateOnly.FromDateTime(DateTime.Now); // local time (IST)

// Background services use UTC for consistency
var today = DateOnly.FromDateTime(DateTime.UtcNow); // UTC

// Timestamps stored in UTC
CreatedAt = DateTime.UtcNow
ExpiryTime = DateTime.UtcNow.AddMinutes(10)
```

The comment in `ReservationService` explains why:
```csharp
// Use local date (not UTC) to avoid timezone issues with IST clients
// Block today and past dates — only allow from tomorrow onwards
var today = DateOnly.FromDateTime(DateTime.Now);
```

If you used UTC, a guest in India at 11 PM IST would be at 5:30 PM UTC — the "today" check would be wrong.

---

## V. Complete Program.cs Walkthrough (Final)

Here is the complete `Program.cs` explained line by line:

```csharp
// Creates the WebApplication builder — reads appsettings.json, environment variables
var builder = WebApplication.CreateBuilder(args);

// ── STEP 1: Register all services into the DI container ──────────────────────

// Adds MVC controllers + Swagger endpoint discovery
RegisterControllers(builder.Services);

// Adds IP rate limiting using in-memory cache
// Reads rules from appsettings.json "IpRateLimiting" section
RegisterRateLimiting(builder.Services, builder.Configuration);

// Adds Swagger UI with JWT Bearer security definition
// Accessible at /swagger in development
RegisterSwagger(builder.Services);

// Adds EF Core DbContext with SQL Server connection
// Uses SplitQuery to avoid cartesian explosion on multi-Include queries
RegisterDatabase(builder.Services, builder.Configuration);

// Adds CORS policy "AngularClient" — allows http://localhost:4200
RegisterCors(builder.Services);

// Registers generic IRepository<,> → Repository<,> (covers ALL entity types)
// Registers IUnitOfWork → UnitOfWork
RegisterRepositories(builder.Services);

// Registers all 22 business services as Scoped
// IAuthService → AuthService, IHotelService → HotelService, etc.
RegisterApplicationServices(builder.Services);

// Registers 3 background services as Singleton hosted services
// They start when the app starts and run forever
RegisterBackgroundServices(builder.Services);

// Configures JWT Bearer authentication
// Validates: lifetime + signature (not issuer/audience)
RegisterAuthentication(builder.Services, builder.Configuration);

// ── STEP 2: Build the app from all registrations ──────────────────────────────
var app = builder.Build();

// ── STEP 3: Configure the middleware pipeline ─────────────────────────────────
ConfigurePipeline(app);

// ── STEP 4: Start listening for HTTP requests ─────────────────────────────────
app.Run(); // blocks here — app runs until Ctrl+C or process kill
```

```csharp
static void ConfigurePipeline(WebApplication app)
{
    // Only show Swagger in development — not in production
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();    // serves /swagger/v1/swagger.json
        app.UseSwaggerUI();  // serves /swagger HTML page
    }

    // MUST be first — wraps everything in try/catch
    // Catches auth errors, validation errors, DB errors, everything
    app.UseMiddleware<GlobalExceptionMiddleware>();

    // Adds CORS headers to responses
    // Without this, browser blocks Angular requests
    app.UseCors("AngularClient");

    // Checks request count per IP — returns 429 if exceeded
    app.UseIpRateLimiting();

    // Matches the URL to a controller route
    // Must come before UseAuthentication
    app.UseRouting();

    // Reads "Authorization: Bearer ..." header
    // Validates JWT and populates HttpContext.User with claims
    app.UseAuthentication();

    // Checks [Authorize] attributes on controllers/actions
    // Returns 403 if role doesn't match
    app.UseAuthorization();

    // Executes the matched controller action method
    app.MapControllers();
}
```

### What Happens on Every Request

```
1. Request arrives at the server
2. GlobalExceptionMiddleware.InvokeAsync() — wraps in try/catch
3. CorsMiddleware — adds CORS headers
4. IpRateLimitMiddleware — checks rate limit
5. RoutingMiddleware — matches URL to GuestReservationController.Create
6. AuthenticationMiddleware — validates JWT, sets User.Identity
7. AuthorizationMiddleware — checks [Authorize(Roles = "Guest")]
8. GuestReservationController.Create() executes
9. Returns Ok(new { success = true, data = result })
10. Response flows back through middleware (in reverse)
11. GlobalExceptionMiddleware — no exception, passes through
12. Response sent to Angular
```

If an exception occurs at step 8:
```
8. GuestReservationController.Create() throws NotFoundException
9. Exception bubbles up through middleware
2. GlobalExceptionMiddleware catches it
   → Logs to ILogger
   → Saves to Logs table in DB
   → Returns { success: false, statusCode: 404, message: "Hotel not found." }
```

---

*End of Backend-Documentation.md*
*This document covers: all 22 services with every function, Program.cs line by line,*
*and all .NET Core competencies with examples from your actual project code.*
