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

    # Get current approval for this group and remove it
    $approvals = $update.GetUpdateApprovals()
    $groupApproval = $approvals | Where-Object { $_.ComputerTargetGroupId -eq $groupGuid }

    if ($groupApproval) {
        foreach ($approval in $groupApproval) {
            $approval.Delete()
        }

        return [PSCustomObject]@{
            Success   = $true
            UpdateId  = $UpdateId
            GroupId   = $GroupId
            GroupName = $group.Name
            Message   = "Update approval removed from group '$($group.Name)'"
        }
    }
    else {
        return [PSCustomObject]@{
            Success   = $true
            UpdateId  = $UpdateId
            GroupId   = $GroupId
            GroupName = $group.Name
            Message   = "Update was not approved for group '$($group.Name)'"
        }
    }
}
catch {
    Write-Error "Failed to unapprove update: $_" -ErrorAction Stop
    throw $_
}
