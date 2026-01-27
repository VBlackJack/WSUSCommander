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
    [string[]]$UpdateIds,

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

    # Get the target group
    $groupGuid = [Guid]$GroupId
    $group = $wsus.GetComputerTargetGroup($groupGuid)

    if (-not $group) {
        Write-Error "Computer group not found: $GroupId" -ErrorAction Stop
    }

    $successCount = 0
    $failedCount = 0
    $errors = @()

    foreach ($updateId in $UpdateIds) {
        try {
            $updateGuid = [Guid]$updateId
            $updateRevisionId = New-Object Microsoft.UpdateServices.Administration.UpdateRevisionId($updateGuid)
            $update = $wsus.GetUpdate($updateRevisionId)

            if ($update) {
                $update.Approve([Microsoft.UpdateServices.Administration.UpdateApprovalAction]::Install, $group)
                $successCount++
            }
            else {
                $failedCount++
                $errors += "Update not found: $updateId"
            }
        }
        catch {
            $failedCount++
            $errors += "Failed to approve update $updateId : $_"
        }
    }

    return [PSCustomObject]@{
        Success      = $true
        TotalCount   = $UpdateIds.Count
        SuccessCount = $successCount
        FailedCount  = $failedCount
        Errors       = $errors
    }
}
catch {
    Write-Error "Failed to approve updates: $_" -ErrorAction Stop
    throw $_
}
