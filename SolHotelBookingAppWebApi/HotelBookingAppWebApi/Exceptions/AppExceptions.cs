namespace HotelBookingAppWebApi.Exceptions
{
    public class AppException : Exception
    {
        public int StatusCode { get; }
        public AppException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
    }

    public class NotFoundException : AppException
    {
        public NotFoundException(string message) : base(message, 404) { }
    }

    public class ConflictException : AppException
    {
        public ConflictException(string message) : base(message, 409) { }
    }

    public class ValidationException : AppException
    {
        public ValidationException(string message) : base(message, 400) { }
    }

    public class UnAuthorizedException : AppException
    {
        public UnAuthorizedException(string message = "Unauthorized") : base(message, 401) { }
    }

    public class PaymentException : AppException
    {
        public PaymentException(string message) : base(message, 400) { }
    }

    public class ReservationFailedException : AppException
    {
        public ReservationFailedException(string message) : base($"{message} — Reservation failed.", 400) { }
    }

    public class InsufficientInventoryException : AppException
    {
        public InsufficientInventoryException(string message) : base($"{message} — Inventory insufficient.", 409) { }
    }

    public class RateNotFoundException : AppException
    {
        public RateNotFoundException(string message) : base($"{message} — Rate not found.", 404) { }
    }

    public class ReviewException : AppException
    {
        public ReviewException(string message) : base(message, 400) { }
    }

    public class UserProfileException : AppException
    {
        public UserProfileException(string message) : base(message, 404) { }
    }

    public class UnableToCreateEntityException : AppException
    {
        public UnableToCreateEntityException(string message = "Unable to create entity") : base(message, 400) { }
    }
}
