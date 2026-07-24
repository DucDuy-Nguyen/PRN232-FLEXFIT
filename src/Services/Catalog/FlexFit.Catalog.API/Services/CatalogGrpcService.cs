using FlexFit.Catalog.Service.Interfaces;
using Grpc.Core;
using System;
using System.Threading.Tasks;
using FlexFit.Catalog.Service.Protos;

namespace FlexFit.Catalog.API.Services
{
    public class CatalogGrpcService : CatalogGrpc.CatalogGrpcBase
    {
        private readonly IBookingSnapshotService _bookingSnapshotService;

        public CatalogGrpcService(IBookingSnapshotService bookingSnapshotService)
        {
            _bookingSnapshotService = bookingSnapshotService;
        }

        public override async Task<BookingSnapshotResponse> GetClassBookingSnapshot(GetClassBookingSnapshotRequest request, ServerCallContext context)
        {
            if (!Guid.TryParse(request.ClassId, out Guid classGuid))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid ClassId format"));
            }

            var dto = await _bookingSnapshotService.GetClassBookingSnapshotAsync(classGuid);

            if (dto == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Class with ID {request.ClassId} not found"));
            }

            return new BookingSnapshotResponse
            {
                ResourceId = dto.ResourceId,
                ResourceType = dto.ResourceType,
                GymId = dto.GymId,
                GymName = dto.GymName,
                BranchId = dto.BranchId,
                BranchName = dto.BranchName,
                Title = dto.Title,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                CreditCost = dto.CreditCost,
                Capacity = dto.Capacity,
                Status = dto.Status,
                IsActive = dto.IsActive
            };
        }

        public override async Task<BookingSnapshotResponse> GetGymSessionBookingSnapshot(GetGymSessionBookingSnapshotRequest request, ServerCallContext context)
        {
            if (!Guid.TryParse(request.SessionId, out Guid sessionGuid))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid SessionId format"));
            }

            var dto = await _bookingSnapshotService.GetGymSessionBookingSnapshotAsync(sessionGuid);

            if (dto == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"GymSession with ID {request.SessionId} not found"));
            }

            return new BookingSnapshotResponse
            {
                ResourceId = dto.ResourceId,
                ResourceType = dto.ResourceType,
                GymId = dto.GymId,
                GymName = dto.GymName,
                BranchId = dto.BranchId,
                BranchName = dto.BranchName,
                Title = dto.Title,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                CreditCost = dto.CreditCost,
                Capacity = dto.Capacity,
                Status = dto.Status,
                IsActive = dto.IsActive
            };
        }
    }
}
