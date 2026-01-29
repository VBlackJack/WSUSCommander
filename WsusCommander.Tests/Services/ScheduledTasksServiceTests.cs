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
using FluentAssertions;
using Moq;
using WsusCommander.Models;
using WsusCommander.Services;

namespace WsusCommander.Tests.Services;

public class ScheduledTasksServiceTests : IDisposable
{
    private readonly ScheduledTasksService _sut;
    private readonly Mock<IConfigurationService> _configMock;
    private readonly Mock<ILoggingService> _loggingMock;
    private readonly Mock<ITaskSchedulerService> _taskSchedulerMock;
    private readonly string _testDataPath;

    public ScheduledTasksServiceTests()
    {
        _testDataPath = Path.Combine(Path.GetTempPath(), $"WsusCommanderTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDataPath);

        _configMock = new Mock<IConfigurationService>();
        _configMock.Setup(c => c.AppSettings).Returns(new AppSettingsConfig { DataPath = _testDataPath });

        _loggingMock = new Mock<ILoggingService>();
        _taskSchedulerMock = new Mock<ITaskSchedulerService>();

        _sut = new ScheduledTasksService(
            _configMock.Object,
            _loggingMock.Object,
            _taskSchedulerMock.Object);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDataPath))
            {
                Directory.Delete(_testDataPath, true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }

        GC.SuppressFinalize(this);
    }

    #region GetTasks Tests

