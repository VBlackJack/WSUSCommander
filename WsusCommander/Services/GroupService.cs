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

using System.Management.Automation;
using WsusCommander.Models;

namespace WsusCommander.Services;

/// <summary>
/// Computer group management service implementation.
/// </summary>
public sealed class GroupService : IGroupService
{
    private readonly IPowerShellService _powerShellService;
    private readonly ILoggingService _loggingService;
    private readonly ICacheService _cacheService;
    private readonly IValidationService _validationService;
    private readonly IConfigurationService _configService;
    private readonly IRetryService _retryService;

    private static readonly string[] SystemGroupNames = ["All Computers", "Unassigned Computers"];

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupService"/> class.
    /// </summary>
    public GroupService(
        IPowerShellService powerShellService,
        ILoggingService loggingService,
        ICacheService cacheService,
        IValidationService validationService,
        IConfigurationService configService,
        IRetryService retryService)
    {
        _powerShellService = powerShellService;
        _loggingService = loggingService;
        _cacheService = cacheService;
        _validationService = validationService;
        _configService = configService;
        _retryService = retryService;
    }

    private Dictionary<string, object> GetConnectionParameters()
    {
        return new Dictionary<string, object>
        {
            ["ServerName"] = _configService.WsusConnection.ServerName,
            ["Port"] = _configService.WsusConnection.Port,
            ["UseSsl"] = _configService.WsusConnection.UseSsl
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ComputerGroup>> GetAllGroupsAsync(
        bool includeSystemGroups = false,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"groups_{includeSystemGroups}";

        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                var result = await _retryService.ExecuteWithRetryAsync(
                    async ct => await _powerShellService.ExecuteScriptAsync(
                        "Get-ComputerGroups.ps1",
                        GetConnectionParameters(),
                        ct),
                    "Get-ComputerGroups",
                    cancellationToken);

                var groups = ParseGroups(result);

                if (!includeSystemGroups)
                {
                    groups = groups.Where(g => !SystemGroupNames.Contains(g.Name)).ToList();
                }

                return groups;
            },
            TimeSpan.FromMinutes(2));
    }

    /// <inheritdoc/>
    public async Task<ComputerGroup?> GetGroupAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = GetConnectionParameters();
            parameters["GroupId"] = groupId.ToString();

            var result = await _retryService.ExecuteWithRetryAsync(
                async ct => await _powerShellService.ExecuteScriptAsync(
                    "Get-ComputerGroup.ps1",
                    parameters,
                    ct),
                "Get-ComputerGroup",
                cancellationToken);

