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

using System.Text.Json;
using WsusCommander.Models;

namespace WsusCommander.Services;

/// <summary>
/// Service for interacting with Windows Task Scheduler via PowerShell.
/// </summary>
public sealed class TaskSchedulerService : ITaskSchedulerService
{
    private readonly IPowerShellService _powerShellService;
    private readonly ILoggingService _loggingService;
    private readonly IConfigurationService _configService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskSchedulerService"/> class.
    /// </summary>
    public TaskSchedulerService(
        IPowerShellService powerShellService,
        ILoggingService loggingService,
        IConfigurationService configService)
    {
        _powerShellService = powerShellService;
        _loggingService = loggingService;
        _configService = configService;
    }

    /// <inheritdoc/>
    public async Task CreateTaskAsync(ScheduledWsusTask task)
    {
        if (string.IsNullOrEmpty(task.WindowsTaskName))
        {
            throw new ArgumentException("Task name is required.", nameof(task));
        }

        var parameters = BuildTaskParameters(task);
        parameters["Operation"] = "Create";

        try
        {
            await _powerShellService.ExecuteScriptAsync(
                "New-WsusScheduledTask.ps1", parameters);

            await _loggingService.LogInfoAsync(
                $"Windows scheduled task '{task.WindowsTaskName}' created.");
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync(
                $"Failed to create Windows scheduled task: {ex.Message}", ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task UpdateTaskAsync(ScheduledWsusTask task)
    {
        if (string.IsNullOrEmpty(task.WindowsTaskName))
        {
            return;
        }

        var parameters = BuildTaskParameters(task);
        parameters["Operation"] = "Update";

        try
        {
            await _powerShellService.ExecuteScriptAsync(
                "Update-WsusScheduledTask.ps1", parameters);

            await _loggingService.LogInfoAsync(
                $"Windows scheduled task '{task.WindowsTaskName}' updated.");
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync(
                $"Failed to update Windows scheduled task: {ex.Message}", ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteTaskAsync(string taskName)
    {
        if (string.IsNullOrEmpty(taskName))
        {
            return;
        }

        var parameters = new Dictionary<string, object>
        {
            ["TaskName"] = taskName
        };

        try
        {
            await _powerShellService.ExecuteScriptAsync(
                "Remove-WsusScheduledTask.ps1", parameters);

            await _loggingService.LogInfoAsync(
                $"Windows scheduled task '{taskName}' deleted.");
        }
        catch (Exception ex)
        {
            await _loggingService.LogWarningAsync(
                $"Failed to delete Windows scheduled task '{taskName}': {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task EnableTaskAsync(string taskName)
    {
        if (string.IsNullOrEmpty(taskName))
        {
            return;
        }

        var parameters = new Dictionary<string, object>
        {
            ["TaskName"] = taskName,
            ["Operation"] = "Enable"
        };

        try
        {
            await _powerShellService.ExecuteScriptAsync(
                "Update-WsusScheduledTask.ps1", parameters);

            await _loggingService.LogInfoAsync(
                $"Windows scheduled task '{taskName}' enabled.");
        }
        catch (Exception ex)
        {
            await _loggingService.LogWarningAsync(
                $"Failed to enable Windows scheduled task '{taskName}': {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task DisableTaskAsync(string taskName)
    {
        if (string.IsNullOrEmpty(taskName))
        {
            return;
        }

        var parameters = new Dictionary<string, object>
        {
            ["TaskName"] = taskName,
            ["Operation"] = "Disable"
        };

        try
        {
            await _powerShellService.ExecuteScriptAsync(
                "Update-WsusScheduledTask.ps1", parameters);

            await _loggingService.LogInfoAsync(
                $"Windows scheduled task '{taskName}' disabled.");
        }
        catch (Exception ex)
        {
            await _loggingService.LogWarningAsync(
                $"Failed to disable Windows scheduled task '{taskName}': {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<WindowsScheduledTaskInfo?> GetTaskInfoAsync(string taskName)
    {
        if (string.IsNullOrEmpty(taskName))
        {
            return null;
        }

        var parameters = new Dictionary<string, object>
        {
            ["TaskName"] = taskName,
            ["Operation"] = "GetInfo"
        };

        try
        {
            var result = await _powerShellService.ExecuteScriptAsync(
                "Update-WsusScheduledTask.ps1", parameters);

            if (result.Count == 0)
            {
                return null;
            }

            var psObject = result[0];
            return new WindowsScheduledTaskInfo
            {
                Name = psObject.Properties["Name"]?.Value?.ToString() ?? string.Empty,
                IsEnabled = psObject.Properties["Enabled"]?.Value is bool enabled && enabled,
                State = psObject.Properties["State"]?.Value?.ToString() ?? string.Empty,
                LastRunTime = ParseDateTime(psObject.Properties["LastRunTime"]?.Value),
                LastTaskResult = psObject.Properties["LastTaskResult"]?.Value is int res ? res : 0,
                NextRunTime = ParseDateTime(psObject.Properties["NextRunTime"]?.Value)
            };
        }
        catch (Exception ex)
        {
            await _loggingService.LogWarningAsync(
                $"Failed to get Windows scheduled task info: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task RunTaskNowAsync(string taskName)
    {
        if (string.IsNullOrEmpty(taskName))
        {
            return;
        }

        var parameters = new Dictionary<string, object>
        {
            ["TaskName"] = taskName,
            ["Operation"] = "Run"
        };

        try
        {
            await _powerShellService.ExecuteScriptAsync(
                "Update-WsusScheduledTask.ps1", parameters);

            await _loggingService.LogInfoAsync(
                $"Windows scheduled task '{taskName}' started.");
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync(
                $"Failed to run Windows scheduled task: {ex.Message}", ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> TaskExistsAsync(string taskName)
    {
        var info = await GetTaskInfoAsync(taskName);
        return info is not null;
    }

    private Dictionary<string, object> BuildTaskParameters(ScheduledWsusTask task)
    {
        var schedule = task.Schedule;
        var dataPath = _configService.AppSettings.DataPath;

        var parameters = new Dictionary<string, object>
        {
            ["TaskName"] = task.WindowsTaskName!,
            ["TaskId"] = task.Id.ToString(),
            ["Description"] = task.Description,
            ["OperationType"] = task.OperationType.ToString(),
            ["Frequency"] = schedule.Frequency.ToString(),
            ["TimeOfDay"] = schedule.TimeOfDay.ToString(@"hh\:mm"),
            ["StartDate"] = schedule.StartDate.ToString("yyyy-MM-dd"),
            ["DataPath"] = dataPath,
            ["Enabled"] = task.IsEnabled
        };

        if (schedule.Frequency == ScheduleFrequency.Weekly && schedule.DaysOfWeek.Count > 0)
        {
            parameters["DaysOfWeek"] = string.Join(",", schedule.DaysOfWeek.Select(d => (int)d));
        }

        if (schedule.Frequency == ScheduleFrequency.Monthly)
        {
            parameters["DayOfMonth"] = schedule.DayOfMonth;
        }

        if (schedule.EndDate.HasValue)
        {
            parameters["EndDate"] = schedule.EndDate.Value.ToString("yyyy-MM-dd");
        }

        // Add operation-specific configuration as JSON
        var configJson = task.OperationType switch
        {
            ScheduledTaskOperationType.StagedApproval when task.StagedApprovalSettings is not null
                => JsonSerializer.Serialize(task.StagedApprovalSettings),
            ScheduledTaskOperationType.Cleanup when task.CleanupSettings is not null
                => JsonSerializer.Serialize(task.CleanupSettings),
            ScheduledTaskOperationType.Synchronization when task.SyncSettings is not null
                => JsonSerializer.Serialize(task.SyncSettings),
            _ => "{}"
        };
        parameters["ConfigJson"] = configJson;

        // Add WSUS connection info
        parameters["WsusServer"] = _configService.Config.WsusConnection.ServerName;
        parameters["WsusPort"] = _configService.Config.WsusConnection.Port;
        parameters["WsusUseSsl"] = _configService.Config.WsusConnection.UseSsl;

        return parameters;
    }

    private static DateTime? ParseDateTime(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is DateTime dt)
        {
            return dt;
        }

        if (value is string str && DateTime.TryParse(str, out var parsed))
        {
            return parsed;
        }

        return null;
    }
}
