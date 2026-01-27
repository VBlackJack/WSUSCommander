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
/// Represents an entry in the WSUS activity log.
/// </summary>
public sealed class ActivityLogEntry
{
    /// <summary>
    /// Gets or sets the timestamp of the activity.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the type of activity.
    /// </summary>
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the activity.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user who performed the activity.
    /// </summary>
    public string User { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target of the activity (update title, computer name, etc.).
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status of the activity.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets the icon for the activity type.
    /// </summary>
    public string Icon => ActivityType switch
    {
        "Approval" => "\u2714",      // Checkmark
        "Decline" => "\u2718",        // X mark
        "Sync" => "\u21BB",           // Clockwise arrow
        "Download" => "\u2193",       // Down arrow
        "Error" => "\u26A0",          // Warning sign
        _ => "\u2022"                 // Bullet
    };
}
