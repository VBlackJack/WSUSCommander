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

using System.Collections.Concurrent;
using WsusCommander.Models;
using WsusCommander.Properties;

namespace WsusCommander.Services;

/// <summary>
/// In-memory scheduler service for recurring operations.
/// </summary>
public sealed class SchedulerService : ISchedulerService, IDisposable
{
    private readonly ILoggingService _loggingService;
    private readonly ConcurrentDictionary<Guid, Timer> _timers = new();
    private readonly List<ScheduledTask> _tasks = [];
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulerService"/> class.
    /// </summary>
    /// <param name="loggingService">Logging service.</param>
    public SchedulerService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ScheduledTask> Tasks
    {
        get
        {
            lock (_lock)
            {
                return _tasks.ToList();
            }
        }
    }

    /// <inheritdoc/>
    public async Task ScheduleAsync(
        ScheduledTask task,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        if (task.Id == Guid.Empty)
        {
            task.Id = Guid.NewGuid();
        }

        lock (_lock)
        {
            _tasks.RemoveAll(t => t.Id == task.Id);
            _tasks.Add(task);
        }

        await _loggingService.LogInfoAsync(string.Format(Resources.LogScheduledTaskAdded, task.Name));
        ScheduleTimer(task, operation);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _tasks.RemoveAll(task => task.Id == taskId);
        }

        if (_timers.TryRemove(taskId, out var timer))
        {
            await timer.DisposeAsync();
        }

        await _loggingService.LogInfoAsync(string.Format(Resources.LogScheduledTaskRemoved, taskId));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var timer in _timers.Values)
        {
            timer.Dispose();
        }

        _timers.Clear();
    }

    private void ScheduleTimer(ScheduledTask task, Func<CancellationToken, Task> operation)
    {
        if (!task.IsEnabled)
        {
            return;
        }

        var delay = task.NextRun - DateTimeOffset.Now;
        if (delay < TimeSpan.Zero)
        {
            delay = TimeSpan.Zero;
        }

        var timer = new Timer(async _ => await ExecuteTaskAsync(task, operation), null, delay, Timeout.InfiniteTimeSpan);
        _timers.AddOrUpdate(task.Id, timer, (_, existing) =>
        {
            existing.Dispose();
            return timer;
        });
    }

    private async Task ExecuteTaskAsync(ScheduledTask task, Func<CancellationToken, Task> operation)
    {
        if (!task.IsEnabled)
        {
            return;
        }

        await _loggingService.LogInfoAsync(string.Format(Resources.LogScheduledTaskRun, task.Name));
        await operation(CancellationToken.None);

        if (task.Recurrence == ScheduleRecurrence.Once)
        {
            await RemoveAsync(task.Id);
            return;
        }

        task.NextRun = GetNextRun(task);
        ScheduleTimer(task, operation);
    }

    private static DateTimeOffset GetNextRun(ScheduledTask task)
    {
        return task.Recurrence switch
        {
            ScheduleRecurrence.Daily => task.NextRun.AddDays(1),
            ScheduleRecurrence.Weekly => task.NextRun.AddDays(7),
            ScheduleRecurrence.Monthly => task.NextRun.AddMonths(1),
            ScheduleRecurrence.Interval => task.Interval.HasValue ? task.NextRun.Add(task.Interval.Value) : task.NextRun.AddHours(1),
            _ => task.NextRun
        };
    }
}
