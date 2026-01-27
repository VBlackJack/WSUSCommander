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
    [string]$GroupId
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

    # Get the group
    $groupGuid = [Guid]$GroupId
    $group = $wsus.GetComputerTargetGroup($groupGuid)

    if (-not $group) {
        Write-Error "Group not found: $GroupId" -ErrorAction Stop
    }

    # Check if this is a system group
    $systemGroupNames = @("All Computers", "Unassigned Computers")
    if ($group.Name -in $systemGroupNames) {
        Write-Error "Cannot delete system group: $($group.Name)" -ErrorAction Stop
    }

    # Check if group has computers
    $computerCount = $group.GetComputerTargets().Count
    if ($computerCount -gt 0) {
        Write-Error "Cannot delete group with $computerCount computers. Move or remove computers first." -ErrorAction Stop
    }

    # Check if group has child groups
    $childGroups = $group.GetChildTargetGroups()
    if ($childGroups.Count -gt 0) {
        Write-Error "Cannot delete group with $($childGroups.Count) child groups. Delete child groups first." -ErrorAction Stop
    }

    $groupName = $group.Name

    # Delete the group
    $wsus.DeleteComputerTargetGroup($groupGuid)

    return [PSCustomObject]@{
        Success   = $true
        GroupId   = $GroupId
        GroupName = $groupName
        Message   = "Group '$groupName' deleted successfully"
    }
}
catch {
    Write-Error "Failed to delete computer group: $_" -ErrorAction Stop
    throw $_
}
