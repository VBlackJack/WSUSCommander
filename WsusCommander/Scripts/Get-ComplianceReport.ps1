# Copyright 2025 Julien Bombled
# Licensed under the Apache License, Version 2.0

param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [ValidatePattern('^[a-zA-Z0-9\-_.]+$')]
    [string]$ServerName,

    [Parameter(Mandatory = $true)]
    [ValidateRange(1, 65535)]
    [int]$Port,

    [Parameter(Mandatory = $true)]
    [bool]$UseSsl,

    [Parameter(Mandatory = $false)]
    [ValidatePattern('^[0-9a-fA-F-]{36}$')]
    [string]$GroupId = "",

    [Parameter(Mandatory = $false)]
    [ValidateRange(1, 3650)]
    [int]$StaleDays = 30,

    [Parameter(Mandatory = $false)]
    [bool]$IncludeSuperseded = $false,

    [Parameter(Mandatory = $false)]
    [bool]$IncludeDeclined = $false
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

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

    # Get target group
    $targetGroup = $null
    if ($GroupId -and $GroupId -ne "") {
        $groupGuid = [Guid]$GroupId
        $targetGroup = $wsus.GetComputerTargetGroup($groupGuid)
    }

    # Get all computer target groups
    $groups = $wsus.GetComputerTargetGroups()

    # Filter to specific group if requested
    if ($targetGroup) {
        $groups = @($targetGroup)
    }

    # Create update scope for later
    $updateScope = New-Object Microsoft.UpdateServices.Administration.UpdateScope

    # Get computers - filter by group if specified
    if ($targetGroup) {
        $allComputers = $targetGroup.GetComputerTargets()
    }
    else {
        $allComputers = $wsus.GetComputerTargets()
    }
    $totalComputers = $allComputers.Count

    # Calculate compliance per group
    $groupCompliance = @()
    $totalNeeded = 0
    $totalFailed = 0
    $compliantComputers = 0

    # Computer details list
    $computerDetails = @()

    foreach ($group in $groups) {
        # Skip "All Computers" group in per-group stats
        if ($group.Name -eq "All Computers") {
            continue
        }

        $groupComputers = $group.GetComputerTargets()
        $groupTotal = $groupComputers.Count

        if ($groupTotal -eq 0) {
            continue
        }

        $groupNeeded = 0
        $groupFailed = 0
        $groupCompliant = 0

        foreach ($computer in $groupComputers) {
            $status = $computer.GetUpdateInstallationSummary($updateScope)
            $needed = $status.NotInstalledCount + $status.DownloadedCount
            $failed = $status.FailedCount

            $groupNeeded += $needed
            $groupFailed += $failed

            if ($needed -eq 0 -and $failed -eq 0) {
                $groupCompliant++
            }
        }

        $compliancePercent = if ($groupTotal -gt 0) { ($groupCompliant / $groupTotal) * 100 } else { 0 }

        $groupCompliance += [PSCustomObject]@{
            GroupId            = $group.Id.ToString()
            GroupName          = $group.Name
            TotalComputers     = $groupTotal
            CompliantComputers = $groupCompliant
            CompliancePercent  = [math]::Round($compliancePercent, 1)
            TotalNeededUpdates = $groupNeeded
            TotalFailedUpdates = $groupFailed
        }

        $totalNeeded += $groupNeeded
        $totalFailed += $groupFailed
    }

    # Calculate overall compliance and collect computer details
    foreach ($computer in $allComputers) {
        $status = $computer.GetUpdateInstallationSummary($updateScope)
        $needed = $status.NotInstalledCount + $status.DownloadedCount
        $failed = $status.FailedCount
        $installed = $status.InstalledCount
        $downloaded = $status.DownloadedCount
        $notInstalled = $status.NotInstalledCount

        $isCompliant = ($needed -eq 0 -and $failed -eq 0)
        if ($isCompliant) {
            $compliantComputers++
        }

        # Calculate computer compliance percent
        $totalApplicable = $installed + $needed + $failed
        $computerCompliancePercent = if ($totalApplicable -gt 0) { ($installed / $totalApplicable) * 100 } else { 100 }

        # Get computer's group memberships
        $computerGroups = @()
        try {
            $computerGroupsObj = $computer.GetComputerTargetGroups()
            $computerGroups = @($computerGroupsObj | ForEach-Object { $_.Name })
        }
        catch {
            $computerGroups = @()
        }

        $computerDetails += [PSCustomObject]@{
            ComputerId        = $computer.Id.ToString()
            ComputerName      = $computer.FullDomainName
            IpAddress         = $computer.IPAddress
            LastReportedTime  = $computer.LastReportedStatusTime
            LastSyncTime      = $computer.LastSyncTime
            OSDescription     = $computer.OSDescription
            IsCompliant       = $isCompliant
            CompliancePercent = [math]::Round($computerCompliancePercent, 1)
            InstalledCount    = $installed
            NeededCount       = $needed
            DownloadedCount   = $downloaded
            NotInstalledCount = $notInstalled
            FailedCount       = $failed
            Groups            = $computerGroups
        }
    }

    $overallCompliancePercent = if ($totalComputers -gt 0) { ($compliantComputers / $totalComputers) * 100 } else { 0 }

    # Get approved updates count
    $approvedScope = New-Object Microsoft.UpdateServices.Administration.UpdateScope
    $approvedScope.ApprovedStates = [Microsoft.UpdateServices.Administration.ApprovedStates]::LatestRevisionApproved
    $approvedUpdates = $wsus.GetUpdates($approvedScope)

    return [PSCustomObject]@{
        TotalComputers        = $totalComputers
        CompliantComputers    = $compliantComputers
        NonCompliantComputers = $totalComputers - $compliantComputers
        TotalApprovedUpdates  = $approvedUpdates.Count
        CompliancePercent     = [math]::Round($overallCompliancePercent, 1)
        GroupCompliance       = $groupCompliance
        ComputerDetails       = $computerDetails
        TotalNeededUpdates    = $totalNeeded
        TotalFailedUpdates    = $totalFailed
    }
}
catch {
    $errorResult = @{
        Success = $false
        Error   = @{
            Message = $_.Exception.Message
            Type    = $_.Exception.GetType().Name
        }
    }
    Write-Output ($errorResult | ConvertTo-Json -Depth 5 -Compress)
    exit 1
}
