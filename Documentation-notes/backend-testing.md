# .NET 10 Backend Testing — Complete Guide
### HotelBookingAppWebApi.Tests

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [NuGet Packages & Why Each Exists](#2-nuget-packages--why-each-exists)
3. [Project Structure](#3-project-structure)
4. [xUnit — The Test Framework](#4-xunit--the-test-framework)
5. [FluentAssertions — Better Assertions](#5-fluentassertions--better-assertions)
6. [Moq — Mocking Dependencies](#6-moq--mocking-dependencies)
7. [MockQueryable.Moq — Mocking IQueryable / EF Core Queries](#7-mockqueryablemoq--mocking-iqueryable--ef-core-queries)
8. [EF Core InMemory — Database Without SQL Server](#8-ef-core-inmemory--database-without-sql-server)
9. [Microsoft.AspNetCore.Mvc.Testing — Integration / DI Tests](#9-microsoftaspnetcoremvctesting--integration--di-tests)
10. [AAA Pattern — Arrange / Act / Assert](#10-aaa-pattern--arrange--act--assert)
11. [Test Layers — What Each Folder Tests](#11-test-layers--what-each-folder-tests)
    - 11.1 Context Tests
    - 11.2 Repository Tests
    - 11.3 Service Tests
    - 11.4 Controller Tests
    - 11.5 Exception Tests
    - 11.6 Middleware Tests
    - 11.7 Background Service Tests
    - 11.8 Program / DI Tests
    - 11.9 DTO Model Tests
12. [GlobalUsings.cs — Shared Imports](#12-globalusingscs--shared-imports)
13. [Code Coverage — coverlet + Fine Code Coverage](#13-code-coverage--coverlet--fine-code-coverage)
14. [Common Patterns Cheat Sheet](#14-common-patterns-cheat-sheet)
15. [Running Tests](#15-running-tests)

---

## 1. Project Overview

This is the **test project** for a Hotel Booking Web API built on **.NET 10**. It tests everything from raw database operations to HTTP controller responses. The project uses a multi-layer approach:

| Layer | What's Tested |
|---|---|
| `Contexts/` | EF Core `DbContext` schema and DbSet validity |
| `Repository/` | Generic CRUD repository operations against an InMemory DB |
| `Services/` | Business logic services with all dependencies mocked |
| `Controllers/` | Controller actions with mocked services |
| `Exceptions/` | Custom exception classes and HTTP status codes |
| `Exceptions/Middleware/` | Global exception-handling middleware |
| `Models/` | DTO (Data Transfer Object) property defaults |
| `ProgramTests.cs` | Full DI container boot via `WebApplicationFactory` |

The test project references the main API project directly (`HotelBookingAppWebApi.csproj`) so it can instantiate real classes and only mock what's needed.

---

## 2. NuGet Packages & Why Each Exists

These are declared in `HotelBookingAppWebApi.Tests.csproj`:

```xml
<PackageReference Include="xunit"                              Version="2.9.3" />
<PackageReference Include="xunit.runner.visualstudio"          Version="3.1.4" />
<PackageReference Include="Microsoft.NET.Test.Sdk"             Version="17.14.1" />
<PackageReference Include="Moq"                                Version="4.20.72" />
<PackageReference Include="FluentAssertions"                   Version="6.12.1" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.0" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt"    Version="8.1.2" />
<PackageReference Include="MockQueryable.Moq"                  Version="7.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing"   Version="10.0.0" />
<PackageReference Include="coverlet.collector"                 Version="6.0.4" />
```

### Package Explanations

**`xunit`**  
The core testing framework. Provides `[Fact]` (single test), `[Theory]` (data-driven test), and the test runner infrastructure. This is the most popular test framework for .NET.

**`xunit.runner.visualstudio`**  
Lets Visual Studio's Test Explorer discover and run xUnit tests. Without this, VS wouldn't see your tests.

**`Microsoft.NET.Test.Sdk`**  
The test host — required by every .NET test project regardless of which framework you use. It sets up `dotnet test` support.

**`Moq`**  
A mocking library. When you write a service test, you don't want a real database or real email sender — you create fake (`Mock<T>`) versions that you control. Moq is the most widely used mocking library for .NET.

**`FluentAssertions`**  
Replaces clunky `Assert.Equal(expected, actual)` calls with readable English-like syntax: `result.Should().Be(42)`. Makes test failures much easier to understand.

**`Microsoft.EntityFrameworkCore.InMemory`**  
An EF Core provider that stores data in memory (no SQL Server needed). Used in context and repository tests to run real database operations without an actual database server.

**`System.IdentityModel.Tokens.Jwt`**  
Allows reading/parsing JWT tokens in tests. Used in `TokenServiceTests.cs` to verify that created tokens actually contain the right claims.

**`MockQueryable.Moq`**  
A bridge package that makes `IQueryable<T>` mockable with Moq. EF Core's async LINQ methods (`ToListAsync`, `FirstOrDefaultAsync`, etc.) require special async providers — MockQueryable provides exactly that so you can mock `GetQueryable()` returns.

**`Microsoft.AspNetCore.Mvc.Testing`**  
Provides `WebApplicationFactory<TProgram>` which boots your real ASP.NET Core application in-process. Used in `ProgramTests.cs` to verify DI registrations.

**`coverlet.collector`**  
Collects code coverage data when tests run. Outputs Cobertura XML reports that tools like Fine Code Coverage can display inside Visual Studio.

---

## 3. Project Structure

```
HotelBookingAppWebApi.Tests/
│
├── GlobalUsings.cs                     ← shared `global using` statements
├── HotelBookingAppWebApi.Tests.csproj  ← package refs + project ref
├── coverage.runsettings                ← coverage configuration
├── ProgramTests.cs                     ← DI container / boot tests
│
├── Contexts/
│   └── HotelBookingContextTests.cs     ← DbContext schema + CRUD
│
├── Repository/
│   └── RepositoryTests.cs              ← Generic repo CRUD + pagination
│
├── Services/
│   ├── AuthServiceTests.cs
│   ├── HotelServiceTests.cs
│   ├── ReservationServiceTests.cs
│   ├── ReviewServiceTests.cs
│   ├── WalletServiceTests.cs
│   ├── TransactionServiceTests.cs
│   ├── TokenServiceTests.cs
│   ├── UnitOfWorkTests.cs
│   ├── DashboardServiceTests.cs
│   ├── CoverageGapTests.cs             ← extra tests to hit uncovered branches
│   ├── CoverageGapTests2.cs            ← (same purpose, split for readability)
│   ├── ... (CoverageGapTests3-6)
│   └── BackgroundServices/
│       ├── ReservationCleanupServiceTests.cs
│       ├── HotelDeactivationRefundServiceTests.cs
│       ├── NoShowAutoCancelServiceTests.cs
│       └── InventoryRestoreHelperTests.cs
│
├── Controllers/
│   ├── ControllerTestHelper.cs         ← shared fake JWT user builder
│   ├── AuthenticationControllerTests.cs
│   ├── DashboardControllerTests.cs
│   ├── ReviewControllerTests.cs
│   ├── TransactionControllerTests.cs
│   ├── UserProfileControllerTests.cs
│   ├── LogControllerTests.cs
│   ├── Admin/
│   │   ├── AdminHotelControllerTests.cs
│   │   ├── AdminRoomControllerTests.cs
│   │   ├── AdminRoomTypeControllerTests.cs
│   │   └── ... (8 files)
│   ├── Guest/
│   │   ├── GuestReservationControllerTests.cs
│   │   └── ... (4 files)
│   ├── Public/
│   │   ├── PublicHotelControllerTests.cs
│   │   └── ... (2 files)
│   └── SuperAdmin/
│       └── ... (6 files)
│
├── Exceptions/
│   ├── AppExceptionsTests.cs
│   └── Middleware/
│       └── GlobalExceptionMiddlewareTests.cs
│
└── Models/
    └── DtoModelTests.cs                ← DTO property default checks
```

---

## 4. xUnit — The Test Framework

xUnit is the backbone. Every test method needs `[Fact]` to be discovered.

### `[Fact]` — A single test

```csharp
[Fact]
public async Task GetAsync_ExistingKey_ReturnsEntity()
{
    // one specific scenario, no parameters
}
```

### `[Theory]` + `[InlineData]` — Data-driven tests

Not used extensively in this project, but the pattern is:

```csharp
[Theory]
[InlineData(404, "not found")]
[InlineData(409, "conflict")]
public void Exception_HasCorrectStatus(int code, string msg) { ... }
```

### Test naming convention used in this project

Every test name follows: `MethodName_Scenario_ExpectedResult`

Examples from the codebase:
- `AddAsync_ValidEntity_ReturnsAddedEntity`
- `GetAsync_MissingKey_ReturnsNull`
- `RegisterGuestAsync_EmailAlreadyExists_ThrowsConflictException`

This makes it immediately obvious what broke when a test fails.

### `IClassFixture<T>` — Shared expensive setup

Used in `ProgramTests.cs` to boot the entire app **once** and share it across all tests in the class:

```csharp
public class ProgramTests : IClassFixture<ProgramTests.AppFactory>
{
    // AppFactory is booted once and injected here
    public ProgramTests(AppFactory factory) => _factory = factory;
}
```

Without `IClassFixture`, the app would restart for every single test, making it very slow.

---

## 5. FluentAssertions — Better Assertions

FluentAssertions replaces `Assert.*` with readable chains. Here's how it's used throughout your project:

### Basic value checks

```csharp
// Instead of: Assert.NotNull(result)
result.Should().NotBeNull();

// Instead of: Assert.Equal("jwt-token", result.Token)
result.Token.Should().Be("jwt-token");

// Instead of: Assert.True(nextCalled)
nextCalled.Should().BeTrue();
```

### Collection checks

```csharp
// Check count
result.Should().HaveCount(2);

// Check empty
result.Should().BeEmpty();

// Check contains
result.Should().Contain("Mumbai");
```

### Type checks

```csharp
// Verify controller returned OkObjectResult
result.Should().BeOfType<OkObjectResult>();

// Get the typed subject to check its value
var ok = result.Should().BeOfType<OkObjectResult>().Subject;
ok.Value.Should().NotBeNull();
```

### Exception checks — async

```csharp
// Verify an async method throws a specific exception
var act = async () => await _sut.RegisterGuest(new RegisterUserDto());
await act.Should().ThrowAsync<ConflictException>()
         .WithMessage("Email already registered.");

// Verify no exception
await act.Should().NotThrowAsync();
```

### HTTP status code check

```csharp
ctx.Response.StatusCode.Should().Be(409);
```

### EF Core model checks

```csharp
indexes.Should().Contain(i =>
    i.IsUnique &&
    i.Properties.Any(p => p.Name == nameof(User.Email)));
```

FluentAssertions failure messages are much more descriptive than plain xUnit — they show both actual and expected values with context.

---

## 6. Moq — Mocking Dependencies

Moq creates fake implementations of interfaces so you can test one class in isolation without spinning up its real dependencies.

### Creating a mock

```csharp
// From AuthServiceTests.cs
private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
private readonly Mock<IPasswordService> _passwordServiceMock = new();
private readonly Mock<ITokenService> _tokenServiceMock = new();
```

Each `Mock<T>` creates a fake version of that interface that records all calls and lets you configure return values.

### Injecting mocks — passing `.Object`

```csharp
private AuthService CreateSut() => new(
    _userRepoMock.Object,      // ← the actual fake object (implements IRepository)
    _hotelRepoMock.Object,
    _profileRepoMock.Object,
    _passwordServiceMock.Object,
    _tokenServiceMock.Object,
    _walletServiceMock.Object,
    _unitOfWorkMock.Object);
```

`.Object` gets the underlying object that implements the interface.

### `Setup` — configure what the mock returns

```csharp
// Return a fixed value
_tokenServiceMock
    .Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>()))
    .Returns("jwt-token");

// Return a value asynchronously
_userRepoMock
    .Setup(r => r.AddAsync(It.IsAny<User>()))
    .ReturnsAsync((User u) => u);   // ← returns whatever was passed in

// Return Task.CompletedTask for void-like tasks
_hotelServiceMock
    .Setup(s => s.UpdateHotelAsync(_userId, dto))
    .Returns(Task.CompletedTask);

// Throw an exception
_authServiceMock
    .Setup(s => s.LoginAsync(It.IsAny<LoginDto>()))
    .ThrowsAsync(new UnAuthorizedException("Invalid credentials."));
```

### `It.IsAny<T>()` vs exact values

```csharp
// Match any value of type T
.Setup(s => s.UpdateHotelAsync(It.IsAny<Guid>(), It.IsAny<UpdateHotelDto>()))

// Match exact value (better for specific scenarios)
.Setup(s => s.UpdateHotelAsync(_userId, dto))
```

Use `It.IsAny` when you don't care what argument is passed. Use exact values when the test is specifically about that input.

### `Verify` — confirm a method was called

Used heavily in Background Service tests:

```csharp
// Verify CommitAsync was never called
unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);

// Verify CreateScope was called once
_scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once);
```

`Times.Never`, `Times.Once`, `Times.Exactly(n)` are the options.

### `It.Ref<T>.IsAny` — out parameters

Used in `AuthServiceTests.cs` for the password hashing method that has an `out` parameter:

```csharp
_passwordServiceMock
    .Setup(p => p.HashPassword(It.IsAny<string>(), It.IsAny<byte[]>(), out It.Ref<byte[]?>.IsAny))
    .Returns(hash);
```

---

## 7. MockQueryable.Moq — Mocking IQueryable / EF Core Queries

This is the most important "bridge" package. The problem: EF Core's async LINQ methods (`ToListAsync`, `FirstOrDefaultAsync`, `AnyAsync`, etc.) require the `IQueryable` to have a special async provider. A plain `List<T>.AsQueryable()` does NOT have this provider and will throw `InvalidOperationException` at runtime.

**MockQueryable.Moq** adds `.BuildMock()` which wraps your list in an async-capable queryable.

### How it's used

```csharp
using MockQueryable.Moq;

// Create test data
var hotels = new List<Hotel>
{
    MakeHotel(isActive: true),
    MakeHotel(isActive: false)
}.AsQueryable().BuildMock();  // ← this is the key line

// Configure the mock repository to return it
_hotelRepoMock
    .Setup(r => r.GetQueryable())
    .Returns(hotels);
```

Now when `HotelService` calls `_hotelRepo.GetQueryable().Where(h => h.IsActive).ToListAsync()`, it works perfectly.

### Empty queryable pattern

```csharp
// Used frequently when testing "not found" scenarios
private void SetupEmptyUserQueryable()
{
    var empty = new List<User>().AsQueryable().BuildMock();
    _userRepoMock.Setup(r => r.GetQueryable()).Returns(empty);
}
```

### Pattern with existing data

```csharp
private void SetupExistingUserQueryable(User user)
{
    var users = new List<User> { user }.AsQueryable().BuildMock();
    _userRepoMock.Setup(r => r.GetQueryable()).Returns(users);
}
```

---

## 8. EF Core InMemory — Database Without SQL Server

The InMemory provider is used wherever the test needs **real** database behavior — actual saves, queries, deletes — without requiring SQL Server to be running.

### Setup pattern — used in Context and Repository tests

```csharp
private static HotelBookingContext CreateContext(string dbName)
{
    var options = new DbContextOptionsBuilder<HotelBookingContext>()
        .UseInMemoryDatabase(dbName)            // ← use memory instead of SQL
        .ConfigureWarnings(w =>
            w.Ignore(InMemoryEventId.TransactionIgnoredWarning))  // ← suppress warning
        .Options;
    return new HotelBookingContext(options);
}
```

**Why unique `dbName` per test?**  
Each test passes its own `nameof(TestMethodName)` as the database name. This means each test gets a **completely isolated** in-memory database. If tests shared a database name, data from one test could corrupt another.

```csharp
[Fact]
public async Task AddAsync_ValidEntity_ReturnsAddedEntity()
{
    using var ctx = CreateContext(nameof(AddAsync_ValidEntity_ReturnsAddedEntity));
    // This DB is brand new and private to this test
}
```

### `ConfigureWarnings` — suppressing the transaction warning

EF Core's InMemory provider doesn't support real transactions. When your code calls `BeginTransactionAsync()`, it works but emits a warning. The `ConfigureWarnings` call silences that warning so test output stays clean.

### What InMemory tests verify

From `RepositoryTests.cs`, the InMemory database tests verify:

- **`AddAsync`** — entity is saved, returned with same ID
- **`GetAsync`** — finds by primary key, returns null for missing key
- **`GetAllAsync`** — returns all, returns empty list when empty
- **`DeleteAsync`** — removes entity, returns null for missing key
- **`UpdateAsync`** — updates fields, returns null for missing key or null entity
- **`FirstOrDefaultAsync`** — predicate match works, null for no match
- **`GetQueryable`** — returns a non-null `IQueryable`
- **`GetAllByForeignKeyAsync`** — pagination + predicate filtering

---

## 9. Microsoft.AspNetCore.Mvc.Testing — Integration / DI Tests

`WebApplicationFactory<TProgram>` boots your **real** ASP.NET Core application in-process. It's used in `ProgramTests.cs` to verify the DI container is correctly wired.

### Custom `AppFactory` in your project

```csharp
public class AppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            // Provide all required config so the app starts
            // without a real DB or secrets
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keys:Jwt"] = "test-secret-key-that-is-long-enough-32chars",
                ["ConnectionStrings:Developer"] = "...",
                ["IpRateLimiting:EnableEndpointRateLimiting"] = "false",
                // ... other required settings
            });
        });
    }
}
```

This prevents the app from failing on missing `appsettings.json` secrets or database connections during tests.

### What ProgramTests verifies

```csharp
[Fact]
public void DI_IUnitOfWork_IsRegistered()
{
    using var scope = _factory.Services.CreateScope();
    var svc = scope.ServiceProvider.GetService<IUnitOfWork>();
    svc.Should().NotBeNull();  // ensures it's registered
}
```

These tests catch the most insidious bugs — your app compiles fine but crashes at startup because you forgot to register something in `Program.cs`.

---

## 10. AAA Pattern — Arrange / Act / Assert

Every single test in this project follows the **AAA pattern** explicitly marked with comments:

```csharp
[Fact]
public async Task RegisterGuestAsync_ValidDto_ReturnsAuthResponseDto()
{
    // Arrange  ← set up mocks, create the object, build input data
    SetupEmptyUserQueryable();
    SetupPasswordService();
    _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
    _tokenServiceMock.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>())).Returns("jwt-token");
    var sut = CreateSut();
    var dto = new RegisterUserDto { Name = "Alice", Email = "alice@test.com", Password = "pass123" };

    // Act  ← call the method being tested (exactly ONE call)
    var result = await sut.RegisterGuestAsync(dto);

    // Assert  ← verify the outcome
    result.Should().NotBeNull();
    result.Token.Should().Be("jwt-token");
}
```

**Why strictly one call in Act?** If you have two method calls in Act, a failure won't tell you which one caused the problem.

**`sut`** stands for **System Under Test** — a consistent name for the class being tested.

---

## 11. Test Layers — What Each Folder Tests

### 11.1 Context Tests (`Contexts/HotelBookingContextTests.cs`)

Tests the `HotelBookingContext` itself — EF Core's `DbContext`.

**What's tested:**
- Constructor creates a valid context
- All `DbSet<T>` properties (Users, Hotels, Rooms, etc.) are not null
- `OnModelCreating` applied unique index on `User.Email`
- Can actually add and retrieve each entity type (User, Hotel, Reservation, Log)

**Key technique:** `InMemoryDatabase` — creates a fresh DB per test.

```csharp
[Fact]
public void OnModelCreating_UserEmail_HasUniqueIndex()
{
    using var ctx = CreateContext(nameof(OnModelCreating_UserEmail_HasUniqueIndex));
    var entityType = ctx.Model.FindEntityType(typeof(User));
    var indexes = entityType!.GetIndexes();
    indexes.Should().Contain(i =>
        i.IsUnique && i.Properties.Any(p => p.Name == nameof(User.Email)));
}
```

---

### 11.2 Repository Tests (`Repository/RepositoryTests.cs`)

Tests the generic `Repository<TKey, TEntity>` class that wraps EF Core.

**What's tested:**
- All CRUD operations (Add, Get, GetAll, Delete, Update)
- Edge cases: null input, missing keys, empty tables
- Pagination via `GetAllByForeignKeyAsync`
- `FirstOrDefaultAsync` with predicate
- `GetQueryable` returns usable IQueryable

**Key technique:** InMemoryDatabase + real EF Core save/query cycle.

```csharp
[Fact]
public async Task DeleteAsync_ExistingKey_RemovesAndReturnsEntity()
{
    using var ctx = CreateContext(nameof(DeleteAsync_ExistingKey_RemovesAndReturnsEntity));
    var repo = new Repository<Guid, User>(ctx);
    var user = MakeUser();
    ctx.Users.Add(user);
    await ctx.SaveChangesAsync();

    var result = await repo.DeleteAsync(user.UserId);
    await ctx.SaveChangesAsync();

    result.Should().NotBeNull();
    ctx.Users.Find(user.UserId).Should().BeNull();  // gone from DB
}
```

---

### 11.3 Service Tests (`Services/`)

The largest section. Each service test class:
- Creates `Mock<IRepository<...>>` for every repo the service needs
- Creates `Mock<IUnitOfWork>`, `Mock<IAuditLogService>`, etc.
- Uses `MockQueryable.Moq` to simulate LINQ queries
- Tests happy paths AND exception paths

**Services tested:**
`AuthService`, `HotelService`, `RoomService`, `RoomTypeService`, `ReservationService`, `ReviewService`, `WalletService`, `TransactionService`, `PromoCodeService`, `AmenityService`, `AmenityRequestService`, `SupportRequestService`, `InventoryService`, `DashboardService`, `AuditLogService`, `LogService`, `UserService`, `TokenService`, `PasswordService`, `QrCodeHelper`, `SuperAdminRevenueService`

**Pattern:**

```csharp
public class HotelServiceTests
{
    private readonly Mock<IRepository<Guid, Hotel>> _hotelRepoMock = new();
    // ... other mocks

    private HotelService CreateSut() => new(
        _hotelRepoMock.Object, ...);

    [Fact]
    public async Task GetTopHotelsAsync_ReturnsActiveHotels()
    {
        // Arrange
        var hotels = new List<Hotel> { MakeHotel(true), MakeHotel(false) }
                        .AsQueryable().BuildMock();
        _hotelRepoMock.Setup(r => r.GetQueryable()).Returns(hotels);
        var sut = CreateSut();

        // Act
        var result = await sut.GetTopHotelsAsync();

        // Assert
        result.Should().HaveCount(1); // only active ones
    }
}
```

**CoverageGapTests1-6** are extra test files created to increase code coverage for edge cases and branches that the primary tests missed. They follow the same structure.

---

### 11.4 Controller Tests (`Controllers/`)

Tests the controller action methods in isolation — no HTTP stack, no routing, just calling the action method directly.

**`ControllerTestHelper.cs` — shared fake user**

```csharp
internal static ControllerContext BuildControllerContext(Guid userId, string role = "Admin")
{
    var claims = new[]
    {
        new Claim("nameid",      userId.ToString()),
        new Claim("role",        role),
        new Claim("unique_name", "TestUser")
    };
    var identity  = new ClaimsIdentity(claims, "Test");
    var principal = new ClaimsPrincipal(identity);
    return new ControllerContext
    {
        HttpContext = new DefaultHttpContext { User = principal }
    };
}
```

Controllers often read `User.FindFirst("nameid")` to get the current user's ID. This helper simulates that without an actual JWT or HTTP request.

**Controller test pattern:**

```csharp
public class AdminHotelControllerTests
{
    private readonly Mock<IHotelService> _hotelServiceMock = new();
    private readonly AdminHotelController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public AdminHotelControllerTests()
    {
        _sut = new AdminHotelController(_hotelServiceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Admin");
    }

    [Fact]
    public async Task Update_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new UpdateHotelDto { Name = "Updated Hotel" };
        _hotelServiceMock.Setup(s => s.UpdateHotelAsync(_userId, dto))
                         .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Update(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_ServiceThrows_PropagatesException()
    {
        // Arrange
        _hotelServiceMock.Setup(s => s.UpdateHotelAsync(It.IsAny<Guid>(), It.IsAny<UpdateHotelDto>()))
                         .ThrowsAsync(new NotFoundException("Hotel not found."));

        // Act
        var act = async () => await _sut.Update(new UpdateHotelDto());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
```

Every controller test has **two tests per action**:
1. **Happy path** — service returns successfully, controller returns `OkObjectResult`
2. **Exception path** — service throws, exception propagates (gets caught by middleware)

**Controllers tested (organized by role):**
- `AuthenticationController` — Register guest, register admin, login
- `Admin/` — Hotel, Room, RoomType, Inventory, Reservation, Review, AmenityRequest, AuditLog, Support, Transaction, Wallet
- `Guest/` — Reservation, Wallet, Payment, PromoCode, Support
- `Public/` — Hotel search, Amenity, Support
- `SuperAdmin/` — Hotel approval, Amenity, AmenityRequest, AuditLog, Revenue, Support

---

### 11.5 Exception Tests (`Exceptions/AppExceptionsTests.cs`)

Tests your custom exception hierarchy. Each exception maps to an HTTP status code.

```csharp
[Fact]
public void NotFoundException_WithMessage_Returns404()
{
    var ex = new NotFoundException("Hotel not found.");
    ex.StatusCode.Should().Be(404);
    ex.Message.Should().Be("Hotel not found.");
}
```

**Exception classes tested:**

| Exception | Status Code |
|---|---|
| `AppException` (base) | configurable |
| `NotFoundException` | 404 |
| `ConflictException` | 409 |
| `ValidationException` | 400 |
| `UnAuthorizedException` | 401 |
| `PaymentException` | 400 |
| `ReservationFailedException` | 400 |
| `InsufficientInventoryException` | 409 |
| `RateNotFoundException` | 404 |
| `ReviewException` | 400 |
| `UserProfileException` | 404 |
| `UnableToCreateEntityException` | 400 |

---

### 11.6 Middleware Tests (`Exceptions/Middleware/GlobalExceptionMiddlewareTests.cs`)

Tests the global exception handling middleware — the code that converts exceptions to JSON HTTP responses.

**Setup pattern — manual DI container:**

```csharp
private static IServiceProvider BuildServiceProvider(string dbName)
{
    var services = new ServiceCollection();
    services.AddDbContext<HotelBookingContext>(o =>
        o.UseInMemoryDatabase(dbName)
         .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
    services.AddLogging();
    return services.BuildServiceProvider();
}
```

**Middleware testing technique:**

```csharp
[Fact]
public async Task InvokeAsync_AppException_ReturnsCorrectStatusAndJson()
{
    var sp = BuildServiceProvider(nameof(...));
    var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();

    // Simulate the next middleware throwing an exception
    RequestDelegate next = _ => throw new ConflictException("Duplicate email.");

    var middleware = new GlobalExceptionMiddleware(next, loggerMock.Object);

    // Build a fake HTTP context with a writable response body
    var ctx = new DefaultHttpContext { RequestServices = sp };
    ctx.Response.Body = new MemoryStream();

    await middleware.InvokeAsync(ctx);

    ctx.Response.StatusCode.Should().Be(409);
}
```

Key tests:
- No exception → next middleware is called
- `AppException` (and subtypes) → correct HTTP status code + JSON body
- Unexpected `Exception` → 500 Internal Server Error

---

### 11.7 Background Service Tests (`Services/BackgroundServices/`)

Tests `IHostedService` background services. These are tricky because they run in loops.

**Services tested:**
- `ReservationCleanupService` — cancels expired pending reservations
- `NoShowAutoCancelService` — cancels no-show reservations
- `HotelDeactivationRefundService` — refunds when hotel deactivated
- `InventoryRestoreHelper` — helper for restoring room inventory

**Pattern — mock the DI scope factory:**

Background services get scoped services via `IServiceScopeFactory`. In tests, this entire chain is mocked:

```csharp
var scopeFactoryMock = new Mock<IServiceScopeFactory>();
var scopeMock        = new Mock<IServiceScope>();
var spMock           = new Mock<IServiceProvider>();

// Wire up the scope → service provider → individual services
spMock.Setup(p => p.GetService(typeof(IRepository<Guid, Reservation>)))
      .Returns(reservationRepoMock.Object);
spMock.Setup(p => p.GetService(typeof(IUnitOfWork)))
      .Returns(unitOfWorkMock.Object);

scopeMock.Setup(s => s.ServiceProvider).Returns(spMock.Object);
scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);
```

**Cancellation token pattern:**

```csharp
[Fact]
public async Task ExecuteAsync_CancelledImmediately_DoesNotProcess()
{
    var sut = new ReservationCleanupService(_scopeFactoryMock.Object, _loggerMock.Object);
    using var cts = new CancellationTokenSource();
    cts.Cancel();  // ← cancel immediately

    await sut.StartAsync(cts.Token);

    // Service should exit without doing any work
    _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Never);
}
```

---

### 11.8 Program / DI Tests (`ProgramTests.cs`)

Uses `WebApplicationFactory` to boot the real app and verify every service is registered in the DI container.

```csharp
[Fact]
public void DI_IHotelService_IsRegistered()
{
    using var scope = _factory.Services.CreateScope();
    var svc = scope.ServiceProvider.GetService<IHotelService>();
    svc.Should().NotBeNull();
}
```

**Why this matters:** You can write perfect service code, but if you forget `services.AddScoped<IHotelService, HotelService>()` in `Program.cs`, the app crashes at runtime. These tests catch that during CI/CD before deployment.

Tests cover: repositories, unit of work, all application services, background services, token service, password service, and supporting infrastructure.

---

### 11.9 DTO Model Tests (`Models/DtoModelTests.cs`)

The simplest tests — verify that DTO properties have the correct default values.

```csharp
[Fact]
public void CreateReviewDto_DefaultValues_AreCorrect()
{
    var dto = new CreateReviewDto();
    dto.Comment.Should().BeEmpty();
    dto.ImageUrl.Should().BeNull();
}
```

These catch accidental breaking changes to DTOs — for example, someone changes a default value from `null` to `""` which could break API consumers.

---

## 12. GlobalUsings.cs — Shared Imports

```csharp
global using Xunit;
global using Microsoft.Extensions.DependencyInjection;
```

These two `global using` statements apply to **every** file in the test project automatically. This means you never need to write `using Xunit;` or `using Microsoft.Extensions.DependencyInjection;` in individual test files — they're already available everywhere.

Other frequently-needed `using` statements (FluentAssertions, Moq, etc.) are still added per-file, which is intentional — it keeps the import explicit and makes each file self-documenting.

---

## 13. Code Coverage — coverlet + Fine Code Coverage

### `coverlet.collector`

This NuGet package hooks into `dotnet test` and collects coverage data. The `coverage.runsettings` file configures it:

```xml
<!-- coverage.runsettings -->
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector ...>
        <Configuration>
          <Format>Cobertura</Format>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

After each test run, `.cobertura.xml` files appear in `TestResults/` — they record which lines of code were executed by tests.

### Fine Code Coverage (Visual Studio extension)

The `bin/Debug/net10.0/fine-code-coverage/` directory contains output from this VS extension. It reads the Cobertura XML and shows colored line-by-line coverage inside the editor — green for covered, red for not covered.

This is why the project has `CoverageGapTests1.cs` through `CoverageGapTests6.cs` — those were created specifically to reach the red lines (uncovered branches) that the main tests missed.

### Running with coverage

```bash
dotnet test --collect:"Code Coverage" --settings coverage.runsettings
```

---

## 14. Common Patterns Cheat Sheet

### Creating a mock and injecting it

```csharp
var mock = new Mock<IMyService>();
var sut  = new MyClass(mock.Object);
```

### Setup a return value

```csharp
mock.Setup(m => m.DoSomethingAsync(It.IsAny<string>()))
    .ReturnsAsync("result");
```

### Setup to throw

```csharp
mock.Setup(m => m.DoSomethingAsync(It.IsAny<string>()))
    .ThrowsAsync(new NotFoundException("not found"));
```

### Verify a method was called

```csharp
mock.Verify(m => m.CommitAsync(), Times.Once);
mock.Verify(m => m.RollbackAsync(), Times.Never);
```

### Mock an EF queryable

```csharp
var data = new List<Hotel> { ... }.AsQueryable().BuildMock();
_repoMock.Setup(r => r.GetQueryable()).Returns(data);
```

### InMemory database (isolated per test)

```csharp
var options = new DbContextOptionsBuilder<MyContext>()
    .UseInMemoryDatabase(nameof(MyTestMethod))
    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
    .Options;
using var ctx = new MyContext(options);
```

### Fake authenticated user for controllers

```csharp
_sut.ControllerContext = ControllerTestHelper.BuildControllerContext(userId, "Admin");
```

### Assert exception with message

```csharp
var act = async () => await sut.MethodAsync(input);
await act.Should().ThrowAsync<ConflictException>()
         .WithMessage("Email already registered.");
```

### Assert no exception

```csharp
await act.Should().NotThrowAsync();
```

### Assert controller result type

```csharp
result.Should().BeOfType<OkObjectResult>();
result.Should().BeOfType<NotFoundObjectResult>();
```

---

## 15. Running Tests

### Run all tests

```bash
dotnet test
```

### Run with verbose output

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Run with coverage

```bash
dotnet test --collect:"Code Coverage" --settings coverage.runsettings
```

### Run a specific test class

```bash
dotnet test --filter "FullyQualifiedName~AuthServiceTests"
```

### Run a specific test method

```bash
dotnet test --filter "FullyQualifiedName~RegisterGuestAsync_ValidDto_ReturnsAuthResponseDto"
```

### Run from Visual Studio

Use **Test → Run All Tests** or the **Test Explorer** panel. The `xunit.runner.visualstudio` package enables this.

---

## Summary Table — Tools and Their Roles

| Tool | Role | Where Used |
|---|---|---|
| **xUnit** | Test framework, discovers `[Fact]` methods | All test files |
| **FluentAssertions** | Readable assertions (`Should().Be(...)`) | All test files |
| **Moq** | Creates fake implementations of interfaces | Service tests, Controller tests, Middleware tests |
| **MockQueryable.Moq** | Makes EF Core async LINQ work on fake data | Service tests (any with `GetQueryable()`) |
| **EF Core InMemory** | Real EF Core operations without SQL Server | Context tests, Repository tests, Middleware tests, UoW tests |
| **WebApplicationFactory** | Boots real app in-process for DI verification | `ProgramTests.cs` |
| **coverlet** | Collects code coverage metrics | All tests (via `dotnet test`) |
| **System.IdentityModel.Tokens.Jwt** | Parses JWT to verify claims | `TokenServiceTests.cs` |
| **ControllerTestHelper** | Builds fake `ClaimsPrincipal` for controller actions | All controller tests |
| **IClassFixture** | Shares expensive setup (app boot) across tests | `ProgramTests.cs` |
