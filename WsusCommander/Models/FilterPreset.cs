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
/// Represents a saved filter preset configuration.
/// </summary>
public sealed class FilterPreset
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the preset name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the search text filter.
    /// </summary>
    public string SearchText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the classification filter.
    /// </summary>
    public string Classification { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the approval status filter.
    /// </summary>
    public string ApprovalFilter { get; set; } = "All";

    /// <summary>
    /// Gets or sets whether this is a built-in preset.
    /// </summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
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
    public List<FilterPreset> Presets { get; set; } = [];
}
