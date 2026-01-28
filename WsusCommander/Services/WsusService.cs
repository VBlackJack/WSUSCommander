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
/// WSUS service implementation using PowerShell scripts.
/// </summary>
public sealed class WsusService : IWsusService
{
    private readonly IPowerShellService _psService;
    private readonly IConfigurationService _configService;
    private readonly ILoggingService _loggingService;

    private string _serverName = string.Empty;
    private int _port;
    private bool _useSsl;
    private bool _isConnected;

    /// <summary>
    /// Initializes a new instance of the <see cref="WsusService"/> class.
    /// </summary>
    public WsusService(
        IPowerShellService psService,
        IConfigurationService configService,
        ILoggingService loggingService)
    {
        _psService = psService;
        _configService = configService;
        _loggingService = loggingService;
    }

    private Dictionary<string, object> GetConnectionParams() => new()
    {
        ["ServerName"] = _serverName,
        ["Port"] = _port,
        ["UseSsl"] = _useSsl
    };

    /// <inheritdoc/>
    public async Task<WsusConnectionResult> ConnectAsync(
        string serverName,
        int port,
        bool useSsl,
        CancellationToken cancellationToken)
    {
        _serverName = serverName;
        _port = port;
        _useSsl = useSsl;

        try
        {
            var result = await _psService.ExecuteScriptAsync(
                "Connect-WsusServer.ps1",
                GetConnectionParams(),
                cancellationToken);

            if (result.Count > 0)
            {
                var obj = result[0];
                var version = obj.Properties["Version"]?.Value?.ToString() ?? "Unknown";
                _isConnected = true;

                return new WsusConnectionResult
                {
                    Success = true,
                    ServerVersion = version
                };
            }

            return new WsusConnectionResult { Success = false, ErrorMessage = "No response from server" };
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync($"Connection failed: {ex.Message}", ex);
            return new WsusConnectionResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <inheritdoc/>
    public void Disconnect()
    {
        _isConnected = false;
        _serverName = string.Empty;
    }

    /// <inheritdoc/>
    public Task<DashboardStats> GetDashboardStatsAsync(CancellationToken cancellationToken)
    {
        return GetDashboardStatsAsync(null, null, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DashboardStats> GetDashboardStatsAsync(string? groupId, string? namePattern, CancellationToken cancellationToken)
    {
        if (!_isConnected) return new DashboardStats();

        try
        {
            var parameters = GetConnectionParams();
            if (!string.IsNullOrEmpty(groupId))
            {
                parameters["GroupId"] = groupId;
            }
            if (!string.IsNullOrEmpty(namePattern))
            {
                parameters["NamePattern"] = namePattern;
            }

            var result = await _psService.ExecuteScriptAsync(
                "Get-DashboardStats.ps1",
                parameters,
                cancellationToken);

            if (result.Count > 0)
            {
                var obj = result[0];
                return new DashboardStats
                {
                    TotalUpdates = GetInt(obj, "TotalUpdates"),
                    UnapprovedUpdates = GetInt(obj, "UnapprovedUpdates"),
                    TotalComputers = GetInt(obj, "TotalComputers"),
                    ComputersUpToDate = GetInt(obj, "ComputersUpToDate"),
                    ComputersNeedingUpdates = GetInt(obj, "ComputersNeedingUpdates"),
                    CriticalPending = GetInt(obj, "CriticalPending"),
                    SecurityPending = GetInt(obj, "SecurityPending"),
                    SupersededUpdates = GetInt(obj, "SupersededUpdates"),
                    CompliancePercent = GetDouble(obj, "CompliancePercent"),
                    LastSyncTime = GetDateTime(obj, "LastSyncTime")
                };
            }
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync($"Failed to get dashboard stats: {ex.Message}", ex);
        }

        return new DashboardStats();
    }

    /// <inheritdoc/>
    public Task<HealthReport> GetHealthReportAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new HealthReport { Status = HealthStatus.Healthy });
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WsusUpdate>> GetUpdatesAsync(CancellationToken cancellationToken)
    {
        if (!_isConnected) return Array.Empty<WsusUpdate>();

        var updates = new List<WsusUpdate>();

        try
        {
            var result = await _psService.ExecuteScriptAsync(
                "Get-WsusUpdates.ps1",
                GetConnectionParams(),
                cancellationToken);

            foreach (var obj in result)
            {
                var idStr = obj.Properties["Id"]?.Value?.ToString();
                if (!Guid.TryParse(idStr, out var id)) continue;

                updates.Add(new WsusUpdate
                {
                    Id = id,
                    Title = obj.Properties["Title"]?.Value?.ToString() ?? string.Empty,
                    KbArticle = obj.Properties["KbArticle"]?.Value?.ToString() ?? string.Empty,
                    Classification = obj.Properties["Classification"]?.Value?.ToString() ?? string.Empty,
                    CreationDate = GetDateTime(obj, "CreationDate") ?? DateTime.MinValue,
                    IsApproved = GetBool(obj, "IsApproved"),
                    IsDeclined = GetBool(obj, "IsDeclined"),
                    IsSuperseded = GetBool(obj, "IsSuperseded"),
                    SupersededBy = obj.Properties["SupersededBy"]?.Value?.ToString() ?? string.Empty
                });
            }
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync($"Failed to get updates: {ex.Message}", ex);
        }

        return updates;
    }

    /// <inheritdoc/>
    public async Task ApproveUpdateAsync(Guid updateId, Guid groupId, CancellationToken cancellationToken)
    {
        if (!_isConnected) return;

        var parameters = GetConnectionParams();
        parameters["UpdateId"] = updateId.ToString();
        parameters["GroupId"] = groupId.ToString();

        await _psService.ExecuteScriptAsync("Approve-WsusUpdate.ps1", parameters, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeclineUpdateAsync(Guid updateId, CancellationToken cancellationToken)
    {
        if (!_isConnected) return;

        var parameters = GetConnectionParams();
        parameters["UpdateId"] = updateId.ToString();

        await _psService.ExecuteScriptAsync("Decline-WsusUpdate.ps1", parameters, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ComputerStatus>> GetComputersAsync(bool excludeUnassigned, CancellationToken cancellationToken)
    {
        if (!_isConnected) return Array.Empty<ComputerStatus>();

        var computers = new List<ComputerStatus>();

        try
        {
            var parameters = GetConnectionParams();
            parameters["ExcludeUnassigned"] = excludeUnassigned;

            var result = await _psService.ExecuteScriptAsync(
                "Get-ComputerStatus.ps1",
                parameters,
                cancellationToken);

            foreach (var obj in result)
            {
                var idStr = obj.Properties["ComputerId"]?.Value?.ToString();
                if (string.IsNullOrEmpty(idStr)) continue;

                computers.Add(new ComputerStatus
                {
                    ComputerId = idStr,
                    Name = obj.Properties["Name"]?.Value?.ToString() ?? string.Empty,
                    IpAddress = obj.Properties["IpAddress"]?.Value?.ToString() ?? string.Empty,
                    GroupName = obj.Properties["GroupName"]?.Value?.ToString() ?? string.Empty,
                    LastReportedTime = GetDateTime(obj, "LastReportedTime"),
                    NeededCount = GetInt(obj, "NeededCount"),
                    InstalledCount = GetInt(obj, "InstalledCount"),
                    FailedCount = GetInt(obj, "FailedCount")
                });
            }
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync($"Failed to get computers: {ex.Message}", ex);
        }

        return computers;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ComputerStatus>> GetStagingComputersAsync(CancellationToken cancellationToken)
    {
        if (!_isConnected) return Array.Empty<ComputerStatus>();

        // Get all computers and filter to only those in the unassigned computers group
        var unassignedGroupName = _configService.AppSettings.UnassignedComputersGroupName;
        var allComputers = await GetComputersAsync(false, cancellationToken);
        return allComputers.Where(c =>
            string.Equals(c.GroupName, unassignedGroupName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ComputerGroup>> GetGroupsAsync(CancellationToken cancellationToken)
    {
        if (!_isConnected) return Array.Empty<ComputerGroup>();

        var groups = new List<ComputerGroup>();

        try
        {
            var result = await _psService.ExecuteScriptAsync(
                "Get-ComputerGroups.ps1",
                GetConnectionParams(),
                cancellationToken);

            foreach (var obj in result)
            {
                var idStr = obj.Properties["Id"]?.Value?.ToString();
                if (!Guid.TryParse(idStr, out var id)) continue;

                groups.Add(new ComputerGroup
                {
                    Id = id,
                    Name = obj.Properties["Name"]?.Value?.ToString() ?? string.Empty,
                    Description = obj.Properties["Description"]?.Value?.ToString() ?? string.Empty,
                    ComputerCount = GetInt(obj, "ComputerCount")
                });
            }
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync($"Failed to get groups: {ex.Message}", ex);
        }

        return groups;
    }

    /// <inheritdoc/>
    public async Task<SyncStatus> StartSyncAsync(CancellationToken cancellationToken)
    {
        if (!_isConnected)
        {
            return new SyncStatus { Status = "NotConnected" };
        }

        try
        {
            await _psService.ExecuteScriptAsync(
                "Start-WsusSync.ps1",
                GetConnectionParams(),
                cancellationToken);

            return new SyncStatus
            {
                Status = "Started",
                LastSyncTime = DateTime.Now,
                IsSyncing = true
            };
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync($"Failed to start sync: {ex.Message}", ex);
            return new SyncStatus { Status = "Error" };
        }
    }

    private static int GetInt(System.Management.Automation.PSObject obj, string propertyName)
    {
        var value = obj.Properties[propertyName]?.Value;
        if (value is int i) return i;
        if (value is long l) return (int)l;
        if (int.TryParse(value?.ToString(), out var result)) return result;
        return 0;
    }

    private static double GetDouble(System.Management.Automation.PSObject obj, string propertyName)
    {
        var value = obj.Properties[propertyName]?.Value;
        if (value is double d) return d;
        if (value is float f) return f;
        if (value is decimal dec) return (double)dec;
        if (double.TryParse(value?.ToString(), out var result)) return result;
        return 0;
    }

    private static bool GetBool(System.Management.Automation.PSObject obj, string propertyName)
    {
        var value = obj.Properties[propertyName]?.Value;
        if (value is bool b) return b;
        if (bool.TryParse(value?.ToString(), out var result)) return result;
        return false;
    }

    private static DateTime? GetDateTime(System.Management.Automation.PSObject obj, string propertyName)
    {
        var value = obj.Properties[propertyName]?.Value;
        if (value is DateTime dt && dt != DateTime.MinValue) return dt;

        var str = value?.ToString();
        if (string.IsNullOrEmpty(str)) return null;

        // Handle WCF JSON date format: /Date(milliseconds)/
        if (str.StartsWith("/Date(") && str.EndsWith(")/"))
        {
            var msStr = str[6..^2]; // Remove "/Date(" and ")/"
            if (long.TryParse(msStr, out var ms) && ms > 0)
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(ms).LocalDateTime;
            }
        }

        if (DateTime.TryParse(str, out var result) && result != DateTime.MinValue) return result;
        return null;
    }
}
