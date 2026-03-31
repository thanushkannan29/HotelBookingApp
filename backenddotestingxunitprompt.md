# Kiro Prompt — 100% xUnit Code Coverage for HotelBookingAppWebApi

## Context

You are generating a complete xUnit test suite for the `HotelBookingAppWebApi` .NET Web API backend.
The goal is **100% code coverage** across every layer using the **AAA (Arrange / Act / Assert)** pattern.

---

## Tech Stack & NuGet Packages (already in .csproj)

```xml
<PackageReference Include="coverlet.collector"               Version="6.0.4" />
<PackageReference Include="Microsoft.NET.Test.Sdk"           Version="17.14.1" />
<PackageReference Include="xunit"                            Version="2.9.3" />
<PackageReference Include="xunit.runner.visualstudio"        Version="3.1.4" />
<PackageReference Include="Moq"                              Version="4.20.72" />
<PackageReference Include="FluentAssertions"                 Version="6.12.1" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.0" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt"  Version="8.1.2" />
<PackageReference Include="MockQueryable.Moq"                Version="7.0.0" />
```

---

## Mandatory Rules for Every Test File

1. **AAA strictly** — every test method must have explicit `// Arrange`, `// Act`, `// Assert` comments.
2. Use **FluentAssertions** (`result.Should().Be(...)`) — never use `Assert.Equal`.
3. Use **Moq** for all interfaces (`Mock<IRepository<Guid,User>>`, etc.).
4. Use **MockQueryable.Moq** (`mock.BuildMock()`) whenever `GetQueryable()` is called and LINQ like `AnyAsync`, `FirstOrDefaultAsync`, `ToListAsync`, `Include(...).Where(...)` is chained on it.
5. Use **EF Core InMemory** (`new DbContextOptionsBuilder<HotelBookingContext>().UseInMemoryDatabase(...)`) for `Repository<TKey,TEntity>`, `UnitOfWork`, `HotelBookingContext`, and `GlobalExceptionMiddleware` tests.
6. Every **happy path** AND every **exception / edge-case branch** must have its own `[Fact]` method.
7. Test class names: `{ClassName}Tests`. Method names: `{MethodName}_{Scenario}_Returns{ExpectedOutcome}`.
8. Use `[Theory] + [InlineData]` wherever multiple similar inputs should be validated.
9. For `BackgroundService` tests, use `CancellationTokenSource` with immediate cancellation to exercise `ExecuteAsync` without infinite loops.
10. No shared mutable state between tests — create fresh mocks/context inside each test.

---

## Files to Test & What to Cover

### 1. `Repository/Repository.cs` → `RepositoryTests.cs`

**Class:** `Repository<Guid, User>` backed by `UseInMemoryDatabase`.

| Method | Happy path | Edge / exception |
|--------|-----------|-----------------|
| `AddAsync(entity)` | Returns added entity | Throws `ArgumentNullException` when entity is null |
| `GetAsync(key)` | Returns entity by key | Returns `null` when key missing |
| `GetAllAsync()` | Returns all entities | Returns empty list when table empty |
| `DeleteAsync(key)` | Removes and returns entity | Returns `null` when key missing |
| `UpdateAsync(key, entity)` | Updates and returns entity | Returns `null` when key missing; returns `null` when entity is null |
| `FirstOrDefaultAsync(predicate)` | Returns matching entity | Returns `null` when no match |
| `GetQueryable()` | Returns `IQueryable<TEntity>` (not null) | — |
| `GetAllByForeignKeyAsync(predicate, limit, page)` | Returns correct page | Returns empty when no match |

---

### 2. `Services/UnitOfWork.cs` → `UnitOfWorkTests.cs`

**Class:** `UnitOfWork` backed by `UseInMemoryDatabase`.

