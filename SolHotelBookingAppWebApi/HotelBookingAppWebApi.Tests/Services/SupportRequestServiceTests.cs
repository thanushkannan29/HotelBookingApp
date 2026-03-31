using FluentAssertions;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.SupportRequest;
using HotelBookingAppWebApi.Services;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

public class SupportRequestServiceTests
{
    private readonly Mock<IRepository<Guid, SupportRequest>> _supportRepoMock = new();
    private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
    private readonly Mock<IRepository<Guid, Hotel>> _hotelRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private SupportRequestService CreateSut() => new(
        _supportRepoMock.Object, _userRepoMock.Object,
        _hotelRepoMock.Object, _unitOfWorkMock.Object);

    private static User MakeUser(UserRole role = UserRole.Guest) => new()
    {
        UserId = Guid.NewGuid(), Name = "User", Email = "user@test.com",
        Password = new byte[] { 1 }, PasswordSaltValue = new byte[] { 2 },
        Role = role, CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task CreatePublicRequestAsync_ValidDto_ReturnsDto()
    {
        // Arrange
        _supportRepoMock.Setup(r => r.AddAsync(It.IsAny<SupportRequest>())).ReturnsAsync((SupportRequest sr) => sr);
        var sut = CreateSut();
        var dto = new PublicSupportRequestDto { Name = "John", Email = "john@test.com", Subject = "Help", Message = "Need help", Category = "General" };

        // Act
        var result = await sut.CreatePublicRequestAsync(dto);

        // Assert
        result.Subject.Should().Be("Help");
        result.SubmitterRole.Should().Be("Public");
    }

    [Fact]
    public async Task CreateGuestRequestAsync_ValidGuest_ReturnsDto()
    {
        // Arrange
        var user = MakeUser();
        _userRepoMock.Setup(r => r.GetAsync(user.UserId)).ReturnsAsync(user);
        _supportRepoMock.Setup(r => r.AddAsync(It.IsAny<SupportRequest>())).ReturnsAsync((SupportRequest sr) => sr);
        var sut = CreateSut();
        var dto = new GuestSupportRequestDto { Subject = "Issue", Message = "Help me", Category = "Billing" };

        // Act
        var result = await sut.CreateGuestRequestAsync(user.UserId, dto);

        // Assert
        result.Subject.Should().Be("Issue");
        result.SubmitterRole.Should().Be("Guest");
    }

    [Fact]
    public async Task CreateGuestRequestAsync_UserNotFound_ThrowsUnAuthorizedException()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.CreateGuestRequestAsync(Guid.NewGuid(), new GuestSupportRequestDto());

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task CreateAdminRequestAsync_ValidAdmin_ReturnsDto()
    {
        // Arrange
        var admin = MakeUser(UserRole.Admin);
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _supportRepoMock.Setup(r => r.AddAsync(It.IsAny<SupportRequest>())).ReturnsAsync((SupportRequest sr) => sr);
        var sut = CreateSut();
        var dto = new AdminSupportRequestDto { Subject = "Bug", Message = "Found a bug", Category = "Technical" };

        // Act
        var result = await sut.CreateAdminRequestAsync(admin.UserId, dto);

        // Assert
        result.Subject.Should().Be("Bug");
        result.SubmitterRole.Should().Be("Admin");
    }

    [Fact]
    public async Task GetGuestRequestsAsync_ValidGuest_ReturnsPaged()
    {
        // Arrange
        var user = MakeUser();
        _userRepoMock.Setup(r => r.GetAsync(user.UserId)).ReturnsAsync(user);
        var requests = new List<SupportRequest>
        {
            new() { SupportRequestId = Guid.NewGuid(), UserId = user.UserId, Subject = "Issue", Message = "Help", Category = "Billing", SubmitterRole = "Guest", Status = SupportRequestStatus.Open, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _supportRepoMock.Setup(r => r.GetQueryable()).Returns(requests);
        var sut = CreateSut();

        // Act
        var result = await sut.GetGuestRequestsAsync(user.UserId, 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task RespondAsync_ValidRequest_UpdatesStatus()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var request = new SupportRequest { SupportRequestId = requestId, Subject = "Issue", Message = "Help", Category = "Billing", SubmitterRole = "Guest", Status = SupportRequestStatus.Open, CreatedAt = DateTime.UtcNow };
        var requests = new List<SupportRequest> { request }.AsQueryable().BuildMock();
        _supportRepoMock.Setup(r => r.GetQueryable()).Returns(requests);
        var sut = CreateSut();

        // Act
        var result = await sut.RespondAsync(requestId, new RespondSupportRequestDto { Response = "Fixed.", Status = "Resolved" });

        // Assert
        result.Status.Should().Be("Resolved");
        request.AdminResponse.Should().Be("Fixed.");
    }

    [Fact]
    public async Task RespondAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        _supportRepoMock.Setup(r => r.GetQueryable()).Returns(new List<SupportRequest>().AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var act = async () => await sut.RespondAsync(Guid.NewGuid(), new RespondSupportRequestDto());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAllRequestsAsync_WithStatusFilter_ReturnsFiltered()
    {
        // Arrange
        var requests = new List<SupportRequest>
        {
            new() { SupportRequestId = Guid.NewGuid(), Subject = "Open Issue", Message = "Help", Category = "Billing", SubmitterRole = "Guest", Status = SupportRequestStatus.Open, CreatedAt = DateTime.UtcNow },
            new() { SupportRequestId = Guid.NewGuid(), Subject = "Resolved Issue", Message = "Fixed", Category = "Technical", SubmitterRole = "Admin", Status = SupportRequestStatus.Resolved, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _supportRepoMock.Setup(r => r.GetQueryable()).Returns(requests);
        var sut = CreateSut();

        // Act
        var result = await sut.GetAllRequestsAsync("Open", null, null, 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
    }
}
