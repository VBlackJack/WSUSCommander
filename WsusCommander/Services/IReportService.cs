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

using WsusCommander.Models;

namespace WsusCommander.Services;

/// <summary>
/// Report type enumeration.
/// </summary>
public enum ReportType
{
    /// <summary>Overall compliance report.</summary>
    Compliance,

    /// <summary>Update status report.</summary>
    UpdateStatus,

    /// <summary>Computer status report.</summary>
    ComputerStatus,

    /// <summary>Stale computers report.</summary>
    StaleComputers,

    /// <summary>Critical updates report.</summary>
    CriticalUpdates
}

/// <summary>
/// Report generation options.
/// </summary>
public sealed class ReportOptions
{
    /// <summary>
    /// Gets or sets the target group ID (null for all groups).
    /// </summary>
    public Guid? GroupId { get; set; }

    /// <summary>
    /// Gets or sets the stale computer threshold in days.
    /// </summary>
    public int StaleDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to include superseded updates.
    /// </summary>
    public bool IncludeSuperseded { get; set; }

    /// <summary>
    /// Gets or sets whether to include declined updates.
    /// </summary>
    public bool IncludeDeclined { get; set; }
}

/// <summary>
/// Interface for report generation service.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Generates a compliance report.
    /// </summary>
    /// <param name="options">Report options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compliance report.</returns>
    Task<ComplianceReport> GenerateComplianceReportAsync(
        ReportOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stale computers that haven't reported recently.
    /// </summary>
    /// <param name="staleDays">Days threshold.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of stale computers.</returns>
    Task<IReadOnlyList<StaleComputerInfo>> GetStaleComputersAsync(
        int staleDays = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets critical updates summary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Critical updates summary.</returns>
    Task<CriticalUpdatesSummary> GetCriticalUpdatesSummaryAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a report to file.
    /// </summary>
    /// <param name="report">Report to export.</param>
    /// <param name="filePath">Target file path.</param>
    /// <param name="format">Export format.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExportReportAsync(
        ComplianceReport report,
        string filePath,
        ExportFormat format,
        CancellationToken cancellationToken = default);
}
