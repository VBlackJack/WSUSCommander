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

    # Load the WSUS assembly
    [reflection.assembly]::LoadWithPartialName("Microsoft.UpdateServices.Administration") | Out-Null

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

    # Get updates with scope, sorted by creation date (most recent first)
    $updates = $wsus.GetUpdates($searchScope) | Sort-Object -Property CreationDate -Descending | Select-Object -First 100

    # Transform to simplified objects for C#
    $result = @()
    foreach ($update in $updates) {
        # Join KB articles if multiple exist
        $kbArticles = ""
        if ($update.KnowledgebaseArticles -and $update.KnowledgebaseArticles.Count -gt 0) {
            $kbArticles = ($update.KnowledgebaseArticles | ForEach-Object { "KB$_" }) -join ", "
        }

        # Get superseding updates KB articles with version info
        $supersededBy = ""
        if ($update.IsSuperseded) {
            try {
                $supersedingUpdates = $update.GetRelatedUpdates(
                    [Microsoft.UpdateServices.Administration.UpdateRelationship]::UpdatesThatSupersedeThisUpdate)
                if ($supersedingUpdates -and $supersedingUpdates.Count -gt 0) {
                    $supersedingInfo = @()
                    foreach ($supUpdate in $supersedingUpdates) {
                        $kbPart = ""
                        if ($supUpdate.KnowledgebaseArticles -and $supUpdate.KnowledgebaseArticles.Count -gt 0) {
                            $kbPart = "KB" + ($supUpdate.KnowledgebaseArticles | Select-Object -First 1)
                        }

                        # Extract version from title (common patterns: "Version X.X.X", "vX.X.X", date patterns)
                        $versionPart = ""
                        $title = $supUpdate.Title
                        if ($title -match "Version\s+([\d\.]+)" -or $title -match "\bv([\d\.]+)" -or $title -match "\(([\d\.]+)\)") {
                            $versionPart = $Matches[1]
                        }
                        elseif ($title -match "(\d{1,2}/\d{1,2}/\d{4})") {
                            # Date format for definition updates
                            $versionPart = $Matches[1]
                        }
                        elseif ($supUpdate.Id.RevisionNumber -gt 1) {
                            $versionPart = "rev." + $supUpdate.Id.RevisionNumber
                        }

                        if ($kbPart -and $versionPart) {
                            $supersedingInfo += "$kbPart ($versionPart)"
                        }
                        elseif ($kbPart) {
                            $supersedingInfo += $kbPart
                        }
                        elseif ($versionPart) {
                            # No KB but has version (rare, but possible for definition updates)
                            $supersedingInfo += $versionPart
                        }
                    }
                    $supersededBy = ($supersedingInfo | Select-Object -Unique) -join ", "
                }
            }
            catch {
                # Silently ignore errors when fetching superseding updates
            }
        }

        $result += [PSCustomObject]@{
            Id             = $update.Id.UpdateId.ToString()
            Title          = $update.Title
            KbArticle      = $kbArticles
            Classification = $update.UpdateClassificationTitle
            CreationDate   = $update.CreationDate
            IsApproved     = $update.IsApproved
            IsDeclined     = $update.IsDeclined
            IsSuperseded   = $update.IsSuperseded
            SupersededBy   = $supersededBy
        }
    }

    return $result
}
catch {
    Write-Error "Failed to retrieve WSUS updates: $_" -ErrorAction Stop
    throw $_
}
