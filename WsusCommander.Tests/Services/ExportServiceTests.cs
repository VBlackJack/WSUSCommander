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
using WsusCommander.Properties;
using WsusCommander.Services;

namespace WsusCommander.Tests.Services;

public class ExportServiceTests : IDisposable
{
    private readonly ExportService _sut;
    private readonly Mock<ILoggingService> _loggingMock;
    private readonly string _tempDirectory;

    public ExportServiceTests()
    {
        _loggingMock = new Mock<ILoggingService>();
        _sut = new ExportService(_loggingMock.Object);
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"WsusCommanderTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
        GC.SuppressFinalize(this);
    }

    private static List<WsusUpdate> CreateTestUpdates()
    {
        return
        [
            new WsusUpdate
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Title = "Security Update",
                KbArticle = "KB123456",
                Classification = "Security Updates",
                IsApproved = true,
                IsDeclined = false,
                CreationDate = new DateTime(2025, 1, 15)
            },
            new WsusUpdate
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Title = "Critical Update",
                KbArticle = "KB789012",
                Classification = "Critical Updates",
                IsApproved = false,
                IsDeclined = false,
                CreationDate = new DateTime(2025, 1, 20)
            }
        ];
    }

    #region GetFileFilter Tests

    [Theory]
    [InlineData(ExportFormat.Csv, "*.csv")]
    [InlineData(ExportFormat.Tsv, "*.tsv")]
    [InlineData(ExportFormat.Json, "*.json")]
    public void GetFileFilter_ReturnsCorrectFilter(ExportFormat format, string expectedExtension)
    {
        // Act
        var result = _sut.GetFileFilter(format);

        // Assert
        result.Should().Contain(expectedExtension);
    }

    #endregion

    #region GetFileExtension Tests

    [Theory]
    [InlineData(ExportFormat.Csv, ".csv")]
    [InlineData(ExportFormat.Tsv, ".tsv")]
    [InlineData(ExportFormat.Json, ".json")]
    public void GetFileExtension_ReturnsCorrectExtension(ExportFormat format, string expected)
    {
        // Act
        var result = _sut.GetFileExtension(format);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region ExportUpdatesAsync Tests

    [Fact]
    public async Task ExportUpdatesAsync_CsvFormat_CreatesFile()
    {
        // Arrange
        var updates = CreateTestUpdates();
        var filePath = Path.Combine(_tempDirectory, "updates.csv");

        // Act
        await _sut.ExportUpdatesAsync(updates, filePath, ExportFormat.Csv);

        // Assert
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public async Task ExportUpdatesAsync_CsvFormat_ContainsHeaders()
    {
        // Arrange
        var updates = CreateTestUpdates();
        var filePath = Path.Combine(_tempDirectory, "updates.csv");

        // Act
        await _sut.ExportUpdatesAsync(updates, filePath, ExportFormat.Csv);
        var content = await File.ReadAllTextAsync(filePath);

        // Assert
        content.Should().Contain(Resources.ExportHeaderUpdateTitle);
    }

    [Fact]
    public async Task ExportUpdatesAsync_CsvFormat_ContainsData()
    {
        // Arrange
        var updates = CreateTestUpdates();
        var filePath = Path.Combine(_tempDirectory, "updates.csv");

        // Act
        await _sut.ExportUpdatesAsync(updates, filePath, ExportFormat.Csv);
        var content = await File.ReadAllTextAsync(filePath);

        // Assert
        content.Should().Contain("Security Update");
        content.Should().Contain("KB123456");
    }

    [Fact]
    public async Task ExportUpdatesAsync_TsvFormat_UsesTabs()
    {
        // Arrange
        var updates = CreateTestUpdates();
        var filePath = Path.Combine(_tempDirectory, "updates.tsv");

        // Act
        await _sut.ExportUpdatesAsync(updates, filePath, ExportFormat.Tsv);
        var content = await File.ReadAllTextAsync(filePath);

        // Assert
        content.Should().Contain("\t");
    }

    [Fact]
    public async Task ExportUpdatesAsync_JsonFormat_CreatesValidJson()
    {
        // Arrange
        var updates = CreateTestUpdates();
        var filePath = Path.Combine(_tempDirectory, "updates.json");

        // Act
        await _sut.ExportUpdatesAsync(updates, filePath, ExportFormat.Json);
        var content = await File.ReadAllTextAsync(filePath);

        // Assert
        content.Should().StartWith("[");
        content.Should().Contain("Title");
    }

    #endregion

    #region ExportComputersAsync Tests

    [Fact]
    public async Task ExportComputersAsync_CsvFormat_CreatesFile()
    {
        // Arrange
        var computers = new List<ComputerStatus>
        {
            new()
            {
                ComputerId = "PC001",
                Name = "WORKSTATION-01",
                IpAddress = "192.168.1.100",
                GroupName = "All Computers",
                InstalledCount = 50,
                NeededCount = 5,
                FailedCount = 0
            }
        };
        var filePath = Path.Combine(_tempDirectory, "computers.csv");

        // Act
        await _sut.ExportComputersAsync(computers, filePath, ExportFormat.Csv);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain("WORKSTATION-01");
    }

    #endregion

    #region ExportGroupsAsync Tests

    [Fact]
    public async Task ExportGroupsAsync_CsvFormat_CreatesFile()
    {
        // Arrange
        var groups = new List<ComputerGroup>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Production Servers",
                Description = "All production servers",
                ComputerCount = 25
            }
        };
        var filePath = Path.Combine(_tempDirectory, "groups.csv");

        // Act
        await _sut.ExportGroupsAsync(groups, filePath, ExportFormat.Csv);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain("Production Servers");
    }

    #endregion
}
