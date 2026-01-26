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

namespace WsusCommander.Services;

/// <summary>
/// Retry service with exponential backoff and circuit breaker pattern.
/// </summary>
public sealed class RetryService : IRetryService
{
    private readonly IConfigurationService _configService;
    private readonly ILoggingService _loggingService;
    private readonly ConcurrentDictionary<string, CircuitBreakerState> _circuits = new();

    private const int CircuitBreakerThreshold = 5;
    private static readonly TimeSpan CircuitBreakerTimeout = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryService"/> class.
    /// </summary>
    public RetryService(IConfigurationService configService, ILoggingService loggingService)
    {
        _configService = configService;
        _loggingService = loggingService;
    }

    /// <inheritdoc/>
    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var circuit = GetOrCreateCircuit(operationName);

        if (circuit.State == CircuitState.Open)
        {
            if (DateTime.UtcNow - circuit.LastFailure < CircuitBreakerTimeout)
            {
                throw new WsusException(
                    WsusErrorCode.ServerUnavailable,
                    $"Circuit breaker is open for operation '{operationName}'. Retry after {CircuitBreakerTimeout.TotalSeconds}s.");
            }

            circuit.State = CircuitState.HalfOpen;
            await _loggingService.LogInfoAsync($"Circuit half-open for '{operationName}', attempting recovery.");
        }

        var maxRetries = _configService.Config.Performance.MaxRetryAttempts;
        var initialDelay = _configService.Config.Performance.InitialRetryDelayMs;
        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= maxRetries)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await operation(cancellationToken);

                // Success - reset circuit breaker
                if (circuit.State == CircuitState.HalfOpen)
                {
                    circuit.State = CircuitState.Closed;
                    circuit.FailureCount = 0;
                    await _loggingService.LogInfoAsync($"Circuit closed for '{operationName}' after successful recovery.");
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (WsusException ex) when (!ex.IsRetryable)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                attempt++;

                if (attempt > maxRetries)
                {
                    RecordFailure(circuit, operationName);
                    break;
                }

                var delay = CalculateDelay(initialDelay, attempt);
                await _loggingService.LogWarningAsync(
                    $"Operation '{operationName}' failed (attempt {attempt}/{maxRetries}). Retrying in {delay}ms. Error: {ex.Message}");

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new WsusException(
            WsusErrorCode.RetryLimitExceeded,
            $"Operation '{operationName}' failed after {maxRetries} attempts.",
            lastException!);
    }

    /// <inheritdoc/>
    public async Task ExecuteWithRetryAsync(
        Func<CancellationToken, Task> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        await ExecuteWithRetryAsync(
            async ct =>
            {
                await operation(ct);
                return true;
            },
            operationName,
            cancellationToken);
    }

    /// <inheritdoc/>
    public CircuitState GetCircuitState(string operationName)
    {
        return GetOrCreateCircuit(operationName).State;
    }

    /// <inheritdoc/>
    public void ResetCircuit(string operationName)
    {
        if (_circuits.TryGetValue(operationName, out var circuit))
        {
            circuit.State = CircuitState.Closed;
            circuit.FailureCount = 0;
            _loggingService.LogInfoAsync($"Circuit manually reset for '{operationName}'.");
        }
    }

    private CircuitBreakerState GetOrCreateCircuit(string operationName)
    {
        return _circuits.GetOrAdd(operationName, _ => new CircuitBreakerState());
    }

    private void RecordFailure(CircuitBreakerState circuit, string operationName)
    {
        circuit.FailureCount++;
        circuit.LastFailure = DateTime.UtcNow;

        if (circuit.FailureCount >= CircuitBreakerThreshold)
        {
            circuit.State = CircuitState.Open;
            _loggingService.LogWarningAsync($"Circuit opened for '{operationName}' after {circuit.FailureCount} failures.");
        }
    }

    private static int CalculateDelay(int initialDelay, int attempt)
    {
        // Exponential backoff with jitter
        var exponentialDelay = initialDelay * Math.Pow(2, attempt - 1);
        var jitter = Random.Shared.Next(0, (int)(exponentialDelay * 0.1));
        return (int)Math.Min(exponentialDelay + jitter, 30000); // Cap at 30 seconds
    }

    private sealed class CircuitBreakerState
    {
        public CircuitState State { get; set; } = CircuitState.Closed;
        public int FailureCount { get; set; }
        public DateTime LastFailure { get; set; }
    }
}
