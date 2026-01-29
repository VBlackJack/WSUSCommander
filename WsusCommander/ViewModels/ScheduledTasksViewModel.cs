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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WsusCommander.Interfaces;
using WsusCommander.Models;
using WsusCommander.Properties;
using WsusCommander.Services;

namespace WsusCommander.ViewModels;

/// <summary>
/// ViewModel for the scheduled tasks management window.
/// </summary>
public sealed partial class ScheduledTasksViewModel : ObservableObject
{
    private readonly IScheduledTasksService _tasksService;
    private readonly ITaskSchedulerService _taskSchedulerService;
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Gets the collection of scheduled tasks.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ScheduledWsusTask> _tasks = [];

    /// <summary>
    /// Gets or sets the currently selected task.
    /// </summary>
    [ObservableProperty]
    private ScheduledWsusTask? _selectedTask;

    /// <summary>
    /// Gets or sets whether the view is loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Event raised when the wizard should be opened.
    /// </summary>
    public event EventHandler<ScheduledWsusTask?>? WizardRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledTasksViewModel"/> class.
    /// </summary>
    public ScheduledTasksViewModel(
        IScheduledTasksService tasksService,
        ITaskSchedulerService taskSchedulerService,
        IDialogService dialogService,
        INotificationService notificationService,
        ILoggingService loggingService)
    {
        _tasksService = tasksService;
        _taskSchedulerService = taskSchedulerService;
        _dialogService = dialogService;
        _notificationService = notificationService;
        _loggingService = loggingService;

        LoadTasks();
    }

    private void LoadTasks()
    {
        Tasks = new ObservableCollection<ScheduledWsusTask>(_tasksService.GetTasks());
        StatusMessage = $"Loaded {Tasks.Count} tasks.";
    }

    /// <summary>
    /// Creates a new scheduled task.
    /// </summary>
    [RelayCommand]
    private void CreateTask()
    {
        WizardRequested?.Invoke(this, null);
    }

    /// <summary>
    /// Edits the selected scheduled task.
    /// </summary>
    [RelayCommand]
    private void EditTask()
    {
        if (SelectedTask is null)
        {
            return;
        }

        WizardRequested?.Invoke(this, SelectedTask);
    }

    /// <summary>
    /// Deletes the selected scheduled task.
    /// </summary>
    [RelayCommand]
    private async Task DeleteTaskAsync()
    {
        if (SelectedTask is null)
        {
            return;
        }

        var result = await _dialogService.ShowConfirmationAsync(
            Resources.DialogConfirm,
            $"Delete scheduled task '{SelectedTask.Name}'? This will also remove it from Windows Task Scheduler.",
            Resources.BtnYes,
            Resources.BtnNo);

        if (result != DialogResult.Confirmed)
        {
            return;
        }

        try
        {
            IsLoading = true;
            await _tasksService.DeleteTaskAsync(SelectedTask.Id);
            Tasks.Remove(SelectedTask);
            SelectedTask = null;
            _notificationService.ShowToast("Task deleted.", ToastType.Success);
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync("Failed to delete task", ex);
            await _notificationService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Runs the selected task immediately.
    /// </summary>
    [RelayCommand]
    private async Task RunTaskNowAsync()
    {
        if (SelectedTask?.WindowsTaskName is null)
        {
            return;
        }

        var result = await _dialogService.ShowConfirmationAsync(
            Resources.DialogConfirm,
            $"Run task '{SelectedTask.Name}' now?",
            Resources.BtnYes,
            Resources.BtnNo);

        if (result != DialogResult.Confirmed)
        {
            return;
        }

        try
        {
            IsLoading = true;
            await _taskSchedulerService.RunTaskNowAsync(SelectedTask.WindowsTaskName);
            _notificationService.ShowToast($"Task '{SelectedTask.Name}' started.", ToastType.Success);
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync("Failed to run task", ex);
            await _notificationService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Toggles the enabled state of the selected task.
    /// </summary>
    [RelayCommand]
    private async Task ToggleTaskEnabledAsync()
    {
        if (SelectedTask is null)
        {
            return;
        }

        try
        {
            var newState = !SelectedTask.IsEnabled;
            await _tasksService.SetTaskEnabledAsync(SelectedTask.Id, newState);
            SelectedTask.IsEnabled = newState;
            OnPropertyChanged(nameof(SelectedTask));
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync("Failed to toggle task state", ex);
            await _notificationService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
    }

    /// <summary>
    /// Refreshes the task list.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            await _tasksService.LoadAsync();
            LoadTasks();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Called after a task is saved in the wizard to refresh the list.
    /// </summary>
    public void OnTaskSaved(ScheduledWsusTask task)
    {
        var existing = Tasks.FirstOrDefault(t => t.Id == task.Id);
        if (existing is not null)
        {
            var index = Tasks.IndexOf(existing);
            Tasks[index] = task;
        }
        else
        {
            Tasks.Add(task);
        }

        SelectedTask = task;
    }

    /// <summary>
    /// Gets a localized display string for the operation type.
    /// </summary>
    public static string GetOperationTypeDisplay(ScheduledTaskOperationType type) =>
        type switch
        {
            ScheduledTaskOperationType.StagedApproval => "Staged Approval",
            ScheduledTaskOperationType.Cleanup => "Cleanup",
            ScheduledTaskOperationType.Synchronization => "Synchronization",
            _ => type.ToString()
        };

    /// <summary>
    /// Gets a localized display string for the execution status.
    /// </summary>
    public static string GetStatusDisplay(TaskExecutionStatus status) =>
        status switch
        {
            TaskExecutionStatus.NeverRun => "Never Run",
            TaskExecutionStatus.Success => "Success",
            TaskExecutionStatus.Warning => "Warning",
            TaskExecutionStatus.Failed => "Failed",
            _ => status.ToString()
        };
}
