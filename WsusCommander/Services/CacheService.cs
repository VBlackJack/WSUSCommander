/*
 * Copyright 2025 Julien Bombled
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Concurrent;
using WsusCommander.Constants;

namespace WsusCommander.Services;

/// <summary>
/// In-memory cache service with TTL support.
/// </summary>
public sealed class CacheService : ICacheService, IDisposable
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly IConfigurationService _configService;
    private readonly Timer _cleanupTimer;
    private long _hitCount;
    private long _missCount;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheService"/> class.
    /// </summary>
    public CacheService(IConfigurationService configService)
    {
        _configService = configService;

        // Cleanup expired entries periodically
        var cleanupInterval = TimeSpan.FromMinutes(AppConstants.Cache.CleanupIntervalMinutes);
        _cleanupTimer = new Timer(CleanupExpired, null, cleanupInterval, cleanupInterval);
    }

    /// <inheritdoc/>
    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null)
    {
        if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
        {
            Interlocked.Increment(ref _hitCount);
            return (T)entry.Value!;
        }

        Interlocked.Increment(ref _missCount);

        var value = await factory();
        var effectiveTtl = ttl ?? TimeSpan.FromSeconds(_configService.Config.Performance.CacheTtlSeconds);

        _cache[key] = new CacheEntry
        {
            Value = value,
            ExpiresAt = DateTime.UtcNow.Add(effectiveTtl)
        };

        return value;
    }

    /// <inheritdoc/>
    public T? Get<T>(string key)
    {
        if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
        {
            Interlocked.Increment(ref _hitCount);
            return (T)entry.Value!;
        }

        Interlocked.Increment(ref _missCount);
        return default;
    }

    /// <inheritdoc/>
    public void Set<T>(string key, T value, TimeSpan? ttl = null)
    {
        var effectiveTtl = ttl ?? TimeSpan.FromSeconds(_configService.Config.Performance.CacheTtlSeconds);

        _cache[key] = new CacheEntry
        {
            Value = value,
            ExpiresAt = DateTime.UtcNow.Add(effectiveTtl)
        };
    }

    /// <inheritdoc/>
    public void Remove(string key)
    {
        _cache.TryRemove(key, out _);
    }

    /// <inheritdoc/>
    public void RemoveByPrefix(string prefix)
    {
        var keysToRemove = _cache.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _cache.Clear();
        Interlocked.Exchange(ref _hitCount, 0);
        Interlocked.Exchange(ref _missCount, 0);
    }

    /// <inheritdoc/>
    public CacheStatistics GetStatistics()
    {
        return new CacheStatistics
        {
            ItemCount = _cache.Count(kv => !kv.Value.IsExpired),
            HitCount = Interlocked.Read(ref _hitCount),
            MissCount = Interlocked.Read(ref _missCount)
        };
    }

    private void CleanupExpired(object? state)
    {
        var expiredKeys = _cache
            .Where(kv => kv.Value.IsExpired)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _cleanupTimer.Dispose();
        _cache.Clear();
        _disposed = true;
    }

    private sealed class CacheEntry
    {
        public object? Value { get; init; }
        public DateTime ExpiresAt { get; init; }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }
}
