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

public class AuthorizationServiceTests
{
    private readonly Mock<IAuthenticationService> _authMock;
    private readonly Mock<ILoggingService> _loggingMock;

    public AuthorizationServiceTests()
    {
        _authMock = new Mock<IAuthenticationService>();
        _loggingMock = new Mock<ILoggingService>();
    }

    private AuthorizationService CreateService()
    {
        return new AuthorizationService(_authMock.Object, _loggingMock.Object);
    }

    #region Administrator Role Tests

    [Fact]
    public void IsAuthorized_Administrator_CanPerformAllOperations()
    {
        // Arrange
        _authMock.Setup(a => a.IsAuthenticated).Returns(true);
        _authMock.Setup(a => a.CurrentUser).Returns(new UserIdentity
        {
            AccountName = "admin",
            Role = UserRole.Administrator
        });

        var sut = CreateService();

        // Act & Assert
        foreach (var operation in Enum.GetValues<WsusOperation>())
        {
            sut.IsAuthorized(operation).Should().BeTrue($"Administrator should be able to {operation}");
        }
    }

    #endregion

    #region Operator Role Tests

    [Theory]
    [InlineData(WsusOperation.ViewUpdates, true)]
    [InlineData(WsusOperation.ViewComputers, true)]
    [InlineData(WsusOperation.ApproveUpdate, true)]
    [InlineData(WsusOperation.DeclineUpdate, true)]
    [InlineData(WsusOperation.StartSync, true)]
    [InlineData(WsusOperation.ExportData, true)]
    [InlineData(WsusOperation.ViewReports, true)]
    [InlineData(WsusOperation.ManageGroups, false)]
    [InlineData(WsusOperation.ConfigureServer, false)]
    public void IsAuthorized_Operator_HasLimitedPermissions(WsusOperation operation, bool expected)
    {
        // Arrange
        _authMock.Setup(a => a.IsAuthenticated).Returns(true);
        _authMock.Setup(a => a.CurrentUser).Returns(new UserIdentity
        {
            AccountName = "operator",
            Role = UserRole.Operator
        });

        var sut = CreateService();

        // Act
        var result = sut.IsAuthorized(operation);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Viewer Role Tests

    [Theory]
    [InlineData(WsusOperation.ViewUpdates, true)]
    [InlineData(WsusOperation.ViewComputers, true)]
    [InlineData(WsusOperation.ViewReports, true)]
    [InlineData(WsusOperation.ExportData, false)]
    [InlineData(WsusOperation.ApproveUpdate, false)]
    [InlineData(WsusOperation.DeclineUpdate, false)]
    [InlineData(WsusOperation.StartSync, false)]
    [InlineData(WsusOperation.ManageGroups, false)]
    [InlineData(WsusOperation.ConfigureServer, false)]
    public void IsAuthorized_Viewer_CanOnlyView(WsusOperation operation, bool expected)
    {
        // Arrange
        _authMock.Setup(a => a.IsAuthenticated).Returns(true);
        _authMock.Setup(a => a.CurrentUser).Returns(new UserIdentity
        {
            AccountName = "viewer",
            Role = UserRole.Viewer
        });

        var sut = CreateService();

        // Act
        var result = sut.IsAuthorized(operation);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Unauthenticated Tests

    [Fact]
    public void IsAuthorized_WhenNotAuthenticated_ReturnsFalseForAllOperations()
    {
        // Arrange
        _authMock.Setup(a => a.IsAuthenticated).Returns(false);
        _authMock.Setup(a => a.CurrentUser).Returns((UserIdentity?)null);

        var sut = CreateService();

        // Act & Assert
        foreach (var operation in Enum.GetValues<WsusOperation>())
        {
            sut.IsAuthorized(operation).Should().BeFalse($"Unauthenticated user should not be able to {operation}");
        }
    }

    #endregion

    #region EnsureAuthorized Tests

    [Fact]
    public void EnsureAuthorized_WhenAuthorized_DoesNotThrow()
    {
        // Arrange
        _authMock.Setup(a => a.IsAuthenticated).Returns(true);
        _authMock.Setup(a => a.CurrentUser).Returns(new UserIdentity
        {
            AccountName = "admin",
            Role = UserRole.Administrator
        });

        var sut = CreateService();

        // Act
        var act = () => sut.EnsureAuthorized(WsusOperation.ApproveUpdate);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureAuthorized_WhenNotAuthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _authMock.Setup(a => a.IsAuthenticated).Returns(true);
        _authMock.Setup(a => a.CurrentUser).Returns(new UserIdentity
        {
            AccountName = "viewer",
            Role = UserRole.Viewer
        });

        var sut = CreateService();

        // Act
        var act = () => sut.EnsureAuthorized(WsusOperation.ApproveUpdate);

        // Assert
        act.Should().Throw<UnauthorizedAccessException>();
    }

    #endregion

    #region GetAuthorizedOperations Tests

    [Fact]
    public void GetAuthorizedOperations_Administrator_ReturnsAllOperations()
    {
        // Arrange
        _authMock.Setup(a => a.IsAuthenticated).Returns(true);
        _authMock.Setup(a => a.CurrentUser).Returns(new UserIdentity
        {
            AccountName = "admin",
            Role = UserRole.Administrator
        });

        var sut = CreateService();

        // Act
        var result = sut.GetAuthorizedOperations().ToList();

        // Assert
        result.Should().HaveCount(Enum.GetValues<WsusOperation>().Length);
    }

    [Fact]
    public void GetAuthorizedOperations_Viewer_ReturnsLimitedOperations()
    {
        // Arrange
        _authMock.Setup(a => a.IsAuthenticated).Returns(true);
        _authMock.Setup(a => a.CurrentUser).Returns(new UserIdentity
        {
            AccountName = "viewer",
            Role = UserRole.Viewer
        });

        var sut = CreateService();

        // Act
        var result = sut.GetAuthorizedOperations().ToList();

        // Assert
        result.Should().Contain(WsusOperation.ViewUpdates);
        result.Should().Contain(WsusOperation.ViewComputers);
        result.Should().Contain(WsusOperation.ViewReports);
        result.Should().NotContain(WsusOperation.ApproveUpdate);
        result.Should().NotContain(WsusOperation.DeclineUpdate);
    }

    #endregion
}
