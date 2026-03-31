using FluentAssertions;
using HotelBookingAppWebApi.Controllers;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Amenity;
using HotelBookingAppWebApi.Models.DTOs.AmenityRequest;
using HotelBookingAppWebApi.Models.DTOs.AuditLog;
using HotelBookingAppWebApi.Models.DTOs.Auth;
using HotelBookingAppWebApi.Models.DTOs.Dashboard;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Public;
using HotelBookingAppWebApi.Models.DTOs.Hotel.SuperAdmin;
using HotelBookingAppWebApi.Models.DTOs.Inventory;
using HotelBookingAppWebApi.Models.DTOs.Log;
using HotelBookingAppWebApi.Models.DTOs.PromoCode;
using HotelBookingAppWebApi.Models.DTOs.Reservation;
using HotelBookingAppWebApi.Models.DTOs.Revenue;
using HotelBookingAppWebApi.Models.DTOs.Review;
using HotelBookingAppWebApi.Models.DTOs.Room;
using HotelBookingAppWebApi.Models.DTOs.RoomType;
using HotelBookingAppWebApi.Models.DTOs.SupportRequest;
using HotelBookingAppWebApi.Models.DTOs.Transactions;
using HotelBookingAppWebApi.Models.DTOs.UserDetails;
using HotelBookingAppWebApi.Models.DTOs.Wallet;

namespace HotelBookingAppWebApi.Tests.Models;

public class DtoModelTests
{
    // ── Review DTOs ───────────────────────────────────────────────────────────

    [Fact]
    public void CreateReviewDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new CreateReviewDto();

        // Assert
        dto.Comment.Should().BeEmpty();
        dto.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void CreateReviewDto_SetProperties_RetainsValues()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();

        // Act
        var dto = new CreateReviewDto
        {
            HotelId = hotelId,
            ReservationId = reservationId,
            Rating = 4.5m,
            Comment = "Great!",
            ImageUrl = "img.jpg"
        };

