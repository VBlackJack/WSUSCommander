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

using System.Windows.Input;

namespace WsusCommander.Models;

/// <summary>
/// Represents a dashboard action item.
/// </summary>
public sealed class ActionItem
{
    /// <summary>
    /// Gets or sets the priority for the action item.
    /// </summary>
    public ActionPriority Priority { get; set; }

    /// <summary>
    /// Gets or sets the action title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command to execute.
    /// </summary>
    public ICommand? ActionCommand { get; set; }
}

/// <summary>
/// Represents an action priority level.
/// </summary>
public enum ActionPriority
{
    /// <summary>
    /// Low priority action.
    /// </summary>
    Low,

    /// <summary>
    /// Medium priority action.
    /// </summary>
    Medium,

    /// <summary>
    /// High priority action.
    /// </summary>
    High,

    /// <summary>
    /// Critical priority action.
    /// </summary>
    Critical
}
