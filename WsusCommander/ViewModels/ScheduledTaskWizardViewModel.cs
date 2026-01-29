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
/// Wizard steps for scheduled task creation/editing.
/// </summary>
public enum WizardStep
{
    /// <summary>
    /// Select task type and template.
    /// </summary>
    SelectType,

    /// <summary>
    /// Configure target groups (for staged approval).
    /// </summary>
    ConfigureGroups,

    /// <summary>
    /// Configure schedule.
    /// </summary>
    ConfigureSchedule,

    /// <summary>
    /// Review and confirm.
    /// </summary>
    Review,

    /// <summary>
    /// Task created successfully.
    /// </summary>
    Complete
}

/// <summary>
/// ViewModel for the scheduled task wizard.
/// </summary>
public sealed partial class ScheduledTaskWizardViewModel : ObservableObject
{
    private readonly IScheduledTasksService _tasksService;
    private readonly IGroupService _groupService;
    private readonly ILoggingService _loggingService;
    private readonly INotificationService _notificationService;
    private readonly bool _isEditMode;

    /// <summary>
    /// Gets the task being created or edited.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowGroupsStep))]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    private ScheduledWsusTask _task;

    /// <summary>
    /// Gets or sets the current wizard step.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFirstStep))]
    [NotifyPropertyChangedFor(nameof(IsLastStep))]
    [NotifyPropertyChangedFor(nameof(ShowGroupsStep))]
    [NotifyPropertyChangedFor(nameof(StepTitle))]
    [NotifyPropertyChangedFor(nameof(ValidationMessage))]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(FinishCommand))]
    private WizardStep _currentStep = WizardStep.SelectType;

    /// <summary>
    /// Gets or sets whether the wizard is saving.
    /// </summary>
    [ObservableProperty]
    private bool _isSaving;

    /// <summary>
    /// Gets the available task templates.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<TaskTemplate> _templates = [];

    /// <summary>
    /// Gets or sets the selected template.
    /// </summary>
    [ObservableProperty]
    private TaskTemplate? _selectedTemplate;

    /// <summary>
    /// Gets the available computer groups.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ComputerGroup> _availableGroups = [];

    /// <summary>
    /// Gets or sets the selected test groups.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    private ObservableCollection<ComputerGroup> _selectedTestGroups = [];

    /// <summary>
    /// Gets or sets the selected production groups.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    private ObservableCollection<ComputerGroup> _selectedProductionGroups = [];

    /// <summary>
    /// Gets or sets the available schedule frequencies.
    /// </summary>
    public IReadOnlyList<ScheduleFrequency> AvailableFrequencies { get; } =
        [ScheduleFrequency.Once, ScheduleFrequency.Daily, ScheduleFrequency.Weekly, ScheduleFrequency.Monthly];

    /// <summary>
    /// Gets or sets the available days of week.
    /// </summary>
    public IReadOnlyList<DayOfWeek> AvailableDaysOfWeek { get; } =
        Enum.GetValues<DayOfWeek>();

    /// <summary>
    /// Gets or sets the selected days of week.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    private ObservableCollection<DayOfWeek> _selectedDaysOfWeek = [];

    /// <summary>
    /// Gets or sets the schedule hour.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    private int _scheduleHour = 3;

    /// <summary>
    /// Gets or sets the schedule minute.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    private int _scheduleMinute = 0;

    /// <summary>
    /// Gets whether this is the first step.
    /// </summary>
    public bool IsFirstStep => CurrentStep == WizardStep.SelectType;

    /// <summary>
    /// Gets whether this is the last step before completion.
    /// </summary>
    public bool IsLastStep => CurrentStep == WizardStep.Review;

    /// <summary>
    /// Gets whether the groups step should be shown.
    /// </summary>
    public bool ShowGroupsStep =>
        Task.OperationType == ScheduledTaskOperationType.StagedApproval;

    /// <summary>
    /// Gets the current step title.
    /// </summary>
    public string StepTitle => CurrentStep switch
    {
        WizardStep.SelectType => "Task Type",
        WizardStep.ConfigureGroups => "Target Groups",
        WizardStep.ConfigureSchedule => "Schedule",
        WizardStep.Review => "Review",
        WizardStep.Complete => "Complete",
        _ => string.Empty
    };

    /// <summary>
    /// Gets the current validation message.
    /// </summary>
    public string ValidationMessage => GetValidationMessage();

    /// <summary>
    /// Event raised when the wizard completes successfully.
    /// </summary>
    public event EventHandler<ScheduledWsusTask>? Completed;

    /// <summary>
    /// Event raised when the wizard is cancelled.
    /// </summary>
    public event EventHandler? Cancelled;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledTaskWizardViewModel"/> class.
    /// </summary>
    public ScheduledTaskWizardViewModel(
        IScheduledTasksService tasksService,
        IGroupService groupService,
        ILoggingService loggingService,
        INotificationService notificationService,
        ScheduledWsusTask? existingTask = null)
    {
        _tasksService = tasksService;
        _groupService = groupService;
        _loggingService = loggingService;
        _notificationService = notificationService;

        if (existingTask is not null)
        {
            _task = existingTask;
            _isEditMode = true;
        }
        else
        {
            _task = new ScheduledWsusTask();
        }

        // Load templates
        Templates = new ObservableCollection<TaskTemplate>(_tasksService.GetTemplates());

        // Initialize from existing task
        if (_isEditMode)
        {
            InitializeFromTask();
        }
        else
        {
            // Default values
            SelectedDaysOfWeek.Add(DayOfWeek.Tuesday);
        }
    }

    /// <summary>
    /// Loads available groups asynchronously.
    /// </summary>
    public async Task LoadGroupsAsync()
    {
        try
        {
            var groups = await _groupService.GetAllGroupsAsync();
            AvailableGroups = new ObservableCollection<ComputerGroup>(groups);

            // If editing, select the configured groups
            if (_isEditMode && Task.StagedApprovalSettings is not null)
            {
                foreach (var group in AvailableGroups)
                {
                    if (Task.StagedApprovalSettings.TestGroupIds.Contains(group.Id))
                    {
                        SelectedTestGroups.Add(group);
                    }

                    if (Task.StagedApprovalSettings.ProductionGroupIds.Contains(group.Id))
                    {
                        SelectedProductionGroups.Add(group);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await _loggingService.LogWarningAsync(
                $"Failed to load groups: {ex.Message}");
        }
    }

    private void InitializeFromTask()
    {
        ScheduleHour = Task.Schedule.TimeOfDay.Hours;
        ScheduleMinute = Task.Schedule.TimeOfDay.Minutes;
        SelectedDaysOfWeek = new ObservableCollection<DayOfWeek>(Task.Schedule.DaysOfWeek);
    }

    /// <summary>
    /// Applies a template to the current task.
    /// </summary>
    [RelayCommand]
    private void ApplyTemplate()
    {
        if (SelectedTemplate is null)
        {
            return;
        }

        Task = _tasksService.CreateFromTemplate(SelectedTemplate.Id);
        Task.Name = GetLocalizedTemplateName(SelectedTemplate.NameKey);
        Task.Description = GetLocalizedTemplateDescription(SelectedTemplate.DescriptionKey);

        // Update schedule UI
        ScheduleHour = Task.Schedule.TimeOfDay.Hours;
        ScheduleMinute = Task.Schedule.TimeOfDay.Minutes;
        SelectedDaysOfWeek = new ObservableCollection<DayOfWeek>(Task.Schedule.DaysOfWeek);

        OnPropertyChanged(nameof(Task));
        OnPropertyChanged(nameof(ShowGroupsStep));
        OnPropertyChanged(nameof(ValidationMessage));
        NextStepCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Moves to the next wizard step.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanNextStep))]
    private void NextStep()
    {
        var nextStep = GetNextStep();
        if (nextStep.HasValue)
        {
            // Update task from current step before moving
            UpdateTaskFromCurrentStep();

            // Initialize settings for the task type if needed
            EnsureSettingsInitialized();

            CurrentStep = nextStep.Value;
        }
    }

    private bool CanNextStep()
    {
        return IsCurrentStepValid();
    }

    /// <summary>
    /// Moves to the previous wizard step.
    /// </summary>
    [RelayCommand]
    private void PreviousStep()
    {
        var prevStep = GetPreviousStep();
        if (prevStep.HasValue)
        {
            CurrentStep = prevStep.Value;
        }
    }

    /// <summary>
    /// Saves the task and completes the wizard.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanFinish))]
    private async Task FinishAsync()
    {
        try
        {
            IsSaving = true;

            // Final update from current step
            UpdateTaskFromCurrentStep();

            // Validate task name
            if (string.IsNullOrWhiteSpace(Task.Name))
            {
                Task.Name = GetDefaultTaskName();
            }

            await _tasksService.SaveTaskAsync(Task);

            CurrentStep = WizardStep.Complete;
            Completed?.Invoke(this, Task);
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync("Failed to save scheduled task", ex);
            await _notificationService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool CanFinish()
    {
        return CurrentStep == WizardStep.Review && !IsSaving;
    }

    /// <summary>
    /// Cancels the wizard.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    private WizardStep? GetNextStep()
    {
        return CurrentStep switch
        {
            WizardStep.SelectType when ShowGroupsStep => WizardStep.ConfigureGroups,
            WizardStep.SelectType => WizardStep.ConfigureSchedule,
            WizardStep.ConfigureGroups => WizardStep.ConfigureSchedule,
            WizardStep.ConfigureSchedule => WizardStep.Review,
            WizardStep.Review => null, // Use Finish command instead
            _ => null
        };
    }

    private WizardStep? GetPreviousStep()
    {
        return CurrentStep switch
        {
            WizardStep.ConfigureGroups => WizardStep.SelectType,
            WizardStep.ConfigureSchedule when ShowGroupsStep => WizardStep.ConfigureGroups,
            WizardStep.ConfigureSchedule => WizardStep.SelectType,
            WizardStep.Review => WizardStep.ConfigureSchedule,
            _ => null
        };
    }

    private bool IsCurrentStepValid()
    {
        return CurrentStep switch
        {
            WizardStep.SelectType => true, // Always valid - a type is always selected
            WizardStep.ConfigureGroups => ValidateGroupsStep(),
            WizardStep.ConfigureSchedule => ValidateScheduleStep(),
            WizardStep.Review => true,
            _ => true
        };
    }

    private bool ValidateGroupsStep()
    {
        // For staged approval, at least one test group and one production group must be selected
        if (Task.OperationType == ScheduledTaskOperationType.StagedApproval)
        {
            return SelectedTestGroups.Count > 0 && SelectedProductionGroups.Count > 0;
        }

        return true;
    }

    private bool ValidateScheduleStep()
    {
        // Validate time
        if (ScheduleHour < 0 || ScheduleHour > 23)
        {
            return false;
        }

        if (ScheduleMinute < 0 || ScheduleMinute > 59)
        {
            return false;
        }

        // For weekly schedule, at least one day must be selected
        if (Task.Schedule.Frequency == ScheduleFrequency.Weekly && SelectedDaysOfWeek.Count == 0)
        {
            return false;
        }

        // For monthly schedule, day of month must be valid
        if (Task.Schedule.Frequency == ScheduleFrequency.Monthly)
        {
            if (Task.Schedule.DayOfMonth < 1 || Task.Schedule.DayOfMonth > 31)
            {
                return false;
            }
        }

        return true;
    }

    private string GetValidationMessage()
    {
        return CurrentStep switch
        {
            WizardStep.ConfigureGroups when SelectedTestGroups.Count == 0 =>
                "Select at least one test group.",
            WizardStep.ConfigureGroups when SelectedProductionGroups.Count == 0 =>
                "Select at least one production group.",
            WizardStep.ConfigureSchedule when ScheduleHour < 0 || ScheduleHour > 23 =>
                "Hour must be between 0 and 23.",
            WizardStep.ConfigureSchedule when ScheduleMinute < 0 || ScheduleMinute > 59 =>
                "Minute must be between 0 and 59.",
            WizardStep.ConfigureSchedule when Task.Schedule.Frequency == ScheduleFrequency.Weekly && SelectedDaysOfWeek.Count == 0 =>
                "Select at least one day of the week.",
            WizardStep.ConfigureSchedule when Task.Schedule.Frequency == ScheduleFrequency.Monthly && (Task.Schedule.DayOfMonth < 1 || Task.Schedule.DayOfMonth > 31) =>
                "Day of month must be between 1 and 31.",
            _ => string.Empty
        };
    }

    private void EnsureSettingsInitialized()
    {
        switch (Task.OperationType)
        {
            case ScheduledTaskOperationType.StagedApproval:
                Task.StagedApprovalSettings ??= new StagedApprovalConfig();
                break;

            case ScheduledTaskOperationType.Cleanup:
                Task.CleanupSettings ??= new CleanupOptions
                {
                    RemoveObsoleteUpdates = true,
                    RemoveExpiredUpdates = true,
                    RemoveObsoleteComputers = true,
                    CompressUpdateRevisions = true,
                    RemoveUnneededContent = true
                };
                break;

            case ScheduledTaskOperationType.Synchronization:
                Task.SyncSettings ??= new SyncConfig
                {
                    WaitForCompletion = true,
                    MaxWaitMinutes = 30,
                    NotifyOnCompletion = false,
                    NotifyOnErrorsOnly = true
                };
                break;
        }
    }

    private void UpdateTaskFromCurrentStep()
    {
        switch (CurrentStep)
        {
            case WizardStep.ConfigureGroups:
                UpdateGroupsConfig();
                break;
            case WizardStep.ConfigureSchedule:
                UpdateScheduleConfig();
                break;
        }
    }

    private void UpdateGroupsConfig()
    {
        if (Task.StagedApprovalSettings is null)
        {
            Task.StagedApprovalSettings = new StagedApprovalConfig();
        }

        Task.StagedApprovalSettings.TestGroupIds =
            SelectedTestGroups.Select(g => g.Id).ToList();
        Task.StagedApprovalSettings.TestGroupNames =
            SelectedTestGroups.Select(g => g.Name).ToList();

        Task.StagedApprovalSettings.ProductionGroupIds =
            SelectedProductionGroups.Select(g => g.Id).ToList();
        Task.StagedApprovalSettings.ProductionGroupNames =
            SelectedProductionGroups.Select(g => g.Name).ToList();
    }

    private void UpdateScheduleConfig()
    {
        Task.Schedule.TimeOfDay = new TimeSpan(ScheduleHour, ScheduleMinute, 0);
        Task.Schedule.DaysOfWeek = SelectedDaysOfWeek.ToList();
    }

    private string GetDefaultTaskName()
    {
        return Task.OperationType switch
        {
            ScheduledTaskOperationType.StagedApproval =>
                $"Staged Approval - {DateTime.Now:yyyy-MM-dd}",
            ScheduledTaskOperationType.Cleanup =>
                $"Cleanup - {DateTime.Now:yyyy-MM-dd}",
            ScheduledTaskOperationType.Synchronization =>
                $"Sync - {DateTime.Now:yyyy-MM-dd}",
            _ => $"Task - {DateTime.Now:yyyy-MM-dd}"
        };
    }

    private static string GetLocalizedTemplateName(string key)
    {
        return key switch
        {
            "TemplateNameStagedSecurity" => "Staged Security Approval (Recommended)",
            "TemplateNameMonthlyCleanup" => "Monthly WSUS Cleanup",
            "TemplateNameDailySync" => "Daily Synchronization",
            _ => key
        };
    }

    private static string GetLocalizedTemplateDescription(string key)
    {
        return key switch
        {
            "TemplateDescStagedSecurity" => "Automatically approve security and critical updates for test groups, then promote to production after a delay.",
            "TemplateDescMonthlyCleanup" => "Remove obsolete updates, expired updates, and stale computers monthly.",
            "TemplateDescDailySync" => "Synchronize with Microsoft Update every day.",
            _ => key
        };
    }

    /// <summary>
    /// Gets a localized frequency display.
    /// </summary>
    public static string GetFrequencyDisplay(ScheduleFrequency frequency) =>
        frequency switch
        {
            ScheduleFrequency.Once => "Once",
            ScheduleFrequency.Daily => "Daily",
            ScheduleFrequency.Weekly => "Weekly",
            ScheduleFrequency.Monthly => "Monthly",
            _ => frequency.ToString()
        };
}
