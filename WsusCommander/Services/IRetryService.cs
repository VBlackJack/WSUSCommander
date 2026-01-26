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

namespace WsusCommander.Services;

/// <summary>
/// Interface for retry service with exponential backoff and circuit breaker.
/// </summary>
public interface IRetryService
{
    /// <summary>
    /// Executes an operation with retry logic.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="operationName">Name of the operation for logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an operation with retry logic (void return).
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="operationName">Name of the operation for logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExecuteWithRetryAsync(
        Func<CancellationToken, Task> operation,
        string operationName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current circuit breaker state.
    /// </summary>
    /// <param name="operationName">Name of the operation.</param>
    /// <returns>The circuit state.</returns>
    CircuitState GetCircuitState(string operationName);

    /// <summary>
    /// Resets the circuit breaker for an operation.
    /// </summary>
    /// <param name="operationName">Name of the operation.</param>
    void ResetCircuit(string operationName);
}

/// <summary>
/// Circuit breaker states.
/// </summary>
public enum CircuitState
{
    /// <summary>Circuit is closed, operations execute normally.</summary>
    Closed,

    /// <summary>Circuit is open, operations fail fast.</summary>
    Open,

    /// <summary>Circuit is half-open, testing if service recovered.</summary>
    HalfOpen
}
