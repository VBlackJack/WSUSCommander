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

using FluentAssertions;
using Moq;
using WsusCommander.Models;
using WsusCommander.Services;

namespace WsusCommander.Tests.Services;

public class SchedulerServiceTests
{
    [Fact]
    public async Task ScheduleAsync_AddsTaskToList()
    {
        // Arrange
        var loggingMock = new Mock<ILoggingService>();
        var scheduler = new SchedulerService(loggingMock.Object);
        var task = new ScheduledTask
        {
            Name = "Test Task",
            Recurrence = ScheduleRecurrence.Once,
            NextRun = DateTimeOffset.Now.AddMinutes(5),
            IsEnabled = true
        };

        // Act
        await scheduler.ScheduleAsync(task, _ => Task.CompletedTask);

        // Assert
        scheduler.Tasks.Should().ContainSingle(t => t.Name == "Test Task");
    }

    [Fact]
    public async Task RemoveAsync_RemovesTaskFromList()
    {
        // Arrange
        var loggingMock = new Mock<ILoggingService>();
        var scheduler = new SchedulerService(loggingMock.Object);
        var task = new ScheduledTask
        {
            Name = "Cleanup",
            Recurrence = ScheduleRecurrence.Once,
            NextRun = DateTimeOffset.Now.AddMinutes(5),
            IsEnabled = true
        };

        await scheduler.ScheduleAsync(task, _ => Task.CompletedTask);

        // Act
        await scheduler.RemoveAsync(task.Id);

        // Assert
        scheduler.Tasks.Should().BeEmpty();
    }

    [Fact]
    public async Task ScheduleAsync_ExecutesOnceAndRemovesTask()
    {
        // Arrange
        var loggingMock = new Mock<ILoggingService>();
        using var scheduler = new SchedulerService(loggingMock.Object);
        var executed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var task = new ScheduledTask
        {
            Name = "Immediate",
            Recurrence = ScheduleRecurrence.Once,
            NextRun = DateTimeOffset.Now.AddMilliseconds(50),
            IsEnabled = true
        };

        // Act
        await scheduler.ScheduleAsync(task, _ =>
        {
            executed.TrySetResult(true);
            return Task.CompletedTask;
        });

        var completed = await Task.WhenAny(executed.Task, Task.Delay(3000));

        // Assert
        completed.Should().Be(executed.Task);
        scheduler.Tasks.Should().BeEmpty();
    }
}
