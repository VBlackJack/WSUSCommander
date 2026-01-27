# Copyright 2025 Julien Bombled
# Licensed under the Apache License, Version 2.0

param(
    [Parameter(Mandatory = $true)]
    [string]$ServerName,

    [Parameter(Mandatory = $true)]
    [int]$Port,

    [Parameter(Mandatory = $true)]
    [bool]$UseSsl,

    [Parameter(Mandatory = $false)]
    [switch]$CountOnly
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

    # Create scope for superseded updates that are not yet declined
    $updateScope = New-Object Microsoft.UpdateServices.Administration.UpdateScope
    $updateScope.IsDeclined = $false
    $updateScope.IsSuperseded = $true

    # Get all superseded, non-declined updates
    $supersededUpdates = $wsus.GetUpdates($updateScope)

    if ($CountOnly) {
        # Return count only for confirmation dialog
        return [PSCustomObject]@{
            Count = $supersededUpdates.Count
        }
    }

    # Decline all superseded updates
    $successCount = 0
    $failedCount = 0
    $errors = @()

    foreach ($update in $supersededUpdates) {
        try {
            $update.Decline()
            $successCount++
        }
        catch {
            $failedCount++
            $errors += "Failed to decline '$($update.Title)': $_"
        }
    }

    return [PSCustomObject]@{
        Success      = $true
        TotalCount   = $supersededUpdates.Count
        SuccessCount = $successCount
        FailedCount  = $failedCount
        Errors       = $errors
    }
}
catch {
    Write-Error "Failed to decline superseded updates: $_" -ErrorAction Stop
    throw $_
}
