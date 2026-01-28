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
/// Email alert evaluation context.
/// </summary>
public sealed class EmailAlertContext
{
    /// <summary>
    /// Gets or sets the age in days for pending critical updates.
    /// </summary>
    public int CriticalUpdatesPendingDays { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a sync failure occurred.
    /// </summary>
    public bool HasSyncFailure { get; set; }

    /// <summary>
    /// Gets or sets the current compliance percentage.
    /// </summary>
    public double CompliancePercent { get; set; }

    /// <summary>
    /// Gets or sets optional additional context for notifications.
    /// </summary>
    public string? Details { get; set; }
}
