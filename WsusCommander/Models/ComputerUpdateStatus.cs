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
/// Installation state of an update on a specific computer.
/// </summary>
public enum UpdateInstallationState
{
    /// <summary>Unknown state.</summary>
    Unknown,

    /// <summary>Update is not applicable to this computer.</summary>
    NotApplicable,

    /// <summary>Update is needed but not installed.</summary>
    NotInstalled,

    /// <summary>Update has been downloaded but not installed.</summary>
    Downloaded,

    /// <summary>Update is installed.</summary>
    Installed,

    /// <summary>Update installation failed.</summary>
    Failed,

    /// <summary>Update is installed but pending reboot.</summary>
    InstalledPendingReboot
}

/// <summary>
/// Approval status of an update.
/// </summary>
public enum UpdateApprovalStatus
{
    /// <summary>Update has not been approved.</summary>
    NotApproved,

    /// <summary>Update is approved for installation.</summary>
    Approved,

    /// <summary>Update is approved for uninstallation.</summary>
    Uninstall
}

/// <summary>
/// Represents the status of an update for a specific computer.
/// </summary>
public sealed class ComputerUpdateStatus
{
    /// <summary>
    /// Gets or sets the update ID.
    /// </summary>
    [Required]
    public Guid UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the update title.
    /// </summary>
    [Required]
    [StringLength(512, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the KB article number.
    /// </summary>
    [StringLength(256)]
    public string KbArticle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the update classification (e.g., Security Updates, Critical Updates).
    /// </summary>
    [StringLength(128)]
    public string Classification { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the installation state on this computer.
    /// </summary>
    public UpdateInstallationState InstallationState { get; set; }

    /// <summary>
    /// Gets or sets the installation state as a display string.
    /// </summary>
    [StringLength(128)]
    public string InstallationStateDisplay { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the approval status.
    /// </summary>
    public UpdateApprovalStatus ApprovalStatus { get; set; }

    /// <summary>
    /// Gets or sets the approval status as a display string.
    /// </summary>
    [StringLength(128)]
    public string ApprovalStatusDisplay { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this update is superseded by a newer update.
    /// </summary>
    public bool IsSuperseded { get; set; }

    /// <summary>
    /// Gets or sets the title of the update that supersedes this one.
    /// </summary>
    [StringLength(512)]
    public string SupersededBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the release date of the update.
    /// </summary>
    [Required]
    public DateTime ReleaseDate { get; set; }

    /// <summary>
    /// Gets or sets the severity level (Critical, Important, Moderate, Low).
    /// </summary>
    [StringLength(128)]
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Gets whether this update needs attention (not installed and not approved, or failed).
    /// </summary>
    public bool NeedsAttention =>
        (InstallationState == UpdateInstallationState.NotInstalled ||
         InstallationState == UpdateInstallationState.Downloaded) &&
        ApprovalStatus == UpdateApprovalStatus.NotApproved;

    /// <summary>
    /// Gets whether this update is in a failed state.
    /// </summary>
    public bool IsFailed => InstallationState == UpdateInstallationState.Failed;

    /// <summary>
    /// Gets whether this update is needed (not installed or downloaded).
    /// </summary>
    public bool IsNeeded =>
        InstallationState == UpdateInstallationState.NotInstalled ||
        InstallationState == UpdateInstallationState.Downloaded ||
        InstallationState == UpdateInstallationState.Failed;
}
