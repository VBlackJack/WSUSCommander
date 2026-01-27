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
    [string]$GroupName,

    [Parameter(Mandatory = $false)]
    [string]$Description = "",

    [Parameter(Mandatory = $false)]
    [string]$ParentGroupId = ""
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

    # Get parent group if specified
    $parentGroup = $null
    if ($ParentGroupId -and $ParentGroupId -ne "") {
        $parentGuid = [Guid]$ParentGroupId
        $parentGroup = $wsus.GetComputerTargetGroup($parentGuid)
        if (-not $parentGroup) {
            Write-Error "Parent group not found: $ParentGroupId" -ErrorAction Stop
        }
    }

    # Create the new group
    if ($parentGroup) {
        $newGroup = $wsus.CreateComputerTargetGroup($GroupName, $parentGroup)
    }
    else {
        $newGroup = $wsus.CreateComputerTargetGroup($GroupName)
    }

    if (-not $newGroup) {
        Write-Error "Failed to create group: $GroupName" -ErrorAction Stop
    }

    # Set description if provided
    if ($Description -and $Description -ne "") {
        $newGroup.Description = $Description
        $newGroup.Save()
    }

    return [PSCustomObject]@{
        Id            = $newGroup.Id.ToString()
        Name          = $newGroup.Name
        Description   = $newGroup.Description
        ComputerCount = 0
        ParentGroupId = if ($parentGroup) { $parentGroup.Id.ToString() } else { $null }
    }
}
catch {
    Write-Error "Failed to create computer group: $_" -ErrorAction Stop
    throw $_
}
