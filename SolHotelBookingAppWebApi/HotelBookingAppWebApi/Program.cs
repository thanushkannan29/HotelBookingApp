using AspNetCoreRateLimit;
using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions.Middleware;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Repository;
using HotelBookingAppWebApi.Services;
using HotelBookingAppWebApi.Services.BackgroundServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

RegisterControllers(builder.Services);
RegisterRateLimiting(builder.Services, builder.Configuration);
RegisterSwagger(builder.Services);
RegisterDatabase(builder.Services, builder.Configuration);
RegisterCors(builder.Services);
RegisterRepositories(builder.Services);
RegisterApplicationServices(builder.Services);
RegisterBackgroundServices(builder.Services);
RegisterAuthentication(builder.Services, builder.Configuration);

var app = builder.Build();

ConfigurePipeline(app);

app.Run();

// ── SERVICE REGISTRATION HELPERS ─────────────────────────────────────────────

static void RegisterControllers(IServiceCollection services)
{
    services.AddControllers();
    services.AddEndpointsApiExplorer();
}

static void RegisterRateLimiting(IServiceCollection services, IConfiguration config)
{
    services.AddMemoryCache();
    services.Configure<IpRateLimitOptions>(config.GetSection("IpRateLimiting"));
    services.AddInMemoryRateLimiting();
    services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
}

static void RegisterSwagger(IServiceCollection services)
{
    services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Hotel Booking API",
            Version = "v1",
            Description = "Complete Hotel Booking System — Guest, Admin, SuperAdmin roles"
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter: Bearer {your JWT token}"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });
}

static void RegisterDatabase(IServiceCollection services, IConfiguration config)
{
    services.AddDbContext<HotelBookingContext>(options =>
        options.UseSqlServer(
            config.GetConnectionString("Developer"),
            sqlOptions => sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
        ));
}

static void RegisterCors(IServiceCollection services)
{
    services.AddCors(options =>
    {
        options.AddPolicy("AngularClient", policy =>
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials());
    });
}

static void RegisterRepositories(IServiceCollection services)
{
    services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
    services.AddScoped<IUnitOfWork, UnitOfWork>();
}

static void RegisterApplicationServices(IServiceCollection services)
{
    services.AddScoped<IPasswordService, PasswordService>();
    services.AddScoped<ITokenService, TokenService>();
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<IUserService, UserService>();
    services.AddScoped<IHotelService, HotelService>();
    services.AddScoped<IRoomTypeService, RoomTypeService>();
    services.AddScoped<IRoomService, RoomService>();
    services.AddScoped<IInventoryService, InventoryService>();
    services.AddScoped<IReservationService, ReservationService>();
    services.AddScoped<ITransactionService, TransactionService>();
    services.AddScoped<IReviewService, ReviewService>();
    services.AddScoped<ILogService, LogService>();
    services.AddScoped<IAuditLogService, AuditLogService>();
    services.AddScoped<IDashboardService, DashboardService>();
    services.AddScoped<IAmenityService, AmenityService>();
    services.AddScoped<IWalletService, WalletService>();
    services.AddScoped<IPromoCodeService, PromoCodeService>();
    services.AddScoped<IAmenityRequestService, AmenityRequestService>();
    services.AddScoped<ISuperAdminRevenueService, SuperAdminRevenueService>();
    services.AddScoped<ISupportRequestService, SupportRequestService>();
}

static void RegisterBackgroundServices(IServiceCollection services)
{
    services.AddHostedService<ReservationCleanupService>();       // cancels expired pending reservations
    services.AddHostedService<HotelDeactivationRefundService>();  // auto-refunds when hotel deactivated
    services.AddHostedService<NoShowAutoCancelService>();          // marks no-shows after checkout
}

static void RegisterAuthentication(IServiceCollection services, IConfiguration config)
{
    var jwtKey = config["Keys:Jwt"]
        ?? throw new InvalidOperationException("JWT Key not found in configuration.");

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            });

    services.AddAuthorization();
}

// ── PIPELINE CONFIGURATION ────────────────────────────────────────────────────

static void ConfigurePipeline(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Global exception handler first — catches everything including auth errors
    app.UseMiddleware<GlobalExceptionMiddleware>();

    app.UseCors("AngularClient");
    app.UseIpRateLimiting();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
}
