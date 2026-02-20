namespace HotelBookingAppWebApi.Models
{
    public class User
    {
        public int UserId { get; set; }

        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;

        public UserRole Role { get; set; }

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        public override string ToString()
        {
            return $"User [{UserId}] | {UserName} | {Email} | Role: {Role}";
        }

    }
    public enum UserRole
    {
        Guest,
        Admin
    }
}
