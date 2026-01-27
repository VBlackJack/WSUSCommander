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
/// Represents the update status of a computer managed by WSUS.
/// </summary>
public sealed class ComputerStatus
{
    /// <summary>
    /// Gets or sets the computer identifier.
    /// </summary>
    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string ComputerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the computer name.
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the IP address of the computer.
    /// </summary>
    [StringLength(64)]
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last time the computer reported to WSUS.
    /// </summary>
    public DateTime? LastReportedTime { get; set; }

    /// <summary>
    /// Gets or sets the count of installed updates.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int InstalledCount { get; set; }

    /// <summary>
    /// Gets or sets the count of updates needed (pending installation).
    /// </summary>
    [Range(0, int.MaxValue)]
    public int NeededCount { get; set; }

    /// <summary>
    /// Gets or sets the count of failed update installations.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int FailedCount { get; set; }

    /// <summary>
    /// Gets or sets the computer group name.
    /// </summary>
    [StringLength(256)]
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of group IDs the computer belongs to.
    /// </summary>
    public List<Guid>? GroupIds { get; set; }
}
