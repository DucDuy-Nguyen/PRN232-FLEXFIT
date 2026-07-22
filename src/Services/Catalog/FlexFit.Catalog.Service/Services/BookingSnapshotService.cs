using FlexFit.Catalog.Repository.Repositories;
using FlexFit.Catalog.Service.DTOs;
using FlexFit.Catalog.Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace FlexFit.Catalog.Service.Services
{
    public class BookingSnapshotService : IBookingSnapshotService
    {
        private readonly IBookingSnapshotRepository _repository;

        public BookingSnapshotService(IBookingSnapshotRepository repository)
        {
            _repository = repository;
        }

        public async Task<BookingSnapshotDto> GetClassBookingSnapshotAsync(Guid classId)
        {
            var cls = await _repository.GetClassWithGymAndBranchAsync(classId);
            if (cls == null) return null!;

            return new BookingSnapshotDto
            {
                ResourceId = cls.ClassId.ToString(),
                ResourceType = "Class",
                GymId = cls.Branch.GymId.ToString(),
                GymName = cls.Branch.Gym.GymName,
                BranchId = cls.BranchId.ToString(),
                BranchName = cls.Branch.BranchName,
                Title = cls.ClassName,
                StartTime = cls.StartTime.ToString("o"),
                EndTime = cls.EndTime.ToString("o"),
                CreditCost = cls.CreditCost,
                Capacity = cls.Capacity,
                Status = cls.Status,
                IsActive = cls.Status == "Open"
            };
        }

        public async Task<BookingSnapshotDto> GetGymSessionBookingSnapshotAsync(Guid sessionId)
        {
            var session = await _repository.GetGymSessionWithGymAndBranchAsync(sessionId);
            if (session == null) return null!;

            return new BookingSnapshotDto
            {
                ResourceId = session.SessionId.ToString(),
                ResourceType = "GymSession",
                GymId = session.Branch.GymId.ToString(),
                GymName = session.Branch.Gym.GymName,
                BranchId = session.BranchId.ToString(),
                BranchName = session.Branch.BranchName,
                Title = session.SessionName ?? "Gym Session",
                StartTime = session.StartTime.ToString("o"),
                EndTime = session.EndTime.ToString("o"),
                CreditCost = session.CreditCost,
                Capacity = session.Capacity,
                Status = session.Status,
                IsActive = session.Status == "Open"
            };
        }
    }
}
