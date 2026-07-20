using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace FlexFit.Caching;

public sealed class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key cannot be null, empty or whitespace.", nameof(key));
        }

        try
        {
            var cachedData = await _cache.GetAsync(key, cancellationToken);
            if (cachedData is null)
            {
                _logger.LogDebug("Cache miss for key: {CacheKey}", key);
                return default;
            }

            _logger.LogDebug("Cache hit for key: {CacheKey}", key);
            return JsonSerializer.Deserialize<T>(cachedData);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize cached item for key: {CacheKey}", key);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading from cache for key: {CacheKey}", key);
            throw;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key cannot be null, empty or whitespace.", nameof(key));
        }

        if (expiration <= TimeSpan.Zero)
        {
            throw new ArgumentException("Expiration timespan must be positive.", nameof(expiration));
        }

        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            var serializedData = JsonSerializer.SerializeToUtf8Bytes(value);
            await _cache.SetAsync(key, serializedData, options, cancellationToken);
            _logger.LogDebug("Successfully set cache for key: {CacheKey} with expiration: {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing to cache for key: {CacheKey}", key);
            throw;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key cannot be null, empty or whitespace.", nameof(key));
        }

        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Removed cache key: {CacheKey}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {CacheKey}", key);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key cannot be null, empty or whitespace.", nameof(key));
        }

        try
        {
            var cachedData = await _cache.GetAsync(key, cancellationToken);
            return cachedData is not null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of cache key: {CacheKey}", key);
            throw;
        }
    }
}
