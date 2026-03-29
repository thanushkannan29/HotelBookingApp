using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.RefundRequest;
using HotelBookingAppWebApi.Services;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

public class RefundRequestServiceTests
{
    private readonly Mock<IRepository<Guid, RefundRequest>> _refundRepo = new();
    private readonly Mock<IRepository<Guid, Transaction>> _transactionRepo = new();
    private readonly Mock<IRepository<Guid, Reservation>> _reservationRepo = new();
    private readonly Mock<IRepository<Guid, User>> _userRepo = new();
    private readonly Mock<IAuditLogService> _auditLog = new();
    private readonly Mock<IWalletService> _walletService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly RefundRequestService _sut;

    public RefundRequestServiceTests()
    {
        _sut = new RefundRequestService(
            _refundRepo.Object, _transactionRepo.Object, _reservationRepo.Object,
            _userRepo.Object, _auditLog.Object, _walletService.Object, _unitOfWork.Object);
    }

    private static Reservation MakeReservation(Guid? userId = null, Guid? hotelId = null) => new()
    {
        ReservationId = Guid.NewGuid(),
        ReservationCode = "RES001",
        UserId = userId ?? Guid.NewGuid(),
        HotelId = hotelId ?? Guid.NewGuid(),
        Status = ReservationStatus.Cancelled,
        TotalAmount = 1000m,
        CheckInDate = DateOnly.FromDateTime(DateTime.Now.AddDays(2)),
        CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(4)),
        CreatedDate = DateTime.UtcNow
    };

    private static RefundRequest MakePendingRefund(Guid reservationId, Guid userId, Guid hotelId) => new()
    {
        RefundRequestId = Guid.NewGuid(),
        ReservationId = reservationId,
        UserId = userId,
        Reason = "Cancelled trip",
        Status = RefundRequestStatus.Pending,
        RefundAmount = 800m,
        CreatedAt = DateTime.UtcNow,
        Reservation = new Reservation
        {
            ReservationId = reservationId,
            ReservationCode = "RES001",
            HotelId = hotelId,
            UserId = userId,
            TotalAmount = 1000m,
            CheckInDate = DateOnly.FromDateTime(DateTime.Now.AddDays(2)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(4)),
            CreatedDate = DateTime.UtcNow,
            Transactions = new List<Transaction>
            {
                new() { TransactionId = Guid.NewGuid(), ReservationId = reservationId, Amount = 1000m, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success, TransactionDate = DateTime.UtcNow }
            }
        },
        User = new User { UserId = userId, Name = "Guest", Email = "g@g.com", CreatedAt = DateTime.UtcNow }
    };

    // ── CreateRefundRequestAsync ──────────────────────────────────────────────

    [Fact]
    public async Task CreateRefundRequestAsync_NoPendingExists_CreatesAndAutoApproves()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var emptyRefunds = new List<RefundRequest>().AsQueryable().BuildMock();
        _refundRepo.Setup(r => r.GetQueryable()).Returns(emptyRefunds);
        _refundRepo.Setup(r => r.AddAsync(It.IsAny<RefundRequest>())).ReturnsAsync((RefundRequest rr) => rr);

        // Act
        await _sut.CreateRefundRequestAsync(reservationId, userId, "Cancelled", 500m, "Full refund");

        // Assert
        _refundRepo.Verify(r => r.AddAsync(It.IsAny<RefundRequest>()), Times.Once);
        _walletService.Verify(w => w.CreditAsync(userId, 500m, It.IsAny<string>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeast(2));
    }

    [Fact]
    public async Task CreateRefundRequestAsync_PendingAlreadyExists_SkipsCreation()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var existingRefunds = new List<RefundRequest>
        {
            new() { RefundRequestId = Guid.NewGuid(), ReservationId = reservationId, UserId = Guid.NewGuid(), Reason = "X", Status = RefundRequestStatus.Pending, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _refundRepo.Setup(r => r.GetQueryable()).Returns(existingRefunds);

        // Act
        await _sut.CreateRefundRequestAsync(reservationId, Guid.NewGuid(), "Reason", 100m, "Note");

        // Assert
        _refundRepo.Verify(r => r.AddAsync(It.IsAny<RefundRequest>()), Times.Never);
        _walletService.Verify(w => w.CreditAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
    }

    // ── ApproveRefundAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveRefundAsync_ValidAdmin_ApprovesAndCreditsWallet()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var refund = MakePendingRefund(reservationId, userId, hotelId);

        var refunds = new List<RefundRequest> { refund }.AsQueryable().BuildMock();
        _refundRepo.Setup(r => r.GetQueryable()).Returns(refunds);

        var admin = new User { UserId = adminId, HotelId = hotelId, Name = "Admin", Email = "a@a.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);

        var dto = new ProcessRefundDto { AdminResponse = "Approved", RefundPaymentMethod = "UPI", RefundTransactionRef = "TXN123" };

        // Act
        var result = await _sut.ApproveRefundAsync(refund.RefundRequestId, adminId, dto);

        // Assert
        result.Status.Should().Be("Approved");
        _walletService.Verify(w => w.CreditAsync(userId, It.IsAny<decimal>(), It.IsAny<string>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task ApproveRefundAsync_RefundNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var refunds = new List<RefundRequest>().AsQueryable().BuildMock();
        _refundRepo.Setup(r => r.GetQueryable()).Returns(refunds);

        // Act & Assert
        await _sut.Invoking(s => s.ApproveRefundAsync(Guid.NewGuid(), Guid.NewGuid(), new ProcessRefundDto { AdminResponse = "OK" }))
            .Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task ApproveRefundAsync_AlreadyApproved_ThrowsValidation()
    {
        // Arrange
        var refund = MakePendingRefund(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        refund.Status = RefundRequestStatus.Approved;

        var refunds = new List<RefundRequest> { refund }.AsQueryable().BuildMock();
        _refundRepo.Setup(r => r.GetQueryable()).Returns(refunds);

        // Act & Assert
        await _sut.Invoking(s => s.ApproveRefundAsync(refund.RefundRequestId, Guid.NewGuid(), new ProcessRefundDto { AdminResponse = "OK" }))
            .Should().ThrowAsync<ValidationException>().WithMessage("*pending*");
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task ApproveRefundAsync_AdminNotFound_ThrowsUnauthorized()
    {
        // Arrange
        var refund = MakePendingRefund(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var refunds = new List<RefundRequest> { refund }.AsQueryable().BuildMock();
        _refundRepo.Setup(r => r.GetQueryable()).Returns(refunds);
        _userRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // Act & Assert
        await _sut.Invoking(s => s.ApproveRefundAsync(refund.RefundRequestId, Guid.NewGuid(), new ProcessRefundDto { AdminResponse = "OK" }))
            .Should().ThrowAsync<UnAuthorizedException>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task ApproveRefundAsync_WrongHotel_ThrowsUnauthorized()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var refund = MakePendingRefund(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var refunds = new List<RefundRequest> { refund }.AsQueryable().BuildMock();
        _refundRepo.Setup(r => r.GetQueryable()).Returns(refunds);

        var admin = new User { UserId = adminId, HotelId = Guid.NewGuid(), Name = "Admin", Email = "a@a.com", CreatedAt = DateTime.UtcNow }; // different hotel
        _userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);

        // Act & Assert
        await _sut.Invoking(s => s.ApproveRefundAsync(refund.RefundRequestId, adminId, new ProcessRefundDto { AdminResponse = "OK" }))
            .Should().ThrowAsync<UnAuthorizedException>().WithMessage("*not authorized*");
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task ApproveRefundAsync_NoSuccessfulTransaction_ThrowsPaymentException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var refund = MakePendingRefund(Guid.NewGuid(), Guid.NewGuid(), hotelId);
        // Remove successful transactions
        refund.Reservation!.Transactions = new List<Transaction>();

        var refunds = new List<RefundRequest> { refund }.AsQueryable().BuildMock();
        _refundRepo.Setup(r => r.GetQueryable()).Returns(refunds);

        var admin = new User { UserId = adminId, HotelId = hotelId, Name = "Admin", Email = "a@a.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);

        // Act & Assert
        await _sut.Invoking(s => s.ApproveRefundAsync(refund.RefundRequestId, adminId, new ProcessRefundDto { AdminResponse = "OK" }))
            .Should().ThrowAsync<PaymentException>().WithMessage("*No successful payment*");
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── RejectRefundAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task RejectRefundAsync_ValidAdmin_RejectsRefund()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var refund = MakePendingRefund(Guid.NewGuid(), Guid.NewGuid(), hotelId);

        var refunds = new List<RefundRequest> { refund }.AsQueryable().BuildMock();
        _refundRepo.Setup(r => r.GetQueryable()).Returns(refunds);

        var admin = new User { UserId = adminId, HotelId = hotelId, Name = "Admin", Email = "a@a.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);

        var transactions = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act
        var result = await _sut.RejectRefundAsync(refund.RefundRequestId, adminId, "Not eligible");

        // Assert
        result.Status.Should().Be("Rejected");
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task RejectRefundAsync_RefundNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var refunds = new List<RefundRequest>().AsQueryable().BuildMock();
        _refundRepo.Setup(r => r.GetQueryable()).Returns(refunds);

        // Act & Assert
        await _sut.Invoking(s => s.RejectRefundAsync(Guid.NewGuid(), Guid.NewGuid(), "reason"))
            .Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task RejectRefundAsync_AlreadyRejected_ThrowsValidation()
    {
        // Arrange
        var refund = MakePendingRefund(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        refund.Status = RefundRequestStatus.Rejected;

        var refunds = new List<RefundRequest> { refund }.AsQueryable().BuildMock();
        _refundRepo.Setup(r => r.GetQueryable()).Returns(refunds);

        // Act & Assert
        await _sut.Invoking(s => s.RejectRefundAsync(refund.RefundRequestId, Guid.NewGuid(), "reason"))
            .Should().ThrowAsync<ValidationException>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task RejectRefundAsync_WrongHotel_ThrowsUnauthorized()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var refund = MakePendingRefund(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var refunds = new List<RefundRequest> { refund }.AsQueryable().BuildMock();
        _refundRepo.Setup(r => r.GetQueryable()).Returns(refunds);

        var admin = new User { UserId = adminId, HotelId = Guid.NewGuid(), Name = "Admin", Email = "a@a.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);

        // Act & Assert
        await _sut.Invoking(s => s.RejectRefundAsync(refund.RefundRequestId, adminId, "reason"))
            .Should().ThrowAsync<UnAuthorizedException>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── GetHotelRefundRequestsPagedAsync ──────────────────────────────────────

    [Fact]
    public async Task GetHotelRefundRequestsPagedAsync_ValidAdmin_ReturnsPaged()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var admin = new User { UserId = adminId, HotelId = hotelId, Name = "Admin", Email = "a@a.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);

        var reservationId = Guid.NewGuid();
        var refund = MakePendingRefund(reservationId, Guid.NewGuid(), hotelId);
        var refunds = new List<RefundRequest> { refund }.AsQueryable().BuildMock();
        _refundRepo.Setup(r => r.GetQueryable()).Returns(refunds);

        var transactions = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act
        var result = await _sut.GetHotelRefundRequestsPagedAsync(adminId, 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
        result.RefundRequests.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetHotelRefundRequestsPagedAsync_AdminNotFound_ThrowsUnauthorized()
    {
        // Arrange
        _userRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // Act & Assert
        await _sut.Invoking(s => s.GetHotelRefundRequestsPagedAsync(Guid.NewGuid(), 1, 10))
            .Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task GetHotelRefundRequestsPagedAsync_AdminNoHotel_ThrowsUnauthorized()
    {
        // Arrange
        var admin = new User { UserId = Guid.NewGuid(), HotelId = null, Name = "A", Email = "a@a.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        // Act & Assert
        await _sut.Invoking(s => s.GetHotelRefundRequestsPagedAsync(admin.UserId, 1, 10))
            .Should().ThrowAsync<UnAuthorizedException>();
    }

    // ── GetGuestRefundRequestsPagedAsync ──────────────────────────────────────

    [Fact]
    public async Task GetGuestRefundRequestsPagedAsync_ValidUser_ReturnsPaged()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var refund = MakePendingRefund(Guid.NewGuid(), userId, hotelId);
        var refunds = new List<RefundRequest> { refund }.AsQueryable().BuildMock();
        _refundRepo.Setup(r => r.GetQueryable()).Returns(refunds);

        var transactions = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act
        var result = await _sut.GetGuestRefundRequestsPagedAsync(userId, 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    // ── GetHotelRefundRequestsAsync (non-paged) ───────────────────────────────

    [Fact]
    public async Task GetHotelRefundRequestsAsync_ValidAdmin_ReturnsAll()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var admin = new User { UserId = adminId, HotelId = hotelId, Name = "Admin", Email = "a@a.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);

        var refund = MakePendingRefund(Guid.NewGuid(), Guid.NewGuid(), hotelId);
        var refunds = new List<RefundRequest> { refund }.AsQueryable().BuildMock();
        _refundRepo.Setup(r => r.GetQueryable()).Returns(refunds);

        var transactions = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act
        var result = await _sut.GetHotelRefundRequestsAsync(adminId);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetHotelRefundRequestsAsync_AdminNoHotel_ThrowsUnauthorized()
    {
        // Arrange
        var admin = new User { UserId = Guid.NewGuid(), HotelId = null, Name = "A", Email = "a@a.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        // Act & Assert
        await _sut.Invoking(s => s.GetHotelRefundRequestsAsync(admin.UserId))
            .Should().ThrowAsync<UnAuthorizedException>();
    }

    // ── GetGuestRefundRequestsAsync (non-paged) ───────────────────────────────

    [Fact]
    public async Task GetGuestRefundRequestsAsync_ValidUser_ReturnsAll()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var refund = MakePendingRefund(Guid.NewGuid(), userId, hotelId);
        var refunds = new List<RefundRequest> { refund }.AsQueryable().BuildMock();
        _refundRepo.Setup(r => r.GetQueryable()).Returns(refunds);

        var transactions = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act
        var result = await _sut.GetGuestRefundRequestsAsync(userId);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetGuestRefundRequestsAsync_NoRefunds_ReturnsEmpty()
    {
        // Arrange
        var refunds = new List<RefundRequest>().AsQueryable().BuildMock();
        _refundRepo.Setup(r => r.GetQueryable()).Returns(refunds);

        var transactions = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act
        var result = await _sut.GetGuestRefundRequestsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeEmpty();
    }
}
