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

using System.IO;
using System.Text.Json;
using WsusCommander.Models;

namespace WsusCommander.Services;

/// <summary>
/// Service for managing scheduled WSUS tasks with JSON persistence.
/// </summary>
public sealed class ScheduledTasksService : IScheduledTasksService
{
    private readonly string _tasksPath;
    private readonly string _trackingPath;
    private readonly ILoggingService _loggingService;
    private readonly ITaskSchedulerService _taskSchedulerService;
    private readonly List<ScheduledWsusTask> _tasks = [];
    private readonly List<StagedApprovalEntry> _trackingEntries = [];
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledTasksService"/> class.
    /// </summary>
    public ScheduledTasksService(
        IConfigurationService configService,
        ILoggingService loggingService,
        ITaskSchedulerService taskSchedulerService)
    {
        _loggingService = loggingService;
        _taskSchedulerService = taskSchedulerService;

        var dataPath = configService.AppSettings.DataPath;
        _tasksPath = Path.Combine(dataPath, "scheduled-tasks.json");
        _trackingPath = Path.Combine(dataPath, "staged-approvals.json");

        var directory = Path.GetDirectoryName(_tasksPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<ScheduledWsusTask> GetTasks() =>
        _tasks.OrderBy(t => t.Name).ToList().AsReadOnly();

    /// <inheritdoc/>
    public ScheduledWsusTask? GetTask(Guid taskId) =>
        _tasks.FirstOrDefault(t => t.Id == taskId);

    /// <inheritdoc/>
    public async Task SaveTaskAsync(ScheduledWsusTask task)
    {
        var existing = _tasks.FirstOrDefault(t => t.Id == task.Id);
        var isNew = existing is null;

        if (existing is not null)
        {
            _tasks.Remove(existing);
        }

        task.ModifiedAt = DateTime.UtcNow;

        // Generate Windows Task Scheduler name if not set
        if (string.IsNullOrEmpty(task.WindowsTaskName))
        {
            task.WindowsTaskName = $"WsusCommander_{task.Id:N}";
        }

        _tasks.Add(task);

        // Sync with Windows Task Scheduler
        if (task.IsEnabled)
        {
            if (isNew)
            {
                await _taskSchedulerService.CreateTaskAsync(task);
            }
            else
            {
                await _taskSchedulerService.UpdateTaskAsync(task);
            }
        }
        else
        {
            await _taskSchedulerService.DisableTaskAsync(task.WindowsTaskName);
        }

        await SaveToFileAsync();
        await _loggingService.LogInfoAsync(
            $"Scheduled task '{task.Name}' {(isNew ? "created" : "updated")}.");
    }

    /// <inheritdoc/>
    public async Task DeleteTaskAsync(Guid taskId)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId);
        if (task is null)
        {
            return;
        }

        // Remove from Windows Task Scheduler
        if (!string.IsNullOrEmpty(task.WindowsTaskName))
        {
            await _taskSchedulerService.DeleteTaskAsync(task.WindowsTaskName);
        }

        _tasks.Remove(task);

        // Remove tracking entries for this task
        _trackingEntries.RemoveAll(e => e.TaskId == taskId);

        await SaveToFileAsync();
        await SaveTrackingToFileAsync();
        await _loggingService.LogInfoAsync($"Scheduled task '{task.Name}' deleted.");
    }

