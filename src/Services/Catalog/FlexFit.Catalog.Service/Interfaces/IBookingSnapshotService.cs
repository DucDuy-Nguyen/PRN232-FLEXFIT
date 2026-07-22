using FlexFit.Catalog.Service.DTOs;
using System;
using System.Threading.Tasks;

namespace FlexFit.Catalog.Service.Interfaces
{
    public interface IBookingSnapshotService
    {
        Task<BookingSnapshotDto> GetClassBookingSnapshotAsync(Guid classId);
        Task<BookingSnapshotDto> GetGymSessionBookingSnapshotAsync(Guid sessionId);
    }
}
