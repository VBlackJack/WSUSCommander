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
    [string]$UpdateId,

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

    # Get the update
    $updateGuid = [Guid]$UpdateId
    $updateRevisionId = New-Object Microsoft.UpdateServices.Administration.UpdateRevisionId($updateGuid)
    $update = $wsus.GetUpdate($updateRevisionId)

    if (-not $update) {
        Write-Error "Update not found: $UpdateId" -ErrorAction Stop
    }

    # Get the target group
    $groupGuid = [Guid]$GroupId
    $group = $wsus.GetComputerTargetGroup($groupGuid)

    if (-not $group) {
        Write-Error "Computer group not found: $GroupId" -ErrorAction Stop
    }

    # Approve the update for installation
    $update.Approve([Microsoft.UpdateServices.Administration.UpdateApprovalAction]::Install, $group)

    return [PSCustomObject]@{
        Success   = $true
        UpdateId  = $UpdateId
        GroupId   = $GroupId
        GroupName = $group.Name
        Action    = "Approved"
    }
}
catch {
    Write-Error "Failed to approve update: $_" -ErrorAction Stop
    throw $_
}
