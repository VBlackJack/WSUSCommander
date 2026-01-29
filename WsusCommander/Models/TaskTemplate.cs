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

using System.ComponentModel.DataAnnotations;

namespace WsusCommander.Models;

/// <summary>
/// Predefined template for creating scheduled tasks.
/// </summary>
public sealed class TaskTemplate
{
    /// <summary>
    /// Gets or sets the template identifier.
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name (localization key).
    /// </summary>
    [Required]
    [StringLength(256)]
    public string NameKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description (localization key).
    /// </summary>
    [StringLength(1024)]
    public string DescriptionKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operation type.
    /// </summary>
    public ScheduledTaskOperationType OperationType { get; set; }

    /// <summary>
    /// Gets or sets the default schedule configuration.
    /// </summary>
    public ScheduleConfig DefaultSchedule { get; set; } = new();

    /// <summary>
    /// Gets or sets the default staged approval settings.
    /// </summary>
    public StagedApprovalConfig? DefaultStagedApproval { get; set; }

    /// <summary>
    /// Gets or sets the default cleanup settings.
    /// </summary>
    public CleanupOptions? DefaultCleanup { get; set; }

    /// <summary>
    /// Gets or sets the default sync settings.
    /// </summary>
    public SyncConfig? DefaultSync { get; set; }

    /// <summary>
    /// Gets or sets whether this template is recommended.
    /// </summary>
    public bool IsRecommended { get; set; }

    /// <summary>
    /// Gets or sets the sort order for display.
    /// </summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// Collection of task templates for serialization.
/// </summary>
public sealed class TaskTemplateCollection
{
    /// <summary>
    /// Gets or sets the list of templates.
    /// </summary>
    public List<TaskTemplate> Templates { get; set; } = [];
}

/// <summary>
/// Provides built-in task templates.
/// </summary>
public static class BuiltInTaskTemplates
{
    /// <summary>
    /// Gets the staged security approval template.
    /// </summary>
    public static TaskTemplate StagedSecurityApproval => new()
    {
        Id = "staged-security",
        NameKey = "TemplateNameStagedSecurity",
        DescriptionKey = "TemplateDescStagedSecurity",
        OperationType = ScheduledTaskOperationType.StagedApproval,
        IsRecommended = true,
        SortOrder = 1,
        DefaultSchedule = new ScheduleConfig
        {
            Frequency = ScheduleFrequency.Weekly,
            DaysOfWeek = [DayOfWeek.Tuesday],
            TimeOfDay = new TimeSpan(3, 0, 0)
        },
        DefaultStagedApproval = new StagedApprovalConfig
        {
            PromotionDelayDays = 7,
            UpdateClassifications = ["Critical Updates", "Security Updates"],
            RequireSuccessfulInstallations = true,
            MinimumSuccessfulInstallations = 1,
            AbortOnFailures = true,
            MaxAllowedFailures = 0,
            DeclineSupersededUpdates = true
        }
    };

    /// <summary>
    /// Gets the monthly cleanup template.
    /// </summary>
    public static TaskTemplate MonthlyCleanup => new()
    {
        Id = "monthly-cleanup",
        NameKey = "TemplateNameMonthlyCleanup",
        DescriptionKey = "TemplateDescMonthlyCleanup",
        OperationType = ScheduledTaskOperationType.Cleanup,
        IsRecommended = false,
        SortOrder = 2,
        DefaultSchedule = new ScheduleConfig
        {
            Frequency = ScheduleFrequency.Monthly,
            DayOfMonth = 1,
            TimeOfDay = new TimeSpan(4, 0, 0)
        },
        DefaultCleanup = new CleanupOptions
        {
            RemoveObsoleteUpdates = true,
            RemoveExpiredUpdates = true,
            RemoveObsoleteComputers = true,
            CompressUpdateRevisions = true,
            RemoveUnneededContent = true
        }
    };

    /// <summary>
    /// Gets the daily sync template.
    /// </summary>
    public static TaskTemplate DailySync => new()
    {
        Id = "daily-sync",
        NameKey = "TemplateNameDailySync",
        DescriptionKey = "TemplateDescDailySync",
        OperationType = ScheduledTaskOperationType.Synchronization,
        IsRecommended = false,
        SortOrder = 3,
        DefaultSchedule = new ScheduleConfig
        {
            Frequency = ScheduleFrequency.Daily,
            TimeOfDay = new TimeSpan(2, 0, 0)
        },
        DefaultSync = new SyncConfig
        {
            WaitForCompletion = true,
            MaxWaitMinutes = 60,
            NotifyOnCompletion = false,
            NotifyOnErrorsOnly = true
        }
    };

    /// <summary>
    /// Gets all built-in templates.
    /// </summary>
    public static IReadOnlyList<TaskTemplate> All =>
        [StagedSecurityApproval, MonthlyCleanup, DailySync];
}
