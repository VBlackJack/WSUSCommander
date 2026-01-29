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

public sealed class RetryServiceTests
{
    private readonly RetryService _sut;
    private readonly Mock<IConfigurationService> _configMock;
    private readonly Mock<ILoggingService> _loggingMock;

    public RetryServiceTests()
    {
        _configMock = new Mock<IConfigurationService>();
        _configMock.Setup(c => c.Config).Returns(CreateConfig());

        _loggingMock = new Mock<ILoggingService>();
        _loggingMock.Setup(l => l.LogInfoAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _loggingMock.Setup(l => l.LogWarningAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        _sut = new RetryService(_configMock.Object, _loggingMock.Object);
    }

    private static AppConfig CreateConfig()
    {
        return new AppConfig
        {
            Performance = new PerformanceConfig
            {
                MaxRetryAttempts = 3,
                InitialRetryDelayMs = 10, // Short delay for tests
                CircuitBreakerThreshold = 3,
                CircuitBreakerTimeoutSeconds = 60
            }
        };
    }

    #region ExecuteWithRetryAsync Success Tests

    [Fact]
    public async Task ExecuteWithRetryAsync_SuccessOnFirstAttempt_ReturnsResult()
    {
        // Arrange
        const string expected = "success";

        // Act
        var result = await _sut.ExecuteWithRetryAsync(
            _ => Task.FromResult(expected),
            "TestOperation");

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_SuccessAfterRetry_ReturnsResult()
    {
        // Arrange
        var attemptCount = 0;
        const string expected = "success";

        // Act
        var result = await _sut.ExecuteWithRetryAsync(
            _ =>
            {
                attemptCount++;
                if (attemptCount < 2)
                {
                    throw new InvalidOperationException("Transient failure");
                }
                return Task.FromResult(expected);
            },
            "TestOperation");

        // Assert
        result.Should().Be(expected);
        attemptCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_VoidOperation_SucceedsOnFirstAttempt()
    {
        // Arrange
        var executed = false;

        // Act
        await _sut.ExecuteWithRetryAsync(
            _ =>
            {
                executed = true;
                return Task.CompletedTask;
            },
            "TestOperation");

        // Assert
        executed.Should().BeTrue();
    }

    #endregion

    #region ExecuteWithRetryAsync Failure Tests

    [Fact]
    public async Task ExecuteWithRetryAsync_ExceedsMaxRetries_ThrowsWsusException()
    {
        // Arrange
        var attemptCount = 0;

        // Act
        var act = () => _sut.ExecuteWithRetryAsync<string>(
            _ =>
            {
                attemptCount++;
                throw new InvalidOperationException("Persistent failure");
            },
            "TestOperation");

        // Assert
        var exception = await act.Should().ThrowAsync<WsusException>();
        exception.Which.ErrorCode.Should().Be(WsusErrorCode.RetryLimitExceeded);
        attemptCount.Should().Be(4); // Initial + 3 retries
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_NonRetryableException_ThrowsImmediately()
    {
        // Arrange
        var attemptCount = 0;
        var nonRetryableException = new WsusException(
            WsusErrorCode.Unauthorized,
            "Access denied");

        // Act
        var act = () => _sut.ExecuteWithRetryAsync<string>(
            _ =>
            {
                attemptCount++;
                throw nonRetryableException;
            },
            "TestOperation");

        // Assert
        await act.Should().ThrowAsync<WsusException>()
            .Where(e => e.ErrorCode == WsusErrorCode.Unauthorized);
        attemptCount.Should().Be(1); // No retries for non-retryable
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_CancellationRequested_ThrowsOperationCancelledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => _sut.ExecuteWithRetryAsync(
            _ => Task.FromResult("result"),
            "TestOperation",
            cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Circuit Breaker Tests

    [Fact]
    public void GetCircuitState_NewOperation_ReturnsClosed()
    {
        // Act
        var state = _sut.GetCircuitState("NewOperation");

        // Assert
        state.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public async Task CircuitBreaker_AfterThresholdFailures_OpensCircuit()
    {
        // Arrange
        const string operationName = "FailingOperation";

        // Act - Trigger failures up to threshold
        for (var i = 0; i < 3; i++)
        {
            try
            {
                await _sut.ExecuteWithRetryAsync<string>(
                    _ => throw new InvalidOperationException("Failure"),
                    operationName);
            }
            catch (WsusException)
            {
                // Expected
            }
        }

        // Assert
        var state = _sut.GetCircuitState(operationName);
        state.Should().Be(CircuitState.Open);
    }

    [Fact]
    public async Task CircuitBreaker_WhenOpen_FailsFast()
    {
        // Arrange
        const string operationName = "OpenCircuitOperation";

        // Open the circuit by causing failures
        for (var i = 0; i < 3; i++)
        {
            try
            {
                await _sut.ExecuteWithRetryAsync<string>(
                    _ => throw new InvalidOperationException("Failure"),
                    operationName);
            }
            catch (WsusException)
            {
                // Expected
            }
        }

        var attemptCount = 0;

        // Act - Try to execute while circuit is open
        var act = () => _sut.ExecuteWithRetryAsync(
            _ =>
            {
                attemptCount++;
                return Task.FromResult("result");
            },
            operationName);

        // Assert
        var exception = await act.Should().ThrowAsync<WsusException>();
        exception.Which.ErrorCode.Should().Be(WsusErrorCode.ServerUnavailable);
        attemptCount.Should().Be(0); // Operation never attempted
    }

    [Fact]
    public async Task ResetCircuit_ClosesOpenCircuit()
    {
        // Arrange
        const string operationName = "CircuitToReset";

        // Force circuit open by triggering failures
        for (var i = 0; i < 3; i++)
        {
            try
            {
                await _sut.ExecuteWithRetryAsync<string>(
                    _ => throw new InvalidOperationException("Failure"),
                    operationName);
            }
            catch (WsusException)
            {
                // Expected
            }
        }

        // Act
        _sut.ResetCircuit(operationName);

        // Assert
        var state = _sut.GetCircuitState(operationName);
        state.Should().Be(CircuitState.Closed);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task ExecuteWithRetryAsync_OnRetry_LogsWarning()
    {
        // Arrange
        var attemptCount = 0;

        // Act
        await _sut.ExecuteWithRetryAsync(
            _ =>
            {
                attemptCount++;
                if (attemptCount < 2)
                {
                    throw new InvalidOperationException("Transient failure");
                }
                return Task.FromResult("success");
            },
            "TestOperation");

        // Assert
        _loggingMock.Verify(
            l => l.LogWarningAsync(It.Is<string>(msg => msg.Contains("TestOperation") && msg.Contains("failed"))),
            Times.Once);
    }

    #endregion
}
