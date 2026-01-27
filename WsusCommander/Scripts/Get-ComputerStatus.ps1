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
    [string]$GroupId = ""
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

    # Get computer targets
    $computers = if ($GroupId -and $GroupId -ne "") {
        $groupGuid = [Guid]$GroupId
        $group = $wsus.GetComputerTargetGroup($groupGuid)
        $wsus.GetComputerTargets() | Where-Object {
            $_.GetComputerTargetGroups() | Where-Object { $_.Id -eq $group.Id }
        }
    }
    else {
        $wsus.GetComputerTargets() | Select-Object -First 100
    }

    # Create update scope for counting
    $updateScope = New-Object Microsoft.UpdateServices.Administration.UpdateScope

    # Transform to simplified objects for C#
    $result = @()
    foreach ($computer in $computers) {
        # Get update status summary
        $summary = $computer.GetUpdateInstallationSummary($updateScope)

        # Get primary group name
        $groups = $computer.GetComputerTargetGroups()
        $groupName = if ($groups -and $groups.Count -gt 0) {
            ($groups | Select-Object -First 1).Name
        }
        else {
            ""
        }

        $result += [PSCustomObject]@{
            ComputerId       = $computer.Id
            Name             = $computer.FullDomainName
            IpAddress        = if ($computer.IPAddress) { $computer.IPAddress.ToString() } else { "" }
            LastReportedTime = $computer.LastReportedStatusTime
            InstalledCount   = $summary.InstalledCount
            NeededCount      = $summary.NotInstalledCount + $summary.DownloadedCount
            FailedCount      = $summary.FailedCount
            GroupName        = $groupName
        }
    }

    return $result
}
catch {
    Write-Error "Failed to retrieve computer status: $_" -ErrorAction Stop
    throw $_
}
