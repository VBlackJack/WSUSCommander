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
/// Status of a staged approval.
/// </summary>
public enum StagedApprovalStatus
{
    /// <summary>
    /// Approved in test group, awaiting testing period.
    /// </summary>
    InTesting,

    /// <summary>
    /// Ready for promotion to production.
    /// </summary>
    ReadyForPromotion,

    /// <summary>
    /// Successfully promoted to production.
    /// </summary>
    Promoted,

    /// <summary>
    /// Blocked due to installation failures.
    /// </summary>
    Blocked,

    /// <summary>
    /// Manually skipped/declined.
    /// </summary>
    Skipped
}

/// <summary>
/// Tracks an individual update in the staged approval workflow.
/// </summary>
public sealed class StagedApprovalEntry
{
    /// <summary>
    /// Gets or sets the update ID.
    /// </summary>
    [Required]
    public Guid UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the update title for display.
    /// </summary>
    [StringLength(512)]
    public string UpdateTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the KB article number.
    /// </summary>
    [StringLength(32)]
    public string? KbArticle { get; set; }

    /// <summary>
    /// Gets or sets the scheduled task ID that created this entry.
    /// </summary>
    [Required]
    public Guid TaskId { get; set; }

    /// <summary>
    /// Gets or sets the current status.
    /// </summary>
    public StagedApprovalStatus Status { get; set; } = StagedApprovalStatus.InTesting;

    /// <summary>
    /// Gets or sets when the update was approved for testing.
    /// </summary>
    public DateTime ApprovedForTestAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the update is eligible for promotion.
    /// </summary>
    public DateTime EligibleForPromotionAt { get; set; }

    /// <summary>
    /// Gets or sets when the update was promoted to production.
    /// </summary>
    public DateTime? PromotedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of successful installations in test groups.
    /// </summary>
    public int SuccessfulInstallations { get; set; }

    /// <summary>
    /// Gets or sets the number of failed installations in test groups.
    /// </summary>
    public int FailedInstallations { get; set; }

    /// <summary>
    /// Gets or sets the number of pending installations in test groups.
    /// </summary>
    public int PendingInstallations { get; set; }

    /// <summary>
    /// Gets or sets any status message or error.
    /// </summary>
    [StringLength(1024)]
    public string? StatusMessage { get; set; }
}

/// <summary>
/// Collection of staged approval tracking entries for persistence.
/// </summary>
public sealed class StagedApprovalTrackerCollection
{
    /// <summary>
    /// Gets or sets the list of tracked approvals.
    /// </summary>
    public List<StagedApprovalEntry> Entries { get; set; } = [];

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
