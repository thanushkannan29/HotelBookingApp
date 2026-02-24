using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Repository;
using HotelBookingAppWebApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;


var builder = WebApplication.CreateBuilder(args);



// Controllers


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();




// Swagger + JWT Support


builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hotel Booking API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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





// Database Context


builder.Services.AddDbContext<HotelBookingContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Developer"));
});



// CORS


builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});




// Repositories


builder.Services.AddScoped<IRepository<Guid, User>, Repository<Guid, User>>();
builder.Services.AddScoped<IRepository<Guid, Hotel>, Repository<Guid, Hotel>>();
builder.Services.AddScoped<IRepository<Guid, Room>, Repository<Guid, Room>>();
builder.Services.AddScoped<IRepository<Guid, Reservation>, Repository<Guid, Reservation>>();
builder.Services.AddScoped<IRepository<Guid, ReservationRoom>, Repository<Guid, ReservationRoom>>();
builder.Services.AddScoped<IRepository<Guid, Transaction>, Repository<Guid, Transaction>>();
builder.Services.AddScoped<IRepository<Guid, Review>, Repository<Guid, Review>>();
builder.Services.AddScoped<IRepository<Guid, RoomType>, Repository<Guid, RoomType>>();
builder.Services.AddScoped<IRepository<Guid, RoomTypeRate>, Repository<Guid, RoomTypeRate>>();



// Services


builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IHotelService, HotelService>();
builder.Services.AddScoped<IRoomTypeService, RoomTypeService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRoomService, RoomService>();





// JWT Authentication


string key = builder.Configuration["Keys:Jwt"]
    ?? throw new InvalidOperationException("JWT Key not found.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        });

builder.Services.AddAuthorization();


// Build App


var app = builder.Build();




// Middleware Pipeline


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
