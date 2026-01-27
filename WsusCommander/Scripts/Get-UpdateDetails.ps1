# Copyright 2025 Julien Bombled
# Licensed under the Apache License, Version 2.0

param(
    [Parameter(Mandatory = $true)]
    [string]$ServerName,

    [Parameter(Mandatory = $true)]
    [int]$Port,

    [Parameter(Mandatory = $true)]
    [bool]$UseSsl,

    [Parameter(Mandatory = $true)]
    [string]$UpdateId
)

try {
    # Defensive coding: Check if module exists
    if (-not (Get-Module -ListAvailable -Name UpdateServices)) {
        Write-Error "WSUS Module (UpdateServices) is not installed on this machine." -ErrorAction Stop
    }

    # Load the WSUS assembly
    [reflection.assembly]::LoadWithPartialName("Microsoft.UpdateServices.Administration") | Out-Null

    # Connect to WSUS server
    $wsus = [Microsoft.UpdateServices.Administration.AdminProxy]::GetUpdateServer($ServerName, $UseSsl, $Port)

    if (-not $wsus) {
        Write-Error "Failed to connect to WSUS server: $ServerName" -ErrorAction Stop
    }

    # Get the update
    $updateGuid = [Guid]$UpdateId
    $updateRevisionId = New-Object Microsoft.UpdateServices.Administration.UpdateRevisionId($updateGuid)
    $update = $wsus.GetUpdate($updateRevisionId)

    if (-not $update) {
        Write-Error "Update not found: $UpdateId" -ErrorAction Stop
    }

    # Get approvals
    $approvals = @()
    $updateApprovals = $update.GetUpdateApprovals()
    foreach ($approval in $updateApprovals) {
        $group = $wsus.GetComputerTargetGroup($approval.ComputerTargetGroupId)
        $approvals += [PSCustomObject]@{
            GroupId        = $approval.ComputerTargetGroupId.ToString()
            GroupName      = $group.Name
            ApprovalAction = $approval.Action.ToString()
            ApprovalDate   = $approval.GoLiveTime
            ApprovedBy     = $approval.AdministratorName
            Deadline       = $approval.Deadline
        }
    }

    # Get files
    $files = @()
    foreach ($file in $update.GetInstallableItems()) {
        foreach ($fileUrl in $file.Files) {
            $files += [PSCustomObject]@{
                FileName      = $fileUrl.Name
                FileSize      = $fileUrl.TotalBytes
                DownloadUrl   = $fileUrl.OriginUri.ToString()
                Hash          = $fileUrl.Hash
                HashAlgorithm = $fileUrl.HashAlgorithm
            }
        }
    }

    # Get installation statistics
    $computerScope = New-Object Microsoft.UpdateServices.Administration.ComputerTargetScope
    $stats = $update.GetSummary($computerScope)
    $installStats = [PSCustomObject]@{
        NeededCount        = $stats.NotInstalledCount + $stats.DownloadedCount
        InstalledCount     = $stats.InstalledCount + $stats.InstalledPendingRebootCount
        FailedCount        = $stats.FailedCount
        NotApplicableCount = $stats.NotApplicableCount
        TotalCount         = $stats.ComputerTargetCount
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

    return [PSCustomObject]@{
        Id                 = $update.Id.UpdateId.ToString()
        Title              = $update.Title
        KbArticle          = $update.KnowledgebaseArticles -join ", "
        Description        = $update.Description
        Classification     = $update.UpdateClassificationTitle
        Severity           = $update.MsrcSeverity
        CreationDate       = $update.CreationDate
        ArrivalDate        = $update.ArrivalDate
        ReleaseNotesUrl    = $update.ReleaseNotes
        SupportUrl         = $update.SupportUrl
        MoreInfoUrl        = $update.AdditionalInformationUrls -join "; "
        ProductTitles      = @($update.ProductTitles)
        SupersededUpdates  = $supersededUpdates
        SupersedingUpdates = $supersedingUpdates
        Prerequisites      = $prerequisites
        Files              = $files
        CveIds             = $cveIds
        Approvals          = $approvals
        InstallationStats  = $installStats
        IsApproved         = $update.IsApproved
        IsDeclined         = $update.IsDeclined
        IsSuperseded       = $update.IsSuperseded
        RequiresReboot     = $update.InstallationBehavior.RebootBehavior -ne "NeverReboots"
        CanUninstall       = $update.UninstallationBehavior -ne $null
        TotalFileSize      = $totalFileSize
    }
}
catch {
    Write-Error "Failed to get update details: $_" -ErrorAction Stop
    throw $_
}
