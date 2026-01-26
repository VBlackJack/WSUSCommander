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
/// Detailed information about an update.
/// </summary>
public sealed class UpdateDetails
{
    /// <summary>
    /// Gets or sets the update ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the update title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the KB article number.
    /// </summary>
    public string? KbArticle { get; set; }

    /// <summary>
    /// Gets or sets the full description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the classification.
    /// </summary>
    public string? Classification { get; set; }

    /// <summary>
    /// Gets or sets the severity.
    /// </summary>
    public string? Severity { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreationDate { get; set; }

    /// <summary>
    /// Gets or sets the arrival date on WSUS.
    /// </summary>
    public DateTime? ArrivalDate { get; set; }

    /// <summary>
    /// Gets or sets the release notes URL.
    /// </summary>
    public string? ReleaseNotesUrl { get; set; }

    /// <summary>
    /// Gets or sets the support URL.
    /// </summary>
    public string? SupportUrl { get; set; }

    /// <summary>
    /// Gets or sets the more info URL.
    /// </summary>
    public string? MoreInfoUrl { get; set; }

    /// <summary>
    /// Gets or sets the product titles.
    /// </summary>
    public List<string> ProductTitles { get; set; } = [];

    /// <summary>
    /// Gets or sets the superseded update IDs.
    /// </summary>
    public List<Guid> SupersededUpdates { get; set; } = [];

    /// <summary>
    /// Gets or sets the superseding update IDs.
    /// </summary>
    public List<Guid> SupersedingUpdates { get; set; } = [];

    /// <summary>
    /// Gets or sets the prerequisite update IDs.
    /// </summary>
    public List<Guid> Prerequisites { get; set; } = [];

    /// <summary>
    /// Gets or sets the file information.
    /// </summary>
    public List<UpdateFileInfo> Files { get; set; } = [];

    /// <summary>
    /// Gets or sets the CVE identifiers.
    /// </summary>
    public List<string> CveIds { get; set; } = [];

    /// <summary>
    /// Gets or sets approval information by group.
    /// </summary>
    public List<UpdateApprovalInfo> Approvals { get; set; } = [];

    /// <summary>
    /// Gets or sets installation statistics.
    /// </summary>
    public UpdateInstallationStats? InstallationStats { get; set; }

    /// <summary>
    /// Gets or sets whether the update is approved.
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Gets or sets whether the update is declined.
    /// </summary>
    public bool IsDeclined { get; set; }

    /// <summary>
    /// Gets or sets whether the update is superseded.
    /// </summary>
    public bool IsSuperseded { get; set; }

    /// <summary>
    /// Gets or sets whether the update requires a reboot.
    /// </summary>
    public bool RequiresReboot { get; set; }

    /// <summary>
    /// Gets or sets whether the update can be uninstalled.
    /// </summary>
    public bool CanUninstall { get; set; }

    /// <summary>
    /// Gets or sets the total file size in bytes.
    /// </summary>
    public long TotalFileSize { get; set; }
}

/// <summary>
/// File information for an update.
/// </summary>
public sealed class UpdateFileInfo
{
    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the download URL.
    /// </summary>
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// Gets or sets the file hash.
    /// </summary>
    public string? Hash { get; set; }

    /// <summary>
    /// Gets or sets the hash algorithm.
    /// </summary>
    public string? HashAlgorithm { get; set; }
}

/// <summary>
/// Approval information for an update.
/// </summary>
public sealed class UpdateApprovalInfo
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
    /// Gets or sets the approval action.
    /// </summary>
    public string ApprovalAction { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the approval date.
    /// </summary>
    public DateTime? ApprovalDate { get; set; }

    /// <summary>
    /// Gets or sets the approving administrator.
    /// </summary>
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Gets or sets the deadline if set.
    /// </summary>
    public DateTime? Deadline { get; set; }
}

/// <summary>
/// Installation statistics for an update.
/// </summary>
public sealed class UpdateInstallationStats
{
    /// <summary>
    /// Gets or sets the number of computers that need the update.
    /// </summary>
    public int NeededCount { get; set; }

    /// <summary>
    /// Gets or sets the number of computers with the update installed.
    /// </summary>
    public int InstalledCount { get; set; }

    /// <summary>
    /// Gets or sets the number of computers with installation failed.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of computers where the update is not applicable.
    /// </summary>
    public int NotApplicableCount { get; set; }

    /// <summary>
    /// Gets or sets the total computer count.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets the installation percentage.
    /// </summary>
    public double InstalledPercent =>
        TotalCount > 0 ? (double)InstalledCount / TotalCount * 100 : 0;
}
