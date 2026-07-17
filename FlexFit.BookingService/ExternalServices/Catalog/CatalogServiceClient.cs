using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FlexFit.BookingService.ExternalServices.Catalog
{
    public class CatalogServiceClient : ICatalogServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CatalogServiceClient> _logger;
        private readonly bool _useMock;

        public CatalogServiceClient(HttpClient httpClient, IConfiguration configuration, ILogger<CatalogServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _useMock = configuration.GetValue<bool>("CatalogConfig:UseMock", true);
        }

        public async Task<CatalogSessionDetails?> GetGymSessionDetailsAsync(Guid sessionId)
        {
            if (_useMock)
            {
                _logger.LogInformation("Using mock data for GetGymSessionDetailsAsync({SessionId})", sessionId);
                return new CatalogSessionDetails
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

            try
            {
                var response = await _httpClient.GetAsync($"/api/catalog/sessions/{sessionId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<CatalogSessionDetails>();
                }
                _logger.LogWarning("Catalog Service returned status {Status} for session {SessionId}", response.StatusCode, sessionId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call Catalog Service for session {SessionId}", sessionId);
                return null;
            }
        }

        public async Task<CatalogClassDetails?> GetClassDetailsAsync(Guid classId)
        {
            if (_useMock)
            {
                _logger.LogInformation("Using mock data for GetClassDetailsAsync({ClassId})", classId);
                return new CatalogClassDetails
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

            try
            {
                var response = await _httpClient.GetAsync($"/api/catalog/classes/{classId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<CatalogClassDetails>();
                }
                _logger.LogWarning("Catalog Service returned status {Status} for class {ClassId}", response.StatusCode, classId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call Catalog Service for class {ClassId}", classId);
                return null;
            }
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
                var response = await _httpClient.GetAsync($"/api/catalog/branches/{branchId}/verify-staff/{staffId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<bool>();
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call Catalog Service to verify staff {StaffId} in Branch {BranchId}", staffId, branchId);
                return false;
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
}
