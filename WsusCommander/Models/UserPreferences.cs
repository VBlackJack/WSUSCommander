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
/// User preferences model for persistence.
/// </summary>
public sealed class UserPreferences
{
    /// <summary>
    /// Gets or sets the window position X coordinate.
    /// </summary>
    public double? WindowLeft { get; set; }

    /// <summary>
    /// Gets or sets the window position Y coordinate.
    /// </summary>
    public double? WindowTop { get; set; }

    /// <summary>
    /// Gets or sets the window width.
    /// </summary>
    public double? WindowWidth { get; set; }

    /// <summary>
    /// Gets or sets the window height.
    /// </summary>
    public double? WindowHeight { get; set; }

    /// <summary>
    /// Gets or sets whether the window is maximized.
    /// </summary>
    public bool WindowMaximized { get; set; }

    /// <summary>
    /// Gets or sets the last selected computer group ID.
    /// </summary>
    public Guid? LastSelectedGroupId { get; set; }

    /// <summary>
    /// Gets or sets whether auto-refresh was enabled.
    /// </summary>
    public bool AutoRefreshEnabled { get; set; }

    /// <summary>
    /// Gets or sets the last selected tab index.
    /// </summary>
    public int LastSelectedTabIndex { get; set; }

    /// <summary>
    /// Gets or sets the updates grid column widths.
    /// </summary>
    public Dictionary<string, double> UpdatesColumnWidths { get; set; } = [];

    /// <summary>
    /// Gets or sets the computers grid column widths.
    /// </summary>
    public Dictionary<string, double> ComputersColumnWidths { get; set; } = [];

    /// <summary>
    /// Gets or sets the last export format used.
    /// </summary>
    public string LastExportFormat { get; set; } = "Csv";

    /// <summary>
    /// Gets or sets the last export directory.
    /// </summary>
    public string? LastExportDirectory { get; set; }

    /// <summary>
    /// Gets or sets the update filter settings.
    /// </summary>
    public UpdateFilterPreferences UpdateFilter { get; set; } = new();

    /// <summary>
    /// Gets or sets additional custom preferences.
    /// </summary>
    public Dictionary<string, object> Custom { get; set; } = [];
}

/// <summary>
/// Update filter preferences.
/// </summary>
public sealed class UpdateFilterPreferences
{
    /// <summary>
    /// Gets or sets the classification filter.
    /// </summary>
    public string? Classification { get; set; }

    /// <summary>
    /// Gets or sets the approval status filter.
    /// </summary>
    public string? ApprovalStatus { get; set; }

    /// <summary>
    /// Gets or sets the search text.
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Gets or sets whether to show only unapproved updates.
    /// </summary>
    public bool ShowOnlyUnapproved { get; set; }

    /// <summary>
    /// Gets or sets whether to hide declined updates.
    /// </summary>
    public bool HideDeclined { get; set; } = true;
}
