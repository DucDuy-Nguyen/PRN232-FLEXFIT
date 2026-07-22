using FlexFit.Catalog.Repository.Data;
using FlexFit.Catalog.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace FlexFit.Catalog.Repository.Repositories
{
    public class BookingSnapshotRepository : IBookingSnapshotRepository
    {
        private readonly CatalogDbContext _db;

        public BookingSnapshotRepository(CatalogDbContext db)
        {
            _db = db;
        }

        public async Task<Class?> GetClassWithGymAndBranchAsync(Guid classId)
        {
            return await _db.Classes
                .Include(c => c.Branch)
                    .ThenInclude(b => b.Gym)
                .FirstOrDefaultAsync(c => c.ClassId == classId);
        }

        public async Task<GymSession?> GetGymSessionWithGymAndBranchAsync(Guid sessionId)
        {
            return await _db.GymSessions
                .Include(s => s.Branch)
                    .ThenInclude(b => b.Gym)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
        }
    }
}