            var groups = ParseGroups(result);
            return groups.FirstOrDefault();
        }
        catch (Exception ex)
        {
            await _loggingService.LogWarningAsync($"Failed to get group {groupId}: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<ComputerGroup> CreateGroupAsync(
        CreateGroupOptions options,
        CancellationToken cancellationToken = default)
    {
        var sanitizedName = _validationService.Sanitize(options.Name);
        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            throw new WsusException(WsusErrorCode.InvalidInput, "Group name is required");
        }

        await _loggingService.LogInfoAsync($"Creating computer group: {sanitizedName}");

        var parameters = GetConnectionParameters();
        parameters["GroupName"] = sanitizedName;

        if (!string.IsNullOrWhiteSpace(options.Description))
        {
            parameters["Description"] = _validationService.Sanitize(options.Description);
        }

        if (options.ParentGroupId.HasValue)
        {
            parameters["ParentGroupId"] = options.ParentGroupId.Value.ToString();
        }

        var result = await _retryService.ExecuteWithRetryAsync(
            async ct => await _powerShellService.ExecuteScriptAsync(
                "New-ComputerGroup.ps1",
                parameters,
                ct),
            "New-ComputerGroup",
            cancellationToken);

        _cacheService.Remove("groups_true");
        _cacheService.Remove("groups_false");

        var groups = ParseGroups(result);
        var createdGroup = groups.FirstOrDefault();

        if (createdGroup == null)
        {
            throw new WsusException(WsusErrorCode.OperationFailed, "Group created but could not be retrieved");
        }

        await _loggingService.LogInfoAsync($"Created computer group: {createdGroup.Name} ({createdGroup.Id})");
        return createdGroup;
    }

    /// <inheritdoc/>
    public async Task<ComputerGroup> UpdateGroupAsync(
        Guid groupId,
        UpdateGroupOptions options,
        CancellationToken cancellationToken = default)
    {
        await _loggingService.LogInfoAsync($"Updating computer group: {groupId}");

        var parameters = GetConnectionParameters();
        parameters["GroupId"] = groupId.ToString();

        if (!string.IsNullOrWhiteSpace(options.Name))
        {
            parameters["NewName"] = _validationService.Sanitize(options.Name);
        }

        if (options.Description != null)
        {
            parameters["Description"] = _validationService.Sanitize(options.Description);
        }

        var result = await _retryService.ExecuteWithRetryAsync(
            async ct => await _powerShellService.ExecuteScriptAsync(
                "Set-ComputerGroup.ps1",
                parameters,
                ct),
            "Set-ComputerGroup",
            cancellationToken);

        _cacheService.Remove("groups_true");
        _cacheService.Remove("groups_false");

        var groups = ParseGroups(result);
        var updatedGroup = groups.FirstOrDefault();

        if (updatedGroup == null)
        {
            throw new WsusException(WsusErrorCode.OperationFailed, "Group updated but could not be retrieved");
        }

        await _loggingService.LogInfoAsync($"Updated computer group: {updatedGroup.Name}");
        return updatedGroup;
    }

    /// <inheritdoc/>
    public async Task DeleteGroupAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        await _loggingService.LogInfoAsync($"Deleting computer group: {groupId}");

        var parameters = GetConnectionParameters();
        parameters["GroupId"] = groupId.ToString();

        await _retryService.ExecuteWithRetryAsync(
            async ct => await _powerShellService.ExecuteScriptAsync(
                "Remove-ComputerGroup.ps1",
                parameters,
                ct),
            "Remove-ComputerGroup",
            cancellationToken);

        _cacheService.Remove("groups_true");
        _cacheService.Remove("groups_false");

        await _loggingService.LogInfoAsync($"Deleted computer group: {groupId}");
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ComputerStatus>> GetGroupComputersAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = GetConnectionParameters();
            parameters["GroupId"] = groupId.ToString();

            var result = await _retryService.ExecuteWithRetryAsync(
                async ct => await _powerShellService.ExecuteScriptAsync(
                    "Get-ComputerStatus.ps1",
                    parameters,
                    ct),
                "Get-ComputerStatus",
                cancellationToken);

            return ParseComputers(result);
        }
        catch (Exception ex)
        {
            await _loggingService.LogWarningAsync($"Failed to get group computers: {ex.Message}");
            return new List<ComputerStatus>();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ComputerGroup>> GetChildGroupsAsync(
        Guid parentGroupId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = GetConnectionParameters();
            parameters["ParentGroupId"] = parentGroupId.ToString();

            var result = await _retryService.ExecuteWithRetryAsync(
                async ct => await _powerShellService.ExecuteScriptAsync(
                    "Get-ChildGroups.ps1",
                    parameters,
                    ct),
                "Get-ChildGroups",
                cancellationToken);

            return ParseGroups(result);
        }
        catch (Exception ex)
        {
            await _loggingService.LogWarningAsync($"Failed to get child groups: {ex.Message}");
            return new List<ComputerGroup>();
        }
    }

    private static List<ComputerGroup> ParseGroups(PSDataCollection<PSObject>? data)
    {
        var groups = new List<ComputerGroup>();

        if (data == null)
        {
            return groups;
        }

        foreach (var item in data)
        {
            if (item.Properties["Id"] != null)
            {
                groups.Add(new ComputerGroup
                {
                    Id = Guid.TryParse(item.Properties["Id"]?.Value?.ToString(), out var id) ? id : Guid.Empty,
                    Name = item.Properties["Name"]?.Value?.ToString() ?? string.Empty,
                    Description = item.Properties["Description"]?.Value?.ToString() ?? string.Empty,
                    ComputerCount = Convert.ToInt32(item.Properties["ComputerCount"]?.Value ?? 0),
                    ParentGroupId = Guid.TryParse(item.Properties["ParentGroupId"]?.Value?.ToString(), out var pid) ? pid : null
                });
            }
        }

        return groups;
    }

    private static List<ComputerStatus> ParseComputers(PSDataCollection<PSObject>? data)
    {
        var computers = new List<ComputerStatus>();

        if (data == null)
        {
            return computers;
        }

        foreach (var item in data)
        {
            if (item.Properties["ComputerId"] != null || item.Properties["Name"] != null)
            {
                DateTime? lastReported = null;
                var lrtValue = item.Properties["LastReportedTime"]?.Value;
                if (lrtValue is DateTime dt)
                {
                    lastReported = dt;
                }

                computers.Add(new ComputerStatus
                {
                    ComputerId = item.Properties["ComputerId"]?.Value?.ToString() ?? string.Empty,
                    Name = item.Properties["Name"]?.Value?.ToString() ?? string.Empty,
                    IpAddress = item.Properties["IpAddress"]?.Value?.ToString() ?? string.Empty,
                    InstalledCount = Convert.ToInt32(item.Properties["InstalledCount"]?.Value ?? 0),
                    NeededCount = Convert.ToInt32(item.Properties["NeededCount"]?.Value ?? 0),
                    FailedCount = Convert.ToInt32(item.Properties["FailedCount"]?.Value ?? 0),
                    LastReportedTime = lastReported
                });
            }
        }

        return computers;
    }
}