    [Fact]
    public void GetTasks_WhenNoTasks_ReturnsEmptyList()
    {
        // Act
        var tasks = _sut.GetTasks();

        // Assert
        tasks.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTasks_AfterSave_ReturnsTask()
    {
        // Arrange
        var task = CreateTestTask();
        _taskSchedulerMock.Setup(t => t.CreateTaskAsync(It.IsAny<ScheduledWsusTask>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SaveTaskAsync(task);
        var tasks = _sut.GetTasks();

        // Assert
        tasks.Should().HaveCount(1);
        tasks[0].Name.Should().Be(task.Name);
    }

    [Fact]
    public async Task GetTasks_ReturnsTasksOrderedByName()
    {
        // Arrange
        var task1 = CreateTestTask("Zebra Task");
        var task2 = CreateTestTask("Alpha Task");
        var task3 = CreateTestTask("Middle Task");

        _taskSchedulerMock.Setup(t => t.CreateTaskAsync(It.IsAny<ScheduledWsusTask>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SaveTaskAsync(task1);
        await _sut.SaveTaskAsync(task2);
        await _sut.SaveTaskAsync(task3);
        var tasks = _sut.GetTasks();

        // Assert
        tasks.Should().HaveCount(3);
        tasks[0].Name.Should().Be("Alpha Task");
        tasks[1].Name.Should().Be("Middle Task");
        tasks[2].Name.Should().Be("Zebra Task");
    }

    #endregion

    #region GetTask Tests

    [Fact]
    public void GetTask_WithNonExistentId_ReturnsNull()
    {
        // Act
        var task = _sut.GetTask(Guid.NewGuid());

        // Assert
        task.Should().BeNull();
    }

    [Fact]
    public async Task GetTask_WithExistingId_ReturnsTask()
    {
        // Arrange
        var task = CreateTestTask();
        _taskSchedulerMock.Setup(t => t.CreateTaskAsync(It.IsAny<ScheduledWsusTask>()))
            .Returns(Task.CompletedTask);
        await _sut.SaveTaskAsync(task);

        // Act
        var result = _sut.GetTask(task.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(task.Id);
    }

    #endregion

    #region SaveTaskAsync Tests

    [Fact]
    public async Task SaveTaskAsync_NewTask_GeneratesWindowsTaskName()
    {
        // Arrange
        var task = CreateTestTask();
        task.WindowsTaskName = null;

        _taskSchedulerMock.Setup(t => t.CreateTaskAsync(It.IsAny<ScheduledWsusTask>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SaveTaskAsync(task);

        // Assert
        task.WindowsTaskName.Should().StartWith("WsusCommander_");
    }

    [Fact]
    public async Task SaveTaskAsync_NewTask_SetsModifiedAt()
    {
        // Arrange
        var task = CreateTestTask();
        var beforeSave = DateTime.UtcNow;

        _taskSchedulerMock.Setup(t => t.CreateTaskAsync(It.IsAny<ScheduledWsusTask>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SaveTaskAsync(task);

        // Assert
        task.ModifiedAt.Should().BeOnOrAfter(beforeSave);
    }

    [Fact]
    public async Task SaveTaskAsync_EnabledTask_CallsTaskSchedulerCreate()
    {
        // Arrange
        var task = CreateTestTask();
        task.IsEnabled = true;

        _taskSchedulerMock.Setup(t => t.CreateTaskAsync(It.IsAny<ScheduledWsusTask>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SaveTaskAsync(task);

        // Assert
        _taskSchedulerMock.Verify(t => t.CreateTaskAsync(task), Times.Once);
    }

    [Fact]
    public async Task SaveTaskAsync_DisabledTask_CallsTaskSchedulerDisable()
    {
        // Arrange
        var task = CreateTestTask();
        task.IsEnabled = false;

        _taskSchedulerMock.Setup(t => t.DisableTaskAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SaveTaskAsync(task);

        // Assert
        _taskSchedulerMock.Verify(t => t.DisableTaskAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SaveTaskAsync_ExistingTask_CallsTaskSchedulerUpdate()
    {
        // Arrange
        var task = CreateTestTask();
        task.IsEnabled = true;

        _taskSchedulerMock.Setup(t => t.CreateTaskAsync(It.IsAny<ScheduledWsusTask>()))
            .Returns(Task.CompletedTask);
        _taskSchedulerMock.Setup(t => t.UpdateTaskAsync(It.IsAny<ScheduledWsusTask>()))
            .Returns(Task.CompletedTask);

        await _sut.SaveTaskAsync(task);

        // Act
        task.Name = "Updated Name";
        await _sut.SaveTaskAsync(task);

        // Assert
        _taskSchedulerMock.Verify(t => t.UpdateTaskAsync(task), Times.Once);
    }

    #endregion

    #region DeleteTaskAsync Tests

    [Fact]
    public async Task DeleteTaskAsync_WithNonExistentId_DoesNotThrow()
    {
        // Act
        var act = async () => await _sut.DeleteTaskAsync(Guid.NewGuid());

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteTaskAsync_WithExistingTask_RemovesTask()
    {
        // Arrange
        var task = CreateTestTask();
        _taskSchedulerMock.Setup(t => t.CreateTaskAsync(It.IsAny<ScheduledWsusTask>()))
            .Returns(Task.CompletedTask);
        _taskSchedulerMock.Setup(t => t.DeleteTaskAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _sut.SaveTaskAsync(task);

        // Act
        await _sut.DeleteTaskAsync(task.Id);

        // Assert
        _sut.GetTask(task.Id).Should().BeNull();
    }

    [Fact]
    public async Task DeleteTaskAsync_CallsTaskSchedulerDelete()
    {
        // Arrange
        var task = CreateTestTask();
        _taskSchedulerMock.Setup(t => t.CreateTaskAsync(It.IsAny<ScheduledWsusTask>()))
            .Returns(Task.CompletedTask);
        _taskSchedulerMock.Setup(t => t.DeleteTaskAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _sut.SaveTaskAsync(task);

        // Act
        await _sut.DeleteTaskAsync(task.Id);

        // Assert
        _taskSchedulerMock.Verify(t => t.DeleteTaskAsync(It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region SetTaskEnabledAsync Tests

    [Fact]
    public async Task SetTaskEnabledAsync_ToTrue_CallsTaskSchedulerEnable()
    {
        // Arrange
        var task = CreateTestTask();
        task.IsEnabled = false;

        _taskSchedulerMock.Setup(t => t.DisableTaskAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _taskSchedulerMock.Setup(t => t.EnableTaskAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _sut.SaveTaskAsync(task);

        // Act
        await _sut.SetTaskEnabledAsync(task.Id, true);

        // Assert
        _taskSchedulerMock.Verify(t => t.EnableTaskAsync(It.IsAny<string>()), Times.Once);
        var updated = _sut.GetTask(task.Id);
        updated!.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task SetTaskEnabledAsync_ToFalse_CallsTaskSchedulerDisable()
    {
        // Arrange
        var task = CreateTestTask();
        task.IsEnabled = true;

        _taskSchedulerMock.Setup(t => t.CreateTaskAsync(It.IsAny<ScheduledWsusTask>()))
            .Returns(Task.CompletedTask);
        _taskSchedulerMock.Setup(t => t.DisableTaskAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _sut.SaveTaskAsync(task);

        // Act
        await _sut.SetTaskEnabledAsync(task.Id, false);

        // Assert
        _taskSchedulerMock.Verify(t => t.DisableTaskAsync(It.IsAny<string>()), Times.AtLeastOnce);
        var updated = _sut.GetTask(task.Id);
        updated!.IsEnabled.Should().BeFalse();
    }

    #endregion

    #region GetTemplates Tests

    [Fact]
    public void GetTemplates_ReturnsBuiltInTemplates()
    {
        // Act
        var templates = _sut.GetTemplates();

        // Assert
        templates.Should().NotBeEmpty();
        templates.Should().Contain(t => t.Id == "staged-security");
        templates.Should().Contain(t => t.Id == "monthly-cleanup");
        templates.Should().Contain(t => t.Id == "daily-sync");
    }

    [Fact]
    public void GetTemplates_ReturnsTemplatesOrderedBySortOrder()
    {
        // Act
        var templates = _sut.GetTemplates();

        // Assert
        templates.Should().BeInAscendingOrder(t => t.SortOrder);
    }

    #endregion

    #region CreateFromTemplate Tests

    [Fact]
    public void CreateFromTemplate_WithValidId_ReturnsConfiguredTask()
    {
        // Act
        var task = _sut.CreateFromTemplate("staged-security");

        // Assert
        task.Should().NotBeNull();
        task.OperationType.Should().Be(ScheduledTaskOperationType.StagedApproval);
        task.StagedApprovalSettings.Should().NotBeNull();
    }

    [Fact]
    public void CreateFromTemplate_WithCleanupId_ReturnsCleanupTask()
    {
        // Act
        var task = _sut.CreateFromTemplate("monthly-cleanup");

        // Assert
        task.Should().NotBeNull();
        task.OperationType.Should().Be(ScheduledTaskOperationType.Cleanup);
        task.CleanupSettings.Should().NotBeNull();
    }

    [Fact]
    public void CreateFromTemplate_WithSyncId_ReturnsSyncTask()
    {
        // Act
        var task = _sut.CreateFromTemplate("daily-sync");

        // Assert
        task.Should().NotBeNull();
        task.OperationType.Should().Be(ScheduledTaskOperationType.Synchronization);
        task.SyncSettings.Should().NotBeNull();
    }

    [Fact]
    public void CreateFromTemplate_WithInvalidId_ThrowsArgumentException()
    {
        // Act
        var act = () => _sut.CreateFromTemplate("invalid-template");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region UpdateTaskExecutionStatusAsync Tests

    [Fact]
    public async Task UpdateTaskExecutionStatusAsync_UpdatesStatus()
    {
        // Arrange
        var task = CreateTestTask();
        _taskSchedulerMock.Setup(t => t.CreateTaskAsync(It.IsAny<ScheduledWsusTask>()))
            .Returns(Task.CompletedTask);
        await _sut.SaveTaskAsync(task);

        // Act
        await _sut.UpdateTaskExecutionStatusAsync(task.Id, TaskExecutionStatus.Success, "Completed successfully");

        // Assert
        var updated = _sut.GetTask(task.Id);
        updated!.LastRunStatus.Should().Be(TaskExecutionStatus.Success);
        updated.LastRunMessage.Should().Be("Completed successfully");
        updated.LastRunAt.Should().NotBeNull();
    }

    #endregion

    #region Persistence Tests

    [Fact]
    public async Task LoadAsync_RestoresPersistedTasks()
    {
        // Arrange
        var task = CreateTestTask();
        _taskSchedulerMock.Setup(t => t.CreateTaskAsync(It.IsAny<ScheduledWsusTask>()))
            .Returns(Task.CompletedTask);
        await _sut.SaveTaskAsync(task);

        // Create new service instance to test loading
        var newService = new ScheduledTasksService(
            _configMock.Object,
            _loggingMock.Object,
            _taskSchedulerMock.Object);

        // Act
        await newService.LoadAsync();
        var loadedTasks = newService.GetTasks();

        // Assert
        loadedTasks.Should().HaveCount(1);
        loadedTasks[0].Name.Should().Be(task.Name);
    }

    #endregion

    private static ScheduledWsusTask CreateTestTask(string name = "Test Task")
    {
        return new ScheduledWsusTask
        {
            Name = name,
            Description = "Test description",
            OperationType = ScheduledTaskOperationType.StagedApproval,
            IsEnabled = true,
            Schedule = new ScheduleConfig
            {
                Frequency = ScheduleFrequency.Weekly,
                TimeOfDay = new TimeSpan(3, 0, 0),
                DaysOfWeek = [DayOfWeek.Tuesday]
            },
            StagedApprovalSettings = new StagedApprovalConfig
            {
                PromotionDelayDays = 7
            }
        };
    }
}
