using Flexfit.Models;

namespace Flexfit.Repositories
{
    public interface IGymRepository
    {
        Task<IEnumerable<Gym>> GetAllAsync();
        Task<Gym?> GetByIdAsync(Guid id);
        Task AddAsync(Gym gym);
        Task UpdateAsync(Gym gym);
        Task DeleteAsync(Guid id);
    }
}