# Backend Testing Documentation
## Hotel Booking App — xUnit Test Guide

This document explains everything about how we test the backend using xUnit, Moq, FluentAssertions, and EF Core InMemory. Every concept is explained in simple words with real examples from our test project.

---

## 1. What is xUnit?

xUnit is a testing framework for .NET. It lets you write automated tests that check if your code works correctly. Instead of running the app manually and clicking around, you write a test that does it for you automatically.

Think of it like this: you write a small program that calls your real code and checks if the result is what you expected.

---

## 2. The AAA Pattern (Arrange, Act, Assert)

Every single test in this project follows the AAA pattern. This is the most important rule.

- **Arrange** — Set up everything the test needs (fake data, mocks, the service to test)
- **Act** — Call the method you want to test
- **Assert** — Check that the result is what you expected

### Example from AuthServiceTests.cs

```csharp
[Fact]
public async Task RegisterGuestAsync_ValidDto_ReturnsAuthResponseDto()
{
    // Arrange
    SetupEmptyUserQueryable();
    SetupPasswordService();
    _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
    _tokenServiceMock.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>())).Returns("jwt-token");
    var sut = CreateSut();
    var dto = new RegisterUserDto { Name = "Alice", Email = "alice@test.com", Password = "pass123" };

    // Act
    var result = await sut.RegisterGuestAsync(dto);

    // Assert
    result.Should().NotBeNull();
    result.Token.Should().Be("jwt-token");
}
```

Arrange sets up the fake repos and token service. Act calls `RegisterGuestAsync`. Assert checks the token came back correctly.

---

## 3. What is [Fact]?

`[Fact]` is an attribute that marks a method as a test. xUnit finds all methods with `[Fact]` and runs them automatically.

```csharp
[Fact]
public async Task LoginAsync_EmailNotFound_ThrowsUnAuthorizedException()
{
    // this is a test
}
```

Every test method must be `public`, return `void` or `Task`, and have `[Fact]` on it.

---

## 4. Test Naming Convention

All tests in this project use this naming pattern:

```
MethodName_Scenario_ExpectedResult
```

Examples from our project:
- `RegisterGuestAsync_ValidDto_ReturnsAuthResponseDto`
- `LoginAsync_WrongPassword_ThrowsUnAuthorizedException`
- `CancelReservationAsync_AlreadyCancelled_ThrowsReservationFailedException`
- `GetAllAsync_EmptyTable_ReturnsEmptyList`

This makes it immediately clear what the test does without reading the code.

---

## 5. What is "sut"?

`sut` stands for **System Under Test**. It is the real class you are testing. All mocks and fakes are just helpers. The sut is the real thing.

```csharp
private AuthService CreateSut() => new(
    _userRepoMock.Object,
    _hotelRepoMock.Object,
    _profileRepoMock.Object,
    _passwordServiceMock.Object,
    _tokenServiceMock.Object,
    _walletServiceMock.Object,
    _unitOfWorkMock.Object);
```

We create a factory method `CreateSut()` so every test gets a fresh instance.

---

## 6. Moq — Faking Dependencies

Moq lets you create fake versions of interfaces. Instead of using a real database, you create a fake repository that returns whatever you tell it to.

### Creating a Mock

```csharp
private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
```

### Setting Up a Mock (telling it what to return)

```csharp
_userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
```

This says: "When `AddAsync` is called with any User, return that same User back."

### It.IsAny<T>()

`It.IsAny<T>()` means "match any value of this type". Use it when you don't care what exact value is passed.

```csharp
_tokenServiceMock.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>())).Returns("jwt-token");
```

### Verify — Checking a method was called

```csharp
_unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
```

This checks that `RollbackAsync` was called exactly once. If it wasn't, the test fails.

```csharp
_walletMock.Verify(w => w.CreditAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
```

`Times.Never` checks that the method was never called at all.

---

## 7. MockQueryable.Moq — Faking IQueryable

EF Core uses `IQueryable` for database queries. Normal Moq can't fake async LINQ operations. `MockQueryable.Moq` solves this.

```csharp
var users = new List<User> { user }.AsQueryable().BuildMock();
_userRepoMock.Setup(r => r.GetQueryable()).Returns(users);
```

`BuildMock()` turns a regular list into a fake async queryable that works with `await`, `.ToListAsync()`, `.FirstOrDefaultAsync()`, etc.

### Empty queryable

```csharp
var empty = new List<User>().AsQueryable().BuildMock();
_userRepoMock.Setup(r => r.GetQueryable()).Returns(empty);
```

