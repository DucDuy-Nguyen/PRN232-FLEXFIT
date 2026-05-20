using Flexfit.Models;

namespace Flexfit.Repositories
{
    public interface IBookingRepository
    {
        // Gym Bookings
        Task<GymBooking?> GetGymBookingByIdAsync(Guid bookingId);
        Task<IEnumerable<GymBooking>> GetGymBookingsByUserIdAsync(Guid userId);
        Task<GymSession?> GetGymSessionByIdAsync(Guid sessionId);
        Task<GymSession?> GetGymSessionByDetailsAsync(Guid branchId, string sessionName, DateTime startTime, DateTime endTime);
        Task AddGymSessionAsync(GymSession session);
        Task AddGymBookingAsync(GymBooking booking);
        Task UpdateGymBookingAsync(GymBooking booking);
        Task<int> CountGymBookingsBySessionIdAsync(Guid sessionId);

        // Class Bookings
        Task<ClassBooking?> GetClassBookingByIdAsync(Guid bookingId);
        Task<IEnumerable<ClassBooking>> GetClassBookingsByUserIdAsync(Guid userId);
        Task<Class?> GetClassByIdAsync(Guid classId);
        Task AddClassBookingAsync(ClassBooking booking);
        Task UpdateClassBookingAsync(ClassBooking booking);
        Task<int> CountClassBookingsByClassIdAsync(Guid classId);
        Task<IEnumerable<GymBooking>> GetGymBookingsByOwnerIdAsync(Guid ownerId);
        Task<IEnumerable<ClassBooking>> GetClassBookingsByOwnerIdAsync(Guid ownerId);
        Task<IEnumerable<GymBooking>> GetGymBookingsToRemindAsync(DateTime now, int hoursLeft);
        Task<IEnumerable<ClassBooking>> GetClassBookingsToRemindAsync(DateTime now, int hoursLeft);

        // Credit operations for bookings
        Task<UserCredit?> GetUserCreditAsync(Guid userId);
        Task AddCreditTransactionAsync(CreditTransaction transaction);

        Task<Branch?> GetBranchByIdAsync(Guid branchId);

        Task<int> GetCancellationCountTodayAsync(Guid userId);

        Task SaveChangesAsync();
    }
}
