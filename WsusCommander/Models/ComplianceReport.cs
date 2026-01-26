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

namespace WsusCommander.Models;

/// <summary>
/// Compliance status enumeration.
/// </summary>
public enum ComplianceStatus
{
    /// <summary>Fully compliant.</summary>
    Compliant,

    /// <summary>Partially compliant.</summary>
    PartiallyCompliant,

    /// <summary>Not compliant.</summary>
    NonCompliant,

    /// <summary>Status unknown.</summary>
    Unknown
}

/// <summary>
/// Overall compliance report.
/// </summary>
public sealed class ComplianceReport
{
    /// <summary>
    /// Gets or sets the report generation date.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the report title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the overall compliance status.
    /// </summary>
    public ComplianceStatus OverallStatus { get; set; }

    /// <summary>
    /// Gets or sets the overall compliance percentage.
    /// </summary>
    public double CompliancePercent { get; set; }

    /// <summary>
    /// Gets or sets the total computer count.
    /// </summary>
    public int TotalComputers { get; set; }

    /// <summary>
    /// Gets or sets the compliant computer count.
    /// </summary>
    public int CompliantComputers { get; set; }

    /// <summary>
    /// Gets or sets the non-compliant computer count.
    /// </summary>
    public int NonCompliantComputers { get; set; }

    /// <summary>
    /// Gets or sets the total approved updates count.
    /// </summary>
    public int TotalApprovedUpdates { get; set; }

    /// <summary>
    /// Gets or sets compliance by group.
    /// </summary>
    public List<GroupComplianceInfo> GroupCompliance { get; set; } = [];

    /// <summary>
    /// Gets or sets compliance by classification.
    /// </summary>
    public List<ClassificationComplianceInfo> ClassificationCompliance { get; set; } = [];

    /// <summary>
    /// Gets or sets the critical updates summary.
    /// </summary>
    public CriticalUpdatesSummary? CriticalUpdates { get; set; }

    /// <summary>
    /// Gets or sets stale computers list.
    /// </summary>
    public List<StaleComputerInfo> StaleComputers { get; set; } = [];
}

/// <summary>
/// Group compliance information.
/// </summary>
public sealed class GroupComplianceInfo
{
    /// <summary>
    /// Gets or sets the group ID.
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// Gets or sets the group name.
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total computer count in the group.
    /// </summary>
    public int TotalComputers { get; set; }

    /// <summary>
    /// Gets or sets the compliant computer count.
    /// </summary>
    public int CompliantComputers { get; set; }

    /// <summary>
    /// Gets or sets the compliance percentage.
    /// </summary>
    public double CompliancePercent { get; set; }

    /// <summary>
    /// Gets or sets the total needed updates.
    /// </summary>
    public int TotalNeededUpdates { get; set; }

    /// <summary>
    /// Gets or sets the total failed updates.
    /// </summary>
    public int TotalFailedUpdates { get; set; }
}

/// <summary>
/// Classification compliance information.
/// </summary>
public sealed class ClassificationComplianceInfo
{
    /// <summary>
    /// Gets or sets the classification name.
    /// </summary>
    public string Classification { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total updates in this classification.
    /// </summary>
    public int TotalUpdates { get; set; }

    /// <summary>
    /// Gets or sets the approved updates count.
    /// </summary>
    public int ApprovedUpdates { get; set; }

    /// <summary>
    /// Gets or sets the declined updates count.
    /// </summary>
    public int DeclinedUpdates { get; set; }

    /// <summary>
    /// Gets or sets the pending updates count.
    /// </summary>
    public int PendingUpdates { get; set; }

    /// <summary>
    /// Gets or sets the installation rate.
    /// </summary>
    public double InstallationRate { get; set; }
}

/// <summary>
/// Critical updates summary.
/// </summary>
public sealed class CriticalUpdatesSummary
{
    /// <summary>
    /// Gets or sets the total critical updates.
    /// </summary>
    public int TotalCritical { get; set; }

    /// <summary>
    /// Gets or sets the approved critical updates.
    /// </summary>
    public int ApprovedCritical { get; set; }

    /// <summary>
    /// Gets or sets the unapproved critical updates.
    /// </summary>
    public int UnapprovedCritical { get; set; }

    /// <summary>
    /// Gets or sets the computers needing critical updates.
    /// </summary>
    public int ComputersNeedingCritical { get; set; }

    /// <summary>
    /// Gets or sets the list of unapproved critical updates.
    /// </summary>
    public List<CriticalUpdateInfo> UnapprovedUpdates { get; set; } = [];
}

/// <summary>
/// Critical update information.
/// </summary>
public sealed class CriticalUpdateInfo
{
    /// <summary>
    /// Gets or sets the update ID.
    /// </summary>
    public Guid UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the update title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the KB article.
    /// </summary>
    public string? KbArticle { get; set; }

    /// <summary>
    /// Gets or sets the severity.
    /// </summary>
    public string? Severity { get; set; }

    /// <summary>
    /// Gets or sets the release date.
    /// </summary>
    public DateTime ReleaseDate { get; set; }

    /// <summary>
    /// Gets or sets the number of computers needing this update.
    /// </summary>
    public int ComputersNeeding { get; set; }
}

/// <summary>
/// Stale computer information.
/// </summary>
public sealed class StaleComputerInfo
{
    /// <summary>
    /// Gets or sets the computer ID.
    /// </summary>
    public string ComputerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the computer name.
    /// </summary>
    public string ComputerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last report time.
    /// </summary>
    public DateTime? LastReportTime { get; set; }

    /// <summary>
    /// Gets or sets the days since last report.
    /// </summary>
    public int DaysSinceLastReport { get; set; }

    /// <summary>
    /// Gets or sets the group names.
    /// </summary>
    public List<string> GroupNames { get; set; } = [];
}
