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
        private readonly IBranchService _branchService;

        public CatalogGrpcService(IBookingSnapshotService bookingSnapshotService, IBranchService branchService)
        {
            _bookingSnapshotService = bookingSnapshotService;
            _branchService = branchService;
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

        public override async Task<BookingSnapshotResponse> GetBranchBookingSnapshot(GetBranchBookingSnapshotRequest request, ServerCallContext context)
        {
            if (!Guid.TryParse(request.BranchId, out Guid branchGuid))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid BranchId format"));
            }

            var branch = await _branchService.GetBranchByIdAsync(branchGuid);
            if (branch == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Branch with ID {request.BranchId} not found"));
            }

            return new BookingSnapshotResponse
            {
                ResourceId = branch.BranchId.ToString(),
                ResourceType = "GymSession",
                GymId = branch.GymId.ToString(),
                GymName = "Flexfit Gym",
                BranchId = branch.BranchId.ToString(),
                BranchName = branch.BranchName,
                Title = "Tập Gym tự do - " + branch.BranchName,
                StartTime = DateTime.UtcNow.ToString("o"),
                EndTime = DateTime.UtcNow.AddHours(2).ToString("o"),
                CreditCost = branch.CreditCost > 0 ? branch.CreditCost : 5,
                Capacity = 100,
                Status = "Open",
                IsActive = branch.IsActive
            };
        }
    }
}
