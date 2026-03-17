using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.RoomType;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class RoomTypeService : IRoomTypeService
    {
        private readonly IRepository<Guid, RoomType> _roomTypeRepo;
        private readonly IRepository<Guid, RoomTypeRate> _rateRepo;
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IUnitOfWork _unitOfWork;

        public RoomTypeService(
            IRepository<Guid, RoomType> roomTypeRepo,
            IRepository<Guid, RoomTypeRate> rateRepo,
            IRepository<Guid, User> userRepo,
            IUnitOfWork unitOfWork)
        {
            _roomTypeRepo = roomTypeRepo;
            _rateRepo = rateRepo;
            _userRepo = userRepo;
            _unitOfWork = unitOfWork;
        }

        #region ADD ROOM TYPE

        public async Task AddRoomTypeAsync(Guid userId, CreateRoomTypeDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var user = await _userRepo.GetAsync(userId)
                    ?? throw new UnAuthorizedException("Unauthorized");

                if (user.HotelId == null)
                    throw new UnAuthorizedException("Unauthorized");

                var roomType = new RoomType
                {
                    RoomTypeId = Guid.NewGuid(),
                    HotelId = user.HotelId.Value,
                    Name = dto.Name,
                    Description = dto.Description,
                    MaxOccupancy = dto.MaxOccupancy,
                    Amenities = dto.Amenities,
                    IsActive = true
                };

                await _roomTypeRepo.AddAsync(roomType);

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region UPDATE ROOM TYPE

        public async Task UpdateRoomTypeAsync(Guid userId, UpdateRoomTypeDto dto)
        {
            var user = await _userRepo.GetAsync(userId)
                ?? throw new UnAuthorizedException("Unauthorized");

            var roomType = await _roomTypeRepo.GetQueryable()
                .FirstOrDefaultAsync(r =>
                    r.RoomTypeId == dto.RoomTypeId &&
                    r.HotelId == user.HotelId);

            if (roomType == null)
                throw new NotFoundException("RoomType not found");

            roomType.Name = dto.Name;
            roomType.Description = dto.Description;
            roomType.MaxOccupancy = dto.MaxOccupancy;
            roomType.Amenities = dto.Amenities;

            await _unitOfWork.SaveChangesAsync();
        }

        #endregion

        #region TOGGLE STATUS

        public async Task ToggleRoomTypeStatusAsync(Guid userId, Guid roomTypeId, bool isActive)
        {
            var user = await _userRepo.GetAsync(userId)
                ?? throw new UnAuthorizedException("Unauthorized");

            var roomType = await _roomTypeRepo.GetQueryable()
                .FirstOrDefaultAsync(r =>
                    r.RoomTypeId == roomTypeId &&
                    r.HotelId == user.HotelId);

            if (roomType == null)
                throw new NotFoundException("RoomType not found");

            roomType.IsActive = isActive;

            await _unitOfWork.SaveChangesAsync();
        }

        #endregion

        #region ADD RATE

        public async Task AddRateAsync(Guid userId, CreateRoomTypeRateDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var user = await _userRepo.GetAsync(userId)
                    ?? throw new UnAuthorizedException("Unauthorized");

                var roomTypeExists = await _roomTypeRepo.GetQueryable()
                    .AnyAsync(r =>
                        r.RoomTypeId == dto.RoomTypeId &&
                        r.HotelId == user.HotelId);

                if (!roomTypeExists)
                    throw new NotFoundException("RoomType not found");

                if (dto.StartDate > dto.EndDate)
                    throw new ValidationException("Invalid date range");

                // Overlap check
                var overlapping = await _rateRepo.GetQueryable()
                    .AnyAsync(r =>
                        r.RoomTypeId == dto.RoomTypeId &&
                        dto.StartDate <= r.EndDate &&
                        dto.EndDate >= r.StartDate);

                if (overlapping)
                    throw new ValidationException("Rate already exists for date range");

                var rate = new RoomTypeRate
                {
                    RoomTypeRateId = Guid.NewGuid(),
                    RoomTypeId = dto.RoomTypeId,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Rate = dto.Rate
                };

                await _rateRepo.AddAsync(rate);

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region UPDATE RATE

        public async Task UpdateRateAsync(Guid userId, UpdateRoomTypeRateDto dto)
        {
            var user = await _userRepo.GetAsync(userId)
                ?? throw new UnAuthorizedException("Unauthorized");

            var rate = await _rateRepo.GetQueryable()
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomTypeRateId == dto.RoomTypeRateId);

            if (rate == null || rate.RoomType!.HotelId != user.HotelId)
                throw new UnAuthorizedException("Unauthorized");

            rate.StartDate = dto.StartDate;
            rate.EndDate = dto.EndDate;
            rate.Rate = dto.Rate;

            await _unitOfWork.SaveChangesAsync();
        }

        #endregion

        #region GET RATE

        public async Task<decimal> GetRateByDateAsync(Guid userId, GetRateByDateRequestDto dto)
        {
            var rate = await _rateRepo.GetQueryable()
                .FirstOrDefaultAsync(r =>
                    r.RoomTypeId == dto.RoomTypeId &&
                    dto.Date >= r.StartDate &&
                    dto.Date <= r.EndDate);

            if (rate == null)
                throw new NotFoundException("Rate not found");

            return rate.Rate;
        }

        #endregion
    }
}
