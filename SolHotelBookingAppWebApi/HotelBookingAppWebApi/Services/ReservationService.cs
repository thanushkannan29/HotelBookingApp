using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Reservation;
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
        private readonly IUnitOfWork _unitOfWork;

        public ReservationService(
            IRepository<Guid, Reservation> reservationRepo,
            IRepository<Guid, Room> roomRepo,
            IRepository<Guid, RoomType> roomTypeRepo,
            IRepository<Guid, RoomTypeInventory> inventoryRepo,
            IRepository<Guid, RoomTypeRate> rateRepo,
            IRepository<Guid, ReservationRoom> reservationRoomRepo,
            IUnitOfWork unitOfWork)
        {
            _reservationRepo = reservationRepo;
            _roomRepo = roomRepo;
            _roomTypeRepo = roomTypeRepo;
            _inventoryRepo = inventoryRepo;
            _rateRepo = rateRepo;
            _reservationRoomRepo = reservationRoomRepo;
            _unitOfWork = unitOfWork;
        }

        #region CREATE

        public async Task<ReservationResponseDto> CreateReservationAsync(Guid userId, CreateReservationDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                if (dto.CheckInDate < today || dto.CheckInDate >= dto.CheckOutDate)
                    throw new ValidationException("Invalid booking dates");

                if (dto.NumberOfRooms <= 0)
                    throw new ValidationException("Invalid room count");

                var totalDays = dto.CheckOutDate.DayNumber - dto.CheckInDate.DayNumber;
                var dates = Enumerable.Range(0, totalDays)
                    .Select(d => dto.CheckInDate.AddDays(d))
                    .ToList();

                //  Validate RoomType
                var roomType = await _roomTypeRepo.GetQueryable()
                    .FirstOrDefaultAsync(r =>
                        r.RoomTypeId == dto.RoomTypeId &&
                        r.HotelId == dto.HotelId &&
                        r.IsActive);

                if (roomType == null)
                    throw new NotFoundException("Invalid room type");

                //  Validate physical rooms
                var totalRooms = await _roomRepo.GetQueryable()
                    .CountAsync(r =>
                        r.RoomTypeId == dto.RoomTypeId &&
                        r.HotelId == dto.HotelId &&
                        r.IsActive);

                if (dto.NumberOfRooms > totalRooms)
                    throw new InsufficientInventoryException($"Only {totalRooms} rooms available");

                //  Fetch inventory + rates (ONE DB HIT EACH)
                var inventories = await _inventoryRepo.GetQueryable()
                    .Where(i => i.RoomTypeId == dto.RoomTypeId && dates.Contains(i.Date))
                    .ToListAsync();

                var rates = await _rateRepo.GetQueryable()
                    .Where(r =>
                        r.RoomTypeId == dto.RoomTypeId &&
                        r.StartDate <= dto.CheckOutDate &&
                        r.EndDate >= dto.CheckInDate)
                    .ToListAsync();

                if (inventories.Count != dates.Count)
                    throw new InsufficientInventoryException("Inventory missing");

                //  Convert to dictionary (O(1))
                var inventoryMap = inventories.ToDictionary(i => i.Date);

                decimal totalAmount = 0;

                foreach (var date in dates)
                {
                    var inventory = inventoryMap[date];

                    if (inventory.AvailableInventory < dto.NumberOfRooms)
                        throw new InsufficientInventoryException($"Insufficient inventory on {date}");

                    var rate = rates.FirstOrDefault(r => date >= r.StartDate && date <= r.EndDate)
                        ?? throw new RateNotFoundException($"Rate missing for {date}");

                    totalAmount += rate.Rate * dto.NumberOfRooms;
                }

                //  Create Reservation
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
                    CreatedDate = DateTime.UtcNow,
                    ExpiryTime = DateTime.UtcNow.AddMinutes(10)
                };

                await _reservationRepo.AddAsync(reservation);

                //  Assign rooms
                var rooms = await _roomRepo.GetQueryable()
                    .Where(r =>
                        r.RoomTypeId == dto.RoomTypeId &&
                        r.HotelId == dto.HotelId &&
                        r.IsActive)
                    .Take(dto.NumberOfRooms)
                    .ToListAsync();

                if (rooms.Count < dto.NumberOfRooms)
                    throw new InsufficientInventoryException("Not enough rooms");

                var pricePerNight = totalAmount / totalDays / dto.NumberOfRooms;

                foreach (var room in rooms)
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

                //  Update inventory
                foreach (var inv in inventories)
                    inv.ReservedInventory += dto.NumberOfRooms;

                await _unitOfWork.CommitAsync();

                return new ReservationResponseDto
                {
                    ReservationId = reservation.ReservationId,
                    ReservationCode = reservation.ReservationCode,
                    TotalAmount = totalAmount,
                    Status = reservation.Status.ToString()
                };
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region GET

        public async Task<ReservationDetailsDto> GetReservationByCodeAsync(Guid userId, string code)
        {
            var res = await _reservationRepo.GetQueryable()
                .Include(r => r.ReservationRooms)
                .FirstOrDefaultAsync(r => r.ReservationCode == code && r.UserId == userId)
                ?? throw new NotFoundException("Reservation not found");

            return MapToDto(res);
        }

        public async Task<IEnumerable<ReservationDetailsDto>> GetMyReservationsAsync(Guid userId)
        {
            var list = await _reservationRepo.GetQueryable()
                .Include(r => r.ReservationRooms)
                .Where(r => r.UserId == userId)
                .ToListAsync();

            return list.Select(MapToDto);
        }

        #endregion

        #region CANCEL

        public async Task<bool> CancelReservationAsync(Guid userId, string code, string reason)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var res = await _reservationRepo.GetQueryable()
                    .Include(r => r.ReservationRooms)
                    .FirstOrDefaultAsync(r => r.ReservationCode == code && r.UserId == userId)
                    ?? throw new NotFoundException("Reservation not found");

                if (res.Status is ReservationStatus.Cancelled)
                    throw new ReservationFailedException("Already cancelled");

                if (res.Status is ReservationStatus.Completed)
                    throw new ValidationException("Cannot cancel completed");

                var dates = Enumerable.Range(0,
                        res.CheckOutDate.DayNumber - res.CheckInDate.DayNumber)
                    .Select(d => res.CheckInDate.AddDays(d))
                    .ToList();

                var roomTypeId = res.ReservationRooms!.First().RoomTypeId;

                var inventories = await _inventoryRepo.GetQueryable()
                    .Where(i => i.RoomTypeId == roomTypeId && dates.Contains(i.Date))
                    .ToListAsync();

                foreach (var inv in inventories)
                    inv.ReservedInventory -= res.ReservationRooms.Count;

                res.Status = ReservationStatus.Cancelled;
                res.CancelledDate = DateTime.UtcNow;
                res.CancellationReason = reason;

                await _unitOfWork.CommitAsync();
                return true;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region COMPLETE

        public async Task<bool> CompleteReservationAsync(string code)
        {
            var res = await _reservationRepo.FirstOrDefaultAsync(r => r.ReservationCode == code)
                ?? throw new NotFoundException("Reservation not found");

            if (res.Status != ReservationStatus.Confirmed)
                throw new ValidationException("Only confirmed can complete");

            res.Status = ReservationStatus.Completed;

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        #endregion

        #region HELPERS

        private static ReservationDetailsDto MapToDto(Reservation r) => new()
        {
            ReservationCode = r.ReservationCode,
            HotelId = r.HotelId,
            RoomTypeId = r.ReservationRooms!.First().RoomTypeId,
            NumberOfRooms = r.ReservationRooms.Count,
            CheckInDate = r.CheckInDate,
            CheckOutDate = r.CheckOutDate,
            TotalAmount = r.TotalAmount,
            Status = r.Status.ToString()
        };

        private static string GenerateCode()
            => $"RES-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        #endregion
    }
}
