# Copyright 2025 Julien Bombled
# Licensed under the Apache License, Version 2.0

param(
    [Parameter(Mandatory = $true)]
    [string]$ServerName,

    [Parameter(Mandatory = $true)]
    [int]$Port,

    [Parameter(Mandatory = $true)]
    [bool]$UseSsl,

    [Parameter(Mandatory = $false)]
    [int]$ComplianceThresholdDays = 3,

    [Parameter(Mandatory = $false)]
    [string]$GroupId = "",

    [Parameter(Mandatory = $false)]
    [string]$NamePattern = ""
)

try {
    # Load the WSUS assembly
    [reflection.assembly]::LoadWithPartialName("Microsoft.UpdateServices.Administration") | Out-Null

    # Connect to WSUS server
    $wsus = [Microsoft.UpdateServices.Administration.AdminProxy]::GetUpdateServer($ServerName, $UseSsl, $Port)

    if (-not $wsus) {
        Write-Error "Failed to connect to WSUS server: $ServerName" -ErrorAction Stop
    }

    # Get all updates using basic scope
    $scope = New-Object Microsoft.UpdateServices.Administration.UpdateScope
    $allRawUpdates = @($wsus.GetUpdates($scope))

    # Count updates manually with safe property access
    $totalUpdates = 0
    $unapprovedUpdates = 0
    $supersededUpdates = 0
    $criticalPending = 0
    $securityPending = 0

    foreach ($update in $allRawUpdates) {
        $isDeclined = $false
        $isSuperseded = $false
        $isApproved = $false
        $classification = ""

        try {
            $declinedProperty = $update.PSObject.Properties["IsDeclined"]
            $isDeclined = if ($null -ne $declinedProperty) { [bool]$declinedProperty.Value } else { $false }
        }
        catch {
            $isDeclined = $false
        }

        try {
            $supersededProperty = $update.PSObject.Properties["IsSuperseded"]
            $isSuperseded = if ($null -ne $supersededProperty) { [bool]$supersededProperty.Value } else { $false }
        }
        catch {
            $isSuperseded = $false
        }

        try {
            $approvedProperty = $update.PSObject.Properties["IsApproved"]
            $isApproved = if ($null -ne $approvedProperty) { [bool]$approvedProperty.Value } else { $false }
        }
        catch {
            $isApproved = $false
        }

        try {
            $classificationProperty = $update.PSObject.Properties["UpdateClassificationTitle"]
            $classification = if ($null -ne $classificationProperty) { [string]$classificationProperty.Value } else { "" }
        }
        catch {
            $classification = ""
        }

        if (-not $isDeclined) {
            $totalUpdates++

            if (-not $isApproved) {
                $unapprovedUpdates++

                if (-not $isSuperseded) {
                    if ($classification -eq "Critical Updates") {
                        $criticalPending++
                    }
                    elseif ($classification -eq "Security Updates") {
                        $securityPending++
                    }
                }
            }

            if ($isSuperseded) {
                $supersededUpdates++
            }
        }
    }

    # Get computer stats - exclude "Unassigned Computers" from compliance calculations
    # Compliance is based on Critical/Security updates approved more than $ComplianceThresholdDays days ago
    # Optional filters: GroupId (specific group) and NamePattern (wildcard match on computer name)
    $targetGroup = $null
    if ($GroupId -and $GroupId -ne "") {
        $groupGuid = [Guid]$GroupId
        $targetGroup = $wsus.GetComputerTargetGroup($groupGuid)
    }
    else {
        $targetGroup = $wsus.GetComputerTargetGroups() | Where-Object { $_.Name -eq "All Computers" } | Select-Object -First 1
    }

    $unassignedGroup = $wsus.GetComputerTargetGroups() | Where-Object { $_.Name -eq "Unassigned Computers" } | Select-Object -First 1
    $totalComputers = 0
    $computersNeedingUpdates = 0
    $computersUpToDate = 0

    # Build list of mandatory update IDs (Critical/Security approved > threshold days ago)
    $thresholdDate = (Get-Date).AddDays(-$ComplianceThresholdDays)
    $mandatoryUpdateIds = @{}

    foreach ($update in $allRawUpdates) {
        try {
            $isDeclined = $false
            $isApproved = $false
            $classification = ""

            $declinedProperty = $update.PSObject.Properties["IsDeclined"]
            $isDeclined = if ($null -ne $declinedProperty) { [bool]$declinedProperty.Value } else { $false }

            if ($isDeclined) { continue }

            $approvedProperty = $update.PSObject.Properties["IsApproved"]
            $isApproved = if ($null -ne $approvedProperty) { [bool]$approvedProperty.Value } else { $false }

            if (-not $isApproved) { continue }

            $classificationProperty = $update.PSObject.Properties["UpdateClassificationTitle"]
            $classification = if ($null -ne $classificationProperty) { [string]$classificationProperty.Value } else { "" }

            # Only Critical and Security updates count for compliance
            if ($classification -ne "Critical Updates" -and $classification -ne "Security Updates") { continue }

            # Check approval date - only count if approved more than threshold days ago
            $approvals = $update.GetUpdateApprovals()
            $isOldEnough = $false
            foreach ($approval in $approvals) {
                if ($approval.Action -eq [Microsoft.UpdateServices.Administration.UpdateApprovalAction]::Install) {
                    if ($approval.GoLiveTime -lt $thresholdDate) {
                        $isOldEnough = $true
                        break
                    }
                }
            }

            if ($isOldEnough) {
                $mandatoryUpdateIds[$update.Id.UpdateId.ToString()] = $true
            }
        }
        catch {
            # Skip updates with access issues
        }
    }

    if ($targetGroup) {
        $computers = $wsus.GetComputerTargetGroup($targetGroup.Id).GetComputerTargets()

        # Build list of unassigned computer IDs to exclude (only if not filtering by specific group)
        $unassignedIds = @()
        if (-not $GroupId -and $unassignedGroup) {
            $unassignedComputers = $wsus.GetComputerTargetGroup($unassignedGroup.Id).GetComputerTargets()
            $unassignedIds = @($unassignedComputers | ForEach-Object { $_.Id })
        }

        foreach ($computer in $computers) {
            # Skip computers in Unassigned Computers group (unless filtering by specific group)
            if ($unassignedIds -contains $computer.Id) {
                continue
            }

            # Apply name pattern filter if specified
            if ($NamePattern -and $NamePattern -ne "") {
                if ($computer.FullDomainName -notlike $NamePattern) {
                    continue
                }
            }

            $totalComputers++

            try {
                # Check if computer has any mandatory updates not installed
                $hasNonCompliantUpdates = $false

                if ($mandatoryUpdateIds.Count -gt 0) {
                    $computerScope = New-Object Microsoft.UpdateServices.Administration.UpdateScope
                    $computerScope.ApprovedStates = [Microsoft.UpdateServices.Administration.ApprovedStates]::HasStaleUpdateApprovals -bor [Microsoft.UpdateServices.Administration.ApprovedStates]::LatestRevisionApproved
                    $computerScope.IncludedInstallationStates = [Microsoft.UpdateServices.Administration.UpdateInstallationStates]::NotInstalled -bor [Microsoft.UpdateServices.Administration.UpdateInstallationStates]::Downloaded -bor [Microsoft.UpdateServices.Administration.UpdateInstallationStates]::Failed

                    $pendingUpdates = $computer.GetUpdateInstallationInfoPerUpdate($computerScope)

                    foreach ($updateInfo in $pendingUpdates) {
                        $updateIdStr = $updateInfo.UpdateId.ToString()
                        if ($mandatoryUpdateIds.ContainsKey($updateIdStr)) {
                            $hasNonCompliantUpdates = $true
                            break
                        }
                    }
                }

                if ($hasNonCompliantUpdates) {
                    $computersNeedingUpdates++
                }
                else {
                    $computersUpToDate++
                }
            }
            catch {
                # Skip computers with access issues - count as up to date to avoid false negatives
                $computersUpToDate++
            }
        }
    }

    # Calculate compliance percentage
    $compliancePercent = 0
    if ($totalComputers -gt 0) {
        $compliancePercent = [math]::Round(($computersUpToDate / $totalComputers) * 100, 1)
    }

    # Get last sync time
    $lastSyncTime = $null
    try {
        $syncInfo = $wsus.GetSubscription()
        $lastSyncTime = $syncInfo.LastSynchronizationTime
    }
    catch {
        $lastSyncTime = $null
    }

    return [PSCustomObject]@{
        TotalUpdates            = $totalUpdates
        UnapprovedUpdates       = $unapprovedUpdates
        SupersededUpdates       = $supersededUpdates
        CriticalPending         = $criticalPending
        SecurityPending         = $securityPending
        TotalComputers          = $totalComputers
        ComputersNeedingUpdates = $computersNeedingUpdates
        ComputersUpToDate       = $computersUpToDate
        CompliancePercent       = $compliancePercent
        LastSyncTime            = $lastSyncTime
    }
}
catch {
    Write-Error "Failed to get dashboard stats: $_" -ErrorAction Stop
    throw $_
}
