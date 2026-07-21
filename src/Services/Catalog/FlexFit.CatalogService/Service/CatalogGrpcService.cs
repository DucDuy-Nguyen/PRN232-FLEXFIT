using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using FlexFit.CatalogService.Models;
using FlexFit.CatalogService.Protos;

namespace FlexFit.CatalogService.Service;

public class CatalogGrpcService : CatalogGrpc.CatalogGrpcBase
{
    private readonly CatalogDbContext _db;

    public CatalogGrpcService(CatalogDbContext db)
    {
        _db = db;
    }

    public override async Task<BookingSnapshotResponse> GetClassBookingSnapshot(GetClassBookingSnapshotRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.ClassId, out Guid classGuid))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid ClassId format"));
        }

        var cls = await _db.Classes
            .Include(c => c.Branch)
                .ThenInclude(b => b.Gym)
            .FirstOrDefaultAsync(c => c.ClassId == classGuid);

        if (cls == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Class with ID {request.ClassId} not found"));
        }

        return new BookingSnapshotResponse
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

    public override async Task<BookingSnapshotResponse> GetGymSessionBookingSnapshot(GetGymSessionBookingSnapshotRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.SessionId, out Guid sessionGuid))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid SessionId format"));
        }

        var session = await _db.GymSessions
            .Include(s => s.Branch)
                .ThenInclude(b => b.Gym)
            .FirstOrDefaultAsync(s => s.SessionId == sessionGuid);

        if (session == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"GymSession with ID {request.SessionId} not found"));
        }

        return new BookingSnapshotResponse
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
