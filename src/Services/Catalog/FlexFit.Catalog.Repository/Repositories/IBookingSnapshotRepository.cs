using FlexFit.Catalog.Repository.Models;
using System;
using System.Threading.Tasks;

namespace FlexFit.Catalog.Repository.Repositories
{
    public interface IBookingSnapshotRepository
    {
        Task<Class?> GetClassWithGymAndBranchAsync(Guid classId);
        Task<GymSession?> GetGymSessionWithGymAndBranchAsync(Guid sessionId);
    }
}
