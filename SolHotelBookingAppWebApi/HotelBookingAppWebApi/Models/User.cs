using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models
{
    public class User
    {
        [Key]
        public Guid UserId { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public byte[] Password { get; set; } = Array.Empty<byte>();

        [Required]
        public byte[] PasswordSaltValue { get; set; } = Array.Empty<byte>();

        public bool IsActive { get; set; } = true;

        [Required]
        public UserRole Role { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public UserProfileDetails? UserDetails { get; set; }
        public Guid? HotelId { get; set; }
        public Hotel? Hotel { get; set; }

        public ICollection<Reservation>? Reservations { get; set; }
        public ICollection<Review>? Reviews { get; set; }
        public ICollection<Log>? Logs { get; set; }
        public ICollection<AuditLog>? AuditLogs { get; set; }
    }

    public enum UserRole
    {
        Guest = 1,
        Admin = 2,
        SuperAdmin = 3
    }
}
