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
    [string]$UpdateScope = "All"
)

try {
    # Defensive coding: Check if module exists
    if (-not (Get-Module -ListAvailable -Name UpdateServices)) {
        Write-Error "WSUS Module (UpdateServices) is not installed on this machine." -ErrorAction Stop
    }

    # Connect to WSUS server
    $wsus = [Microsoft.UpdateServices.Administration.AdminProxy]::GetUpdateServer($ServerName, $UseSsl, $Port)

    if (-not $wsus) {
        Write-Error "Failed to connect to WSUS server: $ServerName" -ErrorAction Stop
    }

    # Create update scope for filtering
    $searchScope = New-Object Microsoft.UpdateServices.Administration.UpdateScope

    # Apply classification filter if specified
    if ($UpdateScope -eq "Critical") {
        $classifications = $wsus.GetUpdateClassifications() | Where-Object {
            $_.Title -eq "Critical Updates" -or $_.Title -eq "Security Updates"
        }
        if ($classifications) {
            $searchScope.Classifications.AddRange($classifications)
        }
    }

    # Get updates with scope
    $updates = $wsus.GetUpdates($searchScope) | Select-Object -First 50

    # Transform to simplified objects for C#
    $result = @()
    foreach ($update in $updates) {
        # Join KB articles if multiple exist
        $kbArticles = ""
        if ($update.KnowledgebaseArticles -and $update.KnowledgebaseArticles.Count -gt 0) {
            $kbArticles = ($update.KnowledgebaseArticles | ForEach-Object { "KB$_" }) -join ", "
        }

        $result += [PSCustomObject]@{
            Id             = $update.Id.UpdateId.ToString()
            Title          = $update.Title
            KbArticle      = $kbArticles
            Classification = $update.UpdateClassificationTitle
            CreationDate   = $update.CreationDate
            IsApproved     = $update.IsApproved
            IsDeclined     = $update.IsDeclined
        }
    }

    return $result
}
catch {
    Write-Error "Failed to retrieve WSUS updates: $_" -ErrorAction Stop
    throw $_
}
