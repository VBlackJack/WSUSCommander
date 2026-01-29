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
/// Service interface for managing scheduled WSUS tasks.
/// </summary>
public interface IScheduledTasksService
{
    /// <summary>
    /// Gets all scheduled tasks.
    /// </summary>
    IReadOnlyList<ScheduledWsusTask> GetTasks();

    /// <summary>
    /// Gets a scheduled task by ID.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <returns>The task or null if not found.</returns>
    ScheduledWsusTask? GetTask(Guid taskId);

    /// <summary>
    /// Saves a scheduled task (creates or updates).
    /// </summary>
    /// <param name="task">The task to save.</param>
    Task SaveTaskAsync(ScheduledWsusTask task);

    /// <summary>
    /// Deletes a scheduled task.
    /// </summary>
    /// <param name="taskId">The task ID to delete.</param>
    Task DeleteTaskAsync(Guid taskId);

    /// <summary>
    /// Enables or disables a task.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="enabled">Whether to enable the task.</param>
    Task SetTaskEnabledAsync(Guid taskId, bool enabled);

    /// <summary>
    /// Loads tasks from storage.
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Gets all available task templates.
    /// </summary>
    IReadOnlyList<TaskTemplate> GetTemplates();

    /// <summary>
    /// Creates a new task from a template.
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    /// <returns>A new task with template defaults.</returns>
    ScheduledWsusTask CreateFromTemplate(string templateId);

    /// <summary>
    /// Gets staged approval tracking entries for a task.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    IReadOnlyList<StagedApprovalEntry> GetStagedApprovalEntries(Guid taskId);

    /// <summary>
    /// Updates the execution status of a task.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="status">The new status.</param>
    /// <param name="message">Optional status message.</param>
    Task UpdateTaskExecutionStatusAsync(
        Guid taskId,
        TaskExecutionStatus status,
        string? message = null);
}
