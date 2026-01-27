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
    [ValidateSet("Critical", "Security", "All")]
    [string]$Classification = "All",

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

    # Create scope for unapproved updates
    $updateScope = New-Object Microsoft.UpdateServices.Administration.UpdateScope
    $updateScope.ApprovedStates = [Microsoft.UpdateServices.Administration.ApprovedStates]::NotApproved
    $updateScope.IsDeclined = $false
    $updateScope.IsSuperseded = $false

    # Apply classification filter
    if ($Classification -ne "All") {
        $classifications = $wsus.GetUpdateClassifications() | Where-Object {
            if ($Classification -eq "Critical") {
                $_.Title -eq "Critical Updates"
            }
            elseif ($Classification -eq "Security") {
                $_.Title -eq "Security Updates"
            }
        }
        if ($classifications) {
            $updateScope.Classifications.AddRange($classifications)
        }
    }

    # Get unapproved updates
    $updates = $wsus.GetUpdates($updateScope)

    if ($CountOnly) {
        return [PSCustomObject]@{
            Count = $updates.Count
        }
    }

    # Return update IDs for bulk approval
    $result = @()
    foreach ($update in $updates) {
        $result += [PSCustomObject]@{
            Id             = $update.Id.UpdateId.ToString()
            Title          = $update.Title
            Classification = $update.UpdateClassificationTitle
        }
    }

    return $result
}
catch {
    Write-Error "Failed to get unapproved updates: $_" -ErrorAction Stop
    throw $_
}
