using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Reservation;
using HotelBookingAppWebApi.Models.DTOs.Room;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IRepository<Guid, Reservation> _reservationRepo;
        private readonly IRepository<Guid, Room> _roomRepo;
        private readonly IRepository<Guid, RoomType> _roomTypeRepo;
        private readonly IRepository<Guid, RoomTypeInventory> _inventoryRepo;
        private readonly IRepository<Guid, RoomTypeRate> _rateRepo;
        private readonly IRepository<Guid, ReservationRoom> _reservationRoomRepo;
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IRefundRequestService _refundRequestService;
        private readonly IUnitOfWork _unitOfWork;

        public ReservationService(
            IRepository<Guid, Reservation> reservationRepo,
            IRepository<Guid, Room> roomRepo,
            IRepository<Guid, RoomType> roomTypeRepo,
            IRepository<Guid, RoomTypeInventory> inventoryRepo,
            IRepository<Guid, RoomTypeRate> rateRepo,
            IRepository<Guid, ReservationRoom> reservationRoomRepo,
            IRepository<Guid, User> userRepo,
            IRefundRequestService refundRequestService,
            IUnitOfWork unitOfWork)
        {
            _reservationRepo = reservationRepo;
            _roomRepo = roomRepo;
            _roomTypeRepo = roomTypeRepo;
            _inventoryRepo = inventoryRepo;
            _rateRepo = rateRepo;
            _reservationRoomRepo = reservationRoomRepo;
            _userRepo = userRepo;
            _refundRequestService = refundRequestService;
            _unitOfWork = unitOfWork;
        }

        // ── CREATE RESERVATION ────────────────────────────────────────────────
        public async Task<ReservationResponseDto> CreateReservationAsync(Guid userId, CreateReservationDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                if (dto.CheckInDate < today)
                    throw new ValidationException("Check-in date cannot be in the past.");

                if (dto.CheckInDate >= dto.CheckOutDate)
                    throw new ValidationException("Check-out must be after check-in.");

                var totalDays = dto.CheckOutDate.DayNumber - dto.CheckInDate.DayNumber;
                if (totalDays < 1)
                    throw new ValidationException("Minimum booking is 1 full night (check-out must be at least 1 day after check-in).");

                if (dto.NumberOfRooms <= 0)
                    throw new ValidationException("Number of rooms must be at least 1.");

                var dates = Enumerable.Range(0, totalDays)
                    .Select(d => dto.CheckInDate.AddDays(d))
                    .ToList();

                // Validate RoomType belongs to the hotel and is active
                var roomType = await _roomTypeRepo.GetQueryable()
                    .FirstOrDefaultAsync(r =>
                        r.RoomTypeId == dto.RoomTypeId &&
                        r.HotelId == dto.HotelId &&
                        r.IsActive)
                    ?? throw new NotFoundException("Invalid or inactive room type.");

                // Validate physical active rooms exist
                var totalActiveRooms = await _roomRepo.GetQueryable()
                    .CountAsync(r =>
                        r.RoomTypeId == dto.RoomTypeId &&
                        r.HotelId == dto.HotelId &&
                        r.IsActive);

                if (dto.NumberOfRooms > totalActiveRooms)
                    throw new InsufficientInventoryException(
                        $"Only {totalActiveRooms} active rooms available for this type.");

                // Fetch inventory for all dates in one query
                var inventories = await _inventoryRepo.GetQueryable()
                    .Where(i => i.RoomTypeId == dto.RoomTypeId && dates.Contains(i.Date))
                    .ToListAsync();

                if (inventories.Count != dates.Count)
                    throw new InsufficientInventoryException(
                        "Inventory not configured for one or more dates in the requested range.");

                // Fetch applicable rates
                var rates = await _rateRepo.GetQueryable()
                    .Where(r =>
                        r.RoomTypeId == dto.RoomTypeId &&
                        r.StartDate <= dto.CheckOutDate &&
                        r.EndDate >= dto.CheckInDate)
                    .ToListAsync();

                var inventoryMap = inventories.ToDictionary(i => i.Date);
                decimal totalAmount = 0;

                foreach (var date in dates)
                {
                    var inv = inventoryMap[date];

                    if (inv.AvailableInventory < dto.NumberOfRooms)
                        throw new InsufficientInventoryException(
                            $"Insufficient inventory on {date}.");

                    var rate = rates.FirstOrDefault(r => date >= r.StartDate && date <= r.EndDate)
                        ?? throw new RateNotFoundException($"No rate configured for {date}.");

                    totalAmount += rate.Rate * dto.NumberOfRooms;
                }

                // Create Reservation entity
                var reservation = new Reservation
                {
                    ReservationId = Guid.NewGuid(),
                    ReservationCode = GenerateCode(),
                    UserId = userId,
                    HotelId = dto.HotelId,
                    CheckInDate = dto.CheckInDate,
                    CheckOutDate = dto.CheckOutDate,
                    TotalAmount = totalAmount,
                    Status = ReservationStatus.Pending,
                    IsCheckedIn = false,
                    CreatedDate = DateTime.UtcNow,
                    ExpiryTime = DateTime.UtcNow.AddMinutes(10)
                };

                await _reservationRepo.AddAsync(reservation);

                // Assign rooms — honour guest's room selection if provided, else auto-assign
                List<Room> assignedRooms;

                if (dto.SelectedRoomIds != null && dto.SelectedRoomIds.Count > 0)
                {
                    if (dto.SelectedRoomIds.Count != dto.NumberOfRooms)
                        throw new ValidationException(
                            "Selected room count must match the requested number of rooms.");

                    assignedRooms = await _roomRepo.GetQueryable()
                        .Where(r =>
                            dto.SelectedRoomIds.Contains(r.RoomId) &&
                            r.RoomTypeId == dto.RoomTypeId &&
                            r.HotelId == dto.HotelId &&
                            r.IsActive)
                        .ToListAsync();

                    if (assignedRooms.Count != dto.NumberOfRooms)
                        throw new ValidationException(
                            "One or more selected rooms are invalid or unavailable.");
                }
                else
                {
                    assignedRooms = await _roomRepo.GetQueryable()
                        .Where(r =>
                            r.RoomTypeId == dto.RoomTypeId &&
                            r.HotelId == dto.HotelId &&
                            r.IsActive)
                        .Take(dto.NumberOfRooms)
                        .ToListAsync();

                    if (assignedRooms.Count < dto.NumberOfRooms)
                        throw new InsufficientInventoryException("Not enough active rooms to assign.");
                }

                var pricePerNight = totalAmount / totalDays / dto.NumberOfRooms;

                foreach (var room in assignedRooms)
                {
                    await _reservationRoomRepo.AddAsync(new ReservationRoom
                    {
                        ReservationRoomId = Guid.NewGuid(),
                        ReservationId = reservation.ReservationId,
                        RoomTypeId = dto.RoomTypeId,
                        RoomId = room.RoomId,
                        PricePerNight = pricePerNight
                    });
                }

                // Decrement available inventory for every date
                foreach (var inv in inventories)
                    inv.ReservedInventory += dto.NumberOfRooms;

                await _unitOfWork.CommitAsync();

                return new ReservationResponseDto
                {
                    ReservationId = reservation.ReservationId,
                    ReservationCode = reservation.ReservationCode,
                    TotalAmount = totalAmount,
                    Status = reservation.Status.ToString(),
                    TotalRooms = assignedRooms.Count,
                    Rooms = assignedRooms.Select(r => new RoomSummaryDto
                    {
                        RoomId = r.RoomId,
                        RoomNumber = r.RoomNumber,
                        Floor = r.Floor
                    }).ToList()
                };
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ── GET BY CODE ───────────────────────────────────────────────────────
        public async Task<ReservationDetailsDto> GetReservationByCodeAsync(Guid userId, string code)
        {
            var res = await _reservationRepo.GetQueryable()
                .Include(r => r.ReservationRooms!)
                    .ThenInclude(rr => rr.Room)
                .Include(r => r.ReservationRooms!)
                    .ThenInclude(rr => rr.RoomType)
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.ReservationCode == code && r.UserId == userId)
                ?? throw new NotFoundException("Reservation not found.");

            return MapToDetailsDto(res);
        }

        // ── GET MY RESERVATIONS (ALL) ─────────────────────────────────────────
        public async Task<IEnumerable<ReservationDetailsDto>> GetMyReservationsAsync(Guid userId)
        {
            var list = await _reservationRepo.GetQueryable()
                .Include(r => r.ReservationRooms!)
                    .ThenInclude(rr => rr.Room)
                .Include(r => r.ReservationRooms!)
                    .ThenInclude(rr => rr.RoomType)
                .Include(r => r.Hotel)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            return list.Select(MapToDetailsDto);
        }

        // ── GET MY RESERVATIONS (PAGED) ───────────────────────────────────────
        public async Task<PagedReservationResponseDto> GetMyReservationsPagedAsync(
            Guid userId, int page, int pageSize)
        {
            var query = _reservationRepo.GetQueryable()
                .Include(r => r.ReservationRooms!)
                    .ThenInclude(rr => rr.Room)
                .Include(r => r.ReservationRooms!)
                    .ThenInclude(rr => rr.RoomType)
                .Include(r => r.Hotel)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedDate);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedReservationResponseDto
            {
                TotalCount = total,
                Reservations = items.Select(MapToDetailsDto)
            };
        }

        // ── GET HOTEL RESERVATIONS (ADMIN, PAGED) ─────────────────────────────
        public async Task<PagedReservationResponseDto> GetHotelReservationsAsync(
            Guid userId, int page, int pageSize)
        {
            var admin = await _userRepo.GetAsync(userId)
                ?? throw new UnAuthorizedException("Unauthorized.");

            if (admin.HotelId == null)
                throw new UnAuthorizedException("No hotel associated with this admin.");

            var query = _reservationRepo.GetQueryable()
                .Include(r => r.ReservationRooms!)
                    .ThenInclude(rr => rr.Room)
                .Include(r => r.ReservationRooms!)
                    .ThenInclude(rr => rr.RoomType)
                .Include(r => r.Hotel)
                .Include(r => r.User)
                .Where(r => r.HotelId == admin.HotelId)
                .OrderByDescending(r => r.CreatedDate);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedReservationResponseDto
            {
                TotalCount = total,
                Reservations = items.Select(MapToDetailsDto)
            };
        }

        // ── CANCEL RESERVATION ────────────────────────────────────────────────
        public async Task<bool> CancelReservationAsync(Guid userId, string code, string reason)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var res = await _reservationRepo.GetQueryable()
                    .Include(r => r.ReservationRooms)
                    .Include(r => r.Transactions)
                    .FirstOrDefaultAsync(r => r.ReservationCode == code && r.UserId == userId)
                    ?? throw new NotFoundException("Reservation not found.");

                if (res.Status == ReservationStatus.Cancelled)
                    throw new ReservationFailedException("Reservation is already cancelled.");

                if (res.Status == ReservationStatus.Completed)
                    throw new ValidationException("Completed reservations cannot be cancelled.");

                var dates = Enumerable.Range(0,
                        res.CheckOutDate.DayNumber - res.CheckInDate.DayNumber)
                    .Select(d => res.CheckInDate.AddDays(d))
                    .ToList();

                var roomTypeId = res.ReservationRooms!.First().RoomTypeId;

                // Restore inventory
                var inventories = await _inventoryRepo.GetQueryable()
                    .Where(i => i.RoomTypeId == roomTypeId && dates.Contains(i.Date))
                    .ToListAsync();

                var roomCount = res.ReservationRooms?.Count ?? 0;
                foreach (var inv in inventories)
                    inv.ReservedInventory = Math.Max(0, inv.ReservedInventory - roomCount);

                res.Status = ReservationStatus.Cancelled;
                res.CancelledDate = DateTime.UtcNow;
                res.CancellationReason = reason;

                await _unitOfWork.CommitAsync();

                // If there was a successful payment, create a refund request (Pending approval)
                var hasPaid = res.Transactions?.Any(t => t.Status == PaymentStatus.Success) ?? false;
                if (hasPaid)
                {
                    await _refundRequestService.CreateRefundRequestAsync(
                        res.ReservationId, userId, reason);
                }

                return true;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ── COMPLETE RESERVATION (Admin) ──────────────────────────────────────
        // When admin marks a reservation as Completed, we also set IsCheckedIn = true.
        // This means "complete" implies the guest checked in and stayed.
        // This prevents the NoShowAutoCancelService from mistakenly flagging it,
        // and gives the frontend a clear checked-in indicator for the guest's history.
        public async Task<bool> CompleteReservationAsync(string code)
        {
            var res = await _reservationRepo.FirstOrDefaultAsync(r => r.ReservationCode == code)
                ?? throw new NotFoundException("Reservation not found.");

            if (res.Status != ReservationStatus.Confirmed)
                throw new ValidationException("Only confirmed reservations can be marked as completed.");

            res.Status = ReservationStatus.Completed;
            res.IsCheckedIn = true; // Guest physically checked in — set alongside completion
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        // ── AVAILABLE ROOMS ───────────────────────────────────────────────────
        public async Task<IEnumerable<AvailableRoomDto>> GetAvailableRoomsAsync(
            Guid hotelId, Guid roomTypeId, DateOnly checkIn, DateOnly checkOut)
        {
            // Rooms currently booked (confirmed/pending) for any overlapping date
            var bookedRoomIds = await _reservationRoomRepo.GetQueryable()
                .Where(rr =>
                    rr.RoomTypeId == roomTypeId &&
                    rr.Reservation!.HotelId == hotelId &&
                    (rr.Reservation.Status == ReservationStatus.Confirmed ||
                     rr.Reservation.Status == ReservationStatus.Pending) &&
                    rr.Reservation.CheckInDate < checkOut &&
                    rr.Reservation.CheckOutDate > checkIn)
                .Select(rr => rr.RoomId)
                .Distinct()
                .ToListAsync();

            var availableRooms = await _roomRepo.GetQueryable()
                .Include(r => r.RoomType)
                .Where(r =>
                    r.HotelId == hotelId &&
                    r.RoomTypeId == roomTypeId &&
                    r.IsActive &&
                    !bookedRoomIds.Contains(r.RoomId))
                .ToListAsync();

            return availableRooms.Select(r => new AvailableRoomDto
            {
                RoomId = r.RoomId,
                RoomNumber = r.RoomNumber,
                Floor = r.Floor,
                RoomTypeName = r.RoomType!.Name
            });
        }

        // ── ROOM OCCUPANCY (Correction 6B) ────────────────────────────────────
        // For a given hotel + date, returns every physical room with IsOccupied flag.
        // NOTE (Correction 10A): Each ReservationRoom has a distinct RoomId because
        // _roomRepo.GetQueryable() returns physical rooms with unique IDs — no duplicates.
        public async Task<IEnumerable<RoomOccupancyDto>> GetRoomOccupancyAsync(Guid adminUserId, DateOnly date)
        {
            var admin = await _userRepo.GetAsync(adminUserId)
                ?? throw new UnAuthorizedException("Unauthorized.");

            if (admin.HotelId == null)
                throw new UnAuthorizedException("Unauthorized.");

            var hotelId = admin.HotelId.Value;

            // Get all active rooms for the hotel
            var rooms = await _roomRepo.GetQueryable()
                .Include(r => r.RoomType)
                .Where(r => r.HotelId == hotelId && r.IsActive)
                .ToListAsync();

            // Get all reservations covering this date (Confirmed or Pending)
            var occupiedRoomIds = await _reservationRoomRepo.GetQueryable()
                .Include(rr => rr.Reservation)
                .Where(rr =>
                    rr.Reservation!.HotelId == hotelId &&
                    (rr.Reservation.Status == ReservationStatus.Confirmed ||
                     rr.Reservation.Status == ReservationStatus.Pending) &&
                    rr.Reservation.CheckInDate <= date &&
                    rr.Reservation.CheckOutDate > date)
                .Select(rr => new { rr.RoomId, rr.Reservation!.ReservationCode })
                .ToListAsync();

            var occupancyMap = occupiedRoomIds.ToDictionary(x => x.RoomId, x => x.ReservationCode);

            return rooms.Select(r => new RoomOccupancyDto
            {
                RoomId = r.RoomId,
                RoomNumber = r.RoomNumber,
                Floor = r.Floor,
                RoomTypeName = r.RoomType?.Name ?? string.Empty,
                IsOccupied = occupancyMap.ContainsKey(r.RoomId),
                ReservationCode = occupancyMap.TryGetValue(r.RoomId, out var code) ? code : null
            });
        }

        // ── HELPERS ───────────────────────────────────────────────────────────
        private static ReservationDetailsDto MapToDetailsDto(Reservation r)
        {
            var firstRoomType = r.ReservationRooms?.FirstOrDefault()?.RoomType;
            return new ReservationDetailsDto
            {
                ReservationCode = r.ReservationCode,
                ReservationId = r.ReservationId,
                HotelId = r.HotelId,
                HotelName = r.Hotel?.Name ?? string.Empty,
                RoomTypeId = firstRoomType?.RoomTypeId ?? Guid.Empty,
                RoomTypeName = firstRoomType?.Name ?? string.Empty,
                CheckInDate = r.CheckInDate,
                CheckOutDate = r.CheckOutDate,
                NumberOfRooms = r.ReservationRooms?.Count ?? 0,
                TotalAmount = r.TotalAmount,
                Status = r.Status.ToString(),
                IsCheckedIn = r.IsCheckedIn,
                CreatedDate = r.CreatedDate,
                Rooms = r.ReservationRooms?.Select(rr => new RoomSummaryDto
                {
                    RoomId = rr.RoomId,
                    RoomNumber = rr.Room?.RoomNumber ?? string.Empty,
                    Floor = rr.Room?.Floor ?? 0
                }).ToList() ?? new List<RoomSummaryDto>()
            };
        }

        private static string GenerateCode()
            => $"RES-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}