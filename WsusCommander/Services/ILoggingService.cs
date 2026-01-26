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
/// Interface for the application logging service.
/// </summary>
public interface ILoggingService
{
    /// <summary>
    /// Logs an informational message asynchronously.
    /// </summary>
    /// <param name="message">The message to log.</param>
    Task LogInfoAsync(string message);

    /// <summary>
    /// Logs a warning message asynchronously.
    /// </summary>
    /// <param name="message">The message to log.</param>
    Task LogWarningAsync(string message);

    /// <summary>
    /// Logs an error message asynchronously.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">Optional exception to include in the log.</param>
    Task LogErrorAsync(string message, Exception? exception = null);

    /// <summary>
    /// Logs a debug message asynchronously.
    /// </summary>
    /// <param name="message">The message to log.</param>
    Task LogDebugAsync(string message);
}
