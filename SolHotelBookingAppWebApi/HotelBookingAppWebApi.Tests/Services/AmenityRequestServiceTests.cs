using FluentAssertions;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.AmenityRequest;
using HotelBookingAppWebApi.Services;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

public class AmenityRequestServiceTests
{
    private readonly Mock<IRepository<Guid, AmenityRequest>> _requestRepoMock = new();
    private readonly Mock<IRepository<Guid, Amenity>> _amenityRepoMock = new();
    private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
    private readonly Mock<IRepository<Guid, Hotel>> _hotelRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private AmenityRequestService CreateSut() => new(
        _requestRepoMock.Object, _amenityRepoMock.Object,
        _userRepoMock.Object, _hotelRepoMock.Object, _unitOfWorkMock.Object);

    private static User MakeAdmin(Guid? hotelId = null) => new()
    {
        UserId = Guid.NewGuid(), Name = "Admin", Email = "admin@test.com",
        Password = new byte[] { 1 }, PasswordSaltValue = new byte[] { 2 },
        Role = UserRole.Admin, HotelId = hotelId ?? Guid.NewGuid(), CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task CreateRequestAsync_ValidAdmin_ReturnsDto()
    {
        // Arrange
        var admin = MakeAdmin();
        var hotel = new Hotel { HotelId = admin.HotelId!.Value, Name = "Grand Hotel", Address = "A", City = "C", ContactNumber = "1234567890", CreatedAt = DateTime.UtcNow };
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _hotelRepoMock.Setup(r => r.GetAsync(admin.HotelId.Value)).ReturnsAsync(hotel);
        _requestRepoMock.Setup(r => r.AddAsync(It.IsAny<AmenityRequest>())).ReturnsAsync((AmenityRequest ar) => ar);
        var sut = CreateSut();

        // Act
        var result = await sut.CreateRequestAsync(admin.UserId, new CreateAmenityRequestDto { AmenityName = "Sauna", Category = "Services" });

        // Assert
        result.AmenityName.Should().Be("Sauna");
    }

    [Fact]
    public async Task CreateRequestAsync_AdminNotFound_ThrowsUnAuthorizedException()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.CreateRequestAsync(Guid.NewGuid(), new CreateAmenityRequestDto());

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task GetAdminRequestsPagedAsync_ValidAdmin_ReturnsPaged()
    {
        // Arrange
        var admin = MakeAdmin();
        var hotel = new Hotel { HotelId = admin.HotelId!.Value, Name = "H1", Address = "A", City = "C", ContactNumber = "1234567890", CreatedAt = DateTime.UtcNow };
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _hotelRepoMock.Setup(r => r.GetAsync(admin.HotelId.Value)).ReturnsAsync(hotel);
        var requests = new List<AmenityRequest>
        {
            new() { AmenityRequestId = Guid.NewGuid(), RequestedByAdminId = admin.UserId, AdminHotelId = admin.HotelId.Value, AmenityName = "Sauna", Category = "Services", Status = AmenityRequestStatus.Pending, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _requestRepoMock.Setup(r => r.GetQueryable()).Returns(requests);
        var sut = CreateSut();

        // Act
        var result = await sut.GetAdminRequestsPagedAsync(admin.UserId, 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ApproveRequestAsync_PendingRequest_ApprovesAndCreatesAmenity()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var request = new AmenityRequest { AmenityRequestId = requestId, RequestedByAdminId = adminId, AdminHotelId = hotelId, AmenityName = "Sauna", Category = "Services", Status = AmenityRequestStatus.Pending, CreatedAt = DateTime.UtcNow };
        _requestRepoMock.Setup(r => r.GetAsync(requestId)).ReturnsAsync(request);
        _amenityRepoMock.Setup(r => r.AddAsync(It.IsAny<Amenity>())).ReturnsAsync((Amenity a) => a);
        _userRepoMock.Setup(r => r.GetAsync(adminId)).ReturnsAsync(new User { UserId = adminId, Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, CreatedAt = DateTime.UtcNow });
        _hotelRepoMock.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(new Hotel { HotelId = hotelId, Name = "H1", Address = "A", City = "C", ContactNumber = "1234567890", CreatedAt = DateTime.UtcNow });
        var sut = CreateSut();

        // Act
        var result = await sut.ApproveRequestAsync(requestId, Guid.NewGuid());

        // Assert
        result.Status.Should().Be("Approved");
        _amenityRepoMock.Verify(r => r.AddAsync(It.IsAny<Amenity>()), Times.Once);
    }

    [Fact]
    public async Task ApproveRequestAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        _requestRepoMock.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((AmenityRequest?)null);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.ApproveRequestAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RejectRequestAsync_PendingRequest_RejectsWithNote()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var request = new AmenityRequest { AmenityRequestId = requestId, RequestedByAdminId = adminId, AdminHotelId = hotelId, AmenityName = "Sauna", Category = "Services", Status = AmenityRequestStatus.Pending, CreatedAt = DateTime.UtcNow };
        _requestRepoMock.Setup(r => r.GetAsync(requestId)).ReturnsAsync(request);
        _userRepoMock.Setup(r => r.GetAsync(adminId)).ReturnsAsync(new User { UserId = adminId, Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, CreatedAt = DateTime.UtcNow });
        _hotelRepoMock.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(new Hotel { HotelId = hotelId, Name = "H1", Address = "A", City = "C", ContactNumber = "1234567890", CreatedAt = DateTime.UtcNow });
        var sut = CreateSut();

        // Act
        var result = await sut.RejectRequestAsync(requestId, Guid.NewGuid(), "Not needed.");

        // Assert
        result.Status.Should().Be("Rejected");
        result.SuperAdminNote.Should().Be("Not needed.");
    }
}
