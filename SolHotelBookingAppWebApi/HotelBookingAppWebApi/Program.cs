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
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── CONTROLLERS ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── RATE LIMITING ──────────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// ── SWAGGER + JWT ──────────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hotel Booking API",
        Version = "v1",
        Description = "Complete Hotel Booking System — Guest, Admin, SuperAdmin roles"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── DATABASE ───────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<HotelBookingContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Developer"),
        sqlOptions => sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
    ));

// ── CORS ───────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ── GENERIC REPOSITORY ─────────────────────────────────────────────────────────
builder.Services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

// ── UNIT OF WORK ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ── APPLICATION SERVICES ──────────────────────────────────────────────────────
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

// ── BACKGROUND SERVICES ────────────────────────────────────────────────────────
builder.Services.AddHostedService<ReservationCleanupService>();          // cancels expired pending reservations
builder.Services.AddHostedService<HotelDeactivationRefundService>();     // auto-refunds when hotel deactivated
builder.Services.AddHostedService<NoShowAutoCancelService>();             // marks no-shows
// SuperAdminRevenueBackgroundService removed — commission recorded inline in CompleteReservationAsync
// PromoCodeGenerationService removed — promo generated inline in CompleteReservationAsync

// ── JWT AUTHENTICATION ─────────────────────────────────────────────────────────
string jwtKey = builder.Configuration["Keys:Jwt"]
    ?? throw new InvalidOperationException("JWT Key not found in configuration.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        });

builder.Services.AddAuthorization();

// ── BUILD ──────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseIpRateLimiting();
app.UseRouting();

// Global exception handler must be BEFORE auth so it catches all exceptions
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

<<<<<<< Updated upstream
app.Run();
=======
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
        {
            // MapInboundClaims = false keeps claim names exactly as written in the token.
            // We write short names ("nameid", "unique_name", "role") so they match
            // what the Angular frontend reads via jwtDecode.
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                NameClaimType = "nameid",
                RoleClaimType = "role"
            };
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
>>>>>>> Stashed changes
