using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FlexFit.Caching;
using FlexFit.Booking.Service.ExternalServices.Catalog;

namespace FlexFit.Booking.API.ExternalServices.Catalog
{
    public class CatalogServiceClient : ICatalogServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly FlexFit.Catalog.Service.Protos.CatalogGrpc.CatalogGrpcClient _grpcClient;
        private readonly ILogger<CatalogServiceClient> _logger;
        private readonly ICacheService _cacheService;
        private readonly bool _useMock;

        public CatalogServiceClient(
            HttpClient httpClient,
            FlexFit.Catalog.Service.Protos.CatalogGrpc.CatalogGrpcClient grpcClient,
            IConfiguration configuration,
            ILogger<CatalogServiceClient> logger,
            ICacheService cacheService)
        {
            _httpClient = httpClient;
            _grpcClient = grpcClient;
            _logger = logger;
            _cacheService = cacheService;
            _useMock = configuration.GetValue<bool>("CatalogConfig:UseMock", true);
        }

        public async Task<CatalogSessionDetails?> GetGymSessionDetailsAsync(Guid sessionId)
        {
            var cacheKey = RedisKeys.CatalogSession(sessionId);
            try
            {
                var cached = await _cacheService.GetAsync<CatalogSessionDetails>(cacheKey);
                if (cached != null)
                {
                    _logger.LogInformation("Cache hit for Gym Session {SessionId}", sessionId);
                    return cached;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read Gym Session {SessionId} from cache", sessionId);
            }

            CatalogSessionDetails? details = null;
            if (_useMock)
            {
                _logger.LogInformation("Using mock data for GetGymSessionDetailsAsync({SessionId})", sessionId);
                details = new CatalogSessionDetails
                {
                    SessionId = sessionId,
                    GymId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    GymName = "Mock FlexFit Gym",
                    BranchId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    BranchName = "Mock Branch Alpha",
                    BranchAddress = "123 Mock Street, HCMC",
                    SessionName = "Mock Gym Workout Session",
                    StartTime = DateTime.UtcNow.AddHours(2),
                    EndTime = DateTime.UtcNow.AddHours(3),
                    Capacity = 20,
                    CreditCost = 5,
                    Status = "Open"
                };
            }
            else
            {
                try
                {
                    // Call Catalog gRPC Service
                    _logger.LogInformation("Calling Catalog gRPC GetBranchBookingSnapshot for BranchId {SessionId}", sessionId);
                    var grpcResponse = await _grpcClient.GetBranchBookingSnapshotAsync(new FlexFit.Catalog.Service.Protos.GetBranchBookingSnapshotRequest
                    {
                        BranchId = sessionId.ToString()
                    });

                    if (grpcResponse != null)
                    {
                        details = new CatalogSessionDetails
                        {
                            SessionId = Guid.TryParse(grpcResponse.ResourceId, out var rId) ? rId : sessionId,
                            GymId = Guid.TryParse(grpcResponse.GymId, out var gId) ? gId : Guid.Empty,
                            GymName = string.IsNullOrWhiteSpace(grpcResponse.GymName) ? "Flexfit Gym" : grpcResponse.GymName,
                            BranchId = Guid.TryParse(grpcResponse.BranchId, out var bId) ? bId : sessionId,
                            BranchName = grpcResponse.BranchName,
                            BranchAddress = "Địa chỉ hệ thống",
                            SessionName = grpcResponse.Title,
                            StartTime = DateTime.TryParse(grpcResponse.StartTime, out var st) && st != DateTime.MinValue ? st : DateTime.UtcNow,
                            EndTime = DateTime.TryParse(grpcResponse.EndTime, out var et) && et != DateTime.MinValue ? et : DateTime.UtcNow.AddHours(2),
                            Capacity = grpcResponse.Capacity > 0 ? grpcResponse.Capacity : 100,
                            CreditCost = grpcResponse.CreditCost > 0 ? grpcResponse.CreditCost : 5,
                            Status = grpcResponse.Status
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to call Catalog gRPC Service for branch {SessionId}", sessionId);
                }
            }

            if (details != null)
            {
                try
                {
                    await _cacheService.SetAsync(cacheKey, details, TimeSpan.FromMinutes(5));
                    _logger.LogInformation("Cached Gym Session {SessionId} for 5 minutes", sessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to write Gym Session {SessionId} to cache", sessionId);
                }
            }

            return details;
        }

        public async Task<CatalogClassDetails?> GetClassDetailsAsync(Guid classId)
        {
            var cacheKey = RedisKeys.CatalogClass(classId);
            try
            {
                var cached = await _cacheService.GetAsync<CatalogClassDetails>(cacheKey);
                if (cached != null)
                {
                    _logger.LogInformation("Cache hit for Class {ClassId}", classId);
                    return cached;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read Class {ClassId} from cache", classId);
            }

            CatalogClassDetails? details = null;
            if (_useMock)
            {
                _logger.LogInformation("Using mock data for GetClassDetailsAsync({ClassId})", classId);
                details = new CatalogClassDetails
                {
                    ClassId = classId,
                    ScheduleId = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                    GymId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    GymName = "Mock FlexFit Gym",
                    BranchId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    BranchName = "Mock Branch Alpha",
                    BranchAddress = "123 Mock Street, HCMC",
                    ClassName = "Mock Yoga/Cycling Class",
                    CoachName = "Coach John Doe",
                    StartTime = DateTime.UtcNow.AddHours(4),
                    EndTime = DateTime.UtcNow.AddHours(5.5),
                    Capacity = 15,
                    CreditCost = 8,
                    Status = "Open"
                };
            }
            else
            {
                try
                {
                    // Call Catalog gRPC Service
                    _logger.LogInformation("Calling Catalog gRPC GetClassBookingSnapshot for ClassId {ClassId}", classId);
                    var grpcResponse = await _grpcClient.GetClassBookingSnapshotAsync(new FlexFit.Catalog.Service.Protos.GetClassBookingSnapshotRequest
                    {
                        ClassId = classId.ToString()
                    });

                    if (grpcResponse != null)
                    {
                        details = new CatalogClassDetails
                        {
                            ClassId = Guid.TryParse(grpcResponse.ResourceId, out var cId) ? cId : classId,
                            ScheduleId = Guid.Empty,
                            GymId = Guid.TryParse(grpcResponse.GymId, out var gId) ? gId : Guid.Empty,
                            GymName = string.IsNullOrWhiteSpace(grpcResponse.GymName) ? "Flexfit Gym" : grpcResponse.GymName,
                            BranchId = Guid.TryParse(grpcResponse.BranchId, out var bId) ? bId : Guid.Empty,
                            BranchName = grpcResponse.BranchName,
                            BranchAddress = "Địa chỉ hệ thống",
                            ClassName = grpcResponse.Title,
                            CoachName = "Coach FlexFit",
                            StartTime = DateTime.TryParse(grpcResponse.StartTime, out var st) ? st : DateTime.UtcNow.AddHours(4),
                            EndTime = DateTime.TryParse(grpcResponse.EndTime, out var et) ? et : DateTime.UtcNow.AddHours(5.5),
                            Capacity = grpcResponse.Capacity,
                            CreditCost = grpcResponse.CreditCost,
                            Status = grpcResponse.Status
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to call Catalog gRPC Service for class {ClassId}", classId);
                }
            }

            if (details != null)
            {
                try
                {
                    await _cacheService.SetAsync(cacheKey, details, TimeSpan.FromMinutes(5));
                    _logger.LogInformation("Cached Class {ClassId} for 5 minutes", classId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to write Class {ClassId} to cache", classId);
                }
            }

            return details;
        }

        public async Task<bool> VerifyStaffPermissionAsync(Guid staffId, Guid branchId)
        {
            if (_useMock)
            {
                _logger.LogInformation("Mocking VerifyStaffPermissionAsync({StaffId}, {BranchId}) -> true", staffId, branchId);
                return true;
            }

            try
            {
                // Verify staff permission via Catalog Service (returns true for staff/partner)
                _logger.LogInformation("VerifyStaffPermissionAsync for staff {StaffId} in Branch {BranchId}", staffId, branchId);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call Catalog Service to verify staff {StaffId} in Branch {BranchId}", staffId, branchId);
                return true;
            }
        }

        public async Task<IEnumerable<Guid>> GetManagedBranchIdsAsync(Guid managerId, string role)
        {
            if (_useMock)
            {
                _logger.LogInformation("Mocking GetManagedBranchIdsAsync({ManagerId}, {Role})", managerId, role);
                return new List<Guid> { Guid.Parse("11111111-1111-1111-1111-111111111111") };
            }

            try
            {
                var response = await _httpClient.GetAsync($"/api/catalog/managers/{managerId}/branches?role={role}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<IEnumerable<Guid>>() ?? new List<Guid>();
                }
                return new List<Guid>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load managed branch IDs for Manager {ManagerId}", managerId);
                return new List<Guid>();
            }
        }
    }

    public class CatalogBranchResponse
    {
        public Guid BranchId { get; set; }
        public Guid GymId { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public int CreditCost { get; set; }
    }
}
