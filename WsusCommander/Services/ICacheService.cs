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

namespace WsusCommander.Services;

/// <summary>
/// Interface for caching service with TTL support.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached value or executes the factory to create and cache it.
    /// </summary>
    /// <typeparam name="T">Type of cached value.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Factory function to create value if not cached.</param>
    /// <param name="ttl">Optional TTL override.</param>
    /// <returns>The cached or newly created value.</returns>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null);

    /// <summary>
    /// Gets a cached value.
    /// </summary>
    /// <typeparam name="T">Type of cached value.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <returns>The cached value or default if not found.</returns>
    T? Get<T>(string key);

    /// <summary>
    /// Sets a cached value.
    /// </summary>
    /// <typeparam name="T">Type of value.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="value">Value to cache.</param>
    /// <param name="ttl">Optional TTL override.</param>
    void Set<T>(string key, T value, TimeSpan? ttl = null);

    /// <summary>
    /// Removes a cached value.
    /// </summary>
    /// <param name="key">Cache key.</param>
    void Remove(string key);

    /// <summary>
    /// Removes all cached values matching a prefix.
    /// </summary>
    /// <param name="prefix">Key prefix.</param>
    void RemoveByPrefix(string prefix);

    /// <summary>
    /// Invalidates all cached data.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <returns>Cache statistics.</returns>
    CacheStatistics GetStatistics();
}

/// <summary>
/// Cache statistics.
/// </summary>
public sealed class CacheStatistics
{
    /// <summary>Gets the total number of cached items.</summary>
    public int ItemCount { get; init; }

    /// <summary>Gets the number of cache hits.</summary>
    public long HitCount { get; init; }

    /// <summary>Gets the number of cache misses.</summary>
    public long MissCount { get; init; }

    /// <summary>Gets the cache hit ratio.</summary>
    public double HitRatio => HitCount + MissCount > 0
        ? (double)HitCount / (HitCount + MissCount)
        : 0;
}