| Scenario | Test |
|---------|------|
| `BeginTransactionAsync()` — first call starts transaction | `BeginTransactionAsync_FirstCall_StartsTransaction` |
| `BeginTransactionAsync()` — second call is no-op (guard) | `BeginTransactionAsync_SecondCall_DoesNotStartNewTransaction` |
| `CommitAsync()` with active transaction — saves and commits | `CommitAsync_WithTransaction_SavesAndCommits` |
| `CommitAsync()` without transaction — falls back to `SaveChangesAsync` | `CommitAsync_WithoutTransaction_CallsSaveChanges` |
| `RollbackAsync()` with active transaction — rolls back | `RollbackAsync_WithTransaction_RollsBack` |
| `RollbackAsync()` without transaction — no-op | `RollbackAsync_WithoutTransaction_DoesNothing` |
| `SaveChangesAsync()` — delegates to context | `SaveChangesAsync_CallsContextSaveChanges` |
| `Dispose()` — cleans up transaction | `Dispose_CleansUpTransaction` |

> Use a real `InMemoryDatabase` context so the transaction lifecycle can be verified indirectly through state.

---

### 3. `Contexts/HotelBookingContext.cs` → `HotelBookingContextTests.cs`

Use `UseInMemoryDatabase`.

| Scenario | Test |
|---------|------|
| Constructor receives options — context is created | `Constructor_ValidOptions_CreatesContext` |
| All `DbSet` properties are non-null | `DbSets_AllPropertiesAreNotNull` |
| `OnModelCreating` — `User.Email` has unique index | `OnModelCreating_UserEmail_HasUniqueIndex` |
| Can add & query `User` entity | `Users_AddAndQuery_ReturnsEntity` |
| Can add & query `Hotel` entity | `Hotels_AddAndQuery_ReturnsEntity` |
| Can add & query `Reservation` with navigation | `Reservations_AddAndQuery_ReturnsEntity` |
| Can add `Log` entity | `Logs_AddAndQuery_ReturnsEntity` |

---

### 4. `Exceptions/AppExceptions.cs` → `AppExceptionsTests.cs`

Test every exception class: `AppException`, `NotFoundException`, `ConflictException`, `ValidationException`, `UnAuthorizedException`, `PaymentException`, `ReservationFailedException`, `InsufficientInventoryException`, `RateNotFoundException`, `ReviewException`, `UserProfileException`, `UnableToCreateEntityException`.

Per class:

```csharp
[Fact]
public void NotFoundException_WithMessage_SetsCorrectStatusCodeAndMessage()
{
    // Arrange
    var message = "Hotel not found.";

    // Act
    var ex = new NotFoundException(message);

    // Assert
    ex.StatusCode.Should().Be(404);
    ex.Message.Should().Be(message);
}
```

| Exception | Status Code | Message behaviour |
|----------|------------|------------------|
| `NotFoundException` | 404 | verbatim |
| `ConflictException` | 409 | verbatim |
| `ValidationException` | 400 | verbatim |
| `UnAuthorizedException` (default) | 401 | "Unauthorized" |
| `UnAuthorizedException` (custom) | 401 | custom message |
| `PaymentException` | 400 | verbatim |
| `ReservationFailedException` | 400 | appended suffix |
| `InsufficientInventoryException` | 409 | appended suffix |
| `RateNotFoundException` | 404 | appended suffix |
| `ReviewException` | 400 | verbatim |
| `UserProfileException` | 404 | verbatim |
| `UnableToCreateEntityException` (default) | 400 | default message |
| `UnableToCreateEntityException` (custom) | 400 | custom message |

---

### 5. `Exceptions/Middleware/GlobalExceptionMiddleware.cs` → `GlobalExceptionMiddlewareTests.cs`

Use `DefaultHttpContext` + real `InMemoryDatabase`.

