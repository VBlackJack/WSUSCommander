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
    [string]$ComputerId,

    [Parameter(Mandatory = $false)]
    [string]$StatusFilter = "All"  # All, Needed, NotApproved, Failed
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

    # Get the computer target
    $computer = $wsus.GetComputerTarget($ComputerId)

    if (-not $computer) {
        Write-Error "Computer not found: $ComputerId" -ErrorAction Stop
    }

    # Create update scope based on filter
    $updateScope = New-Object Microsoft.UpdateServices.Administration.UpdateScope

    # Get update installation info for this computer
    $updateInfos = $computer.GetUpdateInstallationInfoPerUpdate($updateScope)

    # Transform to simplified objects for C#
    $result = @()
    foreach ($info in $updateInfos) {
        # Get the update object for additional details
        $update = $info.GetUpdate()

        # Determine installation state string
        $installState = switch ($info.UpdateInstallationState) {
            "Unknown" { "Unknown" }
            "NotApplicable" { "NotApplicable" }
            "NotInstalled" { "NotInstalled" }
            "Downloaded" { "Downloaded" }
            "Installed" { "Installed" }
            "Failed" { "Failed" }
            "InstalledPendingReboot" { "InstalledPendingReboot" }
            default { $info.UpdateInstallationState.ToString() }
        }

        # Determine approval state string
        $approvalState = switch ($info.UpdateApprovalAction) {
            "Install" { "Approved" }
            "Uninstall" { "Uninstall" }
            "NotApproved" { "NotApproved" }
            default { $info.UpdateApprovalAction.ToString() }
        }

        # Apply status filter
        $include = $false
        switch ($StatusFilter) {
            "All" { $include = $true }
            "Needed" { $include = $installState -in @("NotInstalled", "Downloaded", "Failed") }
            "NotApproved" { $include = $approvalState -eq "NotApproved" -and $installState -in @("NotInstalled", "Downloaded") }
            "Failed" { $include = $installState -eq "Failed" }
            default { $include = $true }
        }

        if (-not $include) {
            continue
        }

        # Skip if update is declined
        if ($update.IsDeclined) {
            continue
        }

        # Join KB articles if multiple exist
        $kbArticles = ""
        if ($update.KnowledgebaseArticles -and $update.KnowledgebaseArticles.Count -gt 0) {
            $kbArticles = ($update.KnowledgebaseArticles | ForEach-Object { "KB$_" }) -join ", "
        }

        # Check if superseded
        $isSuperseded = $update.IsSuperseded
        $supersededBy = ""
        if ($isSuperseded) {
            $supersedingUpdates = $update.GetRelatedUpdates([Microsoft.UpdateServices.Administration.UpdateRelationship]::UpdatesThatSupersedeThisUpdate)
            if ($supersedingUpdates -and $supersedingUpdates.Count -gt 0) {
                $supersededBy = ($supersedingUpdates | Select-Object -First 1).Title
            }
        }

        $result += [PSCustomObject]@{
            UpdateId         = $update.Id.UpdateId.ToString()
            Title            = $update.Title
            KbArticle        = $kbArticles
            Classification   = $update.UpdateClassificationTitle
            InstallationState = $installState
            ApprovalStatus   = $approvalState
            IsSuperseded     = $isSuperseded
            SupersededBy     = $supersededBy
            ReleaseDate      = $update.CreationDate
            Severity         = if ($update.MsrcSeverity) { $update.MsrcSeverity } else { "" }
        }
    }

    return $result
}
catch {
    Write-Error "Failed to retrieve computer updates: $_" -ErrorAction Stop
    throw $_
}
