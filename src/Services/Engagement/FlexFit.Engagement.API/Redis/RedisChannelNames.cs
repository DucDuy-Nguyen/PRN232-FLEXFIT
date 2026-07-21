using FlexFit.Engagement.API.Data;
namespace FlexFit.Engagement.API.Redis;

public static class RedisChannelNames
{
    public const string BookingCreated = "flexfit.booking.gym-booking-created";
    public const string BookingCancelled = "flexfit.booking.booking-cancelled";
    public const string PaymentCompleted = "flexfit.payment.payment-completed";
    public const string UserRegistered = "flexfit.identity.user-registered";
    public const string CheckInCompleted = "flexfit.booking.checkin-completed";
}
// Note: Matches custom project configurations exactly