| Scenario | Test |
|---------|------|
| `InvokeAsync` — no exception — calls next and returns 200 | `InvokeAsync_NoException_CallsNextMiddleware` |
| `InvokeAsync` — `AppException` thrown — returns correct status code & JSON | `InvokeAsync_AppException_ReturnsCorrectStatusAndJson` |
| `InvokeAsync` — `NotFoundException` (404) — response has 404 | `InvokeAsync_NotFoundException_Returns404` |
| `InvokeAsync` — generic `Exception` thrown — returns 500 | `InvokeAsync_GenericException_Returns500` |
| `InvokeAsync` — authenticated user claims are extracted | `InvokeAsync_AuthenticatedUser_LogsUserInfo` |
| `InvokeAsync` — anonymous user — logs "Anonymous" | `InvokeAsync_AnonymousUser_LogsAnonymous` |
| DB persist fails — logs critical but does not rethrow | `InvokeAsync_DbPersistFails_LogsCriticalAndContinues` |

**Setup helper:**
```csharp
private static DefaultHttpContext BuildHttpContext(IServiceProvider sp, ClaimsPrincipal? user = null)
{
    var ctx = new DefaultHttpContext { RequestServices = sp };
    if (user != null) ctx.User = user;
    return ctx;
}
```

---

### 6. `Services/TokenService.cs` → `TokenServiceTests.cs`

| Scenario | Test |
|---------|------|
| Valid config — `CreateToken` returns non-empty JWT string | `CreateToken_ValidPayload_ReturnsJwtString` |
| Token contains `NameIdentifier` claim for UserId | `CreateToken_ValidPayload_ContainsUserIdClaim` |
| Token contains `Name` claim | `CreateToken_ValidPayload_ContainsUserNameClaim` |
| Token contains `Role` claim | `CreateToken_ValidPayload_ContainsRoleClaim` |
| `HotelId` present — token contains `HotelId` claim | `CreateToken_WithHotelId_ContainsHotelIdClaim` |
| `HotelId` is null — token does NOT contain `HotelId` claim | `CreateToken_WithoutHotelId_DoesNotContainHotelIdClaim` |
| Missing JWT config key — throws `InvalidOperationException` | `Constructor_MissingJwtKey_ThrowsInvalidOperationException` |

**Verify JWT by parsing:**
```csharp
var handler = new JwtSecurityTokenHandler();
var token = handler.ReadJwtToken(result);
token.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier);
```

---

### 7. `Services/PasswordService.cs` → `PasswordServiceTests.cs`

| Scenario | Test |
|---------|------|
| `HashPassword` with null salt — generates new salt and returns hash | `HashPassword_NullSalt_GeneratesNewSaltAndReturnsHash` |
| `HashPassword` with existing salt — `newSalt` is null | `HashPassword_ExistingSalt_NewSaltIsNull` |
| Same password + same salt — produces identical hash | `HashPassword_SamePasswordAndSalt_ProducesIdenticalHash` |
| Different passwords — produce different hashes | `HashPassword_DifferentPasswords_ProduceDifferentHashes` |
| Empty password — throws `ArgumentException` | `HashPassword_EmptyPassword_ThrowsArgumentException` |

---

### 8. `Services/AuthService.cs` → `AuthServiceTests.cs`

Mock: `IRepository<Guid,User>`, `IRepository<Guid,Hotel>`, `IRepository<Guid,UserProfileDetails>`, `IPasswordService`, `ITokenService`, `IWalletService`, `IUnitOfWork`.

#### `RegisterGuestAsync`
| Scenario | Test |
|---------|------|
| Happy path — returns `AuthResponseDto` with token | `RegisterGuestAsync_ValidDto_ReturnsAuthResponseDto` |
| Email already registered — throws `ConflictException` | `RegisterGuestAsync_DuplicateEmail_ThrowsConflictException` |
| Any inner exception — calls `RollbackAsync` | `RegisterGuestAsync_InnerException_CallsRollback` |

#### `RegisterHotelAdminAsync`
| Scenario | Test |
|---------|------|
| Happy path — creates hotel + admin + profile | `RegisterHotelAdminAsync_ValidDto_CreatesHotelAndAdmin` |
| Duplicate email — throws `ConflictException` | `RegisterHotelAdminAsync_DuplicateEmail_ThrowsConflictException` |
| Inner exception — calls `RollbackAsync` | `RegisterHotelAdminAsync_InnerException_CallsRollback` |

