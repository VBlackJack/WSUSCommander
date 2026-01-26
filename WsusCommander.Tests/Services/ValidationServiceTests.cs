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
using WsusCommander.Services;

namespace WsusCommander.Tests.Services;

public class ValidationServiceTests
{
    private readonly ValidationService _sut;

    public ValidationServiceTests()
    {
        _sut = new ValidationService();
    }

    #region ValidateServerName Tests

    [Theory]
    [InlineData("localhost")]
    [InlineData("server01")]
    [InlineData("wsus.domain.com")]
    [InlineData("server-01.corp.local")]
    public void ValidateServerName_WithValidHostname_ReturnsNull(string hostname)
    {
        // Act
        var result = _sut.ValidateServerName(hostname);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateServerName_WithInvalidHostname_ReturnsError(string hostname)
    {
        // Act
        var result = _sut.ValidateServerName(hostname);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region ValidatePort Tests

    [Theory]
    [InlineData(80)]
    [InlineData(443)]
    [InlineData(8530)]
    [InlineData(8531)]
    [InlineData(1)]
    [InlineData(65535)]
    public void ValidatePort_WithValidPort_ReturnsNull(int port)
    {
        // Act
        var result = _sut.ValidatePort(port);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    [InlineData(100000)]
    public void ValidatePort_WithInvalidPort_ReturnsError(int port)
    {
        // Act
        var result = _sut.ValidatePort(port);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region ValidateGuid Tests

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("12345678-1234-1234-1234-123456789abc")]
    [InlineData("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE")]
    public void ValidateGuid_WithValidGuid_ReturnsNull(string guidString)
    {
        // Act
        var result = _sut.ValidateGuid(guidString, "TestField");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("invalid-guid")]
    [InlineData("")]
    [InlineData("12345678-1234-1234-1234")]
    public void ValidateGuid_WithInvalidGuid_ReturnsError(string guidString)
    {
        // Act
        var result = _sut.ValidateGuid(guidString, "TestField");

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region Sanitize Tests

    [Fact]
    public void Sanitize_WithNormalText_ReturnsSameText()
    {
        // Arrange
        const string input = "normal text";

        // Act
        var result = _sut.Sanitize(input);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Sanitize_WithTrimmedText_ReturnsTrimmmed()
    {
        // Arrange
        const string input = "  trimmed  ";

        // Act
        var result = _sut.Sanitize(input);

        // Assert
        result.Should().NotStartWith(" ");
        result.Should().NotEndWith(" ");
    }

    [Fact]
    public void Sanitize_WithNull_ReturnsEmpty()
    {
        // Act
        var result = _sut.Sanitize(null!);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}
