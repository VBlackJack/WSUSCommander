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

namespace WsusCommander.Models;

/// <summary>
/// Supported scheduled task types.
/// </summary>
public enum ScheduledTaskType
{
    /// <summary>
    /// Scheduled approval task.
    /// </summary>
    Approval,

    /// <summary>
    /// Scheduled cleanup task.
    /// </summary>
    Cleanup
}

/// <summary>
/// Supported recurrence patterns for scheduled tasks.
/// </summary>
public enum ScheduleRecurrence
{
    /// <summary>
    /// Runs once.
    /// </summary>
    Once,

    /// <summary>
    /// Runs daily.
    /// </summary>
    Daily,

    /// <summary>
    /// Runs weekly.
    /// </summary>
    Weekly,

    /// <summary>
    /// Runs monthly.
    /// </summary>
    Monthly,

    /// <summary>
    /// Runs at a fixed interval.
    /// </summary>
    Interval
}

/// <summary>
/// Scheduled task definition.
/// </summary>
public sealed class ScheduledTask
{
    /// <summary>
    /// Gets or sets the task identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the task name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the task type.
    /// </summary>
    public ScheduledTaskType TaskType { get; set; }

    /// <summary>
    /// Gets or sets the recurrence type.
    /// </summary>
    public ScheduleRecurrence Recurrence { get; set; } = ScheduleRecurrence.Once;

    /// <summary>
    /// Gets or sets the next run time.
    /// </summary>
    public DateTimeOffset NextRun { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// Gets or sets the recurrence interval when <see cref="ScheduleRecurrence.Interval"/> is used.
    /// </summary>
    public TimeSpan? Interval { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the task is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the optional task description.
    /// </summary>
    public string? Description { get; set; }
}
