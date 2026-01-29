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
using System.Windows.Controls;
using System.Windows.Data;
using WsusCommander.Interfaces;
using WsusCommander.Models;
using WsusCommander.Services;
using WsusCommander.ViewModels;

namespace WsusCommander.Views;

/// <summary>
/// Wizard window for creating and editing scheduled WSUS tasks.
/// </summary>
public sealed partial class ScheduledTaskWizardWindow : Window
{
    private readonly ScheduledTaskWizardViewModel _viewModel;

    /// <summary>
    /// Event raised when a task is saved.
    /// </summary>
    public event EventHandler<ScheduledWsusTask>? TaskSaved;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledTaskWizardWindow"/> class.
    /// </summary>
    public ScheduledTaskWizardWindow(
        IScheduledTasksService tasksService,
        IGroupService groupService,
        ILoggingService loggingService,
        INotificationService notificationService,
        ScheduledWsusTask? existingTask = null)
    {
        InitializeComponent();

        _viewModel = new ScheduledTaskWizardViewModel(
            tasksService,
            groupService,
            loggingService,
            notificationService,
            existingTask);

        _viewModel.Completed += OnWizardCompleted;
        _viewModel.Cancelled += OnWizardCancelled;

        DataContext = _viewModel;

        // Add converters to resources
        Resources["StepColorConverter"] = new StepColorConverter();
        Resources["StepVisibilityConverter"] = new StepVisibilityConverter();
        Resources["EnumBoolConverter"] = new EnumBoolConverter();
        Resources["TemplateNameConverter"] = new TemplateNameConverter();
        Resources["TemplateDescConverter"] = new TemplateDescConverter();
        Resources["FrequencyConverter"] = new FrequencyConverter();
        Resources["FrequencyToVisibilityConverter"] = new FrequencyToVisibilityConverter();
        Resources["OperationTypeConverter"] = new WizardOperationTypeConverter();
        Resources["InverseBoolToVisibility"] = new InverseBoolToVisibilityConverter();
        Resources["StringToVisibilityConverter"] = new StringToVisibilityConverter();
        Resources["EnumToVisibilityConverter"] = new EnumToVisibilityConverter();

        Loaded += OnWindowLoaded;
    }

    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadGroupsAsync();
    }

    private void OnWizardCompleted(object? sender, ScheduledWsusTask task)
    {
        TaskSaved?.Invoke(this, task);
        Close();
    }

    private void OnWizardCancelled(object? sender, EventArgs e)
    {
        Close();
    }

    private void TestGroupsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox)
        {
            return;
        }

        _viewModel.SelectedTestGroups.Clear();
        foreach (ComputerGroup group in listBox.SelectedItems)
        {
            _viewModel.SelectedTestGroups.Add(group);
        }
    }

    private void ProdGroupsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox)
        {
            return;
        }

        _viewModel.SelectedProductionGroups.Clear();
        foreach (ComputerGroup group in listBox.SelectedItems)
        {
            _viewModel.SelectedProductionGroups.Add(group);
        }
    }
}

/// <summary>
/// Converts step index to color based on current step.
/// </summary>
internal sealed class StepColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is WizardStep currentStep && parameter is string stepIndexStr
            && int.TryParse(stepIndexStr, out var stepIndex))
        {
            var currentIndex = (int)currentStep;
            if (currentStep == WizardStep.Complete)
            {
                currentIndex = 4;
            }

            if (stepIndex < currentIndex)
            {
                return "#27AE60"; // Completed - green
            }

            if (stepIndex == currentIndex)
            {
                return "#3498DB"; // Current - blue
            }
        }

        return "#BDC3C7"; // Future - gray
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts wizard step to visibility.
/// </summary>
internal sealed class StepVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is WizardStep currentStep && parameter is string stepName)
        {
            var targetStep = Enum.Parse<WizardStep>(stepName);
            return currentStep == targetStep ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts enum value to boolean for radio buttons.
/// </summary>
internal sealed class EnumBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is string enumStr && value is not null)
        {
            return value.ToString() == enumStr;
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter is string enumStr)
        {
            return Enum.Parse(targetType, enumStr);
        }

        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts template name key to localized string.
/// </summary>
internal sealed class TemplateNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string key)
        {
            return key switch
            {
                "TemplateNameStagedSecurity" => "Staged Security Approval (Recommended)",
                "TemplateNameMonthlyCleanup" => "Monthly WSUS Cleanup",
                "TemplateNameDailySync" => "Daily Synchronization",
                _ => key
            };
        }

        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts template description key to localized string.
/// </summary>
internal sealed class TemplateDescConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string key)
        {
            return key switch
            {
                "TemplateDescStagedSecurity" => "Automatically approve security and critical updates for test groups, then promote to production after a delay.",
                "TemplateDescMonthlyCleanup" => "Remove obsolete updates, expired updates, and stale computers monthly.",
                "TemplateDescDailySync" => "Synchronize with Microsoft Update every day.",
                _ => key
            };
        }

        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts schedule frequency to localized string.
/// </summary>
internal sealed class FrequencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ScheduleFrequency frequency)
        {
            return ScheduledTaskWizardViewModel.GetFrequencyDisplay(frequency);
        }

        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts frequency to visibility for conditional UI elements.
/// </summary>
internal sealed class FrequencyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ScheduleFrequency frequency && parameter is string targetFrequency)
        {
            var target = Enum.Parse<ScheduleFrequency>(targetFrequency);
            return frequency == target ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts operation type enum to display string for wizard.
/// </summary>
internal sealed class WizardOperationTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ScheduledTaskOperationType type)
        {
            return type switch
            {
                ScheduledTaskOperationType.StagedApproval => "Staged Approval",
                ScheduledTaskOperationType.Cleanup => "Cleanup",
                ScheduledTaskOperationType.Synchronization => "Synchronization",
                _ => type.ToString()
            };
        }

        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Inverse boolean to visibility converter.
/// </summary>
internal sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }

        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts non-empty string to Visible, empty/null to Collapsed.
/// </summary>
internal sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrEmpty(str))
        {
            return Visibility.Visible;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts enum value to visibility based on parameter match.
/// </summary>
internal sealed class EnumToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null || parameter is not string paramString)
        {
            return Visibility.Collapsed;
        }

        var enumType = value.GetType();
        if (Enum.TryParse(enumType, paramString, out var targetValue))
        {
            return value.Equals(targetValue) ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