#### `LoginAsync`
| Scenario | Test |
|---------|------|
| Valid credentials — returns token | `LoginAsync_ValidCredentials_ReturnsAuthResponseDto` |
| Email not found — throws `UnAuthorizedException` | `LoginAsync_EmailNotFound_ThrowsUnAuthorizedException` |
| Account deactivated — throws `UnAuthorizedException` | `LoginAsync_AccountDeactivated_ThrowsUnAuthorizedException` |
| Wrong password — throws `UnAuthorizedException` | `LoginAsync_WrongPassword_ThrowsUnAuthorizedException` |

**MockQueryable usage:**
```csharp
var users = new List<User> { existingUser }.AsQueryable().BuildMock();
_userRepositoryMock.Setup(r => r.GetQueryable()).Returns(users);
```

---

### 9. Controllers → `{ControllerName}Tests.cs`

**Pattern for all controllers.** Demonstrate with `AuthenticationController` then repeat for every other controller.

```csharp
public class AuthenticationControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock = new();
    private readonly AuthenticationController _sut;

    public AuthenticationControllerTests()
        => _sut = new AuthenticationController(_authServiceMock.Object);

    [Fact]
    public async Task RegisterGuest_ValidDto_ReturnsOkWithToken()
    {
        // Arrange
        var dto = new RegisterUserDto { ... };
        _authServiceMock.Setup(s => s.RegisterGuestAsync(dto))
            .ReturnsAsync(new AuthResponseDto { Token = "jwt" });

        // Act
        var result = await _sut.RegisterGuest(dto);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new { success = true, data = new { Token = "jwt" } });
    }

    [Fact]
    public async Task RegisterGuest_ServiceThrows_PropagatesException()
    {
        // Arrange
        _authServiceMock.Setup(s => s.RegisterGuestAsync(It.IsAny<RegisterUserDto>()))
            .ThrowsAsync(new ConflictException("Email already registered."));

        // Act
        var act = async () => await _sut.RegisterGuest(new RegisterUserDto());

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Email already registered.");
    }
}
```

**Cover for EVERY controller action:**
- Happy path — correct HTTP result type (`OkObjectResult`, `NoContentResult`, etc.)
- Service throws — exception propagates (middleware handles it in production)
- For actions with `User.FindFirstValue(ClaimTypes.NameIdentifier)` — set `_sut.ControllerContext` with a `ClaimsPrincipal`:

```csharp
private static ControllerContext BuildControllerContext(Guid userId, string role = "Admin")
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        new Claim(ClaimTypes.Role, role)
    };
    var identity = new ClaimsIdentity(claims, "Test");
    var principal = new ClaimsPrincipal(identity);
    return new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } };
}
```

**Controllers to cover (one test file each):**

| Controller File | Key Actions |
|----------------|------------|
| `AuthenticationController` | `RegisterGuest`, `RegisterHotelAdmin`, `Login` |
| `AdminHotelController` | `Update`, `ToggleStatus`, `UpdateGst` |
| `AdminAmenityRequestController` | all actions |
| `AdminAuditLogController` | all actions |
| `AdminInventoryController` | all actions |
| `AdminReservationController` | all actions |
| `AdminReviewController` | all actions |
| `AdminRoomController` | all actions |
| `AdminRoomTypeController` | all actions |
| `AdminSupportController` | all actions |
| `AdminTransactionController` | all actions |
| `AdminWalletController` | all actions |
| `DashboardController` | all actions |
| `GuestPaymentController` | all actions |
| `GuestPromoCodeController` | all actions |
| `GuestReservationController` | all actions |
| `GuestSupportController` | all actions |
| `GuestWalletController` | all actions |
| `LogController` | all actions |
| `PublicAmenityController` | all actions |
| `PublicHotelController` | all actions |
| `PublicSupportController` | all actions |
| `ReviewController` | all actions |
| `SuperAdminAmenityController` | all actions |
| `SuperAdminAmenityRequestController` | all actions |
| `SuperAdminAuditLogController` | all actions |
| `SuperAdminHotelController` | all actions |
| `SuperAdminRevenueController` | all actions |
| `SuperAdminSupportController` | all actions |
| `TransactionController` | all actions |
| `UserProfileController` | all actions |

