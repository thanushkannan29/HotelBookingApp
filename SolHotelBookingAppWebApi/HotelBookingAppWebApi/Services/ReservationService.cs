using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Reservation;
using HotelBookingAppWebApi.Models.DTOs.Room;
using Humanizer;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        private readonly IRepository<Guid, Hotel> _hotelRepo;
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IWalletService _walletService;
        private readonly IPromoCodeService _promoCodeService;
        private readonly ISuperAdminRevenueService _superAdminRevenueService;
        private readonly IUnitOfWork _unitOfWork;

        public ReservationService(
            IRepository<Guid, Reservation> reservationRepo,
            IRepository<Guid, Room> roomRepo,
            IRepository<Guid, RoomType> roomTypeRepo,
            IRepository<Guid, RoomTypeInventory> inventoryRepo,
            IRepository<Guid, RoomTypeRate> rateRepo,
            IRepository<Guid, ReservationRoom> reservationRoomRepo,
            IRepository<Guid, Hotel> hotelRepo,
            IRepository<Guid, User> userRepo,
            IWalletService walletService,
            IPromoCodeService promoCodeService,
            ISuperAdminRevenueService superAdminRevenueService,
            IUnitOfWork unitOfWork)
        {
            _reservationRepo = reservationRepo;
            _roomRepo = roomRepo;
            _roomTypeRepo = roomTypeRepo;
            _inventoryRepo = inventoryRepo;
            _rateRepo = rateRepo;
            _reservationRoomRepo = reservationRoomRepo;
            _hotelRepo = hotelRepo;
            _userRepo = userRepo;
            _walletService = walletService;
            _promoCodeService = promoCodeService;
            _superAdminRevenueService = superAdminRevenueService;
            _unitOfWork = unitOfWork;
        }

        // ── CREATE RESERVATION ────────────────────────────────────────────────
        public async Task<ReservationResponseDto> CreateReservationAsync(Guid userId, CreateReservationDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await ValidateDatesAsync(dto);
                var hotel = await GetHotelAsync(dto.HotelId);
                var roomType = await GetRoomTypeAsync(dto.RoomTypeId, dto.HotelId);
                var dates = GetDateRange(dto.CheckInDate, dto.CheckOutDate);
                var inventories = await GetInventoriesAsync(dto.RoomTypeId, dates, dto.NumberOfRooms);
                var totalAmount = await CalculateBaseAmountAsync(dto.RoomTypeId, dto.CheckInDate, dto.CheckOutDate, dto.NumberOfRooms, dates);
                var pricing = await CalculatePricingAsync(userId, dto, hotel, totalAmount);
                var assignedRooms = await AssignRoomsAsync(dto, dates);
                var reservation = await SaveReservationAsync(userId, dto, pricing, assignedRooms, dates, inventories);
                await ProcessWalletDeductionAsync(userId, pricing, reservation.ReservationCode);
                if (!string.IsNullOrWhiteSpace(dto.PromoCodeUsed))
                    await _promoCodeService.MarkUsedAsync(dto.PromoCodeUsed, userId);
                await _unitOfWork.CommitAsync();
                return MapToResponseDto(reservation, assignedRooms, pricing);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ── VALIDATE DATES ────────────────────────────────────────────────────
        private static Task ValidateDatesAsync(CreateReservationDto dto)
        {
            // Use local date (not UTC) to avoid timezone issues with IST clients
            var today = DateOnly.FromDateTime(DateTime.Now);

            // Block today and past dates — only allow from tomorrow onwards
            if (dto.CheckInDate <= today)
                throw new ValidationException("Check-in date must be at least tomorrow.");

            if (dto.CheckInDate >= dto.CheckOutDate)
                throw new ValidationException("Check-out must be after check-in.");

            if (dto.NumberOfRooms <= 0)
                throw new ValidationException("Number of rooms must be at least 1.");

            return Task.CompletedTask;
        }

        // ── GET HOTEL ─────────────────────────────────────────────────────────
        private async Task<Hotel> GetHotelAsync(Guid hotelId)
        {
            var hotel = await _hotelRepo.GetAsync(hotelId)
                ?? throw new NotFoundException("Hotel not found.");
            if (!hotel.IsActive)
                throw new ValidationException("This hotel is currently unavailable for booking.");
            return hotel;
        }

        // ── GET ROOM TYPE ─────────────────────────────────────────────────────
        private async Task<RoomType> GetRoomTypeAsync(Guid roomTypeId, Guid hotelId)
        {
            return await _roomTypeRepo.GetQueryable()
                .FirstOrDefaultAsync(r => r.RoomTypeId == roomTypeId && r.HotelId == hotelId && r.IsActive)
                ?? throw new NotFoundException("Invalid or inactive room type.");
        }

        // ── DATE RANGE ────────────────────────────────────────────────────────
        private static List<DateOnly> GetDateRange(DateOnly checkIn, DateOnly checkOut)
        {
            var totalDays = checkOut.DayNumber - checkIn.DayNumber;
            return Enumerable.Range(0, totalDays).Select(d => checkIn.AddDays(d)).ToList();
        }

        // ── GET INVENTORIES ───────────────────────────────────────────────────
        private async Task<List<RoomTypeInventory>> GetInventoriesAsync(Guid roomTypeId, List<DateOnly> dates, int numberOfRooms)
        {
            var inventories = await _inventoryRepo.GetQueryable()
                .Where(i => i.RoomTypeId == roomTypeId && dates.Contains(i.Date))
                .ToListAsync();

            if (inventories.Count != dates.Count)
                throw new InsufficientInventoryException("Inventory not configured for one or more dates.");

            foreach (var inv in inventories)
                if (inv.AvailableInventory < numberOfRooms)
                    throw new InsufficientInventoryException($"Insufficient inventory on {inv.Date}.");

            return inventories;
        }

        // ── CALCULATE BASE AMOUNT ─────────────────────────────────────────────
        private async Task<decimal> CalculateBaseAmountAsync(Guid roomTypeId, DateOnly checkIn, DateOnly checkOut, int numberOfRooms, List<DateOnly> dates)
        {
            var rates = await _rateRepo.GetQueryable()
                .Where(r => r.RoomTypeId == roomTypeId && r.StartDate <= checkOut && r.EndDate >= checkIn)
                .ToListAsync();

            decimal total = 0;
            foreach (var date in dates)
            {
                var rate = rates.FirstOrDefault(r => date >= r.StartDate && date <= r.EndDate)
                    ?? throw new RateNotFoundException($"No rate configured for {date}.");
                total += rate.Rate * numberOfRooms;
            }
            return total;
        }

        // ── CALCULATE PRICING (GST + PROMO + WALLET) ──────────────────────────
        private async Task<PricingResult> CalculatePricingAsync(Guid userId, CreateReservationDto dto, Hotel hotel, decimal totalAmount)
        {
            var gstPercent = hotel.GstPercent;
            var gstAmount = Math.Round(totalAmount * gstPercent / 100, 2);

            decimal discountPercent = 0;
            decimal discountAmount = 0;

            if (!string.IsNullOrWhiteSpace(dto.PromoCodeUsed))
            {
                var promoResult = await _promoCodeService.ValidateAsync(userId, new Models.DTOs.PromoCode.ValidatePromoCodeDto
                {
                    Code = dto.PromoCodeUsed,
                    HotelId = dto.HotelId,
                    TotalAmount = totalAmount
                });

                if (promoResult.IsValid)
                {
                    discountPercent = promoResult.DiscountPercent;
                    discountAmount = promoResult.DiscountAmount;
                }
            }

            var cancellationFeeAmount = dto.PayCancellationFee
                ? Math.Round(totalAmount * 0.10m, 2)
                : 0m;

            var walletUsed = 0m;
            if (dto.WalletAmountToUse > 0)
            {
                var maxWallet = Math.Max(0, totalAmount + gstAmount - discountAmount + cancellationFeeAmount);
                walletUsed = Math.Min(dto.WalletAmountToUse, maxWallet);
            }

            var finalAmount = Math.Max(0, totalAmount + gstAmount - discountAmount - walletUsed + cancellationFeeAmount);

            return new PricingResult
            {
                TotalAmount = totalAmount,
                GstPercent = gstPercent,
                GstAmount = gstAmount,
                DiscountPercent = discountPercent,
                DiscountAmount = discountAmount,
                WalletAmountUsed = walletUsed,
                FinalAmount = finalAmount,
                CancellationFeePaid = dto.PayCancellationFee,
                CancellationFeeAmount = cancellationFeeAmount
            };
        }

        // ── ASSIGN ROOMS ──────────────────────────────────────────────────────
        private async Task<List<Room>> AssignRoomsAsync(CreateReservationDto dto, List<DateOnly> dates)
        {
            // Get rooms already booked for overlapping dates
            var bookedRoomIds = await _reservationRoomRepo.GetQueryable()
                .Where(rr =>
                    rr.RoomTypeId == dto.RoomTypeId &&
                    rr.Reservation!.HotelId == dto.HotelId &&
                    (rr.Reservation.Status == ReservationStatus.Pending ||
                     rr.Reservation.Status == ReservationStatus.Confirmed) &&
                    rr.Reservation.CheckInDate < dto.CheckOutDate &&
                    rr.Reservation.CheckOutDate > dto.CheckInDate)
                .Select(rr => rr.RoomId)
                .Distinct()
                .ToListAsync();

            List<Room> assignedRooms;

            if (dto.SelectedRoomIds != null && dto.SelectedRoomIds.Count > 0)
            {
                if (dto.SelectedRoomIds.Count != dto.NumberOfRooms)
                    throw new ValidationException("Selected room count must match the requested number of rooms.");

                // Filter out any already-booked rooms
                var conflicting = dto.SelectedRoomIds.Intersect(bookedRoomIds).ToList();
                if (conflicting.Count > 0)
                    throw new ValidationException("One or more selected rooms are already booked for the requested dates.");

                assignedRooms = await _roomRepo.GetQueryable()
                    .Where(r => dto.SelectedRoomIds.Contains(r.RoomId) &&
                                r.RoomTypeId == dto.RoomTypeId &&
                                r.HotelId == dto.HotelId &&
                                r.IsActive)
                    .ToListAsync();

                if (assignedRooms.Count != dto.NumberOfRooms)
                    throw new ValidationException("One or more selected rooms are invalid or unavailable.");
            }
            else
            {
                assignedRooms = await _roomRepo.GetQueryable()
                    .Where(r => r.RoomTypeId == dto.RoomTypeId &&
                                r.HotelId == dto.HotelId &&
                                r.IsActive &&
                                !bookedRoomIds.Contains(r.RoomId))
                    .Take(dto.NumberOfRooms)
                    .ToListAsync();

                if (assignedRooms.Count < dto.NumberOfRooms)
                    throw new InsufficientInventoryException("Not enough available rooms for the requested dates.");
            }

            return assignedRooms;
        }

        // ── SAVE RESERVATION ──────────────────────────────────────────────────
        private async Task<Reservation> SaveReservationAsync(
            Guid userId, CreateReservationDto dto, PricingResult pricing,
            List<Room> rooms, List<DateOnly> dates, List<RoomTypeInventory> inventories)
        {
            var reservation = new Reservation
            {
                ReservationId = Guid.NewGuid(),
                ReservationCode = GenerateCode(),
                UserId = userId,
                HotelId = dto.HotelId,
                CheckInDate = dto.CheckInDate,
                CheckOutDate = dto.CheckOutDate,
                TotalAmount = pricing.TotalAmount,
                GstPercent = pricing.GstPercent,
                GstAmount = pricing.GstAmount,
                DiscountPercent = pricing.DiscountPercent,
                DiscountAmount = pricing.DiscountAmount,
                WalletAmountUsed = pricing.WalletAmountUsed,
                PromoCodeUsed = dto.PromoCodeUsed,
                FinalAmount = pricing.FinalAmount,
                CancellationFeePaid = pricing.CancellationFeePaid,
                CancellationFeeAmount = pricing.CancellationFeeAmount,
                Status = ReservationStatus.Pending,
                IsCheckedIn = false,
                CreatedDate = DateTime.UtcNow,
                ExpiryTime = DateTime.UtcNow.AddMinutes(10)
            };

            await _reservationRepo.AddAsync(reservation);

            var pricePerNight = pricing.TotalAmount / dates.Count / rooms.Count;
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

            foreach (var inv in inventories)
                inv.ReservedInventory += dto.NumberOfRooms;

            return reservation;
        }

        // ── PROCESS WALLET DEDUCTION ──────────────────────────────────────────
        private async Task ProcessWalletDeductionAsync(Guid userId, PricingResult pricing, string reservationCode)
        {
            if (pricing.WalletAmountUsed > 0)
            {
                var deducted = await _walletService.DeductAsync(
                    userId, pricing.WalletAmountUsed,
                    $"Wallet payment for reservation {reservationCode}");

                if (!deducted)
                    throw new ValidationException("Insufficient wallet balance.");
            }
        }

        // ── MAP TO RESPONSE ───────────────────────────────────────────────────
        private static ReservationResponseDto MapToResponseDto(Reservation r, List<Room> rooms, PricingResult pricing) => new()
        {
            ReservationId = r.ReservationId,
            ReservationCode = r.ReservationCode,
            TotalAmount = pricing.TotalAmount,
            GstPercent = pricing.GstPercent,
            GstAmount = pricing.GstAmount,
            DiscountPercent = pricing.DiscountPercent,
            DiscountAmount = pricing.DiscountAmount,
            WalletAmountUsed = pricing.WalletAmountUsed,
            FinalAmount = pricing.FinalAmount,
            Status = r.Status.ToString(),
            TotalRooms = rooms.Count,
            Rooms = rooms.Select(rm => new RoomSummaryDto
            {
                RoomId = rm.RoomId,
                RoomNumber = rm.RoomNumber,
                Floor = rm.Floor
            }).ToList()
        };

        // ── GET BY CODE ───────────────────────────────────────────────────────
        public async Task<ReservationDetailsDto> GetReservationByCodeAsync(Guid userId, string code)
        {
            var res = await _reservationRepo.GetQueryable()
                .Include(r => r.ReservationRooms!).ThenInclude(rr => rr.Room)
                .Include(r => r.ReservationRooms!).ThenInclude(rr => rr.RoomType)
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.ReservationCode == code && r.UserId == userId)
                ?? throw new NotFoundException("Reservation not found.");
            return MapToDetailsDto(res);
        }

        // ── GET MY RESERVATIONS ───────────────────────────────────────────────
        public async Task<IEnumerable<ReservationDetailsDto>> GetMyReservationsAsync(Guid userId)
        {
            var list = await _reservationRepo.GetQueryable()
                .Include(r => r.ReservationRooms!).ThenInclude(rr => rr.Room)
                .Include(r => r.ReservationRooms!).ThenInclude(rr => rr.RoomType)
                .Include(r => r.Hotel)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
            return list.Select(MapToDetailsDto);
        }

        // ── GET MY RESERVATIONS (PAGED + STATUS FILTER) ───────────────────────
        public async Task<PagedReservationResponseDto> GetMyReservationsPagedAsync(Guid userId, int page, int pageSize, string? status = null, string? search = null)
        {
            var query = _reservationRepo.GetQueryable()
                .Include(r => r.ReservationRooms!).ThenInclude(rr => rr.Room)
                .Include(r => r.ReservationRooms!).ThenInclude(rr => rr.RoomType)
                .Include(r => r.Hotel)
                .Where(r => r.UserId == userId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "All" &&
                Enum.TryParse<ReservationStatus>(status, out var statusEnum))
                query = query.Where(r => r.Status == statusEnum);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(r =>
                    r.ReservationCode.Contains(search) ||
                    (r.Hotel != null && r.Hotel.Name.Contains(search)));

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(r => r.CreatedDate)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedReservationResponseDto { TotalCount = total, Reservations = items.Select(MapToDetailsDto) };
        }

        // ── GET ADMIN RESERVATIONS (WITH STATUS + SEARCH FILTER) ─────────────
        public async Task<PagedReservationResponseDto> GetAdminReservationsAsync(
            Guid adminUserId, string? status, string? search, int page, int pageSize,
            string? sortField = null, string? sortDir = null)
        {
            var admin = await _userRepo.GetAsync(adminUserId) ?? throw new UnAuthorizedException("Unauthorized.");
            if (admin.HotelId == null) throw new UnAuthorizedException("No hotel associated with this admin.");

            var query = _reservationRepo.GetQueryable()
                .Include(r => r.ReservationRooms!).ThenInclude(rr => rr.RoomType)
                .Include(r => r.Hotel).Include(r => r.User)
                .Where(r => r.HotelId == admin.HotelId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "All" &&
                Enum.TryParse<ReservationStatus>(status, out var statusEnum))
                query = query.Where(r => r.Status == statusEnum);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(r =>
                    r.ReservationCode.Contains(search) ||
                    (r.User != null && r.User.Name.Contains(search)));

            var total = await query.CountAsync();

            bool desc = string.IsNullOrWhiteSpace(sortDir) || sortDir.ToLower() == "desc";
            query = sortField?.ToLower() switch
            {
                "guestname" => desc ? query.OrderByDescending(r => r.Hotel!.Name) : query.OrderBy(r => r.Hotel!.Name),
                "amount"    => desc ? query.OrderByDescending(r => r.FinalAmount) : query.OrderBy(r => r.FinalAmount),
                _           => query.OrderByDescending(r => r.CreatedDate)
            };

            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedReservationResponseDto { TotalCount = total, Reservations = items.Select(MapToDetailsDto) };
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

                var dates = GetDateRange(res.CheckInDate, res.CheckOutDate);
                var roomTypeId = res.ReservationRooms!.First().RoomTypeId;
                var inventories = await _inventoryRepo.GetQueryable()
                    .Where(i => i.RoomTypeId == roomTypeId && dates.Contains(i.Date))
                    .ToListAsync();

                var roomCount = res.ReservationRooms?.Count ?? 0;
                foreach (var inv in inventories)
                    inv.ReservedInventory = Math.Max(0, inv.ReservedInventory - roomCount);

                res.Status = ReservationStatus.Cancelled;
                res.CancelledDate = DateTime.UtcNow;
                res.CancellationReason = reason;

                // Calculate and apply refund BEFORE commit so cancellation + refund are atomic
                var hasPaid = res.Transactions?.Any(t => t.Status == PaymentStatus.Success) ?? false;
                if (hasPaid)
                {
                    var today = DateOnly.FromDateTime(DateTime.Now); // local date (IST)
                    var daysUntilCheckIn = res.CheckInDate.DayNumber - today.DayNumber;

                    decimal refundPercent;
                    string refundNote;

                    // ── AFTER CHECK-IN: no refund regardless of protection ──────────────
                    if (res.IsCheckedIn || daysUntilCheckIn < 0)
                    {
                        refundPercent = 0;
                        refundNote = "No refund — reservation already checked in or stay has passed.";
                    }
                    // ── WITH CANCELLATION PROTECTION ────────────────────────────────────
                    else if (res.CancellationFeePaid)
                    {
                        if (daysUntilCheckIn > 0)
                        {
                            // Before check-in day: full refund (protection covers this)
                            refundPercent = 100;
                            refundNote = "Full refund — cancellation protection active, cancelled before check-in day.";
                        }
                        else
                        {
                            // On check-in day: 50% refund (protection gives partial benefit)
                            refundPercent = 50;
                            refundNote = "50% refund — cancelled on check-in day (protection provides partial refund).";
                        }
                    }
                    // ── WITHOUT PROTECTION: standard tiered policy ───────────────────────
                    else
                    {
                        if (daysUntilCheckIn >= 7)
                        {
                            refundPercent = 100;
                            refundNote = "Full refund — cancelled 7+ days before check-in.";
                        }
                        else if (daysUntilCheckIn >= 3)
                        {
                            refundPercent = 50;
                            refundNote = "50% refund — cancelled 3–6 days before check-in.";
                        }
                        else if (daysUntilCheckIn >= 1)
                        {
                            refundPercent = 25;
                            refundNote = "25% refund — cancelled 1–2 days before check-in.";
                        }
                        else
                        {
                            // Same day (daysUntilCheckIn == 0)
                            refundPercent = 0;
                            refundNote = "No refund — cancelled on check-in day.";
                        }
                    }

                    // Mark the success transaction as Refunded only on full refund.
                    // For partial refunds the transaction stays Success (hotel kept part of the payment).
                    var successTx = res.Transactions?.FirstOrDefault(t => t.Status == PaymentStatus.Success);
                    if (successTx is not null && refundPercent == 100)
                        successTx.Status = PaymentStatus.Refunded;

                    if (refundPercent > 0)
                    {
                        // Total paid = gateway payment (FinalAmount) + wallet portion (WalletAmountUsed)
                        var gatewayPaid = res.FinalAmount > 0 ? res.FinalAmount : res.TotalAmount;
                        var totalPaid = gatewayPaid + res.WalletAmountUsed;
                        var refundAmount = Math.Round(totalPaid * (refundPercent / 100m), 2);
                        await _walletService.CreditAsync(userId, refundAmount,
                            $"Refund ({refundNote}) for {res.ReservationCode}");
                    }
                }

                await _unitOfWork.CommitAsync();
                return true;
            }
            catch { await _unitOfWork.RollbackAsync(); throw; }
        }

        // ── COMPLETE RESERVATION (Admin) ──────────────────────────────────────
        public async Task<bool> CompleteReservationAsync(string code)
        {
            var res = await _reservationRepo.FirstOrDefaultAsync(r => r.ReservationCode == code)
                ?? throw new NotFoundException("Reservation not found.");

            if (res.Status != ReservationStatus.Confirmed)
                throw new ValidationException("Only confirmed reservations can be marked as completed.");
            var today = DateOnly.FromDateTime(DateTime.Now);
            if (today != res.CheckInDate)
                throw new ValidationException("Check-in can only be completed on the check-in date.");

            res.Status = ReservationStatus.Completed;
            res.IsCheckedIn = true;
            await _unitOfWork.SaveChangesAsync();
            

            // Generate promo code for the guest
            await _promoCodeService.GeneratePromoForCompletedReservationAsync(res.ReservationId);

            // Record 2% commission to SuperAdmin (inline — no background service needed)
            await _superAdminRevenueService.RecordCommissionAsync(res.ReservationId);

            return true;
        }

        // ── CONFIRM RESERVATION (Admin) ───────────────────────────────────────
        public async Task<bool> ConfirmReservationAsync(string code)
        {
            var res = await _reservationRepo.FirstOrDefaultAsync(r => r.ReservationCode == code)
                ?? throw new NotFoundException("Reservation not found.");

            if (res.Status != ReservationStatus.Pending)
                throw new ValidationException("Only pending reservations can be confirmed.");

            res.Status = ReservationStatus.Confirmed;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        // ── AVAILABLE ROOMS ───────────────────────────────────────────────────
        public async Task<IEnumerable<AvailableRoomDto>> GetAvailableRoomsAsync(
            Guid hotelId, Guid roomTypeId, DateOnly checkIn, DateOnly checkOut)
        {
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
                .Where(r => r.HotelId == hotelId && r.RoomTypeId == roomTypeId &&
                            r.IsActive && !bookedRoomIds.Contains(r.RoomId))
                .ToListAsync();

            return availableRooms.Select(r => new AvailableRoomDto
            {
                RoomId = r.RoomId,
                RoomNumber = r.RoomNumber,
                Floor = r.Floor,
                RoomTypeName = r.RoomType!.Name
            });
        }

        // ── ROOM OCCUPANCY ────────────────────────────────────────────────────
        public async Task<IEnumerable<RoomOccupancyDto>> GetRoomOccupancyAsync(Guid adminUserId, DateOnly date)
        {
            var admin = await _userRepo.GetAsync(adminUserId) ?? throw new UnAuthorizedException("Unauthorized.");
            if (admin.HotelId == null) throw new UnAuthorizedException("Unauthorized.");

            var hotelId = admin.HotelId.Value;
            var rooms = await _roomRepo.GetQueryable()
                .Include(r => r.RoomType)
                .Where(r => r.HotelId == hotelId && r.IsActive)
                .ToListAsync();

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
                ReservationCode = occupancyMap.TryGetValue(r.RoomId, out var c) ? c : null
            });
        }

        // ── QR PAYMENT ────────────────────────────────────────────────────────
        public async Task<QrPaymentResponseDto> GetPaymentQrAsync(Guid userId, Guid reservationId)
        {
            var res = await _reservationRepo.GetQueryable()
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId && r.UserId == userId)
                ?? throw new NotFoundException("Reservation not found.");

            var upiId = res.Hotel?.UpiId ?? "hotel@upi";
            var amount = res.FinalAmount > 0 ? res.FinalAmount : res.TotalAmount;
            var hotelName = res.Hotel?.Name ?? "Hotel";
            var upiString = $"upi://pay?pa={upiId}&pn={Uri.EscapeDataString(hotelName)}&am={amount}&cu=INR";

            return new QrPaymentResponseDto
            {
                UpiId = upiId,
                Amount = amount,
                HotelName = hotelName,
                QrCodeBase64 = QrCodeHelper.GenerateQrCodeBase64(upiString)
            };
        }

        // ── HELPERS ───────────────────────────────────────────────────────────
        private static ReservationDetailsDto MapToDetailsDto(Reservation r)
        {
            var firstRoomType = r.ReservationRooms?.FirstOrDefault()?.RoomType;
            string policyText;
            if (r.CancellationFeePaid)
                policyText = "Full refund before check-in day · 50% on check-in day · No refund after check-in (protection fee paid)";
            else
                policyText = "Free cancellation 7+ days before · 50% refund 3–6 days before · 25% refund 1–2 days before · No refund on check-in day or after";

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
                GstPercent = r.GstPercent,
                GstAmount = r.GstAmount,
                DiscountPercent = r.DiscountPercent,
                DiscountAmount = r.DiscountAmount,
                WalletAmountUsed = r.WalletAmountUsed,
                FinalAmount = r.FinalAmount,
                PromoCodeUsed = r.PromoCodeUsed,
                Status = r.Status.ToString(),
                IsCheckedIn = r.IsCheckedIn,
                CreatedDate = r.CreatedDate,
                ExpiryTime = r.ExpiryTime,
                UpiId = r.Hotel?.UpiId,
                CancellationFeePaid = r.CancellationFeePaid,
                CancellationFeeAmount = r.CancellationFeeAmount,
                CancellationPolicyText = policyText,
                Rooms = r.ReservationRooms?.Select(rr => new RoomSummaryDto
                {
                    RoomId = rr.RoomId,
                    RoomNumber = rr.Room?.RoomNumber ?? string.Empty,
                    Floor = rr.Room?.Floor ?? 0
                }).ToList() ?? new List<RoomSummaryDto>()
            };
        }

        private static string GenerateCode() => $"RES-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        // ── PRICING RESULT ────────────────────────────────────────────────────
        private class PricingResult
        {
            public decimal TotalAmount { get; set; }
            public decimal GstPercent { get; set; }
            public decimal GstAmount { get; set; }
            public decimal DiscountPercent { get; set; }
            public decimal DiscountAmount { get; set; }
            public decimal WalletAmountUsed { get; set; }
            public decimal FinalAmount { get; set; }
            public bool CancellationFeePaid { get; set; }
            public decimal CancellationFeeAmount { get; set; }
        }
    }
}
