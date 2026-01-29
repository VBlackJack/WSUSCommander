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
/// Schedule frequency type.
/// </summary>
public enum ScheduleFrequency
{
    /// <summary>
    /// Run once at a specific time.
    /// </summary>
    Once,

    /// <summary>
    /// Run every day.
    /// </summary>
    Daily,

    /// <summary>
    /// Run on specific days of the week.
    /// </summary>
    Weekly,

    /// <summary>
    /// Run on specific days of the month.
    /// </summary>
    Monthly
}

/// <summary>
/// Configuration for task scheduling.
/// </summary>
public sealed class ScheduleConfig
{
    /// <summary>
    /// Gets or sets the schedule frequency.
    /// </summary>
    public ScheduleFrequency Frequency { get; set; } = ScheduleFrequency.Weekly;

    /// <summary>
    /// Gets or sets the time of day to run (local time).
    /// </summary>
    public TimeSpan TimeOfDay { get; set; } = new(3, 0, 0);

    /// <summary>
    /// Gets or sets the days of the week for weekly schedules.
    /// </summary>
    public List<DayOfWeek> DaysOfWeek { get; set; } = [DayOfWeek.Tuesday];

    /// <summary>
    /// Gets or sets the day of month for monthly schedules (1-31).
    /// </summary>
    [Range(1, 31)]
    public int DayOfMonth { get; set; } = 1;

    /// <summary>
    /// Gets or sets the start date for the schedule.
    /// </summary>
    public DateTime StartDate { get; set; } = DateTime.Today;

    /// <summary>
    /// Gets or sets the optional end date for the schedule.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets a human-readable description of the schedule.
    /// </summary>
    public string DisplayText
    {
        get
        {
            var time = TimeOfDay.ToString(@"hh\:mm");
            return Frequency switch
            {
                ScheduleFrequency.Once => $"{StartDate:yyyy-MM-dd} {time}",
                ScheduleFrequency.Daily => $"Daily @ {time}",
                ScheduleFrequency.Weekly when DaysOfWeek.Count > 0 =>
                    $"{string.Join(", ", DaysOfWeek.Select(d => d.ToString()[..3]))} @ {time}",
                ScheduleFrequency.Monthly => $"Day {DayOfMonth} @ {time}",
                _ => time
            };
        }
    }
}