Use this when you want to simulate "nothing in the database".

---

## 8. FluentAssertions — Readable Assertions

FluentAssertions makes your assertions read like English sentences.

### Basic value checks

```csharp
result.Token.Should().Be("jwt-token");
result.Should().NotBeNull();
result.Should().BeEmpty();
result.Should().HaveCount(1);
result.Should().BeTrue();
result.Should().BeFalse();
```

### Checking exceptions

```csharp
var act = async () => await sut.LoginAsync(new LoginDto { Email = "nobody@test.com", Password = "pass" });
await act.Should().ThrowAsync<UnAuthorizedException>();
```

You wrap the call in a lambda, then call `ThrowAsync<ExceptionType>()`.

### Checking exception message

```csharp
await act.Should().ThrowAsync<ConflictException>().WithMessage("*already registered*");
```

The `*` is a wildcard. This checks that the message contains "already registered" anywhere.

### Checking no exception is thrown

```csharp
await act.Should().NotThrowAsync();
```

### Checking collection contents

```csharp
result.Should().Contain("Mumbai");
result.Reservations.First().ReservationCode.Should().Be("R001");
```

---

## 9. EF Core InMemory Database

For tests that need real database behavior (like joins, navigation properties, complex queries), we use EF Core's InMemory provider instead of a real SQL Server.

### Creating an InMemory context

```csharp
private static HotelBookingContext CreateContext(string dbName)
{
    var opts = new DbContextOptionsBuilder<HotelBookingContext>()
        .UseInMemoryDatabase(dbName)
        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options;
    return new HotelBookingContext(opts);
}
```

Each test gets a unique database name so tests don't interfere with each other.

### Why `ConfigureWarnings`?

InMemory doesn't support real transactions. This line suppresses the warning so tests don't fail because of that.

### Seeding data

```csharp
ctx.Hotels.Add(hotel);
ctx.RoomTypes.Add(roomType);
await ctx.SaveChangesAsync();
```

You add data directly to the context before the test runs.

### Using `using` to dispose

```csharp
using var ctx = CreateContext(nameof(GetMyReservationsAsync_NoReservations_ReturnsEmpty));
```

`using` ensures the context is disposed after the test, freeing memory.

---

## 10. Real Repository + InMemory (Integration-style Tests)

Some tests use the real `Repository<>` class with InMemory EF instead of mocking the repository. This tests more of the real code path.

### Example from ReservationServiceTests.cs

```csharp
private ReservationService CreateSut(HotelBookingContext ctx) => new(
    new Repository<Guid, Reservation>(ctx),
    new Repository<Guid, Room>(ctx),
    new Repository<Guid, RoomType>(ctx),
    // ...
    new UnitOfWork(ctx));
```

Here we pass real `Repository` objects backed by InMemory EF. This is more realistic than pure mocking.

---

## 11. Testing Async Methods

All service methods are async. Tests must also be async.

```csharp
[Fact]
public async Task ConfirmReservationAsync_PendingReservation_Confirms()
{
    // ...
    var result = await sut.ConfirmReservationAsync("R001");
    // ...
}
```

Always use `await` when calling async methods in tests.

---

## 12. Testing Exception Scenarios

When you expect a method to throw, wrap it in a lambda and use `ThrowAsync`.

```csharp
[Fact]
public async Task CreateReservationAsync_CheckInInPast_ThrowsValidationException()
{
    // Arrange
    var sut = CreateSut(ctx);
    var dto = new CreateReservationDto
    {
        CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
        // ...
    };

    // Act
    var act = async () => await sut.CreateReservationAsync(guest.UserId, dto);

    // Assert
    await act.Should().ThrowAsync<ValidationException>().WithMessage("*tomorrow*");
}
```

---

## 13. Testing Rollback on Error

When a service catches an error, it should call `RollbackAsync`. We verify this with Moq.

```csharp
[Fact]
public async Task RegisterGuestAsync_InnerException_CallsRollback()
{
    // Arrange
    SetupEmptyUserQueryable();
    SetupPasswordService();
    _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).ThrowsAsync(new Exception("DB error"));
    var sut = CreateSut();

    // Act
    var act = async () => await sut.RegisterGuestAsync(dto);

    // Assert
    await act.Should().ThrowAsync<Exception>();
    _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
}
```

---

## 14. Testing Background Services

Background services run on a timer. We test them by starting them with a cancelled token or a short delay.

### Cancelled immediately — nothing should run

