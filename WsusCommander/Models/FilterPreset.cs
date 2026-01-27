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
/// Represents a saved filter preset configuration.
/// </summary>
public sealed class FilterPreset
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the preset name.
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the search text filter.
    /// </summary>
    [StringLength(256)]
    public string SearchText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the classification filter.
    /// </summary>
    [StringLength(128)]
    public string Classification { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the approval status filter.
    /// </summary>
    [StringLength(64)]
    public string ApprovalFilter { get; set; } = "All";

    /// <summary>
    /// Gets or sets whether this is a built-in preset.
    /// </summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Collection of filter presets for serialization.
/// </summary>
public sealed class FilterPresetsCollection
{
    /// <summary>
    /// Gets or sets the list of presets.
    /// </summary>
    [Required]
    public List<FilterPreset> Presets { get; set; } = [];
}
