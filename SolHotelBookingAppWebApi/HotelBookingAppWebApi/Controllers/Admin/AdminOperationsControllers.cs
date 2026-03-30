using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Inventory;
using HotelBookingAppWebApi.Models.DTOs.Review;
using HotelBookingAppWebApi.Models.DTOs.Room;
using HotelBookingAppWebApi.Models.DTOs.RoomType;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    // ── SHARED PAGINATION DTOs ────────────────────────────────────────────────
    public class PageQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int PageNumber { get => Page; }
    }

    public class ReservationQueryDto : PageQueryDto
    {
        public string? Status { get; set; } = "All";
        public string? Search { get; set; }
        public string? SortField { get; set; }
        public string? SortDir { get; set; }
    }

    public class AmenityRequestAdminQueryDto : PageQueryDto
    {
        public string? Search { get; set; }
    }
    // ── ROOMS ─────────────────────────────────────────────────────────────────
    [Route("api/admin/rooms")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminRoomController : ControllerBase
    {
        private readonly IRoomService _service;
        private readonly IReservationService _reservationService;

        public AdminRoomController(IRoomService service, IReservationService reservationService)
        {
            _service = service;
            _reservationService = reservationService;
        }

        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateRoomDto dto)
        {
            await _service.AddRoomAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "Room added successfully." });
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateRoomDto dto)
        {
            await _service.UpdateRoomAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "Room updated successfully." });
        }

        [HttpPatch("{roomId}/status")]
        public async Task<IActionResult> Toggle(Guid roomId, [FromQuery] bool isActive)
        {
            await _service.ToggleRoomStatusAsync(GetUserId(), roomId, isActive);
            return Ok(new { success = true, message = "Room status updated." });
        }

        /// <summary>List all rooms for this admin's hotel (paged). TotalCount included in response wrapper.</summary>
        [HttpPost("list")]
        public async Task<IActionResult> List([FromBody] PageQueryDto dto)
        {
            var userId = GetUserId();
            var rooms = await _service.GetRoomsByHotelAsync(userId, dto.PageNumber, dto.PageSize);
            var totalCount = await _service.GetRoomCountByHotelAsync(userId);
            return Ok(new { success = true, data = new { totalCount, items = rooms } });
        }

        /// <summary>
        /// Correction 6D: Room occupancy for a specific date.
        /// Returns all physical rooms in the admin's hotel with IsOccupied flag.
        /// GET /api/admin/rooms/occupancy?date=2025-12-25
        /// No pagination needed — always returns the full hotel room list for one date.
        /// </summary>
        [HttpGet("occupancy")]
        public async Task<IActionResult> GetOccupancy([FromQuery] DateOnly date)
        {
            var result = await _reservationService.GetRoomOccupancyAsync(GetUserId(), date);
            return Ok(new { success = true, data = result });
        }
    }

    // ── ROOM TYPES ────────────────────────────────────────────────────────────
    [Route("api/admin/roomtypes")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminRoomTypeController : ControllerBase
    {
        private readonly IRoomTypeService _service;
        public AdminRoomTypeController(IRoomTypeService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>
        /// Correction 9A: Paged room types.
        /// POST /api/admin/roomtypes/list
        /// Returns { totalCount, roomTypes } for Angular Material paginator.
        /// </summary>
        [HttpPost("list")]
        public async Task<IActionResult> List([FromBody] PageQueryDto dto)
        {
            var result = await _service.GetRoomTypesByHotelPagedAsync(GetUserId(), dto.Page, dto.PageSize);
            return Ok(new { success = true, data = result });
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateRoomTypeDto dto)
        {
            await _service.AddRoomTypeAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "RoomType added successfully." });
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateRoomTypeDto dto)
        {
            await _service.UpdateRoomTypeAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "RoomType updated successfully." });
        }

        [HttpPatch("{roomTypeId}/status")]
        public async Task<IActionResult> ToggleStatus(Guid roomTypeId, [FromQuery] bool isActive)
        {
            await _service.ToggleRoomTypeStatusAsync(GetUserId(), roomTypeId, isActive);
            return Ok(new { success = true, message = "RoomType status updated." });
        }

        [HttpPost("rate")]
        public async Task<IActionResult> AddRate([FromBody] CreateRoomTypeRateDto dto)
        {
            await _service.AddRateAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "Rate added successfully." });
        }

        [HttpPut("rate")]
        public async Task<IActionResult> UpdateRate([FromBody] UpdateRoomTypeRateDto dto)
        {
            await _service.UpdateRateAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "Rate updated successfully." });
        }

        [HttpPost("rate-by-date")]
        public async Task<IActionResult> GetRate([FromBody] GetRateByDateRequestDto dto)
        {
            var rate = await _service.GetRateByDateAsync(GetUserId(), dto);
            return Ok(new { success = true, data = rate });
        }

        [HttpGet("{roomTypeId}/rates")]
        public async Task<IActionResult> GetRates(Guid roomTypeId)
        {
            var rates = await _service.GetRatesAsync(GetUserId(), roomTypeId);
            return Ok(new { success = true, data = rates });
        }
    }

    // ── INVENTORY ─────────────────────────────────────────────────────────────
    [Route("api/admin/inventory")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminInventoryController : ControllerBase
    {
        private readonly IInventoryService _service;
        public AdminInventoryController(IInventoryService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateInventoryDto dto)
        {
            await _service.AddInventoryAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "Inventory added successfully." });
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateInventoryDto dto)
        {
            await _service.UpdateInventoryAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "Inventory updated successfully." });
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] Guid roomTypeId,
            [FromQuery] DateOnly start,
            [FromQuery] DateOnly end)
        {
            var data = await _service.GetInventoryAsync(GetUserId(), roomTypeId, start, end);
            return Ok(new { success = true, data });
        }
    }

    // ── RESERVATIONS ──────────────────────────────────────────────────────────
    [Route("api/admin/reservations")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminReservationController : ControllerBase
    {
        private readonly IReservationService _service;
        public AdminReservationController(IReservationService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>List reservations with optional status filter and search</summary>
        [HttpPost("list")]
        public async Task<IActionResult> GetAll([FromBody] ReservationQueryDto dto)
        {
            var result = await _service.GetAdminReservationsAsync(GetUserId(), dto.Status ?? "All", dto.Search, dto.Page, dto.PageSize, dto.SortField, dto.SortDir);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Mark a confirmed reservation as completed</summary>
        [HttpPatch("{code}/complete")]
        public async Task<IActionResult> Complete(string code)
        {
            await _service.CompleteReservationAsync(code);
            return Ok(new { success = true, message = "Reservation marked as completed." });
        }

        /// <summary>Confirm a pending reservation</summary>
        [HttpPatch("{code}/confirm")]
        public async Task<IActionResult> Confirm(string code)
        {
            await _service.ConfirmReservationAsync(code);
            return Ok(new { success = true, message = "Reservation confirmed." });
        }
    }

    // ── ADMIN WALLET VIEW ─────────────────────────────────────────────────────
    [Route("api/admin/wallet")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminWalletController : ControllerBase
    {
        private readonly IWalletService _service;
        public AdminWalletController(IWalletService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>Admin views a guest's wallet balance</summary>
        [HttpGet("guest/{guestUserId}")]
        public async Task<IActionResult> GetGuestWallet(Guid guestUserId)
        {
            var result = await _service.GetGuestWalletByAdminAsync(GetUserId(), guestUserId);
            return Ok(new { success = true, data = result });
        }
    }

    // ── AMENITY REQUESTS (Admin) ──────────────────────────────────────────────
    [Route("api/admin/amenity-requests")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminAmenityRequestController : ControllerBase
    {
        private readonly IAmenityRequestService _service;
        public AdminAmenityRequestController(IAmenityRequestService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] HotelBookingAppWebApi.Models.DTOs.AmenityRequest.CreateAmenityRequestDto dto)
        {
            var result = await _service.CreateRequestAsync(GetUserId(), dto);
            return Ok(new { success = true, data = result });
        }

        [HttpPost("list")]
        public async Task<IActionResult> GetMine([FromBody] AmenityRequestAdminQueryDto dto)
        {
            var result = await _service.GetAdminRequestsPagedAsync(GetUserId(), dto.Page, dto.PageSize, dto.Search);
            return Ok(new { success = true, data = result });
        }
    }

    // ── ADMIN REVIEWS ─────────────────────────────────────────────────────────
    [Route("api/admin/reviews")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        public AdminReviewsController(IReviewService reviewService) => _reviewService = reviewService;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>Admin view of all reviews for their hotel, paged with optional rating filter and sort.</summary>
        [HttpPost]
        public async Task<IActionResult> GetHotelReviews([FromBody] GetHotelReviewsRequestDto dto)
        {
            var result = await _reviewService.GetAdminHotelReviewsAsync(
                GetUserId(), dto.Page, dto.PageSize, dto.MinRating, dto.MaxRating, dto.SortDir);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Admin replies to a guest review.</summary>
        [HttpPatch("{reviewId}/reply")]
        public async Task<IActionResult> Reply(Guid reviewId, [FromBody] ReplyToReviewDto dto)
        {
            await _reviewService.ReplyToReviewAsync(GetUserId(), reviewId, dto.AdminReply);
            return Ok(new { success = true, message = "Reply saved." });
        }
    }
}
