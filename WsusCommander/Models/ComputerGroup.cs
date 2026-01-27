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
/// Represents a WSUS computer target group.
/// </summary>
public sealed class ComputerGroup
{
    /// <summary>
    /// Gets or sets the unique identifier of the computer group.
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the computer group.
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the computer group.
    /// </summary>
    [StringLength(1024)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of computers in the group.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int ComputerCount { get; set; }

    /// <summary>
    /// Gets or sets the parent group ID for hierarchical group structures.
    /// </summary>
    public Guid? ParentGroupId { get; set; }
}
