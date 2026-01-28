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
/// Scheduler window for recurring operations.
/// </summary>
public sealed partial class SchedulerWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulerWindow"/> class.
    /// </summary>
    public SchedulerWindow()
    {
        InitializeComponent();
        DataContext = new SchedulerWindowViewModel();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private sealed class SchedulerWindowViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SchedulerWindowViewModel"/> class.
        /// </summary>
        public SchedulerWindowViewModel()
        {
            Tasks = new ObservableCollection<ScheduledTask>();
            SelectedTask = new ScheduledTask();
            SelectedTaskType = Properties.Resources.SchedulerTaskApproval;
            SelectedRecurrence = Properties.Resources.SchedulerRecurrenceOnce;
        }

        /// <summary>
        /// Gets the scheduled tasks.
        /// </summary>
        public ObservableCollection<ScheduledTask> Tasks { get; }

        /// <summary>
        /// Gets or sets the selected task.
        /// </summary>
        public ScheduledTask SelectedTask { get; set; }

        /// <summary>
        /// Gets or sets the selected task type label.
        /// </summary>
        public string SelectedTaskType { get; set; }

        /// <summary>
        /// Gets or sets the selected recurrence label.
        /// </summary>
        public string SelectedRecurrence { get; set; }

        /// <summary>
        /// Gets or sets the interval minutes value.
        /// </summary>
        public int IntervalMinutes { get; set; }
    }
}
