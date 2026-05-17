using Flexfit.DTOs.Booking;

namespace Flexfit.Service
{
    public interface IBookingService
    {
        Task<GymBookingResponse> BookGymSessionAsync(Guid userId, CreateGymBookingRequest request);
        Task<IEnumerable<GymBookingResponse>> GetMyGymBookingsAsync(Guid userId);
        Task<bool> CancelGymBookingAsync(Guid userId, Guid bookingId);

        Task<ClassBookingResponse> BookClassAsync(Guid userId, CreateClassBookingRequest request);
        Task<IEnumerable<ClassBookingResponse>> GetMyClassBookingsAsync(Guid userId);
        Task<bool> CancelClassBookingAsync(Guid userId, Guid bookingId);

        Task<IEnumerable<GymBookingResponse>> GetPartnerGymBookingsAsync(Guid ownerId);
        Task<IEnumerable<ClassBookingResponse>> GetPartnerClassBookingsAsync(Guid ownerId);
    }
}
