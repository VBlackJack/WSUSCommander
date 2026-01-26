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
    # Defensive coding: Check if module exists
    if (-not (Get-Module -ListAvailable -Name UpdateServices)) {
        Write-Error "WSUS Module (UpdateServices) is not installed on this machine." -ErrorAction Stop
    }

    # Connect to WSUS server
    $wsus = [Microsoft.UpdateServices.Administration.AdminProxy]::GetUpdateServer($ServerName, $UseSsl, $Port)

    if (-not $wsus) {
        Write-Error "Failed to connect to WSUS server: $ServerName" -ErrorAction Stop
    }

    # Get all computer target groups
    $groups = $wsus.GetComputerTargetGroups()

    # Transform to simplified objects for C#
    $result = @()
    foreach ($group in $groups) {
        $computerCount = ($wsus.GetComputerTargets() | Where-Object {
            $_.GetComputerTargetGroups() | Where-Object { $_.Id -eq $group.Id }
        }).Count

        $result += [PSCustomObject]@{
            Id            = $group.Id.ToString()
            Name          = $group.Name
            Description   = if ($group.Description) { $group.Description } else { "" }
            ComputerCount = $computerCount
        }
    }

    return $result
}
catch {
    Write-Error "Failed to retrieve computer groups: $_" -ErrorAction Stop
    throw $_
}
