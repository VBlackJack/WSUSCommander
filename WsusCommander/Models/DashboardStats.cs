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
/// Represents dashboard statistics for the WSUS server.
/// </summary>
public sealed class DashboardStats
{
    /// <summary>
    /// Gets or sets the total number of updates (non-declined).
    /// </summary>
    public int TotalUpdates { get; set; }

    /// <summary>
    /// Gets or sets the number of unapproved updates.
    /// </summary>
    public int UnapprovedUpdates { get; set; }

    /// <summary>
    /// Gets or sets the number of superseded updates (not declined).
    /// </summary>
    public int SupersededUpdates { get; set; }

    /// <summary>
    /// Gets or sets the number of critical updates pending approval.
    /// </summary>
    public int CriticalPending { get; set; }

    /// <summary>
    /// Gets or sets the number of security updates pending approval.
    /// </summary>
    public int SecurityPending { get; set; }

    /// <summary>
    /// Gets or sets the total number of computers.
    /// </summary>
    public int TotalComputers { get; set; }

    /// <summary>
    /// Gets or sets the number of computers needing updates.
    /// </summary>
    public int ComputersNeedingUpdates { get; set; }

    /// <summary>
    /// Gets or sets the number of computers up to date.
    /// </summary>
    public int ComputersUpToDate { get; set; }

    /// <summary>
    /// Gets or sets the compliance percentage.
    /// </summary>
    public double CompliancePercent { get; set; }

    /// <summary>
    /// Gets or sets the last synchronization time.
    /// </summary>
    public DateTime? LastSyncTime { get; set; }

    /// <summary>
    /// Gets whether there are pending critical updates that need attention.
    /// </summary>
    public bool HasCriticalPending => CriticalPending > 0;

    /// <summary>
    /// Gets whether there are superseded updates that should be declined.
    /// </summary>
    public bool HasSupersededToClean => SupersededUpdates > 0;
}
