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

using System.ComponentModel.DataAnnotations;

namespace WsusCommander.Models;

/// <summary>
/// Type of WSUS scheduled operation.
/// </summary>
public enum ScheduledTaskOperationType
{
    /// <summary>
    /// Staged approval workflow (test → delay → production).
    /// </summary>
    StagedApproval,

    /// <summary>
    /// WSUS cleanup maintenance.
    /// </summary>
    Cleanup,

    /// <summary>
    /// WSUS synchronization.
    /// </summary>
    Synchronization
}

/// <summary>
/// Execution status of a scheduled task.
/// </summary>
public enum TaskExecutionStatus
{
    /// <summary>
    /// Task has never run.
    /// </summary>
    NeverRun,

    /// <summary>
    /// Last execution succeeded.
    /// </summary>
    Success,

    /// <summary>
    /// Last execution completed with warnings.
    /// </summary>
    Warning,

    /// <summary>
    /// Last execution failed.
    /// </summary>
    Failed
}

/// <summary>
/// Represents a scheduled WSUS management task with Windows Task Scheduler integration.
/// </summary>
public sealed class ScheduledWsusTask
{
    /// <summary>
    /// Gets or sets the unique identifier for the task.
    /// </summary>
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the display name of the task.
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the task description.
    /// </summary>
    [StringLength(1024)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operation type.
    /// </summary>
    public ScheduledTaskOperationType OperationType { get; set; }

    /// <summary>
    /// Gets or sets whether the task is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the scheduling configuration.
    /// </summary>
    [Required]
    public ScheduleConfig Schedule { get; set; } = new();

    /// <summary>
    /// Gets or sets the staged approval settings (only for StagedApproval type).
    /// </summary>
    public StagedApprovalConfig? StagedApprovalSettings { get; set; }

    /// <summary>
    /// Gets or sets the cleanup settings (only for Cleanup type).
    /// </summary>
    public CleanupOptions? CleanupSettings { get; set; }

    /// <summary>
    /// Gets or sets the sync settings (only for Synchronization type).
    /// </summary>
    public SyncConfig? SyncSettings { get; set; }

    /// <summary>
    /// Gets or sets the Windows Task Scheduler task name.
    /// </summary>
    [StringLength(256)]
    public string? WindowsTaskName { get; set; }

    /// <summary>
    /// Gets or sets the task creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last modification timestamp.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the last execution timestamp.
    /// </summary>
    public DateTime? LastRunAt { get; set; }

    /// <summary>
    /// Gets or sets the next scheduled run timestamp.
    /// </summary>
    public DateTime? NextRunAt { get; set; }

    /// <summary>
    /// Gets or sets the last execution status.
    /// </summary>
    public TaskExecutionStatus LastRunStatus { get; set; } = TaskExecutionStatus.NeverRun;

    /// <summary>
    /// Gets or sets the last execution result message.
    /// </summary>
    [StringLength(2048)]
    public string? LastRunMessage { get; set; }
}

/// <summary>
/// Collection wrapper for scheduled tasks serialization.
/// </summary>
public sealed class ScheduledWsusTaskCollection
{
    /// <summary>
    /// Gets or sets the list of scheduled tasks.
    /// </summary>
    public List<ScheduledWsusTask> Tasks { get; set; } = [];
}
