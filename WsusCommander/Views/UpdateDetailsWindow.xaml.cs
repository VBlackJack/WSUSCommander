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

using System.Windows;
using WsusCommander.Models;
using WsusCommander.Properties;

namespace WsusCommander.Views;

/// <summary>
/// Window for viewing detailed update information.
/// </summary>
public sealed partial class UpdateDetailsWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateDetailsWindow"/> class.
    /// </summary>
    /// <param name="update">Update to display.</param>
    public UpdateDetailsWindow(WsusUpdate update)
    {
        Update = update;
        WindowTitle = string.Format(Properties.Resources.UpdateDetailsWindowTitle, update.Title);
        ApprovalSummaries = [];
        ComputersNeedingUpdate = [];
        SupersedenceChain = [];
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>
    /// Gets the update model.
    /// </summary>
    public WsusUpdate Update { get; }

    /// <summary>
    /// Gets the window title.
    /// </summary>
    public string WindowTitle { get; }

    /// <summary>
    /// Gets the approval summary entries.
    /// </summary>
    public IReadOnlyList<string> ApprovalSummaries { get; }

    /// <summary>
    /// Gets the list of computers needing this update.
    /// </summary>
    public IReadOnlyList<string> ComputersNeedingUpdate { get; }

    /// <summary>
    /// Gets the supersedence chain list.
    /// </summary>
    public IReadOnlyList<string> SupersedenceChain { get; }

    /// <summary>
    /// Gets a value indicating whether approval data is available.
    /// </summary>
    public bool HasApprovals => ApprovalSummaries.Count > 0;

    /// <summary>
    /// Gets a value indicating whether computer data is available.
    /// </summary>
    public bool HasComputers => ComputersNeedingUpdate.Count > 0;

    /// <summary>
    /// Gets a value indicating whether supersedence data is available.
    /// </summary>
    public bool HasSupersedence => SupersedenceChain.Count > 0;

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
