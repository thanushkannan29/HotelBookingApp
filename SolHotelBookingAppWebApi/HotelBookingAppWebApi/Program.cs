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
builder.Services.AddScoped<ISupportRequestService, SupportRequestService>();

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
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = "role",
            NameClaimType = "unique_name"
        };
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

app.Run();

