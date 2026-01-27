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
    [string]$GroupId,

    [Parameter(Mandatory = $false)]
    [string]$NewName = "",

    [Parameter(Mandatory = $false)]
    [string]$Description = ""
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
        Write-Error "Cannot modify system group: $($group.Name)" -ErrorAction Stop
    }

    # Update name if provided
    if ($NewName -and $NewName -ne "") {
        # WSUS doesn't allow renaming groups directly
        Write-Error "Renaming groups is not supported by WSUS. Create a new group and move computers instead." -ErrorAction Stop
    }

    # Update description if provided
    if ($Description -ne "") {
        $group.Description = $Description
        $group.Save()
    }

    return [PSCustomObject]@{
        Id            = $group.Id.ToString()
        Name          = $group.Name
        Description   = $group.Description
        ComputerCount = $group.GetComputerTargets().Count
        ParentGroupId = if ($group.GetParentTargetGroup()) { $group.GetParentTargetGroup().Id.ToString() } else { $null }
    }
}
catch {
    Write-Error "Failed to update computer group: $_" -ErrorAction Stop
    throw $_
}