```csharp
[Fact]
public async Task ExecuteAsync_CancelledImmediately_DoesNotProcess()
{
    var sut = new NoShowAutoCancelService(_scopeFactoryMock.Object, _loggerMock.Object);
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    await sut.StartAsync(cts.Token);

    _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Never);
}
```

### Let it run one iteration then stop

```csharp
await sut.StartAsync(CancellationToken.None);
await Task.Delay(500);
await sut.StopAsync(CancellationToken.None);

_scopeFactoryMock.Verify(f => f.CreateScope(), Times.AtLeastOnce);
```

### Verify error logging

```csharp
_loggerMock.Verify(l => l.Log(
    LogLevel.Error,
    It.IsAny<EventId>(),
    It.Is<It.IsAnyType>((v, _) => true),
    It.IsAny<Exception>(),
    It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
```

This is the standard way to verify that `ILogger.LogError(...)` was called.

---

## 15. Testing Middleware

The `GlobalExceptionMiddleware` is tested by building a real `DefaultHttpContext` and calling `InvokeAsync`.

```csharp
[Fact]
public async Task InvokeAsync_NotFoundException_Returns404()
{
    // Arrange
    var sp = BuildServiceProvider(nameof(InvokeAsync_NotFoundException_Returns404));
    var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
    RequestDelegate next = _ => throw new NotFoundException("Not found.");
    var middleware = new GlobalExceptionMiddleware(next, loggerMock.Object);
    var ctx = BuildHttpContext(sp);

    // Act
    await middleware.InvokeAsync(ctx);

    // Assert
    ctx.Response.StatusCode.Should().Be(404);
}
```

The `next` delegate is a lambda that throws the exception we want to test. The middleware catches it and sets the status code.

---

## 16. Testing Controllers

Controllers are tested by creating them directly and setting a fake `ControllerContext` with a fake user.

```csharp
public AdminReviewControllerTests()
{
    _sut = new AdminReviewController(_serviceMock.Object);
    _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Admin");
}

[Fact]
public async Task GetHotelReviews_ValidDto_ReturnsOk()
{
    // Arrange
    _serviceMock.Setup(s => s.GetAdminHotelReviewsAsync(_userId, 1, 10, null, null, null))
        .ReturnsAsync(new PagedReviewResponseDto());

    // Act
    var result = await _sut.GetHotelReviews(dto);

    // Assert
    result.Should().BeOfType<OkObjectResult>();
}
```

---

## 17. Testing the DI Container (ProgramTests)

`ProgramTests` boots the real application using `WebApplicationFactory` and checks that every service is registered.

```csharp
public class AppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keys:Jwt"] = "test-secret-key-that-is-long-enough-32chars",
                // ...
            });
        });
    }
}
```

Then each test resolves a service from the DI container:

```csharp
[Fact]
public void DI_IAuthService_IsRegistered()
{
    using var scope = _factory.Services.CreateScope();
    var svc = scope.ServiceProvider.GetService<IAuthService>();
    svc.Should().NotBeNull();
}
```

`IClassFixture<AppFactory>` means the factory is created once and shared across all tests in the class.

---

## 18. Testing Repository Methods

`RepositoryTests` uses InMemory EF to test every method of the generic `Repository<>` class.

| Method | What it tests |
|---|---|
| `AddAsync` | Adds entity, returns it |
| `GetAsync` | Finds by key, returns null if missing |
| `GetAllAsync` | Returns all entities |
| `DeleteAsync` | Removes entity, returns null if missing |
| `UpdateAsync` | Updates entity, returns null if missing or null input |
| `FirstOrDefaultAsync` | Finds by predicate |
| `GetQueryable` | Returns non-null IQueryable |
| `GetAllByForeignKeyAsync` | Paged query by predicate |

### Example

```csharp
[Fact]
public async Task DeleteAsync_MissingKey_ReturnsNull()
{
    using var ctx = CreateContext(nameof(DeleteAsync_MissingKey_ReturnsNull));
    var repo = new Repository<Guid, User>(ctx);

    var result = await repo.DeleteAsync(Guid.NewGuid());

    result.Should().BeNull();
}
```

---

## 19. Testing DTO Models

`DtoModelTests` tests every DTO class to make sure properties work correctly and default values are right.

```csharp
[Fact]
public void CreateReservationDto_DefaultValues_AreCorrect()
{
    var dto = new CreateReservationDto();

    dto.WalletAmountToUse.Should().Be(0);
    dto.PayCancellationFee.Should().BeFalse();
    dto.SelectedRoomIds.Should().BeNull();
    dto.PromoCodeUsed.Should().BeNull();
}
```

