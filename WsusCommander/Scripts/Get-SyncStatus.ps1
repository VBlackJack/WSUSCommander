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

    # Load the WSUS assembly
    [reflection.assembly]::LoadWithPartialName("Microsoft.UpdateServices.Administration") | Out-Null

    # Connect to WSUS server
    $wsus = [Microsoft.UpdateServices.Administration.AdminProxy]::GetUpdateServer($ServerName, $UseSsl, $Port)

    if (-not $wsus) {
        Write-Error "Failed to connect to WSUS server: $ServerName" -ErrorAction Stop
    }

    # Get subscription (sync settings)
    $subscription = $wsus.GetSubscription()

    # Get the last synchronization info
    $lastSyncInfo = $subscription.GetLastSynchronizationInfo()

    # Determine sync status
    $isSyncing = $subscription.GetSynchronizationStatus() -eq [Microsoft.UpdateServices.Administration.SynchronizationStatus]::Running
    $status = if ($isSyncing) { "Running" } else { "Idle" }

    # Get last sync result
    $lastSyncResult = "None"
    if ($lastSyncInfo) {
        $lastSyncResult = switch ($lastSyncInfo.Result) {
            "Succeeded" { "Succeeded" }
            "Failed" { "Failed" }
            "PartiallySucceeded" { "PartiallySucceeded" }
            default { "Unknown" }
        }
    }

    return [PSCustomObject]@{
        Status         = $status
        LastSyncTime   = if ($lastSyncInfo) { $lastSyncInfo.StartTime } else { $null }
        NextSyncTime   = $subscription.NextSynchronizationTime
        IsSyncing      = $isSyncing
        LastSyncResult = $lastSyncResult
    }
}
catch {
    Write-Error "Failed to retrieve sync status: $_" -ErrorAction Stop
    throw $_
}
