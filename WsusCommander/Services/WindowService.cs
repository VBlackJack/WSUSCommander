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

using System.Linq;
using System.Windows;
using WsusCommander.Interfaces;
using WsusCommander.Models;
using WsusCommander.ViewModels;
using WsusCommander.Views;

namespace WsusCommander.Services;

/// <summary>
/// WPF implementation of window navigation.
/// </summary>
public sealed class WindowService : IWindowService
{
    private readonly SettingsViewModel _settingsViewModel;
    private readonly ICleanupService _cleanupService;
    private readonly IScheduledTasksService _scheduledTasksService;
    private readonly ITaskSchedulerService _taskSchedulerService;
    private readonly IGroupService _groupService;
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowService"/> class.
    /// </summary>
    public WindowService(
        SettingsViewModel settingsViewModel,
        ICleanupService cleanupService,
        IScheduledTasksService scheduledTasksService,
        ITaskSchedulerService taskSchedulerService,
        IGroupService groupService,
        IDialogService dialogService,
        INotificationService notificationService,
        ILoggingService loggingService)
    {
        _settingsViewModel = settingsViewModel;
        _cleanupService = cleanupService;
        _scheduledTasksService = scheduledTasksService;
        _taskSchedulerService = taskSchedulerService;
        _groupService = groupService;
        _dialogService = dialogService;
        _notificationService = notificationService;
        _loggingService = loggingService;
    }

    /// <inheritdoc/>
    public void ShowComputerUpdates(ComputerStatus computer, IReadOnlyList<ComputerUpdateStatus> updates)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var window = new ComputerUpdatesWindow(computer, updates.ToList())
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        });
    }

    /// <inheritdoc/>
    public void ShowUpdateDetails(WsusUpdate update)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var window = new UpdateDetailsWindow(update)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        });
    }

    /// <inheritdoc/>
    public void ShowGroupEditor(ComputerGroup? group = null)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var window = new GroupEditorWindow(group)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        });
    }

    /// <inheritdoc/>
    public void ShowRuleEditor(ApprovalRule? rule = null)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var window = new RuleEditorWindow(rule)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        });
    }

    /// <inheritdoc/>
    public void ShowSettings()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var window = new SettingsWindow(_settingsViewModel)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        });
    }

    /// <inheritdoc/>
    public void ShowScheduler()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var window = new SchedulerWindow
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        });
    }

    /// <inheritdoc/>
    public void ShowScheduledTasks()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var window = new ScheduledTasksWindow(
                _scheduledTasksService,
                _taskSchedulerService,
                _groupService,
                _dialogService,
                _notificationService,
                _loggingService)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        });
    }

    /// <inheritdoc/>
    public void ShowCleanup()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var window = new CleanupWindow(_cleanupService)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        });
    }

    /// <inheritdoc/>
    public void ShowAbout()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var window = new AboutWindow
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        });
    }
}
