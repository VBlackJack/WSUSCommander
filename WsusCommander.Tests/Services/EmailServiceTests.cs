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

using System;
using System.IO;
using System.Net.Mail;
using FluentAssertions;
using Moq;
using WsusCommander.Models;
using WsusCommander.Properties;
using WsusCommander.Services;

namespace WsusCommander.Tests.Services;

public class EmailServiceTests : IDisposable
{
    private readonly string _pickupDirectory;

    public EmailServiceTests()
    {
        _pickupDirectory = Path.Combine(Path.GetTempPath(), $"WsusCommanderEmail_{Guid.NewGuid()}");
        Directory.CreateDirectory(_pickupDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_pickupDirectory))
        {
            Directory.Delete(_pickupDirectory, true);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SendAlertAsync_WithMissingConfig_LogsWarning()
    {
        // Arrange
        var configMock = new Mock<IConfigurationService>();
        var loggingMock = new Mock<ILoggingService>();

        configMock.SetupGet(c => c.Email).Returns(new EmailConfig
        {
            Enabled = true,
            SmtpServer = string.Empty,
            FromAddress = string.Empty
        });

        var service = new EmailService(configMock.Object, loggingMock.Object, new TestSmtpClientFactory(_pickupDirectory));

        // Act
        await service.SendAlertAsync("subject", "body");

        // Assert
        loggingMock.Verify(log => log.LogWarningAsync(Resources.EmailMissingConfiguration), Times.Once);
    }

    [Fact]
    public async Task SendAlertAsync_WithValidConfig_SendsMail()
    {
        // Arrange
        var configMock = new Mock<IConfigurationService>();
        var loggingMock = new Mock<ILoggingService>();

        configMock.SetupGet(c => c.Email).Returns(new EmailConfig
        {
            Enabled = true,
            SmtpServer = "localhost",
            SmtpPort = 25,
            UseSsl = false,
            FromAddress = "wsus@example.com",
            ToAddresses = ["admin@example.com"]
        });

        var service = new EmailService(configMock.Object, loggingMock.Object, new TestSmtpClientFactory(_pickupDirectory));

        // Act
        await service.SendAlertAsync("Test Alert", "Body");

        // Assert
        Directory.GetFiles(_pickupDirectory).Should().NotBeEmpty();
        loggingMock.Verify(log => log.LogInfoAsync(It.Is<string>(value => value.Contains("Test Alert"))), Times.Once);
    }

    private sealed class TestSmtpClientFactory : ISmtpClientFactory
    {
        private readonly string _pickupDirectory;

        public TestSmtpClientFactory(string pickupDirectory)
        {
            _pickupDirectory = pickupDirectory;
        }

        public SmtpClient Create(string server, int port, bool useSsl)
        {
            return new SmtpClient(server, port)
            {
                EnableSsl = useSsl,
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = _pickupDirectory
            };
        }
    }
}
