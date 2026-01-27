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

using System.ComponentModel.DataAnnotations;

namespace WsusCommander.Models;

/// <summary>
/// Represents the WSUS server synchronization status.
/// </summary>
public sealed class SyncStatus
{
    /// <summary>
    /// Gets or sets the current synchronization status description.
    /// </summary>
    [Required]
    [StringLength(256)]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last synchronization time.
    /// </summary>
    public DateTime? LastSyncTime { get; set; }

    /// <summary>
    /// Gets or sets the next scheduled synchronization time.
    /// </summary>
    public DateTime? NextSyncTime { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a synchronization is currently in progress.
    /// </summary>
    public bool IsSyncing { get; set; }

    /// <summary>
    /// Gets or sets the result of the last synchronization.
    /// </summary>
    [StringLength(256)]
    public string LastSyncResult { get; set; } = string.Empty;
}
