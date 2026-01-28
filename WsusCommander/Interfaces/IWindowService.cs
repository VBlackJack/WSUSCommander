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

using WsusCommander.Models;

namespace WsusCommander.Interfaces;

/// <summary>
/// Provides window navigation for specialized dialogs.
/// </summary>
public interface IWindowService
{
    /// <summary>
    /// Shows the updates for a specific computer.
    /// </summary>
    /// <param name="computer">Target computer.</param>
    /// <param name="updates">Updates associated with the computer.</param>
    void ShowComputerUpdates(ComputerStatus computer, IReadOnlyList<ComputerUpdateStatus> updates);

    /// <summary>
    /// Shows the update details view.
    /// </summary>
    /// <param name="update">Update to display.</param>
    void ShowUpdateDetails(WsusUpdate update);

    /// <summary>
    /// Shows the group editor dialog.
    /// </summary>
    /// <param name="group">Group to edit or null to create.</param>
    void ShowGroupEditor(ComputerGroup? group = null);

    /// <summary>
    /// Shows the rule editor dialog.
    /// </summary>
    /// <param name="rule">Rule to edit or null to create.</param>
    void ShowRuleEditor(ApprovalRule? rule = null);

    /// <summary>
    /// Shows the settings window.
    /// </summary>
    void ShowSettings();

    /// <summary>
    /// Shows the scheduler window.
    /// </summary>
    void ShowScheduler();

    /// <summary>
    /// Shows the cleanup window.
    /// </summary>
    void ShowCleanup();

    /// <summary>
    /// Shows the about dialog.
    /// </summary>
    void ShowAbout();
}
