using HotelBookingAppWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Contexts
{
    public class HotelBookingContext : DbContext
    {
        public HotelBookingContext(DbContextOptions<HotelBookingContext> options)
            : base(options)
        {
        }

        // ─── CORE TABLES ──────────────────────────────────────────────────────
        public DbSet<User> Users { get; set; }
        public DbSet<UserProfileDetails> UserProfileDetails { get; set; }
        public DbSet<Hotel> Hotels { get; set; }
        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<ReservationRoom> ReservationRooms { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<RoomTypeRate> RoomTypeRates { get; set; }
        public DbSet<RoomTypeInventory> RoomTypeInventories { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        // ─── FEATURE TABLES ───────────────────────────────────────────────────
        public DbSet<Amenity> Amenities { get; set; }
        public DbSet<RoomTypeAmenity> RoomTypeAmenities { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<PromoCode> PromoCodes { get; set; }
        public DbSet<AmenityRequest> AmenityRequests { get; set; }
        public DbSet<SuperAdminRevenue> SuperAdminRevenues { get; set; }
        public DbSet<SupportRequest> SupportRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ─── USER ─────────────────────────────────────────────────────────
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<int>();

            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<User>()
                .HasOne(u => u.UserDetails)
                .WithOne(d => d.User)
                .HasForeignKey<UserProfileDetails>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Reservations)
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Reviews)
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Logs)
                .WithOne(l => l.User)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.AuditLogs)
                .WithOne(al => al.User)
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Hotel)
                .WithMany()
                .HasForeignKey(u => u.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            // ─── HOTEL ────────────────────────────────────────────────────────
            modelBuilder.Entity<Hotel>()
                .HasIndex(h => h.City);

            modelBuilder.Entity<Hotel>()
                .HasIndex(h => h.State);

            modelBuilder.Entity<Hotel>()
                .Property(h => h.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // ─── ROOM TYPE ────────────────────────────────────────────────────
            modelBuilder.Entity<RoomType>()
                .HasIndex(rt => rt.HotelId);

            modelBuilder.Entity<RoomType>()
                .HasMany(rt => rt.Rooms)
                .WithOne(r => r.RoomType)
                .HasForeignKey(r => r.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoomType>()
                .HasMany(rt => rt.Rates)
                .WithOne(r => r.RoomType)
                .HasForeignKey(r => r.RoomTypeId);

            modelBuilder.Entity<RoomType>()
                .HasMany(rt => rt.Inventories)
                .WithOne(i => i.RoomType)
                .HasForeignKey(i => i.RoomTypeId);

            // ─── ROOM TYPE RATE ───────────────────────────────────────────────
            modelBuilder.Entity<RoomTypeRate>()
                .Property(r => r.Rate)
                .HasPrecision(18, 2);

            modelBuilder.Entity<RoomTypeRate>()
                .HasIndex(r => new { r.RoomTypeId, r.StartDate, r.EndDate });

            // ─── INVENTORY ────────────────────────────────────────────────────
            modelBuilder.Entity<RoomTypeInventory>()
                .HasIndex(i => new { i.RoomTypeId, i.Date })
                .IsUnique();

            // ─── ROOM ─────────────────────────────────────────────────────────
            modelBuilder.Entity<Room>()
                .HasIndex(r => new { r.HotelId, r.RoomNumber })
                .IsUnique();

            modelBuilder.Entity<Room>()
                .HasMany(r => r.ReservationRooms)
                .WithOne(rr => rr.Room)
                .HasForeignKey(rr => rr.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // ─── RESERVATION ──────────────────────────────────────────────────
            modelBuilder.Entity<Reservation>()
                .HasIndex(r => r.ReservationCode)
                .IsUnique();

            modelBuilder.Entity<Reservation>()
                .Property(r => r.Status)
                .HasConversion<int>();

            modelBuilder.Entity<Reservation>()
                .Property(r => r.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Reservation>()
                .Property(r => r.CreatedDate)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Reservation>()
                .HasMany(r => r.ReservationRooms)
                .WithOne(rr => rr.Reservation)
                .HasForeignKey(rr => rr.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Reservation>()
                .HasMany(r => r.Transactions)
                .WithOne(t => t.Reservation)
                .HasForeignKey(t => t.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            // ─── RESERVATION ROOM ─────────────────────────────────────────────
            modelBuilder.Entity<ReservationRoom>()
                .Property(rr => rr.PricePerNight)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ReservationRoom>()
                .HasOne(rr => rr.RoomType)
                .WithMany()
                .HasForeignKey(rr => rr.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ─── TRANSACTION ──────────────────────────────────────────────────
            modelBuilder.Entity<Transaction>()
                .Property(t => t.PaymentMethod)
                .HasConversion<int>();

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Status)
                .HasConversion<int>();

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.TransactionDate)
                .HasDefaultValueSql("GETUTCDATE()");

            // ─── REVIEW ───────────────────────────────────────────────────────
            modelBuilder.Entity<Review>()
                .Property(r => r.Rating)
                .HasPrecision(3, 2);

            modelBuilder.Entity<Review>()
                .HasIndex(r => r.HotelId);

            // One review per completed reservation (UserId + ReservationId unique)
            modelBuilder.Entity<Review>()
                .HasIndex(r => new { r.UserId, r.ReservationId })
                .IsUnique();

            // Restrict so deleting a reservation does not cascade-delete its reviews
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Reservation)
                .WithMany()
                .HasForeignKey(r => r.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            // ─── AUDIT LOG ────────────────────────────────────────────────────
            modelBuilder.Entity<AuditLog>()
                .Property(al => al.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // ─── LOG ──────────────────────────────────────────────────────────
            modelBuilder.Entity<Log>()
                .Property(l => l.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // ─── AMENITY ──────────────────────────────────────────────────────
            modelBuilder.Entity<Amenity>()
                .HasIndex(a => a.Name)
                .IsUnique();

            // ─── ROOM TYPE AMENITY (many-to-many join) ────────────────────────
            modelBuilder.Entity<RoomTypeAmenity>()
                .HasKey(rta => new { rta.RoomTypeId, rta.AmenityId });

            modelBuilder.Entity<RoomTypeAmenity>()
                .HasOne(rta => rta.RoomType)
                .WithMany(rt => rt.RoomTypeAmenities)
                .HasForeignKey(rta => rta.RoomTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RoomTypeAmenity>()
                .HasOne(rta => rta.Amenity)
                .WithMany()
                .HasForeignKey(rta => rta.AmenityId)
                .OnDelete(DeleteBehavior.Restrict);

            // ─── WALLET ───────────────────────────────────────────────────────
            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Wallet>()
                .Property(w => w.Balance)
                .HasPrecision(18, 2);

            modelBuilder.Entity<WalletTransaction>()
                .HasOne(wt => wt.Wallet)
                .WithMany(w => w.WalletTransactions)
                .HasForeignKey(wt => wt.WalletId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WalletTransaction>()
                .Property(wt => wt.Amount)
                .HasPrecision(18, 2);

            // ─── PROMO CODE ───────────────────────────────────────────────────
            modelBuilder.Entity<PromoCode>()
                .HasIndex(p => p.Code)
                .IsUnique();

            modelBuilder.Entity<PromoCode>()
                .Property(p => p.DiscountPercent)
                .HasPrecision(5, 2);

            modelBuilder.Entity<PromoCode>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PromoCode>()
                .HasOne(p => p.Hotel)
                .WithMany()
                .HasForeignKey(p => p.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PromoCode>()
                .HasOne(p => p.Reservation)
                .WithMany()
                .HasForeignKey(p => p.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            // ─── AMENITY REQUEST ──────────────────────────────────────────────
            modelBuilder.Entity<AmenityRequest>()
                .HasOne(ar => ar.RequestedByAdmin)
                .WithMany()
                .HasForeignKey(ar => ar.RequestedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AmenityRequest>()
                .Property(ar => ar.Status)
                .HasConversion<int>();

            // ─── SUPER ADMIN REVENUE ──────────────────────────────────────────
            modelBuilder.Entity<SuperAdminRevenue>()
                .HasOne(sr => sr.Reservation)
                .WithMany()
                .HasForeignKey(sr => sr.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SuperAdminRevenue>()
                .HasOne(sr => sr.Hotel)
                .WithMany()
                .HasForeignKey(sr => sr.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SuperAdminRevenue>()
                .Property(sr => sr.ReservationAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SuperAdminRevenue>()
                .Property(sr => sr.CommissionAmount)
                .HasPrecision(18, 2);

            // ─── SUPPORT REQUEST ──────────────────────────────────────────────
            modelBuilder.Entity<SupportRequest>()
                .Property(s => s.Status)
                .HasConversion<int>();

            modelBuilder.Entity<SupportRequest>()
                .Property(s => s.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<SupportRequest>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupportRequest>()
                .HasOne(s => s.Hotel)
                .WithMany()
                .HasForeignKey(s => s.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            // ─── RESERVATION — new decimal fields ────────────────────────────
            modelBuilder.Entity<Reservation>()
                .Property(r => r.GstPercent)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Reservation>()
                .Property(r => r.GstAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Reservation>()
                .Property(r => r.DiscountPercent)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Reservation>()
                .Property(r => r.DiscountAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Reservation>()
                .Property(r => r.WalletAmountUsed)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Reservation>()
                .Property(r => r.FinalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Reservation>()
                .Property(r => r.CancellationFeeAmount)
                .HasPrecision(18, 2);

            // ─── HOTEL — GST ──────────────────────────────────────────────────
            modelBuilder.Entity<Hotel>()
                .Property(h => h.GstPercent)
                .HasPrecision(5, 2);

            // ─── TRANSACTION — wallet fields ──────────────────────────────────
            modelBuilder.Entity<Transaction>()
                .Property(t => t.WalletAmountUsed)
                .HasPrecision(18, 2);

            // ─── AMENITY SEED DATA (30 common amenities) ──────────────────────
            modelBuilder.Entity<Amenity>().HasData(
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000001"), Name = "WiFi",               Category = "Tech",      IconName = "wifi",               IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000002"), Name = "AC",                 Category = "Room",      IconName = "ac_unit",            IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000003"), Name = "TV",                 Category = "Room",      IconName = "tv",                 IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000004"), Name = "Pool",               Category = "Services",  IconName = "pool",               IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000005"), Name = "Parking",            Category = "Services",  IconName = "local_parking",      IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000006"), Name = "Gym",                Category = "Services",  IconName = "fitness_center",     IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000007"), Name = "Restaurant",         Category = "Food",      IconName = "restaurant",         IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000008"), Name = "Bar",                Category = "Food",      IconName = "local_bar",          IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000009"), Name = "Room Service",       Category = "Services",  IconName = "room_service",       IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000010"), Name = "Laundry",            Category = "Services",  IconName = "local_laundry_service", IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000011"), Name = "Spa",                Category = "Services",  IconName = "spa",                IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000012"), Name = "Breakfast Included", Category = "Food",      IconName = "free_breakfast",     IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000013"), Name = "Safe",               Category = "Room",      IconName = "lock",               IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000014"), Name = "Mini Bar",           Category = "Room",      IconName = "liquor",             IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000015"), Name = "Balcony",            Category = "Room",      IconName = "balcony",            IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000016"), Name = "Sea View",           Category = "Room",      IconName = "beach_access",       IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000017"), Name = "Mountain View",      Category = "Room",      IconName = "landscape",          IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000018"), Name = "Wheelchair Access",  Category = "Services",  IconName = "accessible",         IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000019"), Name = "Pet Friendly",       Category = "Services",  IconName = "pets",               IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000020"), Name = "Kids Area",          Category = "Services",  IconName = "child_friendly",     IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000021"), Name = "Conference Room",    Category = "Services",  IconName = "meeting_room",       IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000022"), Name = "Airport Shuttle",    Category = "Services",  IconName = "airport_shuttle",    IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000023"), Name = "CCTV",               Category = "Services",  IconName = "videocam",           IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000024"), Name = "24h Reception",      Category = "Services",  IconName = "support_agent",      IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000025"), Name = "Heating",            Category = "Room",      IconName = "whatshot",           IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000026"), Name = "Elevator",           Category = "Services",  IconName = "elevator",           IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000027"), Name = "Hair Dryer",         Category = "Bathroom",  IconName = "dry",                IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000028"), Name = "Iron",               Category = "Room",      IconName = "iron",               IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000029"), Name = "Coffee Maker",       Category = "Room",      IconName = "coffee",             IsActive = true },
                new Amenity { AmenityId = Guid.Parse("10000000-0000-0000-0000-000000000030"), Name = "Bathtub",            Category = "Bathroom",  IconName = "bathtub",            IsActive = true }
            );
        }
    }
}
