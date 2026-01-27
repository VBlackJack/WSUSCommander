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

namespace WsusCommander.Models;

/// <summary>
/// Type of toast notification.
/// </summary>
public enum ToastType
{
    /// <summary>Informational message.</summary>
    Info,

    /// <summary>Success message.</summary>
    Success,

    /// <summary>Warning message.</summary>
    Warning,

    /// <summary>Error message.</summary>
    Error
}

/// <summary>
/// Represents a toast notification message.
/// </summary>
public sealed class ToastNotification
{
    /// <summary>
    /// Gets or sets the notification message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification type.
    /// </summary>
    public ToastType Type { get; init; } = ToastType.Info;

    /// <summary>
    /// Gets or sets the duration in milliseconds.
    /// </summary>
    public int Duration { get; init; } = 3000;

    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
}
