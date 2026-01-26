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

using System.Diagnostics;
using System.IO;

namespace WsusCommander.Services;

/// <summary>
/// Health monitoring service implementation.
/// </summary>
public sealed class HealthService : IHealthService, IDisposable
{
    private readonly IConfigurationService _configService;
    private readonly IPowerShellService _powerShellService;
    private readonly ILoggingService _loggingService;
    private readonly System.Timers.Timer? _monitorTimer;
    private HealthReport? _lastReport;
    private HealthStatus _previousStatus = HealthStatus.Healthy;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthService"/> class.
    /// </summary>
    public HealthService(
        IConfigurationService configService,
        IPowerShellService powerShellService,
        ILoggingService loggingService,
        bool enableAutoMonitoring = true)
    {
        _configService = configService;
        _powerShellService = powerShellService;
        _loggingService = loggingService;

        if (enableAutoMonitoring)
        {
            _monitorTimer = new System.Timers.Timer(60000); // Check every minute
            _monitorTimer.Elapsed += async (_, _) => await CheckHealthAsync();
            _monitorTimer.AutoReset = true;
        }
    }

    /// <inheritdoc/>
    public HealthReport? LastReport => _lastReport;

    /// <inheritdoc/>
    public event EventHandler<HealthReport>? HealthStatusChanged;

    /// <summary>
    /// Starts automatic health monitoring.
    /// </summary>
    public void StartMonitoring()
    {
        _monitorTimer?.Start();
    }

    /// <summary>
    /// Stops automatic health monitoring.
    /// </summary>
    public void StopMonitoring()
    {
        _monitorTimer?.Stop();
    }

    /// <inheritdoc/>
    public async Task<HealthReport> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var checks = new List<HealthCheckResult>();

        try
        {
            // Run all checks in parallel
            var tasks = new[]
            {
                CheckWsusConnectivityAsync(cancellationToken),
                CheckPowerShellAsync(cancellationToken),
                CheckDiskSpaceAsync(cancellationToken),
                CheckMemoryAsync(cancellationToken)
            };

            var results = await Task.WhenAll(tasks);
            checks.AddRange(results);
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync("Health check failed", ex);
            checks.Add(new HealthCheckResult
            {
                Name = "HealthCheck",
                Status = HealthStatus.Unhealthy,
                Description = $"Health check failed: {ex.Message}",
                Exception = ex
            });
        }

        sw.Stop();

        // Determine overall status
        var overallStatus = HealthStatus.Healthy;
        if (checks.Any(c => c.Status == HealthStatus.Unhealthy))
        {
            overallStatus = HealthStatus.Unhealthy;
        }
        else if (checks.Any(c => c.Status == HealthStatus.Degraded))
        {
            overallStatus = HealthStatus.Degraded;
        }

        var report = new HealthReport
        {
            Status = overallStatus,
            TotalDurationMs = sw.ElapsedMilliseconds,
            Checks = checks
        };

        _lastReport = report;

        // Notify if status changed
        if (overallStatus != _previousStatus)
        {
            _previousStatus = overallStatus;
            HealthStatusChanged?.Invoke(this, report);

            await _loggingService.LogInfoAsync($"Health status changed to: {overallStatus}");
        }

