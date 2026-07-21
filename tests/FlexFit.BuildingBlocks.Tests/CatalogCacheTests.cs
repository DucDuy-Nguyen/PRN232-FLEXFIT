using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using FlexFit.Caching;
using FlexFit.BookingService.ExternalServices.Catalog;
using Xunit;

namespace FlexFit.BuildingBlocks.Tests
{
    public class CatalogCacheTests
    {
        private readonly ICacheService _cacheService = Substitute.For<ICacheService>();
        private readonly ILogger<CatalogServiceClient> _logger = Substitute.For<ILogger<CatalogServiceClient>>();
        private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();

        public CatalogCacheTests()
        {
            var configSection = Substitute.For<IConfigurationSection>();
            configSection.Value = "true";
            _configuration.GetSection("CatalogConfig:UseMock").Returns(configSection);
        }

        [Fact]
        public async Task GetGymSessionDetails_Should_ReturnCachedValue_OnCacheHit()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var cachedDetails = new CatalogSessionDetails
            {
                SessionId = sessionId,
                GymId = Guid.NewGuid(),
                GymName = "Cached Gym",
                BranchId = Guid.NewGuid(),
                BranchName = "Cached Branch",
                BranchAddress = "Cached Address",
                SessionName = "Cached Session",
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(1),
                Capacity = 10,
                CreditCost = 2,
                Status = "Open"
            };

            _cacheService.GetAsync<CatalogSessionDetails>(RedisKeys.CatalogSession(sessionId), Arg.Any<CancellationToken>())
                .Returns(cachedDetails);

            var handler = new MockHttpMessageHandler
            {
                SendAsyncFunc = req => throw new Exception("HTTP client should not be called on cache hit")
            };
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };

            var client = new CatalogServiceClient(httpClient, _configuration, _logger, _cacheService);

            // Act
            var result = await client.GetGymSessionDetailsAsync(sessionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Cached Gym", result.GymName);
            await _cacheService.Received(1).GetAsync<CatalogSessionDetails>(RedisKeys.CatalogSession(sessionId), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetGymSessionDetails_Should_FetchFromMock_WhenCacheMiss_And_UseMockIsTrue()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            _cacheService.GetAsync<CatalogSessionDetails>(RedisKeys.CatalogSession(sessionId), Arg.Any<CancellationToken>())
                .Returns((CatalogSessionDetails?)null);

            var handler = new MockHttpMessageHandler
            {
                SendAsyncFunc = req => throw new Exception("HTTP client should not be called when UseMock is true")
            };
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };

            var client = new CatalogServiceClient(httpClient, _configuration, _logger, _cacheService);

            // Act
            var result = await client.GetGymSessionDetailsAsync(sessionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Mock FlexFit Gym", result.GymName);
            Assert.Equal(sessionId, result.SessionId);

            // Verify was stored in cache with 5 minutes expiration
            await _cacheService.Received(1).SetAsync(RedisKeys.CatalogSession(sessionId), Arg.Is<CatalogSessionDetails>(d => d.SessionId == sessionId), TimeSpan.FromMinutes(5), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetClassDetails_Should_ReturnCachedValue_OnCacheHit()
        {
            // Arrange
            var classId = Guid.NewGuid();
            var cachedDetails = new CatalogClassDetails
            {
                ClassId = classId,
                GymId = Guid.NewGuid(),
                GymName = "Cached Class Gym",
                BranchId = Guid.NewGuid(),
                BranchName = "Cached Class Branch",
                BranchAddress = "Cached Class Address",
                ClassName = "Cached Yoga",
                CoachName = "Coach cached",
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(1),
                Capacity = 10,
                CreditCost = 3,
                Status = "Open"
            };

            _cacheService.GetAsync<CatalogClassDetails>(RedisKeys.CatalogClass(classId), Arg.Any<CancellationToken>())
                .Returns(cachedDetails);

            var handler = new MockHttpMessageHandler
            {
                SendAsyncFunc = req => throw new Exception("HTTP client should not be called on cache hit")
            };
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };

            var client = new CatalogServiceClient(httpClient, _configuration, _logger, _cacheService);

            // Act
            var result = await client.GetClassDetailsAsync(classId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Cached Yoga", result.ClassName);
            await _cacheService.Received(1).GetAsync<CatalogClassDetails>(RedisKeys.CatalogClass(classId), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetClassDetails_Should_FetchFromMock_WhenCacheMiss_And_UseMockIsTrue()
        {
            // Arrange
            var classId = Guid.NewGuid();
            _cacheService.GetAsync<CatalogClassDetails>(RedisKeys.CatalogClass(classId), Arg.Any<CancellationToken>())
                .Returns((CatalogClassDetails?)null);

            var handler = new MockHttpMessageHandler
            {
                SendAsyncFunc = req => throw new Exception("HTTP client should not be called when UseMock is true")
            };
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };

            var client = new CatalogServiceClient(httpClient, _configuration, _logger, _cacheService);

            // Act
            var result = await client.GetClassDetailsAsync(classId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Mock FlexFit Gym", result.GymName);
            Assert.Equal(classId, result.ClassId);

            // Verify was stored in cache with 5 minutes expiration
            await _cacheService.Received(1).SetAsync(RedisKeys.CatalogClass(classId), Arg.Is<CatalogClassDetails>(d => d.ClassId == classId), TimeSpan.FromMinutes(5), Arg.Any<CancellationToken>());
        }
    }

    public class MockHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, Task<HttpResponseMessage>> SendAsyncFunc { get; set; } = null!;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return SendAsyncFunc(request);
        }
    }
}
