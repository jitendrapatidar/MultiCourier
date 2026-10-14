namespace MultiCourie.Application.Common;

public static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string UnknownCourier = "UNKNOWN_COURIER_PARTNER";
    public const string OrderNotFound = "ORDER_NOT_FOUND";
    public const string BatchNotFound = "BATCH_NOT_FOUND";
    public const string CourierClientError = "COURIER_CLIENT_ERROR";
    public const string CourierUnavailable = "COURIER_UNAVAILABLE";
    public const string CancelNotAllowed = "CANCEL_NOT_ALLOWED";
    public const string InternalError = "INTERNAL_ERROR";
}
