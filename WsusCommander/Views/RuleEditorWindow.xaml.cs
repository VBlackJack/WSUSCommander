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

using System.Collections.ObjectModel;
using System.Windows;
using WsusCommander.Models;
using WsusCommander.Properties;

namespace WsusCommander.Views;

/// <summary>
/// Window for creating and editing approval rules.
/// </summary>
public sealed partial class RuleEditorWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuleEditorWindow"/> class.
    /// </summary>
    /// <param name="rule">Optional rule to edit.</param>
    public RuleEditorWindow(ApprovalRule? rule = null)
    {
        InitializeComponent();
        DataContext = new RuleEditorViewModel(rule ?? new ApprovalRule());
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private sealed class RuleEditorViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RuleEditorViewModel"/> class.
        /// </summary>
        /// <param name="rule">Rule to edit.</param>
        public RuleEditorViewModel(ApprovalRule rule)
        {
            Rule = rule;
            TargetGroups = new ObservableCollection<ComputerGroup>();
            SelectedConditionType = Resources.ConditionClassification;
        }

        /// <summary>
        /// Gets the rule being edited.
        /// </summary>
        public ApprovalRule Rule { get; }

        /// <summary>
        /// Gets the available target groups.
        /// </summary>
        public ObservableCollection<ComputerGroup> TargetGroups { get; }

        /// <summary>
        /// Gets or sets the selected target group.
        /// </summary>
        public ComputerGroup? SelectedGroup { get; set; }

        /// <summary>
        /// Gets or sets the selected condition type.
        /// </summary>
        public string SelectedConditionType { get; set; }

        /// <summary>
        /// Gets or sets the deadline offset in days.
        /// </summary>
        public int DeadlineOffsetDays { get; set; }
    }
}
