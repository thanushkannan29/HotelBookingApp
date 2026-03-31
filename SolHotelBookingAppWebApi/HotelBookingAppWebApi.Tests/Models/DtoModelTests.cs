using FluentAssertions;
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
        var dto = new CreateReviewDto();
        dto.Comment.Should().BeEmpty();
        dto.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void CreateReviewDto_SetProperties_RetainsValues()
    {
        var hotelId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var dto = new CreateReviewDto
        {
            HotelId = hotelId,
            ReservationId = reservationId,
            Rating = 4.5m,
            Comment = "Great!",
            ImageUrl = "img.jpg"
        };
        dto.HotelId.Should().Be(hotelId);
        dto.ReservationId.Should().Be(reservationId);
        dto.Rating.Should().Be(4.5m);
        dto.Comment.Should().Be("Great!");
        dto.ImageUrl.Should().Be("img.jpg");
    }
}