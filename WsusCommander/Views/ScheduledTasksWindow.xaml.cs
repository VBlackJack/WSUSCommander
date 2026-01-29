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

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using WsusCommander.Interfaces;
using WsusCommander.Models;
using WsusCommander.Properties;
using WsusCommander.Services;
using WsusCommander.ViewModels;

namespace WsusCommander.Views;

/// <summary>
/// Window for managing scheduled WSUS tasks.
/// </summary>
public sealed partial class ScheduledTasksWindow : Window
{
    private readonly ScheduledTasksViewModel _viewModel;
    private readonly IScheduledTasksService _tasksService;
    private readonly IGroupService _groupService;
    private readonly ILoggingService _loggingService;
    private readonly INotificationService _notificationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledTasksWindow"/> class.
    /// </summary>
    public ScheduledTasksWindow(
        IScheduledTasksService tasksService,
        ITaskSchedulerService taskSchedulerService,
        IGroupService groupService,
        IDialogService dialogService,
        INotificationService notificationService,
        ILoggingService loggingService)
    {
        InitializeComponent();

        _tasksService = tasksService;
        _groupService = groupService;
        _loggingService = loggingService;
        _notificationService = notificationService;

        _viewModel = new ScheduledTasksViewModel(
            tasksService,
            taskSchedulerService,
            dialogService,
            notificationService,
            loggingService);

        _viewModel.WizardRequested += OnWizardRequested;

        DataContext = _viewModel;

        // Add converters to resources
        Resources["OperationTypeConverter"] = new OperationTypeConverter();
        Resources["StatusConverter"] = new TaskStatusConverter();
        Resources["NullToBoolConverter"] = new NullToBoolConverter();
    }

    private void OnWizardRequested(object? sender, ScheduledWsusTask? existingTask)
    {
        var wizard = new ScheduledTaskWizardWindow(
            _tasksService,
            _groupService,
            _loggingService,
            _notificationService,
            existingTask)
        {
            Owner = this
        };

        wizard.TaskSaved += (s, task) => _viewModel.OnTaskSaved(task);
        wizard.ShowDialog();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

/// <summary>
/// Converts operation type enum to localized string.
/// </summary>
internal sealed class OperationTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ScheduledTaskOperationType type)
        {
            return ScheduledTasksViewModel.GetOperationTypeDisplay(type);
        }

        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts task execution status to localized string.
/// </summary>
internal sealed class TaskStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TaskExecutionStatus status)
        {
            return ScheduledTasksViewModel.GetStatusDisplay(status);
        }

        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts null to false, non-null to true.
/// </summary>
internal sealed class NullToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is not null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
