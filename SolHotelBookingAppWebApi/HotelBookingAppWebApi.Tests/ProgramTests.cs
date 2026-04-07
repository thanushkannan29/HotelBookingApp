using FluentAssertions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotelBookingAppWebApi.Tests;

/// <summary>
/// Boots the real application via WebApplicationFactory and verifies
/// that every service, repository, and background service is correctly
/// registered in the DI container (AAA pattern).
/// </summary>
public class ProgramTests : IClassFixture<ProgramTests.AppFactory>
{
    // ── WebApplicationFactory ─────────────────────────────────────────────────

    public class AppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Arrange: supply all required config so the app starts without a real DB or secrets
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Keys:Jwt"] = "test-secret-key-that-is-long-enough-32chars",
                    ["ConnectionStrings:Developer"] =
                        "Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=True;",
                    ["IpRateLimiting:EnableEndpointRateLimiting"] = "false",
                    ["IpRateLimiting:StackBlockedRequests"] = "false",
                    ["IpRateLimiting:RealIpHeader"] = "X-Real-IP",
                    ["IpRateLimiting:ClientIdHeader"] = "X-ClientId",
                    ["IpRateLimiting:HttpStatusCode"] = "429",
                    ["IpRateLimiting:GeneralRules:0:Endpoint"] = "*",
                    ["IpRateLimiting:GeneralRules:0:Period"] = "1s",
                    ["IpRateLimiting:GeneralRules:0:Limit"] = "1000"
                });
            });
        }
    }

    private readonly AppFactory _factory;

    public ProgramTests(AppFactory factory) => _factory = factory;

    // ── Repository & UnitOfWork ───────────────────────────────────────────────

    [Fact]
    public void DI_IRepository_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act — open generics can't be resolved directly; verify via a service that depends on IRepository
        var hotelSvc = scope.ServiceProvider.GetService<IHotelService>();

        // Assert
        hotelSvc.Should().NotBeNull();
    }

    [Fact]
    public void DI_IUnitOfWork_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var svc = scope.ServiceProvider.GetService<IUnitOfWork>();

        // Assert
        svc.Should().NotBeNull();
    }

    // ── Application Services ──────────────────────────────────────────────────

    [Fact]
    public void DI_IPasswordService_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var svc = scope.ServiceProvider.GetService<IPasswordService>();

        // Assert
        svc.Should().NotBeNull();
    }

    [Fact]
    public void DI_ITokenService_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var svc = scope.ServiceProvider.GetService<ITokenService>();

        // Assert
        svc.Should().NotBeNull();
    }

    [Fact]
    public void DI_IAuthService_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var svc = scope.ServiceProvider.GetService<IAuthService>();

        // Assert
        svc.Should().NotBeNull();
    }

    [Fact]
    public void DI_IUserService_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var svc = scope.ServiceProvider.GetService<IUserService>();

        // Assert
        svc.Should().NotBeNull();
    }

    [Fact]
    public void DI_IHotelService_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var svc = scope.ServiceProvider.GetService<IHotelService>();

        // Assert
        svc.Should().NotBeNull();
    }

    [Fact]
    public void DI_IRoomTypeService_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var svc = scope.ServiceProvider.GetService<IRoomTypeService>();

        // Assert
        svc.Should().NotBeNull();
    }

    [Fact]
    public void DI_IRoomService_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var svc = scope.ServiceProvider.GetService<IRoomService>();

        // Assert
        svc.Should().NotBeNull();
    }

    [Fact]
    public void DI_IInventoryService_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var svc = scope.ServiceProvider.GetService<IInventoryService>();

        // Assert
        svc.Should().NotBeNull();
    }

    [Fact]
    public void DI_IReservationService_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var svc = scope.ServiceProvider.GetService<IReservationService>();

        // Assert
        svc.Should().NotBeNull();
    }

    [Fact]
    public void DI_ITransactionService_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var svc = scope.ServiceProvider.GetService<ITransactionService>();

        // Assert
        svc.Should().NotBeNull();
    }

    [Fact]
    public void DI_IReviewService_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var svc = scope.ServiceProvider.GetService<IReviewService>();

        // Assert
        svc.Should().NotBeNull();
    }

    [Fact]
    public void DI_ILogService_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var svc = scope.ServiceProvider.GetService<ILogService>();

        // Assert
        svc.Should().NotBeNull();
    }

    [Fact]
    public void DI_IAuditLogService_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var svc = scope.ServiceProvider.GetService<IAuditLogService>();

        // Assert
        svc.Should().NotBeNull();
    }

    [Fact]
    public void DI_IDashboardService_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var svc = scope.ServiceProvider.GetService<IDashboardService>();

        // Assert
        svc.Should().NotBeNull();
    }

    [Fact]
    public void DI_IAmenityService_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var svc = scope.ServiceProvider.GetService<IAmenityService>();

        // Assert
        svc.Should().NotBeNull();
    }

    [Fact]
    public void DI_IWalletService_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var svc = scope.ServiceProvider.GetService<IWalletService>();

        // Assert
        svc.Should().NotBeNull();
    }

    [Fact]
    public void DI_IPromoCodeService_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var svc = scope.ServiceProvider.GetService<IPromoCodeService>();

        // Assert
        svc.Should().NotBeNull();
    }

    [Fact]
    public void DI_IAmenityRequestService_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var svc = scope.ServiceProvider.GetService<IAmenityRequestService>();

        // Assert
        svc.Should().NotBeNull();
    }

    [Fact]
    public void DI_ISuperAdminRevenueService_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var svc = scope.ServiceProvider.GetService<ISuperAdminRevenueService>();

        // Assert
        svc.Should().NotBeNull();
    }

    [Fact]
    public void DI_ISupportRequestService_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var svc = scope.ServiceProvider.GetService<ISupportRequestService>();

        // Assert
        svc.Should().NotBeNull();
    }

    // ── Background Services ───────────────────────────────────────────────────

    [Fact]
    public void DI_BackgroundServices_AllThreeRegistered()
    {
        // Arrange
        var hostedServices = _factory.Services.GetServices<IHostedService>().ToList();

        // Act & Assert
        hostedServices.Should().Contain(s =>
            s.GetType().Name == "ReservationCleanupService");
        hostedServices.Should().Contain(s =>
            s.GetType().Name == "HotelDeactivationRefundService");
        hostedServices.Should().Contain(s =>
            s.GetType().Name == "NoShowAutoCancelService");
    }

    // ── HTTP Pipeline ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Pipeline_UnknownRoute_Returns404()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/does-not-exist");

        // Assert
        ((int)response.StatusCode).Should().Be(404);
    }

    [Fact]
    public async Task Pipeline_ProtectedEndpoint_WithoutToken_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        var response = await client.GetAsync("/api/dashboard/admin");

        // Assert
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Pipeline_SwaggerEndpoint_InDevelopment_Returns200()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    // ── Missing JWT Key ───────────────────────────────────────────────────────

    [Fact]
    public void RegisterAuthentication_WithValidJwtKey_AuthenticationIsConfigured()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act — IAuthService depends on ITokenService which requires the JWT key
        var authSvc = scope.ServiceProvider.GetService<IAuthService>();

        // Assert — if JWT key was missing, the app would have thrown during startup
        authSvc.Should().NotBeNull();
    }
}
