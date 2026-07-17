namespace FlexFit.Engagement.Infrastructure.Redis;

public static class RedisChannelNames
{
    public const string GymBookingCreated = "flexfit.booking.gym-booking-created";
    public const string BookingCancelled = "flexfit.booking.booking-cancelled";
    public const string PaymentCompleted = "flexfit.payment.payment-completed";
    public const string UserRegistered = "flexfit.user.user-registered";
    public const string CheckInCompleted = "flexfit.checkin.check-in-completed";
}
