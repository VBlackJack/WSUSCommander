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

    # Get computers
    $allComputers = $wsus.GetComputerTargets()
    $totalComputers = $allComputers.Count

    # Create update scope for later
    $updateScope = New-Object Microsoft.UpdateServices.Administration.UpdateScope

    # Calculate compliance per group
    $groupCompliance = @()
    $totalNeeded = 0
    $totalFailed = 0
    $compliantComputers = 0

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

    # Calculate overall compliance
    foreach ($computer in $allComputers) {
        $status = $computer.GetUpdateInstallationSummary($updateScope)
        $needed = $status.NotInstalledCount + $status.DownloadedCount
        $failed = $status.FailedCount

        if ($needed -eq 0 -and $failed -eq 0) {
            $compliantComputers++
        }
    }

    $overallCompliancePercent = if ($totalComputers -gt 0) { ($compliantComputers / $totalComputers) * 100 } else { 0 }

    # Get approved updates count
    $approvedScope = New-Object Microsoft.UpdateServices.Administration.UpdateScope
    $approvedScope.ApprovedStates = [Microsoft.UpdateServices.Administration.ApprovedStates]::LatestRevisionApproved
    $approvedUpdates = $wsus.GetUpdates($approvedScope)

    return [PSCustomObject]@{
        TotalComputers       = $totalComputers
        CompliantComputers   = $compliantComputers
        NonCompliantComputers = $totalComputers - $compliantComputers
        TotalApprovedUpdates = $approvedUpdates.Count
        CompliancePercent    = [math]::Round($overallCompliancePercent, 1)
        GroupCompliance      = $groupCompliance
        TotalNeededUpdates   = $totalNeeded
        TotalFailedUpdates   = $totalFailed
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
