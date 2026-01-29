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
using FluentAssertions;
using Moq;
using WsusCommander.Models;
using WsusCommander.Services;

namespace WsusCommander.Tests.Services;

public class TaskSchedulerServiceTests
{
    private readonly TaskSchedulerService _sut;
    private readonly Mock<IPowerShellService> _powerShellMock;
    private readonly Mock<ILoggingService> _loggingMock;
    private readonly Mock<IConfigurationService> _configMock;

    public TaskSchedulerServiceTests()
    {
        _powerShellMock = new Mock<IPowerShellService>();
        _loggingMock = new Mock<ILoggingService>();
        _configMock = new Mock<IConfigurationService>();

        _configMock.Setup(c => c.AppSettings).Returns(new AppSettingsConfig { DataPath = "C:\\TestData" });
        _configMock.Setup(c => c.Config).Returns(new AppConfig
        {
            WsusConnection = new WsusConnectionConfig
            {
                ServerName = "wsus-server",
                Port = 8530,
                UseSsl = false
            }
        });

        _sut = new TaskSchedulerService(
            _powerShellMock.Object,
            _loggingMock.Object,
            _configMock.Object);
    }

    private void SetupPowerShellMock(PSDataCollection<PSObject>? result = null)
    {
        _powerShellMock.Setup(p => p.ExecuteScriptAsync(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result ?? []);
    }

    #region CreateTaskAsync Tests

    [Fact]
    public async Task CreateTaskAsync_WithValidTask_CallsPowerShell()
    {
        // Arrange
        var task = CreateTestTask();
        SetupPowerShellMock();

        // Act
        await _sut.CreateTaskAsync(task);

        // Assert
        _powerShellMock.Verify(p => p.ExecuteScriptAsync(
            "New-WsusScheduledTask.ps1",
            It.IsAny<Dictionary<string, object>?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_WithNullTaskName_ThrowsArgumentException()
    {
        // Arrange
        var task = CreateTestTask();
        task.WindowsTaskName = null;

        // Act
        var act = async () => await _sut.CreateTaskAsync(task);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateTaskAsync_WithEmptyTaskName_ThrowsArgumentException()
    {
        // Arrange
        var task = CreateTestTask();
        task.WindowsTaskName = string.Empty;

        // Act
        var act = async () => await _sut.CreateTaskAsync(task);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateTaskAsync_LogsSuccess()
    {
        // Arrange
        var task = CreateTestTask();
        SetupPowerShellMock();

        // Act
        await _sut.CreateTaskAsync(task);

        // Assert
        _loggingMock.Verify(l => l.LogInfoAsync(
            It.Is<string>(s => s.Contains("created"))),
            Times.Once);
    }

    #endregion

    #region UpdateTaskAsync Tests

    [Fact]
    public async Task UpdateTaskAsync_WithValidTask_CallsPowerShell()
    {
        // Arrange
        var task = CreateTestTask();
        SetupPowerShellMock();

        // Act
        await _sut.UpdateTaskAsync(task);

        // Assert
        _powerShellMock.Verify(p => p.ExecuteScriptAsync(
            "Update-WsusScheduledTask.ps1",
            It.IsAny<Dictionary<string, object>?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateTaskAsync_WithNullTaskName_DoesNotCallPowerShell()
    {
        // Arrange
        var task = CreateTestTask();
        task.WindowsTaskName = null;

        // Act
        await _sut.UpdateTaskAsync(task);

        // Assert
        _powerShellMock.Verify(p => p.ExecuteScriptAsync(
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>?>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region DeleteTaskAsync Tests

    [Fact]
    public async Task DeleteTaskAsync_WithValidName_CallsPowerShell()
    {
        // Arrange
        const string taskName = "WsusCommander_Test";
        SetupPowerShellMock();

        // Act
        await _sut.DeleteTaskAsync(taskName);

        // Assert
        _powerShellMock.Verify(p => p.ExecuteScriptAsync(
            "Remove-WsusScheduledTask.ps1",
            It.IsAny<Dictionary<string, object>?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteTaskAsync_WithNullName_DoesNotCallPowerShell()
    {
        // Act
        await _sut.DeleteTaskAsync(null!);

        // Assert
        _powerShellMock.Verify(p => p.ExecuteScriptAsync(
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>?>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteTaskAsync_WithEmptyName_DoesNotCallPowerShell()
    {
        // Act
        await _sut.DeleteTaskAsync(string.Empty);

        // Assert
        _powerShellMock.Verify(p => p.ExecuteScriptAsync(
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>?>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region EnableTaskAsync Tests

    [Fact]
    public async Task EnableTaskAsync_WithValidName_CallsPowerShell()
    {
        // Arrange
        const string taskName = "WsusCommander_Test";
        SetupPowerShellMock();

        // Act
        await _sut.EnableTaskAsync(taskName);

        // Assert
        _powerShellMock.Verify(p => p.ExecuteScriptAsync(
            "Update-WsusScheduledTask.ps1",
            It.IsAny<Dictionary<string, object>?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region DisableTaskAsync Tests

    [Fact]
    public async Task DisableTaskAsync_WithValidName_CallsPowerShell()
    {
        // Arrange
        const string taskName = "WsusCommander_Test";
        SetupPowerShellMock();

        // Act
        await _sut.DisableTaskAsync(taskName);

        // Assert
        _powerShellMock.Verify(p => p.ExecuteScriptAsync(
            "Update-WsusScheduledTask.ps1",
            It.IsAny<Dictionary<string, object>?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region RunTaskNowAsync Tests

    [Fact]
    public async Task RunTaskNowAsync_WithValidName_CallsPowerShell()
    {
        // Arrange
        const string taskName = "WsusCommander_Test";
        SetupPowerShellMock();

        // Act
        await _sut.RunTaskNowAsync(taskName);

        // Assert
        _powerShellMock.Verify(p => p.ExecuteScriptAsync(
            "Update-WsusScheduledTask.ps1",
            It.IsAny<Dictionary<string, object>?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunTaskNowAsync_WithNullName_DoesNotCallPowerShell()
    {
        // Act
        await _sut.RunTaskNowAsync(null!);

        // Assert
        _powerShellMock.Verify(p => p.ExecuteScriptAsync(
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>?>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region TaskExistsAsync Tests

    [Fact]
    public async Task TaskExistsAsync_WhenTaskExists_ReturnsTrue()
    {
        // Arrange
        const string taskName = "WsusCommander_Test";
        var psObject = new PSObject();
        psObject.Properties.Add(new PSNoteProperty("Name", taskName));
        psObject.Properties.Add(new PSNoteProperty("Enabled", true));
        psObject.Properties.Add(new PSNoteProperty("State", "Ready"));

        var result = new PSDataCollection<PSObject> { psObject };
        SetupPowerShellMock(result);

        // Act
        var exists = await _sut.TaskExistsAsync(taskName);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task TaskExistsAsync_WhenTaskDoesNotExist_ReturnsFalse()
    {
        // Arrange
        const string taskName = "NonExistent_Task";
        SetupPowerShellMock([]);

        // Act
        var exists = await _sut.TaskExistsAsync(taskName);

        // Assert
        exists.Should().BeFalse();
    }

    #endregion

    #region Parameter Building Tests

    [Fact]
    public async Task CreateTaskAsync_IncludesRequiredParameters()
    {
        // Arrange
        var task = CreateTestTask();
        Dictionary<string, object>? capturedParams = null;

        _powerShellMock.Setup(p => p.ExecuteScriptAsync(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Dictionary<string, object>?, CancellationToken>((_, p, _) => capturedParams = p)
            .ReturnsAsync([]);

        // Act
        await _sut.CreateTaskAsync(task);

        // Assert
        capturedParams.Should().NotBeNull();
        capturedParams!.Should().ContainKey("TaskName");
        capturedParams.Should().ContainKey("TaskId");
        capturedParams.Should().ContainKey("Operation");
        capturedParams["Operation"].Should().Be("Create");
    }

    [Fact]
    public async Task CreateTaskAsync_IncludesWsusConnectionInfo()
    {
        // Arrange
        var task = CreateTestTask();
        Dictionary<string, object>? capturedParams = null;

        _powerShellMock.Setup(p => p.ExecuteScriptAsync(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Dictionary<string, object>?, CancellationToken>((_, p, _) => capturedParams = p)
            .ReturnsAsync([]);

        // Act
        await _sut.CreateTaskAsync(task);

        // Assert
        capturedParams.Should().NotBeNull();
        capturedParams!["WsusServer"].Should().Be("wsus-server");
        capturedParams["WsusPort"].Should().Be(8530);
        capturedParams["WsusUseSsl"].Should().Be(false);
    }

    [Fact]
    public async Task CreateTaskAsync_WithWeeklySchedule_IncludesDaysOfWeek()
    {
        // Arrange
        var task = CreateTestTask();
        task.Schedule.Frequency = ScheduleFrequency.Weekly;
        task.Schedule.DaysOfWeek = [DayOfWeek.Tuesday, DayOfWeek.Thursday];

        Dictionary<string, object>? capturedParams = null;

        _powerShellMock.Setup(p => p.ExecuteScriptAsync(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Dictionary<string, object>?, CancellationToken>((_, p, _) => capturedParams = p)
            .ReturnsAsync([]);

        // Act
        await _sut.CreateTaskAsync(task);

        // Assert
        capturedParams.Should().NotBeNull();
        capturedParams!.Should().ContainKey("DaysOfWeek");
        capturedParams["DaysOfWeek"].Should().Be("2,4"); // Tuesday=2, Thursday=4
    }

    [Fact]
    public async Task CreateTaskAsync_WithStagedApproval_IncludesConfigJson()
    {
        // Arrange
        var task = CreateTestTask();
        task.StagedApprovalSettings = new StagedApprovalConfig
        {
            PromotionDelayDays = 14,
            RequireSuccessfulInstallations = true
        };

        Dictionary<string, object>? capturedParams = null;

        _powerShellMock.Setup(p => p.ExecuteScriptAsync(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Dictionary<string, object>?, CancellationToken>((_, p, _) => capturedParams = p)
            .ReturnsAsync([]);

        // Act
        await _sut.CreateTaskAsync(task);

        // Assert
        capturedParams.Should().NotBeNull();
        capturedParams!.Should().ContainKey("ConfigJson");
        var configJson = capturedParams["ConfigJson"].ToString();
        configJson.Should().Contain("14");
    }

    #endregion

    private static ScheduledWsusTask CreateTestTask()
    {
        return new ScheduledWsusTask
        {
            Name = "Test Task",
            WindowsTaskName = "WsusCommander_Test123",
            Description = "Test description",
            OperationType = ScheduledTaskOperationType.StagedApproval,
            IsEnabled = true,
            Schedule = new ScheduleConfig
            {
                Frequency = ScheduleFrequency.Weekly,
                TimeOfDay = new TimeSpan(3, 0, 0),
                DaysOfWeek = [DayOfWeek.Tuesday],
                StartDate = DateTime.Today
            },
            StagedApprovalSettings = new StagedApprovalConfig
            {
                PromotionDelayDays = 7
            }
        };
    }
}
