# Copyright 2025 Julien Bombled
# Licensed under the Apache License, Version 2.0

param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [ValidatePattern('^[a-zA-Z0-9\-_.]+$')]
    [string]$ServerName,

    [Parameter(Mandatory = $true)]
    [ValidateRange(1, 65535)]
    [int]$Port,

    [Parameter(Mandatory = $true)]
    [bool]$UseSsl,

    [Parameter(Mandatory = $false)]
    [ValidateRange(1, 1000)]
    [int]$MaxEntries = 100
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

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

    $activities = @()

    # Get recent synchronizations
    $syncInfo = $wsus.GetSubscription()
    $lastSync = $syncInfo.GetLastSynchronizationInfo()

    if ($lastSync) {
        $activities += [PSCustomObject]@{
            Timestamp    = $lastSync.StartTime
            ActivityType = "Sync"
            Description  = "Synchronization completed"
            User         = "System"
            Target       = "Microsoft Update"
            Status       = $lastSync.Result.ToString()
        }
    }

    # Get recent update approvals (from changes to updates)
    $updateScope = New-Object Microsoft.UpdateServices.Administration.UpdateScope
    $updateScope.IsDeclined = $false
    $recentUpdates = $wsus.GetUpdates($updateScope) |
        Where-Object { $_.IsApproved } |
        Sort-Object -Property CreationDate -Descending |
        Select-Object -First ($MaxEntries / 2)

    foreach ($update in $recentUpdates) {
        $approvals = $update.GetUpdateApprovals()
        foreach ($approval in $approvals) {
            $activities += [PSCustomObject]@{
                Timestamp    = $approval.CreationDate
                ActivityType = "Approval"
                Description  = "Update approved"
                User         = $approval.AdministratorName
                Target       = $update.Title
                Status       = "Success"
            }
        }
    }

    # Get declined updates
    $declinedScope = New-Object Microsoft.UpdateServices.Administration.UpdateScope
    $declinedScope.IsDeclined = $true
    $declinedUpdates = $wsus.GetUpdates($declinedScope) |
        Sort-Object -Property CreationDate -Descending |
        Select-Object -First ($MaxEntries / 4)

    foreach ($update in $declinedUpdates) {
        $activities += [PSCustomObject]@{
            Timestamp    = $update.CreationDate
            ActivityType = "Decline"
            Description  = "Update declined"
            User         = "Administrator"
            Target       = $update.Title
            Status       = "Success"
        }
    }

    # Sort all activities by timestamp and limit
    $result = $activities |
        Sort-Object -Property Timestamp -Descending |
        Select-Object -First $MaxEntries

    return $result
}
catch {
    $errorResult = @{
        Success = $false
        Error   = @{
            Message = $_.Exception.Message
            Type    = $_.Exception.GetType().Name
        }
    }
    Write-Output ($errorResult | ConvertTo-Json -Depth 5 -Compress)
    exit 1
}
