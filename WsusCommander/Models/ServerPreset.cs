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
/// Represents a saved WSUS server preset.
/// </summary>
public sealed class ServerPreset
{
    /// <summary>
    /// Gets or sets the display name for the preset.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the server name.
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the server port.
    /// </summary>
    public int Port { get; set; } = 8530;

    /// <summary>
    /// Gets or sets a value indicating whether SSL is used.
    /// </summary>
    public bool UseSsl { get; set; }
}
