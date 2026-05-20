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
        Task SaveChangesAsync();
    }
}