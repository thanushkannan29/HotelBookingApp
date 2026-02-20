using HotelBookingAppWebApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace HotelBookingAppWebApi.Contexts
{
    public class HotelBookingAppWebApi : DbContext
    {
        public HotelBookingAppWebApi(DbContextOptions<HotelBookingAppWebApi> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Hotel> Hotels { get; set; } = null!;
        public DbSet<RoomType> RoomTypes { get; set; } = null!;
        public DbSet<Room> Rooms { get; set; } = null!;
        public DbSet<Reservation> Reservations { get; set; } = null!;
        public DbSet<ReservationRoom> ReservationRooms { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<RoomTypeRate> RoomTypeRates { get; set; } = null!;
        public DbSet<RoomTypeInventory> RoomTypeInventories { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;

        override protected void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------------- USER ----------------
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

            // ---------------- HOTEL ----------------
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

            // ---------------- ROOMTYPE ----------------
            modelBuilder.Entity<RoomType>()
                .HasMany(rt => rt.Rooms)
                .WithOne(r => r.RoomType)
                .HasForeignKey(r => r.RoomTypeId);

            modelBuilder.Entity<RoomType>()
                .HasMany(rt => rt.Rates)
                .WithOne(r => r.RoomType)
                .HasForeignKey(r => r.RoomTypeId);

            modelBuilder.Entity<RoomType>()
                .HasMany(rt => rt.Inventories)
                .WithOne(i => i.RoomType)
                .HasForeignKey(i => i.RoomTypeId);

            // ---------------- RESERVATION ----------------
            modelBuilder.Entity<Reservation>()
                .HasMany(r => r.ReservationRooms)
                .WithOne(rr => rr.Reservation)
                .HasForeignKey(rr => rr.ReservationId);

            modelBuilder.Entity<Reservation>()
                .HasMany(r => r.Transactions)
                .WithOne(t => t.Reservation)
                .HasForeignKey(t => t.ReservationId);

            // ---------------- RESERVATION ROOM ----------------
            modelBuilder.Entity<ReservationRoom>()
                .HasOne(rr => rr.Room)
                .WithMany(r => r.ReservationRooms)
                .HasForeignKey(rr => rr.RoomId);

            // ---------------- UNIQUE CONSTRAINTS ----------------

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Reservation>()
                .HasIndex(r => r.ReservationCode)
                .IsUnique();

            // ---------------- ENUM STORAGE ----------------
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<int>();

            modelBuilder.Entity<Reservation>()
                .Property(r => r.Status)
                .HasConversion<int>();

            modelBuilder.Entity<Transaction>()
                .Property(t => t.PaymentMethod)
                .HasConversion<int>();

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Status)
                .HasConversion<int>();
        }
    }
}
