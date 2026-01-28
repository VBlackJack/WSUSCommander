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
/// Represents a connection attempt result.
/// </summary>
public sealed class WsusConnectionResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the connection succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the server version string.
    /// </summary>
    public string ServerVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message if connection failed.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}
