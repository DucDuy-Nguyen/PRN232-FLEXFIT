using Flexfit.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flexfit.Repositories
{
    public interface ICheckInLogRepository
    {
        Task<IEnumerable<CheckInLog>> GetAllAsync();
        Task<IEnumerable<CheckInLog>> GetByUserIdAsync(Guid userId);
        Task<CheckInLog?> GetByIdAsync(Guid id);
        Task AddAsync(CheckInLog log);
        Task<bool> IsStaffOrOwnerForGymBookingAsync(Guid gymBookingId, Guid scannerId);
        Task<bool> IsStaffOrOwnerForClassBookingAsync(Guid classBookingId, Guid scannerId);
        Task<IEnumerable<CheckInLog>> GetLogsForManagerAsync(Guid managerId);
        Task<GymBooking?> GetGymBookingByIdAsync(Guid bookingId);
        Task<ClassBooking?> GetClassBookingByIdAsync(Guid bookingId);
        Task UpdateGymBookingAsync(GymBooking booking);
        Task UpdateClassBookingAsync(ClassBooking booking);
        Task SaveChangesAsync();
    }
}