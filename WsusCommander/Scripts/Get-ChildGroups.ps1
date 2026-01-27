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
    [string]$ParentGroupId
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

    # Get the parent group
    $parentGuid = [Guid]$ParentGroupId
    $parentGroup = $wsus.GetComputerTargetGroup($parentGuid)

    if (-not $parentGroup) {
        Write-Error "Parent group not found: $ParentGroupId" -ErrorAction Stop
    }

    # Get child groups
    $childGroups = $parentGroup.GetChildTargetGroups()

    $result = @()
    foreach ($child in $childGroups) {
        $result += [PSCustomObject]@{
            Id            = $child.Id.ToString()
            Name          = $child.Name
            Description   = $child.Description
            ComputerCount = $child.GetComputerTargets().Count
            ParentGroupId = $ParentGroupId
        }
    }

    return $result
}
catch {
    Write-Error "Failed to get child groups: $_" -ErrorAction Stop
    throw $_
}
