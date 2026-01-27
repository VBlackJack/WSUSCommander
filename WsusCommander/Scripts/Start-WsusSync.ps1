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

    # Check if sync is already running
    $currentStatus = $subscription.GetSynchronizationStatus()
    if ($currentStatus -eq [Microsoft.UpdateServices.Administration.SynchronizationStatus]::Running) {
        return [PSCustomObject]@{
            Success = $false
            Message = "Synchronization is already in progress."
        }
    }

    # Start synchronization
    $subscription.StartSynchronization()

    return [PSCustomObject]@{
        Success   = $true
        Message   = "Synchronization started successfully."
        StartTime = Get-Date
    }
}
catch {
    Write-Error "Failed to start synchronization: $_" -ErrorAction Stop
    throw $_
}