This catches bugs where someone accidentally changes a default value.

---

## 20. Test Isolation — Each Test is Independent

Every test must be completely independent. No test should depend on another test running first.

Rules we follow:
- Each test creates its own InMemory database with a unique name
- Each test creates its own mocks
- Each test creates its own `sut` via `CreateSut()`
- No shared mutable state between tests

---

## 21. Packages Used

| Package | Purpose |
|---|---|
| `xunit` | Test framework — `[Fact]`, test runner |
| `xunit.runner.visualstudio` | Runs tests inside Visual Studio |
| `Moq` | Creates fake implementations of interfaces |
| `FluentAssertions` | Readable assertion syntax |
| `MockQueryable.Moq` | Makes `IQueryable` work with Moq for EF async queries |
| `Microsoft.EntityFrameworkCore.InMemory` | In-memory database for tests |
| `Microsoft.AspNetCore.Mvc.Testing` | Boots real app for integration tests |
| `coverlet.collector` | Measures code coverage |
| `Microsoft.NET.Test.Sdk` | Required by all .NET test projects |

---

## 22. How to Run Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run a specific test class
dotnet test --filter "FullyQualifiedName~AuthServiceTests"

# Run a specific test method
dotnet test --filter "FullyQualifiedName~RegisterGuestAsync_ValidDto_ReturnsAuthResponseDto"
```

---

## 23. Test File Structure

```
HotelBookingAppWebApi.Tests/
├── Services/
│   ├── AuthServiceTests.cs
│   ├── ReservationServiceTests.cs
│   ├── WalletServiceTests.cs
│   ├── HotelServiceTests.cs
│   ├── AmenityServiceTests.cs
│   ├── PromoCodeServiceTests.cs
│   ├── ReviewServiceTests.cs
│   ├── UserServiceTests.cs
│   ├── RoomServiceTests.cs
│   ├── RoomTypeServiceTests.cs
│   ├── InventoryServiceTests.cs
│   ├── TransactionServiceTests.cs
│   ├── AuditLogServiceTests.cs
│   ├── LogServiceTests.cs
│   ├── DashboardServiceTests.cs
│   ├── SupportRequestServiceTests.cs
│   ├── AmenityRequestServiceTests.cs
│   ├── SuperAdminRevenueServiceTests.cs
│   ├── CoverageGapTests.cs  (fills gaps found in coverage report)
│   ├── CoverageGapTests2.cs
│   ├── CoverageGapTests3.cs
│   ├── CoverageGapTests4.cs
│   ├── CoverageGapTests5.cs
│   └── BackgroundServices/
│       ├── NoShowAutoCancelServiceTests.cs
│       ├── HotelDeactivationRefundServiceTests.cs
│       ├── ReservationCleanupServiceTests.cs
│       └── InventoryRestoreHelperTests.cs
├── Controllers/
│   └── Admin/
│       └── AdminReviewControllerTests.cs
├── Repository/
│   └── RepositoryTests.cs
├── Models/
│   └── DtoModelTests.cs
├── Contexts/
│   └── HotelBookingContextTests.cs
├── Exceptions/
│   └── Middleware/
│       └── GlobalExceptionMiddlewareTests.cs
└── ProgramTests.cs
```

---

## 24. Common Patterns Quick Reference

### Mock a repo that returns a list
```csharp
var items = new List<Hotel> { hotel }.AsQueryable().BuildMock();
_hotelRepoMock.Setup(r => r.GetQueryable()).Returns(items);
```

### Mock a repo that returns a single entity
```csharp
_hotelRepoMock.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(hotel);
```

### Mock a repo that returns null (not found)
```csharp
_hotelRepoMock.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((Hotel?)null);
```

### Mock AddAsync to return the same object
```csharp
_hotelRepoMock.Setup(r => r.AddAsync(It.IsAny<Hotel>())).ReturnsAsync((Hotel h) => h);
```

### Mock a method that throws
```csharp
_userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).ThrowsAsync(new Exception("DB error"));
```

### Assert exception with message
```csharp
await act.Should().ThrowAsync<ConflictException>().WithMessage("*already registered*");
```

### Assert method was called N times
```csharp
_unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
_unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
```

### Assert collection size
```csharp
result.Should().HaveCount(2);
result.Should().BeEmpty();
```

### Assert object property
```csharp
result.Token.Should().Be("jwt-token");
result.Balance.Should().Be(300m);
hotel.IsActive.Should().BeFalse();
```
