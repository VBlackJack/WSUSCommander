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
/// Represents a WSUS update retrieved from the server.
/// </summary>
public sealed class WsusUpdate
{
    /// <summary>
    /// Gets or sets the unique identifier of the update.
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the title of the update.
    /// </summary>
    [Required]
    [StringLength(512, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Knowledge Base article reference(s).
    /// </summary>
    [StringLength(256)]
    public string KbArticle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the classification of the update (e.g., Critical, Security).
    /// </summary>
    [StringLength(128)]
    public string Classification { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation date of the update.
    /// </summary>
    public DateTime CreationDate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the update is approved.
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the update is declined.
    /// </summary>
    public bool IsDeclined { get; set; }

    /// <summary>
    /// Gets or sets the description of the update.
    /// </summary>
    [StringLength(2048)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the product titles associated with this update.
    /// </summary>
    public List<string>? ProductTitles { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the update has been superseded.
    /// </summary>
    public bool IsSuperseded { get; set; }

    /// <summary>
    /// Gets or sets the KB articles of updates that supersede this one.
    /// </summary>
    [StringLength(512)]
    public string SupersededBy { get; set; } = string.Empty;
}