---

### 10. Background Services → `{ServiceName}Tests.cs`

Cover all four background services: `ReservationCleanupService`, `HotelDeactivationRefundService`, `NoShowAutoCancelService`, `InventoryRestoreHelper`.

**Pattern:**

```csharp
public class ReservationCleanupServiceTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<ILogger<ReservationCleanupService>> _loggerMock = new();

    [Fact]
    public async Task ExecuteAsync_CancelledImmediately_DoesNotProcess()
    {
        // Arrange
        var sut = new ReservationCleanupService(_scopeFactoryMock.Object, _loggerMock.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // cancel before start

        // Act
        await sut.StartAsync(cts.Token);

        // Assert
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ExpiredReservationsExist_CancelsAndRefunds()
    {
        // Arrange
        // -- build scope chain mocks
        var reservationRepoMock = new Mock<IRepository<Guid, Reservation>>();
        var inventoryRepoMock  = new Mock<IRepository<Guid, RoomTypeInventory>>();
        var unitOfWorkMock     = new Mock<IUnitOfWork>();
        var walletServiceMock  = new Mock<IWalletService>();

        var expiredReservation = new Reservation
        {
            ReservationId   = Guid.NewGuid(),
            UserId          = Guid.NewGuid(),
            Status          = ReservationStatus.Pending,
            ExpiryTime      = DateTime.UtcNow.AddMinutes(-10),
            WalletAmountUsed = 100m,
            ReservationCode = "RES001",
            ReservationRooms = new List<ReservationRoom>()
        };

        var queryable = new List<Reservation> { expiredReservation }
            .AsQueryable().BuildMock();
        reservationRepoMock.Setup(r => r.GetQueryable()).Returns(queryable);

        // build scope / service provider mock chain
        var scopeMock = new Mock<IServiceScope>();
        var spMock    = new Mock<IServiceProvider>();
        spMock.Setup(p => p.GetService(typeof(IRepository<Guid, Reservation>)))
              .Returns(reservationRepoMock.Object);
        spMock.Setup(p => p.GetService(typeof(IRepository<Guid, RoomTypeInventory>)))
              .Returns(inventoryRepoMock.Object);
        spMock.Setup(p => p.GetService(typeof(IUnitOfWork)))
              .Returns(unitOfWorkMock.Object);
        spMock.Setup(p => p.GetService(typeof(IWalletService)))
              .Returns(walletServiceMock.Object);
        scopeMock.Setup(s => s.ServiceProvider).Returns(spMock.Object);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var sut = new ReservationCleanupService(_scopeFactoryMock.Object, _loggerMock.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(200); // let one loop run

        // Assert
        unitOfWorkMock.Verify(u => u.CommitAsync(), Times.AtLeastOnce);
        walletServiceMock.Verify(
            w => w.CreditAsync(expiredReservation.UserId, 100m, It.IsAny<string>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ProcessingThrows_LogsErrorAndContinues()
    {
        // Arrange
        _scopeFactoryMock.Setup(f => f.CreateScope()).Throws(new Exception("DB error"));
        var sut = new ReservationCleanupService(_scopeFactoryMock.Object, _loggerMock.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        Func<Task> act = async () =>
        {
            await sut.StartAsync(cts.Token);
            await Task.Delay(200);
        };

        // Assert — does NOT throw; error is caught and logged
        await act.Should().NotThrowAsync();
        _loggerMock.Verify(
            l => l.Log(LogLevel.Error, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
```

