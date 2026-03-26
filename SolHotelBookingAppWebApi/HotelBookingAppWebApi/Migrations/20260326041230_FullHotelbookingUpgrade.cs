using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HotelBookingAppWebApi.Migrations
{
    /// <inheritdoc />
    public partial class FullHotelbookingUpgrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Amenities",
                columns: table => new
                {
                    AmenityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IconName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Amenities", x => x.AmenityId);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    CityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StateName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PinCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.CityId);
                });

            migrationBuilder.CreateTable(
                name: "Hotels",
                columns: table => new
                {
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsBlockedBySuperAdmin = table.Column<bool>(type: "bit", nullable: false),
                    UpiId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GstPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hotels", x => x.HotelId);
                });

            migrationBuilder.CreateTable(
                name: "RoomTypes",
                columns: table => new
                {
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxOccupancy = table.Column<int>(type: "int", nullable: false),
                    Amenities = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomTypes", x => x.RoomTypeId);
                    table.ForeignKey(
                        name: "FK_RoomTypes_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "Hotels",
                        principalColumn: "HotelId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Password = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    PasswordSaltValue = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "Hotels",
                        principalColumn: "HotelId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Floor = table.Column<int>(type: "int", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.RoomId);
                    table.ForeignKey(
                        name: "FK_Rooms_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "Hotels",
                        principalColumn: "HotelId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Rooms_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalTable: "RoomTypes",
                        principalColumn: "RoomTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoomTypeAmenities",
                columns: table => new
                {
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmenityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomTypeAmenities", x => new { x.RoomTypeId, x.AmenityId });
                    table.ForeignKey(
                        name: "FK_RoomTypeAmenities_Amenities_AmenityId",
                        column: x => x.AmenityId,
                        principalTable: "Amenities",
                        principalColumn: "AmenityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomTypeAmenities_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalTable: "RoomTypes",
                        principalColumn: "RoomTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomTypeInventories",
                columns: table => new
                {
                    RoomTypeInventoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalInventory = table.Column<int>(type: "int", nullable: false),
                    ReservedInventory = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomTypeInventories", x => x.RoomTypeInventoryId);
                    table.ForeignKey(
                        name: "FK_RoomTypeInventories_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalTable: "RoomTypes",
                        principalColumn: "RoomTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomTypeRates",
                columns: table => new
                {
                    RoomTypeRateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomTypeRates", x => x.RoomTypeRateId);
                    table.ForeignKey(
                        name: "FK_RoomTypeRates_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalTable: "RoomTypes",
                        principalColumn: "RoomTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AmenityRequests",
                columns: table => new
                {
                    AmenityRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminHotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmenityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IconName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SuperAdminNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmenityRequests", x => x.AmenityRequestId);
                    table.ForeignKey(
                        name: "FK_AmenityRequests_Users_RequestedByAdminId",
                        column: x => x.RequestedByAdminId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    AuditLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Changes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.AuditLogId);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    LogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExceptionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Controller = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_Logs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckInDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckOutDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsCheckedIn = table.Column<bool>(type: "bit", nullable: false),
                    CancelledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiryTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GstPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    GstAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    WalletAmountUsed = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PromoCodeUsed = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FinalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.ReservationId);
                    table.ForeignKey(
                        name: "FK_Reservations_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "Hotels",
                        principalColumn: "HotelId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reservations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserProfileDetails",
                columns: table => new
                {
                    UserDetailsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Pincode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProfileImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfileDetails", x => x.UserDetailsId);
                    table.ForeignKey(
                        name: "FK_UserProfileDetails_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Wallets",
                columns: table => new
                {
                    WalletId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wallets", x => x.WalletId);
                    table.ForeignKey(
                        name: "FK_Wallets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromoCodes",
                columns: table => new
                {
                    PromoCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromoCodes", x => x.PromoCodeId);
                    table.ForeignKey(
                        name: "FK_PromoCodes_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "Hotels",
                        principalColumn: "HotelId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromoCodes_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "ReservationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromoCodes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefundRequests",
                columns: table => new
                {
                    RefundRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AdminResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefundPaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefundTransactionRef = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundRequests", x => x.RefundRequestId);
                    table.ForeignKey(
                        name: "FK_RefundRequests_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "ReservationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RefundRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReservationRooms",
                columns: table => new
                {
                    ReservationRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PricePerNight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationRooms", x => x.ReservationRoomId);
                    table.ForeignKey(
                        name: "FK_ReservationRooms_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "ReservationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReservationRooms_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalTable: "RoomTypes",
                        principalColumn: "RoomTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationRooms_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    ReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.ReviewId);
                    table.ForeignKey(
                        name: "FK_Reviews_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "Hotels",
                        principalColumn: "HotelId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "ReservationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SuperAdminRevenues",
                columns: table => new
                {
                    SuperAdminRevenueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SuperAdminUpiId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuperAdminRevenues", x => x.SuperAdminRevenueId);
                    table.ForeignKey(
                        name: "FK_SuperAdminRevenues_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "Hotels",
                        principalColumn: "HotelId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SuperAdminRevenues_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "ReservationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    WalletUsed = table.Column<bool>(type: "bit", nullable: false),
                    WalletAmountUsed = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_Transactions_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "ReservationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    WalletTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WalletId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.WalletTransactionId);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "WalletId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Amenities",
                columns: new[] { "AmenityId", "Category", "IconName", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "Tech", "wifi", true, "WiFi" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "Room", "ac_unit", true, "AC" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "Room", "tv", true, "TV" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "Services", "pool", true, "Pool" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "Services", "local_parking", true, "Parking" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "Services", "fitness_center", true, "Gym" },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "Food", "restaurant", true, "Restaurant" },
                    { new Guid("10000000-0000-0000-0000-000000000008"), "Food", "local_bar", true, "Bar" },
                    { new Guid("10000000-0000-0000-0000-000000000009"), "Services", "room_service", true, "Room Service" },
                    { new Guid("10000000-0000-0000-0000-000000000010"), "Services", "local_laundry_service", true, "Laundry" },
                    { new Guid("10000000-0000-0000-0000-000000000011"), "Services", "spa", true, "Spa" },
                    { new Guid("10000000-0000-0000-0000-000000000012"), "Food", "free_breakfast", true, "Breakfast Included" },
                    { new Guid("10000000-0000-0000-0000-000000000013"), "Room", "lock", true, "Safe" },
                    { new Guid("10000000-0000-0000-0000-000000000014"), "Room", "liquor", true, "Mini Bar" },
                    { new Guid("10000000-0000-0000-0000-000000000015"), "Room", "balcony", true, "Balcony" },
                    { new Guid("10000000-0000-0000-0000-000000000016"), "Room", "beach_access", true, "Sea View" },
                    { new Guid("10000000-0000-0000-0000-000000000017"), "Room", "landscape", true, "Mountain View" },
                    { new Guid("10000000-0000-0000-0000-000000000018"), "Services", "accessible", true, "Wheelchair Access" },
                    { new Guid("10000000-0000-0000-0000-000000000019"), "Services", "pets", true, "Pet Friendly" },
                    { new Guid("10000000-0000-0000-0000-000000000020"), "Services", "child_friendly", true, "Kids Area" },
                    { new Guid("10000000-0000-0000-0000-000000000021"), "Services", "meeting_room", true, "Conference Room" },
                    { new Guid("10000000-0000-0000-0000-000000000022"), "Services", "airport_shuttle", true, "Airport Shuttle" },
                    { new Guid("10000000-0000-0000-0000-000000000023"), "Services", "videocam", true, "CCTV" },
                    { new Guid("10000000-0000-0000-0000-000000000024"), "Services", "support_agent", true, "24h Reception" },
                    { new Guid("10000000-0000-0000-0000-000000000025"), "Room", "whatshot", true, "Heating" },
                    { new Guid("10000000-0000-0000-0000-000000000026"), "Services", "elevator", true, "Elevator" },
                    { new Guid("10000000-0000-0000-0000-000000000027"), "Bathroom", "dry", true, "Hair Dryer" },
                    { new Guid("10000000-0000-0000-0000-000000000028"), "Room", "iron", true, "Iron" },
                    { new Guid("10000000-0000-0000-0000-000000000029"), "Room", "coffee", true, "Coffee Maker" },
                    { new Guid("10000000-0000-0000-0000-000000000030"), "Bathroom", "bathtub", true, "Bathtub" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Amenities_Name",
                table: "Amenities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AmenityRequests_RequestedByAdminId",
                table: "AmenityRequests",
                column: "RequestedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_CityName",
                table: "Cities",
                column: "CityName");

            migrationBuilder.CreateIndex(
                name: "IX_Hotels_City",
                table: "Hotels",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_Logs_UserId",
                table: "Logs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodes_Code",
                table: "PromoCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodes_HotelId",
                table: "PromoCodes",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodes_ReservationId",
                table: "PromoCodes",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodes_UserId",
                table: "PromoCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequests_ReservationId",
                table: "RefundRequests",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequests_UserId",
                table: "RefundRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationRooms_ReservationId",
                table: "ReservationRooms",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationRooms_RoomId",
                table: "ReservationRooms",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationRooms_RoomTypeId",
                table: "ReservationRooms",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_HotelId",
                table: "Reservations",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ReservationCode",
                table: "Reservations",
                column: "ReservationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_UserId",
                table: "Reservations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_HotelId",
                table: "Reviews",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ReservationId",
                table: "Reviews",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId_ReservationId",
                table: "Reviews",
                columns: new[] { "UserId", "ReservationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_HotelId_RoomNumber",
                table: "Rooms",
                columns: new[] { "HotelId", "RoomNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_RoomTypeId",
                table: "Rooms",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypeAmenities_AmenityId",
                table: "RoomTypeAmenities",
                column: "AmenityId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypeInventories_RoomTypeId_Date",
                table: "RoomTypeInventories",
                columns: new[] { "RoomTypeId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypeRates_RoomTypeId_StartDate_EndDate",
                table: "RoomTypeRates",
                columns: new[] { "RoomTypeId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypes_HotelId",
                table: "RoomTypes",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_SuperAdminRevenues_HotelId",
                table: "SuperAdminRevenues",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_SuperAdminRevenues_ReservationId",
                table: "SuperAdminRevenues",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ReservationId",
                table: "Transactions",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfileDetails_UserId",
                table: "UserProfileDetails",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_HotelId",
                table: "Users",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_UserId",
                table: "Wallets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_WalletId",
                table: "WalletTransactions",
                column: "WalletId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AmenityRequests");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "PromoCodes");

            migrationBuilder.DropTable(
                name: "RefundRequests");

            migrationBuilder.DropTable(
                name: "ReservationRooms");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "RoomTypeAmenities");

            migrationBuilder.DropTable(
                name: "RoomTypeInventories");

            migrationBuilder.DropTable(
                name: "RoomTypeRates");

            migrationBuilder.DropTable(
                name: "SuperAdminRevenues");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "UserProfileDetails");

            migrationBuilder.DropTable(
                name: "WalletTransactions");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "Amenities");

            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropTable(
                name: "Wallets");

            migrationBuilder.DropTable(
                name: "RoomTypes");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Hotels");
        }
    }
}
