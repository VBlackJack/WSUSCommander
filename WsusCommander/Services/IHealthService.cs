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

using WsusCommander.Models;

namespace WsusCommander.Services;

/// <summary>
/// Health check status enumeration.
/// </summary>
public enum HealthStatus
{
    /// <summary>Service is healthy.</summary>
    Healthy,

    /// <summary>Service has warnings but is operational.</summary>
    Degraded,

    /// <summary>Service is unhealthy or unavailable.</summary>
    Unhealthy
}

/// <summary>
/// Individual health check result.
/// </summary>
public sealed class HealthCheckResult
{
    /// <summary>
    /// Gets or sets the check name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the health status.
    /// </summary>
    public HealthStatus Status { get; init; }

    /// <summary>
    /// Gets or sets the status description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the response time in milliseconds.
    /// </summary>
    public long? ResponseTimeMs { get; init; }

    /// <summary>
    /// Gets or sets the exception if unhealthy.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets or sets additional data.
    /// </summary>
    public Dictionary<string, object> Data { get; init; } = [];
}

/// <summary>
/// Aggregate health report.
/// </summary>
public sealed class HealthReport
{
    /// <summary>
    /// Gets or sets the overall status.
    /// </summary>
    public HealthStatus Status { get; init; }

    /// <summary>
    /// Gets or sets the report timestamp.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the total duration in milliseconds.
    /// </summary>
    public long TotalDurationMs { get; init; }

    /// <summary>
    /// Gets or sets individual check results.
    /// </summary>
    public IReadOnlyList<HealthCheckResult> Checks { get; init; } = [];
}

/// <summary>
/// Interface for health monitoring service.
/// </summary>
public interface IHealthService
{
    /// <summary>
    /// Runs all health checks and returns aggregated report.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health report.</returns>
    Task<HealthReport> CheckHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks WSUS server connectivity.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health check result.</returns>
    Task<HealthCheckResult> CheckWsusConnectivityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks PowerShell availability.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health check result.</returns>
    Task<HealthCheckResult> CheckPowerShellAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks disk space availability.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health check result.</returns>
    Task<HealthCheckResult> CheckDiskSpaceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks memory usage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health check result.</returns>
    Task<HealthCheckResult> CheckMemoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last health report without running new checks.
    /// </summary>
    HealthReport? LastReport { get; }

    /// <summary>
    /// Event raised when health status changes.
    /// </summary>
    event EventHandler<HealthReport>? HealthStatusChanged;
}
