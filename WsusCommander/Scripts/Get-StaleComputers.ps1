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
    [int]$StaleDays = 30
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

    $staleDate = (Get-Date).AddDays(-$StaleDays)
    $staleComputers = @()

    # Get all computers
    $computers = $wsus.GetComputerTargets()

    foreach ($computer in $computers) {
        $lastReport = $computer.LastReportedStatusTime

        if ($null -eq $lastReport -or $lastReport -lt $staleDate) {
            # Get group memberships
            $groups = $computer.GetComputerTargetGroups()
            $groupNames = @($groups | ForEach-Object { $_.Name })

            $daysSinceReport = if ($null -eq $lastReport) {
                9999
            }
            else {
                [math]::Floor(((Get-Date) - $lastReport).TotalDays)
            }

            $staleComputers += [PSCustomObject]@{
                ComputerId          = $computer.Id
                ComputerName        = $computer.FullDomainName
                LastReportTime      = $lastReport
                DaysSinceLastReport = $daysSinceReport
                GroupNames          = $groupNames
            }
        }
    }

    # Sort by days since last report (most stale first)
    return $staleComputers | Sort-Object -Property DaysSinceLastReport -Descending
}
catch {
    Write-Error "Failed to get stale computers: $_" -ErrorAction Stop
    throw $_
}
