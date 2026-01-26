# Copyright 2025 Julien Bombled
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

<#
.SYNOPSIS
    Gets detailed information about a WSUS update.

.PARAMETER UpdateId
    The GUID of the update.

.PARAMETER WsusServer
    The WSUS server name.

.PARAMETER WsusPort
    The WSUS server port.

.PARAMETER UseSsl
    Whether to use SSL connection.
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$UpdateId,

    [Parameter(Mandatory = $false)]
    [string]$WsusServer = "localhost",

    [Parameter(Mandatory = $false)]
    [int]$WsusPort = 8530,

    [Parameter(Mandatory = $false)]
    [bool]$UseSsl = $false
)

try {
    # Import WSUS module
    if (-not (Get-Module -ListAvailable -Name UpdateServices)) {
        throw "WSUS PowerShell module (UpdateServices) is not installed."
    }

    Import-Module UpdateServices -ErrorAction Stop

    # Connect to WSUS server
    $wsus = Get-WsusServer -Name $WsusServer -PortNumber $WsusPort -UseSsl:$UseSsl

    if (-not $wsus) {
        throw "Failed to connect to WSUS server: $WsusServer"
    }

    # Get the update
    $updateGuid = [Guid]::Parse($UpdateId)
    $update = $wsus.GetUpdate([Microsoft.UpdateServices.Administration.UpdateRevisionId]::new($updateGuid, 0))

    if (-not $update) {
        throw "Update not found: $UpdateId"
    }

    # Get approvals
    $approvals = @()
    $updateApprovals = $update.GetUpdateApprovals()
    foreach ($approval in $updateApprovals) {
        $group = $wsus.GetComputerTargetGroup($approval.ComputerTargetGroupId)
        $approvals += @{
            GroupId = $approval.ComputerTargetGroupId.ToString()
            GroupName = $group.Name
            ApprovalAction = $approval.Action.ToString()
            ApprovalDate = $approval.GoLiveTime
            ApprovedBy = $approval.AdministratorName
            Deadline = $approval.Deadline
        }
    }

    # Get files
    $files = @()
    foreach ($file in $update.GetInstallableItems()) {
        foreach ($fileUrl in $file.Files) {
            $files += @{
                FileName = $fileUrl.Name
                FileSize = $fileUrl.TotalBytes
                DownloadUrl = $fileUrl.OriginUri.ToString()
                Hash = $fileUrl.Hash
                HashAlgorithm = $fileUrl.HashAlgorithm
            }
        }
    }

    # Get installation statistics
    $stats = $update.GetSummary()
    $installStats = @{
        NeededCount = $stats.NotInstalledCount + $stats.DownloadedCount
        InstalledCount = $stats.InstalledCount + $stats.InstalledPendingRebootCount
        FailedCount = $stats.FailedCount
        NotApplicableCount = $stats.NotApplicableCount
        TotalCount = $stats.ComputerTargetCount
    }

    # Get supersedence information
    $supersededUpdates = @()
    $supersedingUpdates = @()

    try {
        foreach ($supersededId in $update.GetRelatedUpdates([Microsoft.UpdateServices.Administration.UpdateRelationship]::UpdatesSupersededByThisUpdate)) {
            $supersededUpdates += $supersededId.UpdateId.ToString()
        }
    }
    catch {
        # Ignore errors getting superseded updates
    }

    try {
        foreach ($supersedingId in $update.GetRelatedUpdates([Microsoft.UpdateServices.Administration.UpdateRelationship]::UpdatesThatSupersedeThisUpdate)) {
            $supersedingUpdates += $supersedingId.UpdateId.ToString()
        }
    }
    catch {
        # Ignore errors getting superseding updates
    }

    # Get prerequisites
    $prerequisites = @()
    try {
        foreach ($prereqId in $update.GetRelatedUpdates([Microsoft.UpdateServices.Administration.UpdateRelationship]::UpdatesThatRequireThisUpdate)) {
            $prerequisites += $prereqId.UpdateId.ToString()
        }
    }
    catch {
        # Ignore errors getting prerequisites
    }

    # Get CVE IDs
    $cveIds = @()
    try {
        foreach ($cve in $update.SecurityBulletins) {
            $cveIds += $cve
        }
        foreach ($cve in $update.CveIds) {
            if ($cve -notin $cveIds) {
                $cveIds += $cve
            }
        }
    }
    catch {
        # Ignore errors getting CVEs
    }

    # Calculate total file size
    $totalFileSize = ($files | Measure-Object -Property FileSize -Sum).Sum

    @{
        Id = $update.Id.UpdateId.ToString()
        Title = $update.Title
        KbArticle = $update.KnowledgebaseArticles -join ", "
        Description = $update.Description
        Classification = $update.UpdateClassificationTitle
        Severity = $update.MsrcSeverity
        CreationDate = $update.CreationDate
        ArrivalDate = $update.ArrivalDate
        ReleaseNotesUrl = $update.ReleaseNotes
        SupportUrl = $update.SupportUrl
        MoreInfoUrl = $update.AdditionalInformationUrls -join "; "
        ProductTitles = @($update.ProductTitles)
        SupersededUpdates = $supersededUpdates
        SupersedingUpdates = $supersedingUpdates
        Prerequisites = $prerequisites
        Files = $files
        CveIds = $cveIds
        Approvals = $approvals
        InstallationStats = $installStats
        IsApproved = $update.IsApproved
        IsDeclined = $update.IsDeclined
        IsSuperseded = $update.IsSuperseded
        RequiresReboot = $update.InstallationBehavior.RebootBehavior -ne "NeverReboots"
        CanUninstall = $update.UninstallationBehavior -ne $null
        TotalFileSize = $totalFileSize
    }
}
catch {
    @{
        Error = $_.Exception.Message
        UpdateId = $UpdateId
    }
}
