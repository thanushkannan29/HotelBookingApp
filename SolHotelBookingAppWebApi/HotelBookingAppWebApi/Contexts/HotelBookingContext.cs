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

        // TABLES
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            
            // USER
            
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
                .HasOne(u => u.Hotel)
                .WithMany()
                .HasForeignKey(u => u.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            
            // HOTEL
            
            modelBuilder.Entity<Hotel>()
                .HasIndex(h => h.City);

            modelBuilder.Entity<Hotel>()
                .Property(h => h.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            
            // ROOM TYPE
            
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

            
            // ROOM TYPE RATE
            
            modelBuilder.Entity<RoomTypeRate>()
                .Property(r => r.Rate)
                .HasPrecision(18, 2);

            modelBuilder.Entity<RoomTypeRate>()
                .HasIndex(r => new { r.RoomTypeId, r.StartDate, r.EndDate });

            
            // INVENTORY
            
            modelBuilder.Entity<RoomTypeInventory>()
                .HasIndex(i => new { i.RoomTypeId, i.Date })
                .IsUnique();

            
            // ROOM
            
            modelBuilder.Entity<Room>()
                .HasIndex(r => new { r.HotelId, r.RoomNumber })
                .IsUnique();

            modelBuilder.Entity<Room>()
                .HasMany(r => r.ReservationRooms)
                .WithOne(rr => rr.Room)
                .HasForeignKey(rr => rr.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            
            // RESERVATION
            
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

            
            // RESERVATION ROOM
            
            modelBuilder.Entity<ReservationRoom>()
                .Property(rr => rr.PricePerNight)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ReservationRoom>()
                .HasOne(rr => rr.RoomType)
                .WithMany()
                .HasForeignKey(rr => rr.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            
            // TRANSACTION
            
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

            
            // REVIEW
            
            modelBuilder.Entity<Review>()
                .Property(r => r.Rating)
                .HasPrecision(3, 2);

            modelBuilder.Entity<Review>()
                .HasIndex(r => r.HotelId);

            
            // LOG
            
            modelBuilder.Entity<Log>()
                .Property(l => l.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