    /// <inheritdoc/>
    public async Task SetTaskEnabledAsync(Guid taskId, bool enabled)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId);
        if (task is null)
        {
            return;
        }

        task.IsEnabled = enabled;
        task.ModifiedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(task.WindowsTaskName))
        {
            if (enabled)
            {
                await _taskSchedulerService.EnableTaskAsync(task.WindowsTaskName);
            }
            else
            {
                await _taskSchedulerService.DisableTaskAsync(task.WindowsTaskName);
            }
        }

        await SaveToFileAsync();
        await _loggingService.LogInfoAsync(
            $"Scheduled task '{task.Name}' {(enabled ? "enabled" : "disabled")}.");
    }

    /// <inheritdoc/>
    public async Task LoadAsync()
    {
        await LoadTasksAsync();
        await LoadTrackingAsync();
    }

    /// <inheritdoc/>
    public IReadOnlyList<TaskTemplate> GetTemplates() =>
        BuiltInTaskTemplates.All.OrderBy(t => t.SortOrder).ToList().AsReadOnly();

    /// <inheritdoc/>
    public ScheduledWsusTask CreateFromTemplate(string templateId)
    {
        var template = BuiltInTaskTemplates.All.FirstOrDefault(t => t.Id == templateId);

        if (template is null)
        {
            throw new ArgumentException("Task template not found.", nameof(templateId));
        }

        return new ScheduledWsusTask
        {
            OperationType = template.OperationType,
            Schedule = CloneSchedule(template.DefaultSchedule),
            StagedApprovalSettings = template.DefaultStagedApproval is not null
                ? CloneStagedApproval(template.DefaultStagedApproval)
                : null,
            CleanupSettings = template.DefaultCleanup is not null
                ? CloneCleanup(template.DefaultCleanup)
                : null,
            SyncSettings = template.DefaultSync is not null
                ? CloneSync(template.DefaultSync)
                : null
        };
    }

    /// <inheritdoc/>
    public IReadOnlyList<StagedApprovalEntry> GetStagedApprovalEntries(Guid taskId) =>
        _trackingEntries.Where(e => e.TaskId == taskId)
            .OrderByDescending(e => e.ApprovedForTestAt)
            .ToList()
            .AsReadOnly();

    /// <inheritdoc/>
    public async Task UpdateTaskExecutionStatusAsync(
        Guid taskId,
        TaskExecutionStatus status,
        string? message = null)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId);
        if (task is null)
        {
            return;
        }

        task.LastRunAt = DateTime.UtcNow;
        task.LastRunStatus = status;
        task.LastRunMessage = message;

        await SaveToFileAsync();
    }

    private async Task LoadTasksAsync()
    {
        try
        {
            if (!File.Exists(_tasksPath))
            {
                return;
            }

            var content = await File.ReadAllTextAsync(_tasksPath);
            var collection = JsonSerializer.Deserialize<ScheduledWsusTaskCollection>(
                content, JsonOptions);

            if (collection?.Tasks is not null)
            {
                _tasks.Clear();
                _tasks.AddRange(collection.Tasks);
            }

            await _loggingService.LogDebugAsync($"Loaded {_tasks.Count} scheduled tasks.");
        }
        catch (Exception ex)
        {
            await _loggingService.LogWarningAsync(
                $"Failed to load scheduled tasks: {ex.Message}");
        }
    }

    private async Task LoadTrackingAsync()
    {
        try
        {
            if (!File.Exists(_trackingPath))
            {
                return;
            }

            var content = await File.ReadAllTextAsync(_trackingPath);
            var collection = JsonSerializer.Deserialize<StagedApprovalTrackerCollection>(
                content, JsonOptions);

            if (collection?.Entries is not null)
            {
                _trackingEntries.Clear();
                _trackingEntries.AddRange(collection.Entries);
            }

            await _loggingService.LogDebugAsync(
                $"Loaded {_trackingEntries.Count} staged approval entries.");
        }
        catch (Exception ex)
        {
            await _loggingService.LogWarningAsync(
                $"Failed to load staged approval tracking: {ex.Message}");
        }
    }

    private async Task SaveToFileAsync()
    {
        try
        {
            var collection = new ScheduledWsusTaskCollection { Tasks = _tasks };
            var content = JsonSerializer.Serialize(collection, JsonOptions);
            await File.WriteAllTextAsync(_tasksPath, content);
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync(
                "Failed to save scheduled tasks", ex);
        }
    }

    private async Task SaveTrackingToFileAsync()
    {
        try
        {
            var collection = new StagedApprovalTrackerCollection
            {
                Entries = _trackingEntries,
                LastUpdated = DateTime.UtcNow
            };
            var content = JsonSerializer.Serialize(collection, JsonOptions);
            await File.WriteAllTextAsync(_trackingPath, content);
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync(
                "Failed to save staged approval tracking", ex);
        }
    }

    private static ScheduleConfig CloneSchedule(ScheduleConfig source) => new()
    {
        Frequency = source.Frequency,
        TimeOfDay = source.TimeOfDay,
        DaysOfWeek = [.. source.DaysOfWeek],
        DayOfMonth = source.DayOfMonth,
        StartDate = source.StartDate,
        EndDate = source.EndDate
    };

    private static StagedApprovalConfig CloneStagedApproval(StagedApprovalConfig source) => new()
    {
        TestGroupIds = [.. source.TestGroupIds],
        ProductionGroupIds = [.. source.ProductionGroupIds],
        TestGroupNames = [.. source.TestGroupNames],
        ProductionGroupNames = [.. source.ProductionGroupNames],
        PromotionDelayDays = source.PromotionDelayDays,
        UpdateClassifications = [.. source.UpdateClassifications],
        RequireSuccessfulInstallations = source.RequireSuccessfulInstallations,
        MinimumSuccessfulInstallations = source.MinimumSuccessfulInstallations,
        AbortOnFailures = source.AbortOnFailures,
        MaxAllowedFailures = source.MaxAllowedFailures,
        DeclineSupersededUpdates = source.DeclineSupersededUpdates
    };

    private static CleanupOptions CloneCleanup(CleanupOptions source) => new()
    {
        RemoveObsoleteUpdates = source.RemoveObsoleteUpdates,
        RemoveExpiredUpdates = source.RemoveExpiredUpdates,
        RemoveObsoleteComputers = source.RemoveObsoleteComputers,
        CompressUpdateRevisions = source.CompressUpdateRevisions,
        RemoveUnneededContent = source.RemoveUnneededContent
    };

    private static SyncConfig CloneSync(SyncConfig source) => new()
    {
        WaitForCompletion = source.WaitForCompletion,
        MaxWaitMinutes = source.MaxWaitMinutes,
        NotifyOnCompletion = source.NotifyOnCompletion,
        NotifyOnErrorsOnly = source.NotifyOnErrorsOnly
    };
}
