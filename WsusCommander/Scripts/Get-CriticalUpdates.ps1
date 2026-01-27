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

    # Get critical and security updates
    $updateScope = New-Object Microsoft.UpdateServices.Administration.UpdateScope

    # Get all updates first
    $allUpdates = $wsus.GetUpdates($updateScope)

    # Filter for critical and security updates
    $criticalUpdates = $allUpdates | Where-Object {
        $_.UpdateClassificationTitle -eq "Critical Updates" -or
        $_.UpdateClassificationTitle -eq "Security Updates" -or
        $_.MsrcSeverity -eq "Critical"
    }

    $totalCritical = 0
    $approvedCritical = 0
    $unapprovedCritical = 0
    $computersNeedingCritical = 0
    $unapprovedUpdatesList = @()
    $computerScope = New-Object Microsoft.UpdateServices.Administration.ComputerTargetScope

    foreach ($update in $criticalUpdates) {
        # Skip superseded and declined
        if ($update.IsSuperseded -or $update.IsDeclined) {
            continue
        }

        $totalCritical++

        if ($update.IsApproved) {
            $approvedCritical++
        }
        else {
            $unapprovedCritical++

            # Get computers needing this update
            $summary = $update.GetSummary($computerScope)
            $needed = $summary.NotInstalledCount + $summary.DownloadedCount

            $unapprovedUpdatesList += [PSCustomObject]@{
                UpdateId         = $update.Id.UpdateId.ToString()
                Title            = $update.Title
                KbArticle        = ($update.KnowledgebaseArticles -join ", ")
                Severity         = $update.MsrcSeverity
                ReleaseDate      = $update.CreationDate
                ComputersNeeding = $needed
            }
        }

        # Count computers needing this update
        $summary = $update.GetSummary($computerScope)
        $computersNeedingCritical += ($summary.NotInstalledCount + $summary.DownloadedCount)
    }

    # Sort unapproved updates by computers needing (most needed first)
    $unapprovedUpdatesList = $unapprovedUpdatesList | Sort-Object -Property ComputersNeeding -Descending

    return [PSCustomObject]@{
        TotalCritical            = $totalCritical
        ApprovedCritical         = $approvedCritical
        UnapprovedCritical       = $unapprovedCritical
        ComputersNeedingCritical = $computersNeedingCritical
        UnapprovedUpdates        = $unapprovedUpdatesList
    }
}
catch {
    Write-Error "Failed to get critical updates: $_" -ErrorAction Stop
    throw $_
}