        return report;
    }

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckWsusConnectivityAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var serverName = _configService.WsusConnection.ServerName;
            var port = _configService.WsusConnection.Port;

            if (string.IsNullOrEmpty(serverName))
            {
                return new HealthCheckResult
                {
                    Name = "WSUS Connectivity",
                    Status = HealthStatus.Degraded,
                    Description = "WSUS server not configured",
                    ResponseTimeMs = sw.ElapsedMilliseconds
                };
            }

            // Test TCP connectivity
            using var client = new System.Net.Sockets.TcpClient();
            var connectTask = client.ConnectAsync(serverName, port);
            var completed = await Task.WhenAny(connectTask, Task.Delay(5000, cancellationToken));

            sw.Stop();

            if (completed != connectTask || !client.Connected)
            {
                return new HealthCheckResult
                {
                    Name = "WSUS Connectivity",
                    Status = HealthStatus.Unhealthy,
                    Description = $"Cannot connect to WSUS server {serverName}:{port}",
                    ResponseTimeMs = sw.ElapsedMilliseconds,
                    Data = new Dictionary<string, object>
                    {
                        ["Server"] = serverName,
                        ["Port"] = port
                    }
                };
            }

            return new HealthCheckResult
            {
                Name = "WSUS Connectivity",
                Status = sw.ElapsedMilliseconds > 2000 ? HealthStatus.Degraded : HealthStatus.Healthy,
                Description = sw.ElapsedMilliseconds > 2000
                    ? $"WSUS server responding slowly ({sw.ElapsedMilliseconds}ms)"
                    : "WSUS server is reachable",
                ResponseTimeMs = sw.ElapsedMilliseconds,
                Data = new Dictionary<string, object>
                {
                    ["Server"] = serverName,
                    ["Port"] = port
                }
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new HealthCheckResult
            {
                Name = "WSUS Connectivity",
                Status = HealthStatus.Unhealthy,
                Description = $"WSUS connectivity check failed: {ex.Message}",
                ResponseTimeMs = sw.ElapsedMilliseconds,
                Exception = ex
            };
        }
    }

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckPowerShellAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            // Simple PowerShell test
            var result = await _powerShellService.ExecuteScriptAsync(
                "$PSVersionTable.PSVersion.ToString()",
                new Dictionary<string, object>());

            sw.Stop();

            if (result != null && result.Count > 0)
            {
                var version = result.FirstOrDefault()?.ToString() ?? "Unknown";
                return new HealthCheckResult
                {
                    Name = "PowerShell",
                    Status = HealthStatus.Healthy,
                    Description = $"PowerShell {version} available",
                    ResponseTimeMs = sw.ElapsedMilliseconds,
                    Data = new Dictionary<string, object>
                    {
                        ["Version"] = version
                    }
                };
            }

            return new HealthCheckResult
            {
                Name = "PowerShell",
                Status = HealthStatus.Unhealthy,
                Description = "PowerShell execution returned no results",
                ResponseTimeMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new HealthCheckResult
            {
                Name = "PowerShell",
                Status = HealthStatus.Unhealthy,
                Description = $"PowerShell check failed: {ex.Message}",
                ResponseTimeMs = sw.ElapsedMilliseconds,
                Exception = ex
            };
        }
    }

    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckDiskSpaceAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var dataPath = _configService.AppSettings.DataPath;
            var driveLetter = Path.GetPathRoot(dataPath) ?? "C:\\";
            var driveInfo = new DriveInfo(driveLetter.TrimEnd('\\'));

            sw.Stop();

            var freeSpaceGb = driveInfo.AvailableFreeSpace / (1024.0 * 1024 * 1024);
            var totalSpaceGb = driveInfo.TotalSize / (1024.0 * 1024 * 1024);
            var usedPercent = 100 - (freeSpaceGb / totalSpaceGb * 100);

            HealthStatus status;
            string description;

            if (freeSpaceGb < 1)
            {
                status = HealthStatus.Unhealthy;
                description = $"Critical: Only {freeSpaceGb:F1} GB free on {driveLetter}";
            }
            else if (freeSpaceGb < 5 || usedPercent > 90)
            {
                status = HealthStatus.Degraded;
                description = $"Low disk space: {freeSpaceGb:F1} GB free on {driveLetter}";
            }
            else
            {
                status = HealthStatus.Healthy;
                description = $"{freeSpaceGb:F1} GB free on {driveLetter}";
            }

            return Task.FromResult(new HealthCheckResult
            {
                Name = "Disk Space",
                Status = status,
                Description = description,
                ResponseTimeMs = sw.ElapsedMilliseconds,
                Data = new Dictionary<string, object>
                {
                    ["Drive"] = driveLetter,
                    ["FreeSpaceGB"] = Math.Round(freeSpaceGb, 2),
                    ["TotalSpaceGB"] = Math.Round(totalSpaceGb, 2),
                    ["UsedPercent"] = Math.Round(usedPercent, 1)
                }
            });
        }
        catch (Exception ex)
        {
            sw.Stop();
            return Task.FromResult(new HealthCheckResult
            {
                Name = "Disk Space",
                Status = HealthStatus.Unhealthy,
                Description = $"Disk space check failed: {ex.Message}",
                ResponseTimeMs = sw.ElapsedMilliseconds,
                Exception = ex
            });
        }
    }

    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckMemoryAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var process = Process.GetCurrentProcess();
            var workingSetMb = process.WorkingSet64 / (1024.0 * 1024);
            var privateMemoryMb = process.PrivateMemorySize64 / (1024.0 * 1024);

            sw.Stop();

            HealthStatus status;
            string description;

            if (workingSetMb > 1024)
            {
                status = HealthStatus.Degraded;
                description = $"High memory usage: {workingSetMb:F0} MB";
            }
            else
            {
                status = HealthStatus.Healthy;
                description = $"Memory usage: {workingSetMb:F0} MB";
            }

            return Task.FromResult(new HealthCheckResult
            {
                Name = "Memory",
                Status = status,
                Description = description,
                ResponseTimeMs = sw.ElapsedMilliseconds,
                Data = new Dictionary<string, object>
                {
                    ["WorkingSetMB"] = Math.Round(workingSetMb, 2),
                    ["PrivateMemoryMB"] = Math.Round(privateMemoryMb, 2)
                }
            });
        }
        catch (Exception ex)
        {
            sw.Stop();
            return Task.FromResult(new HealthCheckResult
            {
                Name = "Memory",
                Status = HealthStatus.Unhealthy,
                Description = $"Memory check failed: {ex.Message}",
                ResponseTimeMs = sw.ElapsedMilliseconds,
                Exception = ex
            });
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _monitorTimer?.Stop();
        _monitorTimer?.Dispose();
        _disposed = true;
    }
}
