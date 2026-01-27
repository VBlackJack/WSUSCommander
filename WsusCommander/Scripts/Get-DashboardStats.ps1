# Copyright 2025 Julien Bombled
# Licensed under the Apache License, Version 2.0

param(
    [Parameter(Mandatory = $true)]
    [string]$ServerName,

    [Parameter(Mandatory = $true)]
    [int]$Port,

    [Parameter(Mandatory = $true)]
    [bool]$UseSsl
)

try {
    # Load the WSUS assembly
    [reflection.assembly]::LoadWithPartialName("Microsoft.UpdateServices.Administration") | Out-Null

    # Connect to WSUS server
    $wsus = [Microsoft.UpdateServices.Administration.AdminProxy]::GetUpdateServer($ServerName, $UseSsl, $Port)

    if (-not $wsus) {
        Write-Error "Failed to connect to WSUS server: $ServerName" -ErrorAction Stop
    }

    # Helper function to safely get property value
    function Get-SafeProperty {
        param($Object, [string]$PropertyName, $DefaultValue = $false)
        try {
            $prop = $Object.PSObject.Properties[$PropertyName]
            if ($null -ne $prop) {
                return $prop.Value
            }
            # Try direct access
            return $Object.$PropertyName
        }
        catch {
            return $DefaultValue
        }
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

        try { $isDeclined = $update.IsDeclined } catch { $isDeclined = $false }
        try { $isSuperseded = $update.IsSuperseded } catch { $isSuperseded = $false }
        try { $isApproved = $update.IsApproved } catch { $isApproved = $false }
        try { $classification = $update.UpdateClassificationTitle } catch { $classification = "" }

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

    # Get computer stats
    $allComputersGroup = $wsus.GetComputerTargetGroups() | Where-Object { $_.Name -eq "All Computers" } | Select-Object -First 1
    $totalComputers = 0
    $computersNeedingUpdates = 0
    $computersUpToDate = 0

    if ($allComputersGroup) {
        $computers = $wsus.GetComputerTargetGroup($allComputersGroup.Id).GetComputerTargets()
        $totalComputers = @($computers).Count

        foreach ($computer in $computers) {
            try {
                $summary = $computer.GetUpdateInstallationSummary()
                if ($summary.NotInstalledCount -gt 0 -or $summary.DownloadedCount -gt 0 -or $summary.FailedCount -gt 0) {
                    $computersNeedingUpdates++
                }
                else {
                    $computersUpToDate++
                }
            }
            catch {
                # Skip computers with access issues
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
