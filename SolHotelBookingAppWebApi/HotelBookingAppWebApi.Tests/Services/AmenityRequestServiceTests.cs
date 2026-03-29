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
    private readonly Mock<IRepository<Guid, AmenityRequest>> _requestRepo = new();
    private readonly Mock<IRepository<Guid, Amenity>> _amenityRepo = new();
    private readonly Mock<IRepository<Guid, User>> _userRepo = new();
    private readonly Mock<IRepository<Guid, Hotel>> _hotelRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly AmenityRequestService _sut;

    public AmenityRequestServiceTests()
    {
        _sut = new AmenityRequestService(
            _requestRepo.Object, _amenityRepo.Object,
            _userRepo.Object, _hotelRepo.Object, _unitOfWork.Object);
    }

    private static User MakeAdmin(Guid? hotelId = null)
        => new() { UserId = Guid.NewGuid(), Name = "Admin", Email = "a@a.com", Role = UserRole.Admin, HotelId = hotelId ?? Guid.NewGuid(), CreatedAt = DateTime.UtcNow };

    private static Hotel MakeHotel(Guid id) => new() { HotelId = id, Name = "Grand", Address = "A", City = "C", ContactNumber = "123", CreatedAt = DateTime.UtcNow };

    // ── CreateRequestAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateRequestAsync_ValidAdmin_CreatesRequest()
    {
        var admin = MakeAdmin();
        var hotel = MakeHotel(admin.HotelId!.Value);
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _hotelRepo.Setup(r => r.GetAsync(admin.HotelId.Value)).ReturnsAsync(hotel);
        _requestRepo.Setup(r => r.AddAsync(It.IsAny<AmenityRequest>())).ReturnsAsync((AmenityRequest ar) => ar);

        var dto = new CreateAmenityRequestDto { AmenityName = "Sauna", Category = "Services", IconName = "spa" };
        var result = await _sut.CreateRequestAsync(admin.UserId, dto);

        result.AmenityName.Should().Be("Sauna");
        result.Status.Should().Be("Pending");
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateRequestAsync_AdminNotFound_ThrowsUnauthorized()
    {
        _userRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.CreateRequestAsync(Guid.NewGuid(), new CreateAmenityRequestDto { AmenityName = "X", Category = "Y" }))
            .Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task CreateRequestAsync_AdminNoHotel_ThrowsValidation()
    {
        var admin = new User { UserId = Guid.NewGuid(), Name = "A", Email = "a@a.com", HotelId = null, CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        await _sut.Invoking(s => s.CreateRequestAsync(admin.UserId, new CreateAmenityRequestDto { AmenityName = "X", Category = "Y" }))
            .Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateRequestAsync_HotelNotFound_ThrowsNotFound()
    {
        var admin = MakeAdmin();
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _hotelRepo.Setup(r => r.GetAsync(admin.HotelId!.Value)).ReturnsAsync((Hotel?)null);

        await _sut.Invoking(s => s.CreateRequestAsync(admin.UserId, new CreateAmenityRequestDto { AmenityName = "X", Category = "Y" }))
            .Should().ThrowAsync<NotFoundException>();
    }

    // ── GetAdminRequestsAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAdminRequestsAsync_ReturnsAdminRequests()
    {
        var admin = MakeAdmin();
        var hotel = MakeHotel(admin.HotelId!.Value);
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _hotelRepo.Setup(r => r.GetAsync(admin.HotelId.Value)).ReturnsAsync(hotel);
        var requests = new List<AmenityRequest>
        {
            new() { AmenityRequestId = Guid.NewGuid(), RequestedByAdminId = admin.UserId, AdminHotelId = admin.HotelId.Value, AmenityName = "Pool", Category = "Services", Status = AmenityRequestStatus.Pending, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _requestRepo.Setup(r => r.GetQueryable()).Returns(requests);

        var result = await _sut.GetAdminRequestsAsync(admin.UserId);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAdminRequestsAsync_AdminNotFound_ThrowsUnauthorized()
    {
        _userRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.GetAdminRequestsAsync(Guid.NewGuid()))
            .Should().ThrowAsync<UnAuthorizedException>();
    }

    // ── GetAdminRequestsPagedAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetAdminRequestsPagedAsync_ReturnsPaged()
    {
        var admin = MakeAdmin();
        var hotel = MakeHotel(admin.HotelId!.Value);
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _hotelRepo.Setup(r => r.GetAsync(admin.HotelId.Value)).ReturnsAsync(hotel);
        var requests = new List<AmenityRequest>
        {
            new() { AmenityRequestId = Guid.NewGuid(), RequestedByAdminId = admin.UserId, AdminHotelId = admin.HotelId.Value, AmenityName = "Gym", Category = "Services", Status = AmenityRequestStatus.Pending, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _requestRepo.Setup(r => r.GetQueryable()).Returns(requests);

        var result = await _sut.GetAdminRequestsPagedAsync(admin.UserId, 1, 10);

        result.TotalCount.Should().Be(1);
        result.Requests.Should().HaveCount(1);
    }

    // ── GetAllRequestsAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAllRequestsAsync_NoFilter_ReturnsAll()
    {
        var hotelId = Guid.NewGuid();
        var requests = new List<AmenityRequest>
        {
            new() { AmenityRequestId = Guid.NewGuid(), RequestedByAdminId = Guid.NewGuid(), AdminHotelId = hotelId, AmenityName = "Bar", Category = "Food", Status = AmenityRequestStatus.Pending, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _requestRepo.Setup(r => r.GetQueryable()).Returns(requests);
        var hotels = new List<Hotel> { MakeHotel(hotelId) }.AsQueryable().BuildMock();
        _hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);

        var result = await _sut.GetAllRequestsAsync(null, 1, 10);

        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAllRequestsAsync_WithStatusFilter_FiltersCorrectly()
    {
        var hotelId = Guid.NewGuid();
        var requests = new List<AmenityRequest>
        {
            new() { AmenityRequestId = Guid.NewGuid(), RequestedByAdminId = Guid.NewGuid(), AdminHotelId = hotelId, AmenityName = "Bar", Category = "Food", Status = AmenityRequestStatus.Approved, CreatedAt = DateTime.UtcNow },
            new() { AmenityRequestId = Guid.NewGuid(), RequestedByAdminId = Guid.NewGuid(), AdminHotelId = hotelId, AmenityName = "Spa", Category = "Services", Status = AmenityRequestStatus.Pending, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _requestRepo.Setup(r => r.GetQueryable()).Returns(requests);
        var hotels = new List<Hotel> { MakeHotel(hotelId) }.AsQueryable().BuildMock();
        _hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);

        var result = await _sut.GetAllRequestsAsync("Approved", 1, 10);

        result.TotalCount.Should().Be(1);
    }

    // ── ApproveRequestAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ApproveRequestAsync_PendingRequest_ApprovesAndCreatesAmenity()
    {
        var requestId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var request = new AmenityRequest { AmenityRequestId = requestId, RequestedByAdminId = adminId, AdminHotelId = hotelId, AmenityName = "Sauna", Category = "Services", Status = AmenityRequestStatus.Pending, CreatedAt = DateTime.UtcNow };
        _requestRepo.Setup(r => r.GetAsync(requestId)).ReturnsAsync(request);
        _amenityRepo.Setup(r => r.AddAsync(It.IsAny<Amenity>())).ReturnsAsync(new Amenity());
        _userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(new User { Name = "Admin", Email = "a@a.com", CreatedAt = DateTime.UtcNow });
        _hotelRepo.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(MakeHotel(hotelId));

        var result = await _sut.ApproveRequestAsync(requestId, Guid.NewGuid());

        result.Status.Should().Be("Approved");
        _amenityRepo.Verify(r => r.AddAsync(It.IsAny<Amenity>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ApproveRequestAsync_NotFound_ThrowsNotFound()
    {
        _requestRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((AmenityRequest?)null);

        await _sut.Invoking(s => s.ApproveRequestAsync(Guid.NewGuid(), Guid.NewGuid()))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ApproveRequestAsync_AlreadyApproved_ThrowsValidation()
    {
        var requestId = Guid.NewGuid();
        var request = new AmenityRequest { AmenityRequestId = requestId, Status = AmenityRequestStatus.Approved, AmenityName = "X", Category = "Y", CreatedAt = DateTime.UtcNow };
        _requestRepo.Setup(r => r.GetAsync(requestId)).ReturnsAsync(request);

        await _sut.Invoking(s => s.ApproveRequestAsync(requestId, Guid.NewGuid()))
            .Should().ThrowAsync<ValidationException>();
    }

    // ── RejectRequestAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task RejectRequestAsync_PendingRequest_Rejects()
    {
        var requestId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var request = new AmenityRequest { AmenityRequestId = requestId, RequestedByAdminId = adminId, AdminHotelId = hotelId, AmenityName = "X", Category = "Y", Status = AmenityRequestStatus.Pending, CreatedAt = DateTime.UtcNow };
        _requestRepo.Setup(r => r.GetAsync(requestId)).ReturnsAsync(request);
        _userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(new User { Name = "Admin", Email = "a@a.com", CreatedAt = DateTime.UtcNow });
        _hotelRepo.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(MakeHotel(hotelId));

        var result = await _sut.RejectRequestAsync(requestId, Guid.NewGuid(), "Not needed");

        result.Status.Should().Be("Rejected");
        result.SuperAdminNote.Should().Be("Not needed");
    }

    [Fact]
    public async Task RejectRequestAsync_NotFound_ThrowsNotFound()
    {
        _requestRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((AmenityRequest?)null);

        await _sut.Invoking(s => s.RejectRequestAsync(Guid.NewGuid(), Guid.NewGuid(), "note"))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RejectRequestAsync_AlreadyRejected_ThrowsValidation()
    {
        var requestId = Guid.NewGuid();
        var request = new AmenityRequest { AmenityRequestId = requestId, Status = AmenityRequestStatus.Rejected, AmenityName = "X", Category = "Y", CreatedAt = DateTime.UtcNow };
        _requestRepo.Setup(r => r.GetAsync(requestId)).ReturnsAsync(request);

        await _sut.Invoking(s => s.RejectRequestAsync(requestId, Guid.NewGuid(), "note"))
            .Should().ThrowAsync<ValidationException>();
    }
}