Apply same pattern to `HotelDeactivationRefundService` and `NoShowAutoCancelService`.

#### `InventoryRestoreHelper` → `InventoryRestoreHelperTests.cs`

```csharp
[Fact]
public async Task BuildInventoryLookupAsync_ValidReservations_ReturnsLookup() { ... }

[Fact]
public void RestoreInventory_ValidReservation_IncrementsAvailableCount() { ... }
```

---

### 11. All Services → `{ServiceName}Tests.cs`

For every service file, follow this template and cover **every public method** and **every branch**.

```
Services/
  AmenityRequestService.cs  → AmenityRequestServiceTests.cs
  AmenityService.cs         → AmenityServiceTests.cs
  AuditLogService.cs        → AuditLogServiceTests.cs
  AuthService.cs            → AuthServiceTests.cs  (covered above)
  DashboardService.cs       → DashboardServiceTests.cs
  HotelService.cs           → HotelServiceTests.cs
  InventoryService.cs       → InventoryServiceTests.cs
  LogService.cs             → LogServiceTests.cs
  PasswordService.cs        → PasswordServiceTests.cs (covered above)
  PromoCodeService.cs       → PromoCodeServiceTests.cs
  QrCodeHelper.cs           → QrCodeHelperTests.cs
  ReservationService.cs     → ReservationServiceTests.cs
  ReviewService.cs          → ReviewServiceTests.cs
  RoomService.cs            → RoomServiceTests.cs
  RoomTypeService.cs        → RoomTypeServiceTests.cs
  SuperAdminRevenueService  → SuperAdminRevenueServiceTests.cs
  SupportRequestService.cs  → SupportRequestServiceTests.cs
  TokenService.cs           → TokenServiceTests.cs (covered above)
  TransactionService.cs     → TransactionServiceTests.cs
  UnitOfWork.cs             → UnitOfWorkTests.cs (covered above)
  UserService.cs            → UserServiceTests.cs
  WalletService.cs          → WalletServiceTests.cs
```

**For each service test:**
- Mock every `IRepository<TKey, TEntity>` that the service depends on
- Mock `IUnitOfWork`
- Mock peer services (e.g., `IWalletService`, `ITokenService`)
- Use `MockQueryable.Moq` for every `GetQueryable()` call
- Cover: success, not-found → `NotFoundException`, conflict → `ConflictException`, validation → `ValidationException`, unauthorized → `UnAuthorizedException`

---

## Project Structure

