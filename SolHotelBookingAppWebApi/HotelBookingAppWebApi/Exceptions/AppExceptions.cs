namespace HotelBookingAppWebApi.Exceptions
{
    /// <summary>Base application exception. Carries an HTTP status code for the middleware.</summary>
    public class AppException : Exception
    {
        public int StatusCode { get; }
        public AppException(string message, int statusCode) : base(message)
            => StatusCode = statusCode;
    }

    /// <summary>404 — Resource not found.</summary>
    public class NotFoundException : AppException
    {
        public NotFoundException(string message) : base(message, 404) { }
    }

    /// <summary>409 — Duplicate or conflicting resource.</summary>
    public class ConflictException : AppException
    {
        public ConflictException(string message) : base(message, 409) { }
    }

    /// <summary>400 — Invalid input or business rule violation.</summary>
    public class ValidationException : AppException
    {
        public ValidationException(string message) : base(message, 400) { }
    }

    /// <summary>401 — Authentication or authorization failure.</summary>
    public class UnAuthorizedException : AppException
    {
        public UnAuthorizedException(string message = "Unauthorized") : base(message, 401) { }
    }

    /// <summary>400 — Payment processing failure.</summary>
    public class PaymentException : AppException
    {
        public PaymentException(string message) : base(message, 400) { }
    }

    /// <summary>400 — Reservation workflow failure.</summary>
    public class ReservationFailedException : AppException
    {
        public ReservationFailedException(string message)
            : base($"{message} — Reservation failed.", 400) { }
    }

    /// <summary>409 — Not enough inventory to fulfil the request.</summary>
    public class InsufficientInventoryException : AppException
    {
        public InsufficientInventoryException(string message)
            : base($"{message} — Inventory insufficient.", 409) { }
    }

    /// <summary>404 — No pricing rate configured for the requested date.</summary>
    public class RateNotFoundException : AppException
    {
        public RateNotFoundException(string message)
            : base($"{message} — Rate not found.", 404) { }
    }

    /// <summary>400 — Review business rule violation.</summary>
    public class ReviewException : AppException
    {
        public ReviewException(string message) : base(message, 400) { }
    }

    /// <summary>404 — User profile details not found.</summary>
    public class UserProfileException : AppException
    {
        public UserProfileException(string message) : base(message, 404) { }
    }

    /// <summary>400 — Entity creation failed.</summary>
    public class UnableToCreateEntityException : AppException
    {
        public UnableToCreateEntityException(string message = "Unable to create entity")
            : base(message, 400) { }
    }
}
