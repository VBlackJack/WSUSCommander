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
/// Service interface for interacting with Windows Task Scheduler.
/// </summary>
public interface ITaskSchedulerService
{
    /// <summary>
    /// Creates a new Windows scheduled task.
    /// </summary>
    /// <param name="task">The WSUS task to schedule.</param>
    Task CreateTaskAsync(ScheduledWsusTask task);

    /// <summary>
    /// Updates an existing Windows scheduled task.
    /// </summary>
    /// <param name="task">The WSUS task with updated settings.</param>
    Task UpdateTaskAsync(ScheduledWsusTask task);

    /// <summary>
    /// Deletes a Windows scheduled task.
    /// </summary>
    /// <param name="taskName">The Windows task name.</param>
    Task DeleteTaskAsync(string taskName);

    /// <summary>
    /// Enables a Windows scheduled task.
    /// </summary>
    /// <param name="taskName">The Windows task name.</param>
    Task EnableTaskAsync(string taskName);

    /// <summary>
    /// Disables a Windows scheduled task.
    /// </summary>
    /// <param name="taskName">The Windows task name.</param>
    Task DisableTaskAsync(string taskName);

    /// <summary>
    /// Gets the status of a Windows scheduled task.
    /// </summary>
    /// <param name="taskName">The Windows task name.</param>
    /// <returns>Task info or null if not found.</returns>
    Task<WindowsScheduledTaskInfo?> GetTaskInfoAsync(string taskName);

    /// <summary>
    /// Runs a Windows scheduled task immediately.
    /// </summary>
    /// <param name="taskName">The Windows task name.</param>
    Task RunTaskNowAsync(string taskName);

    /// <summary>
    /// Checks if a Windows scheduled task exists.
    /// </summary>
    /// <param name="taskName">The Windows task name.</param>
    Task<bool> TaskExistsAsync(string taskName);
}

/// <summary>
/// Information about a Windows scheduled task.
/// </summary>
public sealed class WindowsScheduledTaskInfo
{
    /// <summary>
    /// Gets or sets the task name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the task is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the task state.
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last run time.
    /// </summary>
    public DateTime? LastRunTime { get; set; }

    /// <summary>
    /// Gets or sets the last run result.
    /// </summary>
    public int LastTaskResult { get; set; }

    /// <summary>
    /// Gets or sets the next run time.
    /// </summary>
    public DateTime? NextRunTime { get; set; }
}
