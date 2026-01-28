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
using WsusCommander.Properties;
using WsusCommander.Services;

namespace WsusCommander.Tests.Services;

public class CleanupServiceTests
{
    [Fact]
    public async Task RunCleanupAsync_PassesOptionsToPowerShell()
    {
        // Arrange
        var powerShellMock = new Mock<IPowerShellService>();
        var loggingMock = new Mock<ILoggingService>();
        var configMock = new Mock<IConfigurationService>();
        var capturedParameters = new Dictionary<string, object>();

        configMock.SetupGet(c => c.WsusConnection).Returns(new WsusConnectionConfig
        {
            ServerName = "wsus-test",
            Port = 8530,
            UseSsl = true
        });

        powerShellMock
            .Setup(ps => ps.ExecuteScriptAsync(
                "Invoke-WsusCleanup.ps1",
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Dictionary<string, object>?, CancellationToken>((_, parameters, _) =>
            {
                if (parameters != null)
                {
                    capturedParameters = new Dictionary<string, object>(parameters);
                }
            })
            .ReturnsAsync(new PSDataCollection<PSObject>());

        var service = new CleanupService(powerShellMock.Object, loggingMock.Object, configMock.Object);
        var options = new CleanupOptions
        {
            RemoveObsoleteUpdates = true,
            RemoveObsoleteComputers = false,
            RemoveExpiredUpdates = true,
            CompressUpdateRevisions = false,
            RemoveUnneededContent = true
        };

        // Act
        await service.RunCleanupAsync(options);

        // Assert
        capturedParameters["ServerName"].Should().Be("wsus-test");
        capturedParameters["Port"].Should().Be(8530);
        capturedParameters["UseSsl"].Should().Be(true);
        capturedParameters["RemoveObsoleteUpdates"].Should().Be(true);
        capturedParameters["RemoveObsoleteComputers"].Should().Be(false);
        capturedParameters["RemoveExpiredUpdates"].Should().Be(true);
        capturedParameters["CompressUpdateRevisions"].Should().Be(false);
        capturedParameters["RemoveUnneededContent"].Should().Be(true);
    }

    [Fact]
    public async Task RunCleanupAsync_LogsStartAndCompletion()
    {
        // Arrange
        var powerShellMock = new Mock<IPowerShellService>();
        var loggingMock = new Mock<ILoggingService>();
        var configMock = new Mock<IConfigurationService>();

        configMock.SetupGet(c => c.WsusConnection).Returns(new WsusConnectionConfig());
        powerShellMock
            .Setup(ps => ps.ExecuteScriptAsync(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PSDataCollection<PSObject>());

        var service = new CleanupService(powerShellMock.Object, loggingMock.Object, configMock.Object);

        // Act
        await service.RunCleanupAsync(new CleanupOptions());

        // Assert
        loggingMock.Verify(log => log.LogInfoAsync(Resources.LogCleanupStarted), Times.Once);
        loggingMock.Verify(log => log.LogInfoAsync(Resources.LogCleanupCompleted), Times.Once);
    }
}
