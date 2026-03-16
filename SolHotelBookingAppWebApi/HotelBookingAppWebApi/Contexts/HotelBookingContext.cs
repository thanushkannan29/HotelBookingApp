using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.QueryModels;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Contexts
{
    public class HotelBookingContext : DbContext
    {
        public HotelBookingContext(DbContextOptions<HotelBookingContext> options)
            : base(options)
        {
        }

        // TABLES
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserProfileDetails> UserProfileDetails { get; set; } = null!;
        public DbSet<Hotel> Hotels { get; set; } = null!;
        public DbSet<RoomType> RoomTypes { get; set; } = null!;
        public DbSet<Room> Rooms { get; set; } = null!;
        public DbSet<Reservation> Reservations { get; set; } = null!;
        public DbSet<ReservationRoom> ReservationRooms { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<RoomTypeRate> RoomTypeRates { get; set; } = null!;
        public DbSet<RoomTypeInventory> RoomTypeInventories { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;
        public DbSet<Log> Logs { get; set; } = null!;

        // QUERY MODELS
        public DbSet<RoomListQueryModel> RoomListQueryModel { get; set; }
        public DbSet<TopHotelView> TopHotelViews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            /*
            ==================================================
            USER
            ==================================================
            */

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
                .HasForeignKey<UserProfileDetails>(d => d.UserId);

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

            // Admin -> Hotel relation
            modelBuilder.Entity<User>()
                .HasOne(u => u.Hotel)
                .WithMany()
                .HasForeignKey(u => u.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            /*
            ==================================================
            HOTEL
            ==================================================
            */

            modelBuilder.Entity<Hotel>()
                .HasIndex(h => h.City);

            modelBuilder.Entity<Hotel>()
                .Property(h => h.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Hotel>()
                .HasMany(h => h.RoomTypes)
                .WithOne(rt => rt.Hotel)
                .HasForeignKey(rt => rt.HotelId);

            modelBuilder.Entity<Hotel>()
                .HasMany(h => h.Rooms)
                .WithOne(r => r.Hotel)
                .HasForeignKey(r => r.HotelId);

            modelBuilder.Entity<Hotel>()
                .HasMany(h => h.Reviews)
                .WithOne(r => r.Hotel)
                .HasForeignKey(r => r.HotelId);

            modelBuilder.Entity<Hotel>()
                .HasMany(h => h.Reservations)
                .WithOne(r => r.Hotel)
                .HasForeignKey(r => r.HotelId);

            /*
            ==================================================
            ROOM TYPE
            ==================================================
            */

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

            modelBuilder.Entity<RoomTypeRate>()
                .Property(r => r.Rate)
                .HasPrecision(18, 2);

            modelBuilder.Entity<RoomTypeRate>()
                .HasIndex(r => new { r.RoomTypeId, r.StartDate, r.EndDate });

            /*
            ==================================================
            INVENTORY
            ==================================================
            */

            modelBuilder.Entity<RoomTypeInventory>()
                .HasIndex(i => new { i.RoomTypeId, i.Date })
                .IsUnique();

            modelBuilder.Entity<RoomTypeInventory>()
                .HasIndex(i => i.Date);

            /*
            ==================================================
            ROOM
            ==================================================
            */

            modelBuilder.Entity<Room>()
                .HasIndex(r => new { r.HotelId, r.RoomNumber })
                .IsUnique();

            /*
            ==================================================
            RESERVATION
            ==================================================
            */

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
                .HasForeignKey(rr => rr.ReservationId);

            modelBuilder.Entity<Reservation>()
                .HasMany(r => r.Transactions)
                .WithOne(t => t.Reservation)
                .HasForeignKey(t => t.ReservationId);

            // Performance index for expiry cleanup
            modelBuilder.Entity<Reservation>()
                .HasIndex(r => new { r.Status, r.ExpiryTime });

            /*
            ==================================================
            RESERVATION ROOM
            ==================================================
            */

            modelBuilder.Entity<ReservationRoom>()
                .HasOne(rr => rr.RoomType)
                .WithMany()
                .HasForeignKey(rr => rr.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReservationRoom>()
                .HasOne(rr => rr.Room)
                .WithMany()
                .HasForeignKey(rr => rr.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReservationRoom>()
                .Property(rr => rr.PricePerNight)
                .HasPrecision(18, 2);


            /*
            ==================================================
            TRANSACTION
            ==================================================
            */

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

            /*
            ==================================================
            REVIEW
            ==================================================
            */

            modelBuilder.Entity<Review>()
                .Property(r => r.Rating)
                .HasPrecision(3, 2);

            /*
            ==================================================
            KEYLESS QUERY MODELS
            ==================================================
            */

            modelBuilder.Entity<RoomListQueryModel>()
                .HasNoKey();

            modelBuilder.Entity<TopHotelView>()
                .HasNoKey();

            modelBuilder.Entity<TopHotelView>()
                .Property(t => t.AverageRating)
                .HasPrecision(3, 2);

            modelBuilder.Entity<TopHotelView>()
                .Property(t => t.StartingPrice)
                .HasPrecision(18, 2);
        }
    }
}