        // Assert
        dto.HotelId.Should().Be(hotelId);
        dto.ReservationId.Should().Be(reservationId);
        dto.Rating.Should().Be(4.5m);
        dto.Comment.Should().Be("Great!");
        dto.ImageUrl.Should().Be("img.jpg");
    }


    // ── Amenity DTOs ──────────────────────────────────────────────────────────

    [Fact]
    public void AmenityResponseDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var dto = new AmenityResponseDto
        {
            AmenityId = id,
            Name = "Pool",
            Category = "Recreation",
            IconName = "pool-icon",
            IsActive = true
        };

        // Assert
        dto.AmenityId.Should().Be(id);
        dto.Name.Should().Be("Pool");
        dto.Category.Should().Be("Recreation");
        dto.IconName.Should().Be("pool-icon");
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public void AmenityResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new AmenityResponseDto();

        // Assert
        dto.Name.Should().BeEmpty();
        dto.Category.Should().BeEmpty();
        dto.IconName.Should().BeNull();
        dto.IsActive.Should().BeFalse();
    }

    [Fact]
    public void CreateAmenityDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new CreateAmenityDto { Name = "Gym", Category = "Fitness", IconName = "gym" };

        // Assert
        dto.Name.Should().Be("Gym");
        dto.Category.Should().Be("Fitness");
        dto.IconName.Should().Be("gym");
    }

    [Fact]
    public void CreateAmenityDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new CreateAmenityDto();

        // Assert
        dto.Name.Should().BeEmpty();
        dto.Category.Should().BeEmpty();
        dto.IconName.Should().BeNull();
    }

    [Fact]
    public void UpdateAmenityDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var dto = new UpdateAmenityDto { AmenityId = id, Name = "Spa", Category = "Wellness", IconName = "spa", IsActive = true };

        // Assert
        dto.AmenityId.Should().Be(id);
        dto.Name.Should().Be("Spa");
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public void PagedAmenityResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PagedAmenityResponseDto();

        // Assert
        dto.TotalCount.Should().Be(0);
        dto.Amenities.Should().BeEmpty();
    }

    [Fact]
    public void PagedAmenityResponseDto_SetProperties_RetainsValues()
    {
        // Arrange
        var items = new List<AmenityResponseDto> { new() { Name = "WiFi" } };

        // Act
        var dto = new PagedAmenityResponseDto { TotalCount = 1, Amenities = items };

        // Assert
        dto.TotalCount.Should().Be(1);
        dto.Amenities.Should().HaveCount(1);
    }


    // ── AmenityRequest DTOs ───────────────────────────────────────────────────

    [Fact]
    public void AmenityRequestResponseDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var dto = new AmenityRequestResponseDto
        {
            AmenityRequestId = id,
            AmenityName = "Sauna",
            Category = "Wellness",
            IconName = "sauna",
            Status = "Pending",
            SuperAdminNote = "Under review",
            AdminName = "Admin1",
            HotelName = "Grand Hotel",
            CreatedAt = now,
            ProcessedAt = now
        };

        // Assert
        dto.AmenityRequestId.Should().Be(id);
        dto.AmenityName.Should().Be("Sauna");
        dto.Status.Should().Be("Pending");
        dto.SuperAdminNote.Should().Be("Under review");
        dto.ProcessedAt.Should().Be(now);
    }

    [Fact]
    public void AmenityRequestResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new AmenityRequestResponseDto();

        // Assert
        dto.AmenityName.Should().BeEmpty();
        dto.IconName.Should().BeNull();
        dto.SuperAdminNote.Should().BeNull();
        dto.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public void CreateAmenityRequestDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new CreateAmenityRequestDto { AmenityName = "Jacuzzi", Category = "Luxury", IconName = "jacuzzi" };

        // Assert
        dto.AmenityName.Should().Be("Jacuzzi");
        dto.Category.Should().Be("Luxury");
        dto.IconName.Should().Be("jacuzzi");
    }

    [Fact]
    public void RejectAmenityRequestDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new RejectAmenityRequestDto { Note = "Not suitable" };

        // Assert
        dto.Note.Should().Be("Not suitable");
    }

    [Fact]
    public void PagedAmenityRequestResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PagedAmenityRequestResponseDto();

        // Assert
        dto.TotalCount.Should().Be(0);
        dto.Requests.Should().BeEmpty();
    }

    [Fact]
    public void PagedAmenityRequestResponseDto_SetProperties_RetainsValues()
    {
        // Arrange
        var items = new List<AmenityRequestResponseDto> { new() { AmenityName = "Pool" } };

        // Act
        var dto = new PagedAmenityRequestResponseDto { TotalCount = 1, Requests = items };

        // Assert
        dto.TotalCount.Should().Be(1);
        dto.Requests.Should().HaveCount(1);
    }


    // ── AuditLog DTOs ─────────────────────────────────────────────────────────

    [Fact]
    public void AuditLogResponseDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var dto = new AuditLogResponseDto
        {
            AuditLogId = id,
            UserId = userId,
            Action = "Create",
            EntityName = "Hotel",
            EntityId = entityId,
            Changes = "{}",
            CreatedAt = now
        };

        // Assert
        dto.AuditLogId.Should().Be(id);
        dto.UserId.Should().Be(userId);
        dto.Action.Should().Be("Create");
        dto.EntityId.Should().Be(entityId);
        dto.Changes.Should().Be("{}");
    }

    [Fact]
    public void AuditLogResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new AuditLogResponseDto();

        // Assert
        dto.UserId.Should().BeNull();
        dto.EntityId.Should().BeNull();
        dto.Action.Should().BeEmpty();
    }

    [Fact]
    public void PagedAuditLogResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PagedAuditLogResponseDto();

        // Assert
        dto.TotalCount.Should().Be(0);
        dto.Logs.Should().BeEmpty();
    }

    [Fact]
    public void PagedAuditLogResponseDto_SetProperties_RetainsValues()
    {
        // Arrange
        var items = new List<AuditLogResponseDto> { new() { Action = "Delete" } };

        // Act
        var dto = new PagedAuditLogResponseDto { TotalCount = 1, Logs = items };

        // Assert
        dto.TotalCount.Should().Be(1);
        dto.Logs.Should().HaveCount(1);
    }

    // ── Auth DTOs ─────────────────────────────────────────────────────────────

    [Fact]
    public void AuthResponseDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new AuthResponseDto { Token = "jwt-token-value" };

        // Assert
        dto.Token.Should().Be("jwt-token-value");
    }

    [Fact]
    public void AuthResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new AuthResponseDto();

        // Assert
        dto.Token.Should().BeEmpty();
    }

    [Fact]
    public void LoginDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new LoginDto { Email = "user@example.com", Password = "pass123" };

        // Assert
        dto.Email.Should().Be("user@example.com");
        dto.Password.Should().Be("pass123");
    }

    [Fact]
    public void RegisterUserDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new RegisterUserDto { Name = "Alice", Email = "alice@example.com", Password = "secret" };

        // Assert
        dto.Name.Should().Be("Alice");
        dto.Email.Should().Be("alice@example.com");
        dto.Password.Should().Be("secret");
    }

    [Fact]
    public void RegisterHotelAdminDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new RegisterHotelAdminDto
        {
            Name = "Bob",
            Email = "bob@hotel.com",
            Password = "pass",
            HotelName = "Grand",
            Address = "123 Main St",
            City = "Mumbai",
            State = "MH",
            Description = "Luxury hotel",
            ContactNumber = "9876543210"
        };

        // Assert
        dto.Name.Should().Be("Bob");
        dto.HotelName.Should().Be("Grand");
        dto.City.Should().Be("Mumbai");
        dto.ContactNumber.Should().Be("9876543210");
    }

    [Fact]
    public void TokenPayloadDto_SetProperties_RetainsValues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();

        // Act
        var dto = new TokenPayloadDto { UserId = userId, UserName = "Admin", Role = "Admin", HotelId = hotelId };

        // Assert
        dto.UserId.Should().Be(userId);
        dto.UserName.Should().Be("Admin");
        dto.Role.Should().Be("Admin");
        dto.HotelId.Should().Be(hotelId);
    }

    [Fact]
    public void TokenPayloadDto_HotelId_CanBeNull()
    {
        // Arrange & Act
        var dto = new TokenPayloadDto { HotelId = null };

        // Assert
        dto.HotelId.Should().BeNull();
    }


    // ── Dashboard DTOs ────────────────────────────────────────────────────────

    [Fact]
    public void AdminDashboardDto_SetProperties_RetainsValues()
    {
        // Arrange
        var hotelId = Guid.NewGuid();

        // Act
        var dto = new AdminDashboardDto
        {
            HotelId = hotelId,
            HotelName = "Grand Hotel",
            HotelImageUrl = "img.jpg",
            IsActive = true,
            IsBlockedBySuperAdmin = false,
            TotalRooms = 50,
            ActiveRooms = 45,
            TotalRoomTypes = 5,
            TotalReservations = 100,
            PendingReservations = 10,
            ActiveReservations = 20,
            CompletedReservations = 60,
            CancelledReservations = 10,
            TotalRevenue = 500000m,
            TotalReviews = 80,
            AverageRating = 4.2m
        };

        // Assert
        dto.HotelId.Should().Be(hotelId);
        dto.TotalRooms.Should().Be(50);
        dto.TotalRevenue.Should().Be(500000m);
        dto.AverageRating.Should().Be(4.2m);
        dto.IsBlockedBySuperAdmin.Should().BeFalse();
    }

    [Fact]
    public void AdminDashboardDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new AdminDashboardDto();

        // Assert
        dto.HotelName.Should().BeEmpty();
        dto.HotelImageUrl.Should().BeNull();
        dto.IsActive.Should().BeFalse();
    }

    [Fact]
    public void GuestDashboardDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new GuestDashboardDto
        {
            TotalBookings = 10,
            ActiveBookings = 2,
            CompletedBookings = 7,
            CancelledBookings = 1,
            TotalSpent = 25000m
        };

        // Assert
        dto.TotalBookings.Should().Be(10);
        dto.TotalSpent.Should().Be(25000m);
    }

    [Fact]
    public void SuperAdminDashboardDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new SuperAdminDashboardDto
        {
            TotalHotels = 20,
            ActiveHotels = 18,
            BlockedHotels = 2,
            TotalUsers = 500,
            TotalReservations = 1000,
            TotalRevenue = 1000000m,
            TotalReviews = 300
        };

        // Assert
        dto.TotalHotels.Should().Be(20);
        dto.BlockedHotels.Should().Be(2);
        dto.TotalRevenue.Should().Be(1000000m);
    }


    // ── Hotel DTOs ────────────────────────────────────────────────────────────

    [Fact]
    public void UpdateHotelDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new UpdateHotelDto
        {
            Name = "Sunset Inn",
            Address = "456 Beach Rd",
            City = "Goa",
            State = "GA",
            Description = "Beachside hotel",
            ContactNumber = "9000000001",
            ImageUrl = "sunset.jpg",
            UpiId = "sunset@upi"
        };

        // Assert
        dto.Name.Should().Be("Sunset Inn");
        dto.UpiId.Should().Be("sunset@upi");
    }

    [Fact]
    public void UpdateHotelDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new UpdateHotelDto();

        // Assert
        dto.UpiId.Should().BeNull();
        dto.Name.Should().BeEmpty();
    }

    [Fact]
    public void UpdateHotelGstDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new UpdateHotelGstDto { GstPercent = 18m };

        // Assert
        dto.GstPercent.Should().Be(18m);
    }

    [Fact]
    public void HotelDetailsDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new HotelDetailsDto();

        // Assert
        dto.Amenities.Should().BeEmpty();
        dto.Reviews.Should().BeEmpty();
        dto.RoomTypes.Should().BeEmpty();
        dto.UpiId.Should().BeNull();
    }

    [Fact]
    public void HotelDetailsDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var dto = new HotelDetailsDto
        {
            HotelId = id,
            Name = "Grand",
            Address = "1 Main",
            City = "Delhi",
            State = "DL",
            Description = "Luxury",
            ImageUrl = "grand.jpg",
            ContactNumber = "9000000002",
            UpiId = "grand@upi",
            AverageRating = 4.5m,
            ReviewCount = 100,
            GstPercent = 12m
        };

        // Assert
        dto.HotelId.Should().Be(id);
        dto.AverageRating.Should().Be(4.5m);
        dto.GstPercent.Should().Be(12m);
    }

    [Fact]
    public void HotelListItemDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var dto = new HotelListItemDto
        {
            HotelId = id,
            Name = "Budget Inn",
            City = "Pune",
            ImageUrl = "budget.jpg",
            AverageRating = 3.8m,
            ReviewCount = 50,
            StartingPrice = 1500m
        };

        // Assert
        dto.HotelId.Should().Be(id);
        dto.StartingPrice.Should().Be(1500m);
    }

    [Fact]
    public void HotelListItemDto_StartingPrice_CanBeNull()
    {
        // Arrange & Act
        var dto = new HotelListItemDto { StartingPrice = null };

        // Assert
        dto.StartingPrice.Should().BeNull();
    }

    [Fact]
    public void ReviewDto_SetProperties_RetainsValues()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var dto = new ReviewDto
        {
            UserName = "Alice",
            UserProfileImageUrl = "alice.jpg",
            Rating = 5m,
            Comment = "Excellent!",
            ImageUrl = "room.jpg",
            AdminReply = "Thank you!",
            CreatedDate = now
        };

        // Assert
        dto.UserName.Should().Be("Alice");
        dto.Rating.Should().Be(5m);
        dto.AdminReply.Should().Be("Thank you!");
    }

    [Fact]
    public void ReviewDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new ReviewDto();

        // Assert
        dto.UserProfileImageUrl.Should().BeNull();
        dto.ImageUrl.Should().BeNull();
        dto.AdminReply.Should().BeNull();
    }

    [Fact]
    public void RoomAvailabilityDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var dto = new RoomAvailabilityDto
        {
            RoomTypeId = id,
            RoomTypeName = "Deluxe",
            PricePerNight = 3000m,
            AvailableRooms = 5,
            ImageUrl = "deluxe.jpg"
        };

        // Assert
        dto.RoomTypeId.Should().Be(id);
        dto.PricePerNight.Should().Be(3000m);
        dto.AvailableRooms.Should().Be(5);
    }

    [Fact]
    public void RoomTypePublicDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new RoomTypePublicDto();

        // Assert
        dto.Amenities.Should().BeEmpty();
        dto.AmenityList.Should().BeEmpty();
        dto.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void SearchHotelRequestDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new SearchHotelRequestDto();

        // Assert
        dto.PageNumber.Should().Be(1);
        dto.PageSize.Should().Be(10);
        dto.City.Should().BeNull();
        dto.AmenityIds.Should().BeNull();
        dto.MinPrice.Should().BeNull();
        dto.MaxPrice.Should().BeNull();
    }

    [Fact]
    public void SearchHotelResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new SearchHotelResponseDto();

        // Assert
        dto.Hotels.Should().BeEmpty();
        dto.PageNumber.Should().Be(0);
        dto.TotalCount.Should().Be(0);
    }

    [Fact]
    public void AmenityPublicDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var dto = new AmenityPublicDto { AmenityId = id, Name = "WiFi", Category = "Tech", IconName = "wifi" };

        // Assert
        dto.AmenityId.Should().Be(id);
        dto.Name.Should().Be("WiFi");
    }

    [Fact]
    public void SuperAdminHotelListDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var dto = new SuperAdminHotelListDto
        {
            HotelId = id,
            Name = "Luxury Palace",
            City = "Chennai",
            ContactNumber = "9000000003",
            IsActive = true,
            IsBlockedBySuperAdmin = false,
            CreatedAt = now,
            TotalReservations = 200,
            TotalRevenue = 800000m
        };

        // Assert
        dto.HotelId.Should().Be(id);
        dto.IsActive.Should().BeTrue();
        dto.TotalRevenue.Should().Be(800000m);
    }

    [Fact]
    public void PagedSuperAdminHotelResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PagedSuperAdminHotelResponseDto();

        // Assert
        dto.TotalCount.Should().Be(0);
        dto.Hotels.Should().BeEmpty();
    }


    // ── Inventory DTOs ────────────────────────────────────────────────────────

    [Fact]
    public void CreateInventoryDto_SetProperties_RetainsValues()
    {
        // Arrange
        var rtId = Guid.NewGuid();
        var start = new DateOnly(2026, 1, 1);
        var end = new DateOnly(2026, 1, 31);

        // Act
        var dto = new CreateInventoryDto { RoomTypeId = rtId, StartDate = start, EndDate = end, TotalInventory = 10 };

        // Assert
        dto.RoomTypeId.Should().Be(rtId);
        dto.TotalInventory.Should().Be(10);
        dto.StartDate.Should().Be(start);
        dto.EndDate.Should().Be(end);
    }

    [Fact]
    public void InventoryResponseDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var date = new DateOnly(2026, 6, 1);

        // Act
        var dto = new InventoryResponseDto
        {
            RoomTypeInventoryId = id,
            Date = date,
            TotalInventory = 20,
            ReservedInventory = 5,
            Available = 15
        };

        // Assert
        dto.RoomTypeInventoryId.Should().Be(id);
        dto.Available.Should().Be(15);
    }

    [Fact]
    public void UpdateInventoryDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var dto = new UpdateInventoryDto { RoomTypeInventoryId = id, TotalInventory = 25 };

        // Assert
        dto.RoomTypeInventoryId.Should().Be(id);
        dto.TotalInventory.Should().Be(25);
    }

    // ── Log DTOs ──────────────────────────────────────────────────────────────

    [Fact]
    public void LogResponseDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var dto = new LogResponseDto
        {
            LogId = id,
            Message = "Error occurred",
            ExceptionType = "NullReferenceException",
            StackTrace = "at line 1",
            StatusCode = 500,
            UserName = "admin",
            Role = "Admin",
            UserId = userId,
            Controller = "HotelController",
            Action = "GetHotel",
            HttpMethod = "GET",
            RequestPath = "/api/hotel",
            CreatedAt = now
        };

        // Assert
        dto.LogId.Should().Be(id);
        dto.StatusCode.Should().Be(500);
        dto.UserId.Should().Be(userId);
    }

    [Fact]
    public void LogResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new LogResponseDto();

        // Assert
        dto.UserId.Should().BeNull();
        dto.Message.Should().BeEmpty();
    }

    [Fact]
    public void PagedLogResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PagedLogResponseDto();

        // Assert
        dto.TotalCount.Should().Be(0);
        dto.Logs.Should().BeEmpty();
    }

    // ── PromoCode DTOs ────────────────────────────────────────────────────────

    [Fact]
    public void PromoCodeResponseDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var expiry = DateTime.UtcNow.AddDays(30);

        // Act
        var dto = new PromoCodeResponseDto
        {
            PromoCodeId = id,
            Code = "SAVE20",
            HotelName = "Grand",
            HotelId = hotelId,
            DiscountPercent = 20m,
            ExpiryDate = expiry,
            IsUsed = false,
            Status = "Active"
        };

        // Assert
        dto.Code.Should().Be("SAVE20");
        dto.DiscountPercent.Should().Be(20m);
        dto.IsUsed.Should().BeFalse();
    }

    [Fact]
    public void PromoCodeValidationResultDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new PromoCodeValidationResultDto
        {
            IsValid = true,
            DiscountPercent = 15m,
            DiscountAmount = 750m,
            Message = "Promo applied"
        };

        // Assert
        dto.IsValid.Should().BeTrue();
        dto.DiscountAmount.Should().Be(750m);
        dto.Message.Should().Be("Promo applied");
    }

    [Fact]
    public void ValidatePromoCodeDto_SetProperties_RetainsValues()
    {
        // Arrange
        var hotelId = Guid.NewGuid();

        // Act
        var dto = new ValidatePromoCodeDto { Code = "PROMO10", HotelId = hotelId, TotalAmount = 5000m };

        // Assert
        dto.Code.Should().Be("PROMO10");
        dto.TotalAmount.Should().Be(5000m);
    }

    [Fact]
    public void PagedPromoCodeResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PagedPromoCodeResponseDto();

        // Assert
        dto.TotalCount.Should().Be(0);
        dto.PromoCodes.Should().BeEmpty();
    }


    // ── Reservation DTOs ──────────────────────────────────────────────────────

    [Fact]
    public void AvailableRoomDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var dto = new AvailableRoomDto { RoomId = id, RoomNumber = "101", Floor = 1, RoomTypeName = "Standard" };

        // Assert
        dto.RoomId.Should().Be(id);
        dto.RoomNumber.Should().Be("101");
        dto.Floor.Should().Be(1);
    }

    [Fact]
    public void CancelReservationDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new CancelReservationDto { Reason = "Change of plans" };

        // Assert
        dto.Reason.Should().Be("Change of plans");
    }

    [Fact]
    public void CreateReservationDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new CreateReservationDto();

        // Assert
        dto.WalletAmountToUse.Should().Be(0);
        dto.PayCancellationFee.Should().BeFalse();
        dto.SelectedRoomIds.Should().BeNull();
        dto.PromoCodeUsed.Should().BeNull();
    }

    [Fact]
    public void CreateReservationDto_SetProperties_RetainsValues()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var rtId = Guid.NewGuid();
        var checkIn = new DateOnly(2026, 7, 1);
        var checkOut = new DateOnly(2026, 7, 5);

        // Act
        var dto = new CreateReservationDto
        {
            HotelId = hotelId,
            RoomTypeId = rtId,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            NumberOfRooms = 2,
            PromoCodeUsed = "SAVE10",
            WalletAmountToUse = 500m,
            PayCancellationFee = true,
            SelectedRoomIds = new List<Guid> { Guid.NewGuid() }
        };

        // Assert
        dto.HotelId.Should().Be(hotelId);
        dto.NumberOfRooms.Should().Be(2);
        dto.WalletAmountToUse.Should().Be(500m);
        dto.PayCancellationFee.Should().BeTrue();
        dto.SelectedRoomIds.Should().HaveCount(1);
    }

    [Fact]
    public void QrPaymentResponseDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new QrPaymentResponseDto
        {
            UpiId = "hotel@upi",
            Amount = 3000m,
            QrCodeBase64 = "base64data",
            HotelName = "Grand"
        };

        // Assert
        dto.UpiId.Should().Be("hotel@upi");
        dto.Amount.Should().Be(3000m);
        dto.QrCodeBase64.Should().Be("base64data");
    }

    [Fact]
    public void ReservationDetailsDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new ReservationDetailsDto();

        // Assert
        dto.Rooms.Should().BeEmpty();
        dto.PromoCodeUsed.Should().BeNull();
        dto.UpiId.Should().BeNull();
        dto.ExpiryTime.Should().BeNull();
        dto.CancellationFeePaid.Should().BeFalse();
    }

    [Fact]
    public void ReservationDetailsDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var rtId = Guid.NewGuid();

        // Act
        var dto = new ReservationDetailsDto
        {
            ReservationCode = "RES001",
            ReservationId = id,
            HotelId = hotelId,
            HotelName = "Grand",
            RoomTypeId = rtId,
            RoomTypeName = "Deluxe",
            TotalAmount = 6000m,
            GstPercent = 12m,
            GstAmount = 720m,
            DiscountPercent = 10m,
            DiscountAmount = 600m,
            WalletAmountUsed = 500m,
            FinalAmount = 5620m,
            Status = "Confirmed",
            IsCheckedIn = true,
            CancellationFeePaid = true,
            CancellationFeeAmount = 600m,
            CancellationPolicyText = "10% fee applies"
        };

        // Assert
        dto.ReservationCode.Should().Be("RES001");
        dto.FinalAmount.Should().Be(5620m);
        dto.IsCheckedIn.Should().BeTrue();
        dto.CancellationFeePaid.Should().BeTrue();
    }

    [Fact]
    public void ReservationResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new ReservationResponseDto();

        // Assert
        dto.Rooms.Should().BeEmpty();
        dto.ReservationCode.Should().BeEmpty();
    }

    [Fact]
    public void RoomSummaryDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var dto = new RoomSummaryDto { RoomId = id, RoomNumber = "202", Floor = 2 };

        // Assert
        dto.RoomId.Should().Be(id);
        dto.RoomNumber.Should().Be("202");
        dto.Floor.Should().Be(2);
    }

    [Fact]
    public void PagedReservationResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PagedReservationResponseDto();

        // Assert
        dto.TotalCount.Should().Be(0);
        dto.Reservations.Should().BeEmpty();
    }


    // ── Revenue DTOs ──────────────────────────────────────────────────────────

    [Fact]
    public void SuperAdminRevenueDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var dto = new SuperAdminRevenueDto
        {
            SuperAdminRevenueId = id,
            ReservationCode = "RES100",
            HotelName = "Grand",
            ReservationAmount = 10000m,
            CommissionAmount = 500m,
            SuperAdminUpiId = "superadmin@upi",
            CreatedAt = now
        };

        // Assert
        dto.SuperAdminRevenueId.Should().Be(id);
        dto.CommissionAmount.Should().Be(500m);
        dto.SuperAdminUpiId.Should().Be("superadmin@upi");
    }

    [Fact]
    public void RevenueSummaryDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new RevenueSummaryDto { TotalCommissionEarned = 75000m };

        // Assert
        dto.TotalCommissionEarned.Should().Be(75000m);
    }

    [Fact]
    public void PagedRevenueResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PagedRevenueResponseDto();

        // Assert
        dto.TotalCount.Should().Be(0);
        dto.Items.Should().BeEmpty();
    }

    // ── Review DTOs (remaining) ───────────────────────────────────────────────

    [Fact]
    public void ReviewResponseDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var resId = Guid.NewGuid();

        // Act
        var dto = new ReviewResponseDto
        {
            ReviewId = id,
            HotelId = hotelId,
            UserId = userId,
            UserName = "Alice",
            ReservationId = resId,
            ReservationCode = "RES001",
            Rating = 4.5m,
            Comment = "Great stay",
            ImageUrl = "img.jpg",
            UserProfileImageUrl = "alice.jpg",
            AdminReply = "Thank you",
            ContributionPoints = 100
        };

        // Assert
        dto.ReviewId.Should().Be(id);
        dto.Rating.Should().Be(4.5m);
        dto.ContributionPoints.Should().Be(100);
    }

    [Fact]
    public void ReviewResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new ReviewResponseDto();

        // Assert
        dto.ImageUrl.Should().BeNull();
        dto.AdminReply.Should().BeNull();
        dto.ContributionPoints.Should().Be(100);
    }

    [Fact]
    public void UpdateReviewDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new UpdateReviewDto { Rating = 3m, Comment = "Average", ImageUrl = "img.jpg" };

        // Assert
        dto.Rating.Should().Be(3m);
        dto.Comment.Should().Be("Average");
    }

    [Fact]
    public void ReplyToReviewDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new ReplyToReviewDto { AdminReply = "Thank you for your feedback!" };

        // Assert
        dto.AdminReply.Should().Be("Thank you for your feedback!");
    }

    [Fact]
    public void GetHotelReviewsRequestDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new GetHotelReviewsRequestDto();

        // Assert
        dto.Page.Should().Be(1);
        dto.PageSize.Should().Be(10);
        dto.MinRating.Should().BeNull();
        dto.MaxRating.Should().BeNull();
        dto.SortDir.Should().BeNull();
    }

    [Fact]
    public void MyReviewsResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new MyReviewsResponseDto();

        // Assert
        dto.ContributionPoints.Should().Be(100);
        dto.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void PagedMyReviewsResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PagedMyReviewsResponseDto();

        // Assert
        dto.TotalCount.Should().Be(0);
        dto.Reviews.Should().BeEmpty();
    }

    [Fact]
    public void PagedReviewResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PagedReviewResponseDto();

        // Assert
        dto.TotalCount.Should().Be(0);
        dto.Reviews.Should().BeEmpty();
    }


    // ── Room DTOs ─────────────────────────────────────────────────────────────

    [Fact]
    public void CreateRoomDto_SetProperties_RetainsValues()
    {
        // Arrange
        var rtId = Guid.NewGuid();

        // Act
        var dto = new CreateRoomDto { RoomNumber = "301", Floor = 3, RoomTypeId = rtId };

        // Assert
        dto.RoomNumber.Should().Be("301");
        dto.Floor.Should().Be(3);
        dto.RoomTypeId.Should().Be(rtId);
    }

    [Fact]
    public void RoomListResponseDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var rtId = Guid.NewGuid();

        // Act
        var dto = new RoomListResponseDto
        {
            RoomId = id,
            RoomNumber = "401",
            Floor = 4,
            RoomTypeId = rtId,
            RoomTypeName = "Suite",
            IsActive = true
        };

        // Assert
        dto.RoomId.Should().Be(id);
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public void RoomOccupancyDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var dto = new RoomOccupancyDto
        {
            RoomId = id,
            RoomNumber = "501",
            Floor = 5,
            RoomTypeName = "Penthouse",
            IsOccupied = true,
            ReservationCode = "RES999"
        };

        // Assert
        dto.IsOccupied.Should().BeTrue();
        dto.ReservationCode.Should().Be("RES999");
    }

    [Fact]
    public void RoomOccupancyDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new RoomOccupancyDto();

        // Assert
        dto.IsOccupied.Should().BeFalse();
        dto.ReservationCode.Should().BeNull();
    }

    [Fact]
    public void UpdateRoomDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var rtId = Guid.NewGuid();

        // Act
        var dto = new UpdateRoomDto { RoomId = id, RoomNumber = "601", Floor = 6, RoomTypeId = rtId };

        // Assert
        dto.RoomId.Should().Be(id);
        dto.Floor.Should().Be(6);
    }

    // ── RoomType DTOs ─────────────────────────────────────────────────────────

    [Fact]
    public void AmenityItemDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var dto = new AmenityItemDto { AmenityId = id, Name = "AC", Category = "Comfort", IconName = "ac" };

        // Assert
        dto.AmenityId.Should().Be(id);
        dto.Name.Should().Be("AC");
        dto.IconName.Should().Be("ac");
    }

    [Fact]
    public void CreateRoomTypeDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new CreateRoomTypeDto
        {
            Name = "Deluxe",
            Description = "Spacious room",
            MaxOccupancy = 3,
            AmenityIds = new List<Guid> { Guid.NewGuid() },
            ImageUrl = "deluxe.jpg"
        };

        // Assert
        dto.Name.Should().Be("Deluxe");
        dto.MaxOccupancy.Should().Be(3);
        dto.AmenityIds.Should().HaveCount(1);
    }

    [Fact]
    public void CreateRoomTypeDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new CreateRoomTypeDto();

        // Assert
        dto.AmenityIds.Should().BeNull();
        dto.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void CreateRoomTypeRateDto_SetProperties_RetainsValues()
    {
        // Arrange
        var rtId = Guid.NewGuid();
        var start = new DateOnly(2026, 1, 1);
        var end = new DateOnly(2026, 3, 31);

        // Act
        var dto = new CreateRoomTypeRateDto { RoomTypeId = rtId, StartDate = start, EndDate = end, Rate = 2500m };

        // Assert
        dto.RoomTypeId.Should().Be(rtId);
        dto.Rate.Should().Be(2500m);
    }

    [Fact]
    public void GetRateByDateRequestDto_SetProperties_RetainsValues()
    {
        // Arrange
        var rtId = Guid.NewGuid();
        var date = new DateOnly(2026, 8, 15);

        // Act
        var dto = new GetRateByDateRequestDto { RoomTypeId = rtId, Date = date };

        // Assert
        dto.RoomTypeId.Should().Be(rtId);
        dto.Date.Should().Be(date);
    }

    [Fact]
    public void RoomTypeListDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new RoomTypeListDto();

        // Assert
        dto.AmenityList.Should().BeEmpty();
        dto.ImageUrl.Should().BeNull();
        dto.IsActive.Should().BeFalse();
    }

    [Fact]
    public void RoomTypeRateDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var rtId = Guid.NewGuid();

        // Act
        var dto = new RoomTypeRateDto
        {
            RoomTypeRateId = id,
            RoomTypeId = rtId,
            StartDate = new DateOnly(2026, 4, 1),
            EndDate = new DateOnly(2026, 6, 30),
            Rate = 3500m
        };

        // Assert
        dto.RoomTypeRateId.Should().Be(id);
        dto.Rate.Should().Be(3500m);
    }

    [Fact]
    public void UpdateRoomTypeDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var dto = new UpdateRoomTypeDto
        {
            RoomTypeId = id,
            Name = "Suite",
            Description = "Luxury suite",
            MaxOccupancy = 4,
            AmenityIds = new List<Guid>(),
            ImageUrl = "suite.jpg"
        };

        // Assert
        dto.RoomTypeId.Should().Be(id);
        dto.MaxOccupancy.Should().Be(4);
    }

    [Fact]
    public void UpdateRoomTypeRateDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var dto = new UpdateRoomTypeRateDto
        {
            RoomTypeRateId = id,
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 9, 30),
            Rate = 4000m
        };

        // Assert
        dto.RoomTypeRateId.Should().Be(id);
        dto.Rate.Should().Be(4000m);
    }

    [Fact]
    public void PagedRoomTypeResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PagedRoomTypeResponseDto();

        // Assert
        dto.TotalCount.Should().Be(0);
        dto.RoomTypes.Should().BeEmpty();
    }


    // ── SupportRequest DTOs ───────────────────────────────────────────────────

    [Fact]
    public void AdminSupportRequestDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new AdminSupportRequestDto { Subject = "Billing Issue", Message = "Overcharged", Category = "Billing" };

        // Assert
        dto.Subject.Should().Be("Billing Issue");
        dto.Category.Should().Be("Billing");
    }

    [Fact]
    public void GuestSupportRequestDto_SetProperties_RetainsValues()
    {
        // Arrange
        var hotelId = Guid.NewGuid();

        // Act
        var dto = new GuestSupportRequestDto
        {
            Subject = "Room Issue",
            Message = "AC not working",
            Category = "Maintenance",
            ReservationCode = "RES001",
            HotelId = hotelId
        };

        // Assert
        dto.Subject.Should().Be("Room Issue");
        dto.ReservationCode.Should().Be("RES001");
        dto.HotelId.Should().Be(hotelId);
    }

    [Fact]
    public void GuestSupportRequestDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new GuestSupportRequestDto();

        // Assert
        dto.ReservationCode.Should().BeNull();
        dto.HotelId.Should().BeNull();
    }

    [Fact]
    public void PublicSupportRequestDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new PublicSupportRequestDto
        {
            Name = "John",
            Email = "john@example.com",
            Subject = "General Query",
            Message = "How to book?",
            Category = "General"
        };

        // Assert
        dto.Name.Should().Be("John");
        dto.Email.Should().Be("john@example.com");
    }

    [Fact]
    public void RespondSupportRequestDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new RespondSupportRequestDto();

        // Assert
        dto.Status.Should().Be("Resolved");
        dto.Response.Should().BeEmpty();
    }

    [Fact]
    public void RespondSupportRequestDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new RespondSupportRequestDto { Response = "Issue resolved", Status = "Closed" };

        // Assert
        dto.Response.Should().Be("Issue resolved");
        dto.Status.Should().Be("Closed");
    }

    [Fact]
    public void SupportRequestResponseDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var dto = new SupportRequestResponseDto
        {
            SupportRequestId = id,
            Subject = "Refund",
            Message = "Need refund",
            Category = "Billing",
            Status = "Open",
            AdminResponse = "Processing",
            SubmitterRole = "Guest",
            SubmitterName = "Alice",
            SubmitterEmail = "alice@example.com",
            ReservationCode = "RES001",
            HotelName = "Grand",
            CreatedAt = now,
            RespondedAt = now
        };

        // Assert
        dto.SupportRequestId.Should().Be(id);
        dto.Status.Should().Be("Open");
        dto.RespondedAt.Should().Be(now);
    }

    [Fact]
    public void SupportRequestResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new SupportRequestResponseDto();

        // Assert
        dto.AdminResponse.Should().BeNull();
        dto.ReservationCode.Should().BeNull();
        dto.HotelName.Should().BeNull();
        dto.RespondedAt.Should().BeNull();
    }

    [Fact]
    public void PagedSupportRequestResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PagedSupportRequestResponseDto();

        // Assert
        dto.TotalCount.Should().Be(0);
        dto.Requests.Should().BeEmpty();
    }


    // ── Transaction DTOs ──────────────────────────────────────────────────────

    [Fact]
    public void CreatePaymentDto_SetProperties_RetainsValues()
    {
        // Arrange
        var resId = Guid.NewGuid();

        // Act
        var dto = new CreatePaymentDto { ReservationId = resId, PaymentMethod = PaymentMethod.UPI };

        // Assert
        dto.ReservationId.Should().Be(resId);
        dto.PaymentMethod.Should().Be(PaymentMethod.UPI);
    }

    [Fact]
    public void TransactionResponseDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var resId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var dto = new TransactionResponseDto
        {
            TransactionId = id,
            ReservationId = resId,
            ReservationCode = "RES001",
            HotelName = "Grand",
            GuestName = "Alice",
            Amount = 5000m,
            PaymentMethod = PaymentMethod.Wallet,
            Status = PaymentStatus.Success,
            TransactionDate = now,
            TransactionType = "Payment",
            Description = "Room booking"
        };

        // Assert
        dto.TransactionId.Should().Be(id);
        dto.Amount.Should().Be(5000m);
        dto.Status.Should().Be(PaymentStatus.Success);
        dto.PaymentMethod.Should().Be(PaymentMethod.Wallet);
    }

    [Fact]
    public void TransactionResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new TransactionResponseDto();

        // Assert
        dto.TransactionType.Should().Be("Payment");
        dto.Description.Should().BeNull();
    }

    [Fact]
    public void PaymentIntentDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new PaymentIntentDto
        {
            UpiId = "hotel@upi",
            Amount = 4500m,
            PaymentRef = "REF123",
            HotelName = "Grand"
        };

        // Assert
        dto.UpiId.Should().Be("hotel@upi");
        dto.Amount.Should().Be(4500m);
        dto.PaymentRef.Should().Be("REF123");
    }

    [Fact]
    public void PaymentIntentDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PaymentIntentDto();

        // Assert
        dto.UpiId.Should().BeNull();
        dto.PaymentRef.Should().BeEmpty();
    }

    [Fact]
    public void RefundRequestDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new RefundRequestDto { Reason = "Cancelled trip" };

        // Assert
        dto.Reason.Should().Be("Cancelled trip");
    }

    [Fact]
    public void PagedTransactionResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PagedTransactionResponseDto();

        // Assert
        dto.TotalCount.Should().Be(0);
        dto.Transactions.Should().BeEmpty();
    }

    // ── UserDetails DTOs ──────────────────────────────────────────────────────

    [Fact]
    public void BookingHistoryDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var checkIn = new DateOnly(2026, 5, 1);
        var checkOut = new DateOnly(2026, 5, 5);
        var now = DateTime.UtcNow;

        // Act
        var dto = new BookingHistoryDto
        {
            ReservationId = id,
            ReservationCode = "RES001",
            HotelName = "Grand",
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            TotalAmount = 8000m,
            Status = "Completed",
            CreatedDate = now
        };

        // Assert
        dto.ReservationId.Should().Be(id);
        dto.TotalAmount.Should().Be(8000m);
        dto.Status.Should().Be("Completed");
    }

    [Fact]
    public void PagedBookingHistoryDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PagedBookingHistoryDto();

        // Assert
        dto.TotalCount.Should().Be(0);
        dto.Bookings.Should().BeEmpty();
    }

    [Fact]
    public void PaginationDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PaginationDto();

        // Assert
        dto.Page.Should().Be(1);
        dto.PageSize.Should().Be(10);
    }

    [Fact]
    public void PaginationDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new PaginationDto { Page = 3, PageSize = 25 };

        // Assert
        dto.Page.Should().Be(3);
        dto.PageSize.Should().Be(25);
    }

    [Fact]
    public void UpdateUserProfileDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new UpdateUserProfileDto
        {
            Name = "Alice",
            PhoneNumber = "9876543210",
            Address = "123 Main St",
            State = "MH",
            City = "Mumbai",
            Pincode = "400001",
            ProfileImageUrl = "alice.jpg"
        };

        // Assert
        dto.Name.Should().Be("Alice");
        dto.PhoneNumber.Should().Be("9876543210");
        dto.ProfileImageUrl.Should().Be("alice.jpg");
    }

    [Fact]
    public void UpdateUserProfileDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new UpdateUserProfileDto();

        // Assert
        dto.Name.Should().BeNull();
        dto.PhoneNumber.Should().BeNull();
        dto.ProfileImageUrl.Should().BeNull();
    }

    [Fact]
    public void UserProfileResponseDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var dto = new UserProfileResponseDto
        {
            UserId = id,
            Email = "user@example.com",
            Role = "Guest",
            Name = "Alice",
            PhoneNumber = "9876543210",
            Address = "123 Main",
            State = "MH",
            City = "Mumbai",
            Pincode = "400001",
            ProfileImageUrl = "alice.jpg",
            CreatedAt = now,
            TotalReviewPoints = 300
        };

        // Assert
        dto.UserId.Should().Be(id);
        dto.TotalReviewPoints.Should().Be(300);
        dto.ProfileImageUrl.Should().Be("alice.jpg");
    }

    [Fact]
    public void UserProfileResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new UserProfileResponseDto();

        // Assert
        dto.ProfileImageUrl.Should().BeNull();
        dto.Email.Should().BeEmpty();
    }


    // ── Wallet DTOs ───────────────────────────────────────────────────────────

    [Fact]
    public void WalletResponseDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var dto = new WalletResponseDto { WalletId = id, Balance = 2500m, UpdatedAt = now };

        // Assert
        dto.WalletId.Should().Be(id);
        dto.Balance.Should().Be(2500m);
    }

    [Fact]
    public void WalletTransactionDto_SetProperties_RetainsValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var dto = new WalletTransactionDto
        {
            WalletTransactionId = id,
            Amount = 500m,
            Type = "Credit",
            Description = "Top-up",
            CreatedAt = now
        };

        // Assert
        dto.WalletTransactionId.Should().Be(id);
        dto.Amount.Should().Be(500m);
        dto.Type.Should().Be("Credit");
    }

    [Fact]
    public void TopUpWalletDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new TopUpWalletDto { Amount = 1000m };

        // Assert
        dto.Amount.Should().Be(1000m);
    }

    [Fact]
    public void PagedWalletTransactionDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PagedWalletTransactionDto();

        // Assert
        dto.TotalCount.Should().Be(0);
        dto.Transactions.Should().BeEmpty();
        dto.Wallet.Should().NotBeNull();
    }

    [Fact]
    public void PagedWalletTransactionDto_SetProperties_RetainsValues()
    {
        // Arrange
        var wallet = new WalletResponseDto { Balance = 3000m };
        var txns = new List<WalletTransactionDto> { new() { Amount = 500m, Type = "Debit" } };

        // Act
        var dto = new PagedWalletTransactionDto { TotalCount = 1, Wallet = wallet, Transactions = txns };

        // Assert
        dto.TotalCount.Should().Be(1);
        dto.Wallet.Balance.Should().Be(3000m);
        dto.Transactions.Should().HaveCount(1);
    }

    // ── QueryDtos ─────────────────────────────────────────────────────────────

    [Fact]
    public void PageQueryDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PageQueryDto();

        // Assert
        dto.Page.Should().Be(1);
        dto.PageSize.Should().Be(10);
        dto.PageNumber.Should().Be(1);
    }

    [Fact]
    public void PageQueryDto_PageNumber_ReturnsPageValue()
    {
        // Arrange & Act
        var dto = new PageQueryDto { Page = 5 };

        // Assert
        dto.PageNumber.Should().Be(5);
    }

    [Fact]
    public void LogQueryDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new LogQueryDto { Page = 2, PageSize = 20, Search = "error" };

        // Assert
        dto.Search.Should().Be("error");
        dto.Page.Should().Be(2);
    }

    [Fact]
    public void LogQueryDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new LogQueryDto();

        // Assert
        dto.Search.Should().BeNull();
    }

    [Fact]
    public void TransactionQueryDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new TransactionQueryDto { SortField = "Amount", SortDir = "desc" };

        // Assert
        dto.SortField.Should().Be("Amount");
        dto.SortDir.Should().Be("desc");
    }

    [Fact]
    public void TransactionQueryDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new TransactionQueryDto();

        // Assert
        dto.SortField.Should().BeNull();
        dto.SortDir.Should().BeNull();
    }

    [Fact]
    public void ReservationQueryDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new ReservationQueryDto();

        // Assert
        dto.Status.Should().Be("All");
        dto.Search.Should().BeNull();
        dto.SortField.Should().BeNull();
        dto.SortDir.Should().BeNull();
    }

    [Fact]
    public void ReservationQueryDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new ReservationQueryDto { Status = "Confirmed", Search = "RES001", SortField = "Date", SortDir = "asc" };

        // Assert
        dto.Status.Should().Be("Confirmed");
        dto.Search.Should().Be("RES001");
    }

    [Fact]
    public void AuditLogQueryDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new AuditLogQueryDto { Search = "hotel" };

        // Assert
        dto.Search.Should().Be("hotel");
    }

    [Fact]
    public void AuditLogSuperAdminQueryDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new AuditLogSuperAdminQueryDto
        {
            HotelId = "hotel-id",
            UserId = "user-id",
            Action = "Create",
            DateFrom = "2026-01-01",
            DateTo = "2026-12-31"
        };

        // Assert
        dto.HotelId.Should().Be("hotel-id");
        dto.Action.Should().Be("Create");
        dto.DateFrom.Should().Be("2026-01-01");
    }

    [Fact]
    public void AuditLogSuperAdminQueryDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new AuditLogSuperAdminQueryDto();

        // Assert
        dto.HotelId.Should().BeNull();
        dto.UserId.Should().BeNull();
        dto.Action.Should().BeNull();
        dto.DateFrom.Should().BeNull();
        dto.DateTo.Should().BeNull();
    }

    [Fact]
    public void AmenityRequestAdminQueryDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new AmenityRequestAdminQueryDto { Search = "pool" };

        // Assert
        dto.Search.Should().Be("pool");
    }

    [Fact]
    public void AmenityRequestQueryDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new AmenityRequestQueryDto();

        // Assert
        dto.Status.Should().Be("All");
    }

    [Fact]
    public void AmenityRequestQueryDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new AmenityRequestQueryDto { Status = "Pending" };

        // Assert
        dto.Status.Should().Be("Pending");
    }

    [Fact]
    public void RevenueQueryDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new RevenueQueryDto();

        // Assert
        dto.Page.Should().Be(1);
        dto.PageSize.Should().Be(10);
    }

    [Fact]
    public void HotelQueryDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new HotelQueryDto { Search = "grand", Status = "Active" };

        // Assert
        dto.Search.Should().Be("grand");
        dto.Status.Should().Be("Active");
    }

    [Fact]
    public void HotelQueryDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new HotelQueryDto();

        // Assert
        dto.Search.Should().BeNull();
        dto.Status.Should().BeNull();
    }

    [Fact]
    public void SupportQueryDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new SupportQueryDto { Status = "Open", Role = "Guest", Search = "billing" };

        // Assert
        dto.Status.Should().Be("Open");
        dto.Role.Should().Be("Guest");
        dto.Search.Should().Be("billing");
    }

    [Fact]
    public void SupportQueryDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new SupportQueryDto();

        // Assert
        dto.Status.Should().BeNull();
        dto.Role.Should().BeNull();
        dto.Search.Should().BeNull();
    }

    [Fact]
    public void PromoQueryDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new PromoQueryDto { Status = "Active" };

        // Assert
        dto.Status.Should().Be("Active");
    }

    [Fact]
    public void PromoQueryDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PromoQueryDto();

        // Assert
        dto.Status.Should().BeNull();
    }

    [Fact]
    public void ReservationHistoryQueryDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new ReservationHistoryQueryDto { Status = "Completed", Search = "RES" };

        // Assert
        dto.Status.Should().Be("Completed");
        dto.Search.Should().Be("RES");
    }

    [Fact]
    public void ReservationHistoryQueryDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new ReservationHistoryQueryDto();

        // Assert
        dto.Status.Should().BeNull();
        dto.Search.Should().BeNull();
    }

    // ── Enums ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(PaymentMethod.CreditCard, 1)]
    [InlineData(PaymentMethod.DebitCard, 2)]
    [InlineData(PaymentMethod.UPI, 3)]
    [InlineData(PaymentMethod.NetBanking, 4)]
    [InlineData(PaymentMethod.Wallet, 5)]
    public void PaymentMethod_EnumValues_AreCorrect(PaymentMethod method, int expected)
    {
        // Arrange & Act & Assert
        ((int)method).Should().Be(expected);
    }

    [Theory]
    [InlineData(PaymentStatus.Pending, 1)]
    [InlineData(PaymentStatus.Success, 2)]
    [InlineData(PaymentStatus.Failed, 3)]
    [InlineData(PaymentStatus.Refunded, 4)]
    public void PaymentStatus_EnumValues_AreCorrect(PaymentStatus status, int expected)
    {
        // Arrange & Act & Assert
        ((int)status).Should().Be(expected);
    }

    [Theory]
    [InlineData(ReservationStatus.Pending, 1)]
    [InlineData(ReservationStatus.Confirmed, 2)]
    [InlineData(ReservationStatus.Cancelled, 3)]
    [InlineData(ReservationStatus.Completed, 4)]
    [InlineData(ReservationStatus.NoShow, 5)]
    public void ReservationStatus_EnumValues_AreCorrect(ReservationStatus status, int expected)
    {
        // Arrange & Act & Assert
        ((int)status).Should().Be(expected);
    }

    [Theory]
    [InlineData(UserRole.Guest, 1)]
    [InlineData(UserRole.Admin, 2)]
    [InlineData(UserRole.SuperAdmin, 3)]
    public void UserRole_EnumValues_AreCorrect(UserRole role, int expected)
    {
        // Arrange & Act & Assert
        ((int)role).Should().Be(expected);
    }
}