```
HotelBookingAppWebApi.Tests/
├── HotelBookingAppWebApi.Tests.csproj
├── Repository/
│   └── RepositoryTests.cs
├── Services/
│   ├── AuthServiceTests.cs
│   ├── TokenServiceTests.cs
│   ├── PasswordServiceTests.cs
│   ├── UnitOfWorkTests.cs
│   ├── AmenityServiceTests.cs
│   ├── AmenityRequestServiceTests.cs
│   ├── AuditLogServiceTests.cs
│   ├── DashboardServiceTests.cs
│   ├── HotelServiceTests.cs
│   ├── InventoryServiceTests.cs
│   ├── LogServiceTests.cs
│   ├── PromoCodeServiceTests.cs
│   ├── QrCodeHelperTests.cs
│   ├── ReservationServiceTests.cs
│   ├── ReviewServiceTests.cs
│   ├── RoomServiceTests.cs
│   ├── RoomTypeServiceTests.cs
│   ├── SuperAdminRevenueServiceTests.cs
│   ├── SupportRequestServiceTests.cs
│   ├── TransactionServiceTests.cs
│   ├── UserServiceTests.cs
│   ├── WalletServiceTests.cs
│   └── BackgroundServices/
│       ├── ReservationCleanupServiceTests.cs
│       ├── HotelDeactivationRefundServiceTests.cs
│       ├── NoShowAutoCancelServiceTests.cs
│       └── InventoryRestoreHelperTests.cs
├── Controllers/
│   ├── AuthenticationControllerTests.cs
│   ├── DashboardControllerTests.cs
│   ├── LogControllerTests.cs
│   ├── ReviewControllerTests.cs
│   ├── TransactionControllerTests.cs
│   ├── UserProfileControllerTests.cs
│   ├── Admin/
│   │   ├── AdminAmenityRequestControllerTests.cs
│   │   ├── AdminAuditLogControllerTests.cs
│   │   ├── AdminHotelControllerTests.cs
│   │   ├── AdminInventoryControllerTests.cs
│   │   ├── AdminReservationControllerTests.cs
│   │   ├── AdminReviewControllerTests.cs
│   │   ├── AdminRoomControllerTests.cs
│   │   ├── AdminRoomTypeControllerTests.cs
│   │   ├── AdminSupportControllerTests.cs
│   │   ├── AdminTransactionControllerTests.cs
│   │   └── AdminWalletControllerTests.cs
│   ├── Guest/
│   │   ├── GuestPaymentControllerTests.cs
│   │   ├── GuestPromoCodeControllerTests.cs
│   │   ├── GuestReservationControllerTests.cs
│   │   ├── GuestSupportControllerTests.cs
│   │   └── GuestWalletControllerTests.cs
│   ├── Public/
│   │   ├── PublicAmenityControllerTests.cs
│   │   ├── PublicHotelControllerTests.cs
│   │   └── PublicSupportControllerTests.cs
│   └── SuperAdmin/
│       ├── SuperAdminAmenityControllerTests.cs
│       ├── SuperAdminAmenityRequestControllerTests.cs
│       ├── SuperAdminAuditLogControllerTests.cs
│       ├── SuperAdminHotelControllerTests.cs
│       ├── SuperAdminRevenueControllerTests.cs
│       └── SuperAdminSupportControllerTests.cs
├── Contexts/
│   └── HotelBookingContextTests.cs
└── Exceptions/
    ├── AppExceptionsTests.cs
    └── Middleware/
        └── GlobalExceptionMiddlewareTests.cs
```

---

## .csproj for the Test Project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector"                    Version="6.0.4" />
    <PackageReference Include="Microsoft.NET.Test.Sdk"                Version="17.14.1" />
    <PackageReference Include="xunit"                                 Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio"             Version="3.1.4" />
    <PackageReference Include="Moq"                                   Version="4.20.72" />
    <PackageReference Include="FluentAssertions"                      Version="6.12.1" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.0" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt"       Version="8.1.2" />
    <PackageReference Include="MockQueryable.Moq"                     Version="7.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing"      Version="10.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\HotelBookingAppWebApi\HotelBookingAppWebApi.csproj" />
  </ItemGroup>
</Project>
```

---

## Run Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

Target: **Line coverage ≥ 100%, Branch coverage ≥ 95%**.

---

## Key Gotchas to Avoid

| Gotcha | Fix |
|--------|-----|
| `GetQueryable()` returns `IQueryable` — `AnyAsync` fails on plain `List.AsQueryable()` | Always use `BuildMock()` from MockQueryable.Moq |
| `UnitOfWork` uses real `IDbContextTransaction` | Use InMemoryDatabase (transactions are no-ops but the guard logic is still exercised) |
| Background service loops forever | Cancel `CancellationTokenSource` after one tick; use `Task.Delay` to let one iteration complete |
| Controllers use `User.FindFirstValue(ClaimTypes.NameIdentifier)` | Set `ControllerContext.HttpContext.User` with a real `ClaimsPrincipal` |
| `GlobalExceptionMiddleware` resolves `HotelBookingContext` from `IServiceProvider` | Build a real `ServiceCollection` with InMemory EF and call `BuildServiceProvider()` |
| `TokenService` reads `IConfiguration["Keys:Jwt"]` | Use `new ConfigurationBuilder().AddInMemoryCollection(...)` |