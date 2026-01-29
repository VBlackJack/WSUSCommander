# Copyright 2025 Julien Bombled
# Licensed under the Apache License, Version 2.0
#
# Staged approval workflow script - approves updates for test groups and promotes
# to production after the testing delay period.

param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-fA-F-]{36}$')]
    [string]$TaskId,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$DataPath,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$WsusServer,

    [Parameter(Mandatory = $true)]
    [ValidateRange(1, 65535)]
    [int]$WsusPort,

    [Parameter(Mandatory = $true)]
    [bool]$WsusUseSsl,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ConfigJson
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Parse configuration
$config = $ConfigJson | ConvertFrom-Json

$trackingFile = Join-Path $DataPath "staged-approvals.json"
$logPath = Join-Path $DataPath "task-logs"
$logFile = Join-Path $logPath "$(Get-Date -Format 'yyyy-MM-dd')_staged_$TaskId.log"

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"
    Add-Content -Path $logFile -Value $logMessage
}

function Get-TrackingData {
    if (Test-Path $trackingFile) {
        return Get-Content $trackingFile -Raw | ConvertFrom-Json
    }
    return [PSCustomObject]@{
        Entries     = @()
        LastUpdated = (Get-Date).ToUniversalTime().ToString("o")
    }
}

function Save-TrackingData {
    param($Data)
    $Data.LastUpdated = (Get-Date).ToUniversalTime().ToString("o")
    $Data | ConvertTo-Json -Depth 10 | Set-Content $trackingFile
}

try {
    Write-Log "Starting staged approval check for task $TaskId"

    # Connect to WSUS
    $wsus = Get-WsusServer -Name $WsusServer -PortNumber $WsusPort -UseSsl:$WsusUseSsl

    # Load tracking data
    $tracking = Get-TrackingData

    $newApprovals = 0
    $promotions = 0
    $blocked = 0

    # Phase 1: Find new updates to approve for test groups
    Write-Log "Phase 1: Checking for new updates to approve for test groups"

    foreach ($testGroupId in $config.TestGroupIds) {
        $testGroup = $wsus.GetComputerTargetGroup([Guid]$testGroupId)
        if (-not $testGroup) {
            Write-Log "Test group not found: $testGroupId" -Level "WARNING"
            continue
        }

        Write-Log "Processing test group: $($testGroup.Name)"

        # Get unapproved updates matching classifications
        $updateScope = New-Object Microsoft.UpdateServices.Administration.UpdateScope
        $updateScope.ApprovedStates = [Microsoft.UpdateServices.Administration.ApprovedStates]::NotApproved

        $updates = $wsus.GetUpdates($updateScope)

        foreach ($update in $updates) {
            # Check if classification matches
            $classification = $update.UpdateClassificationTitle
            if ($config.UpdateClassifications.Count -gt 0 -and $classification -notin $config.UpdateClassifications) {
                continue
            }

            # Skip if already being tracked
            $existingEntry = $tracking.Entries | Where-Object {
                $_.UpdateId -eq $update.Id.UpdateId.ToString() -and $_.TaskId -eq $TaskId
            }
            if ($existingEntry) {
                continue
            }

            # Approve for test group
            try {
                $update.Approve([Microsoft.UpdateServices.Administration.UpdateApprovalAction]::Install, $testGroup)

                # Add to tracking
                $entry = [PSCustomObject]@{
                    UpdateId                 = $update.Id.UpdateId.ToString()
                    UpdateTitle              = $update.Title
                    KbArticle                = if ($update.KnowledgebaseArticles.Count -gt 0) { "KB$($update.KnowledgebaseArticles[0])" } else { $null }
                    TaskId                   = $TaskId
                    Status                   = "InTesting"
                    ApprovedForTestAt        = (Get-Date).ToUniversalTime().ToString("o")
                    EligibleForPromotionAt   = (Get-Date).AddDays($config.PromotionDelayDays).ToUniversalTime().ToString("o")
                    PromotedAt               = $null
                    SuccessfulInstallations  = 0
                    FailedInstallations      = 0
                    PendingInstallations     = 0
                    StatusMessage            = "Approved for test group: $($testGroup.Name)"
                }

                $tracking.Entries += $entry
                $newApprovals++

                Write-Log "Approved for test: $($update.Title)"
            } catch {
                Write-Log "Failed to approve update $($update.Title): $($_.Exception.Message)" -Level "ERROR"
            }
        }
    }

    # Phase 2: Check test installations and promote eligible updates
    Write-Log "Phase 2: Checking for updates ready for promotion"

    $now = Get-Date

    foreach ($entry in ($tracking.Entries | Where-Object { $_.TaskId -eq $TaskId -and $_.Status -eq "InTesting" })) {
        $eligibleDate = [DateTime]::Parse($entry.EligibleForPromotionAt)

        if ($now -lt $eligibleDate) {
            Write-Log "Update $($entry.UpdateTitle) not yet eligible (eligible: $eligibleDate)"
            continue
        }

        # Get installation status from test groups
        $statusScript = Join-Path $PSScriptRoot "Get-StagedApprovalStatus.ps1"
        if (Test-Path $statusScript) {
            $status = & $statusScript `
                -UpdateId $entry.UpdateId `
                -GroupIds ($config.TestGroupIds -join ",") `
                -WsusServer $WsusServer `
                -WsusPort $WsusPort `
                -WsusUseSsl $WsusUseSsl

            $entry.SuccessfulInstallations = $status.Installed
            $entry.FailedInstallations = $status.Failed
            $entry.PendingInstallations = $status.Pending
        }

        # Check if promotion criteria met
        $canPromote = $true
        $blockReason = $null

        if ($config.RequireSuccessfulInstallations) {
            if ($entry.SuccessfulInstallations -lt $config.MinimumSuccessfulInstallations) {
                $canPromote = $false
                $blockReason = "Insufficient successful installations: $($entry.SuccessfulInstallations)/$($config.MinimumSuccessfulInstallations)"
            }
        }

        if ($config.AbortOnFailures -and $entry.FailedInstallations -gt $config.MaxAllowedFailures) {
            $canPromote = $false
            $blockReason = "Too many failures: $($entry.FailedInstallations) (max: $($config.MaxAllowedFailures))"
        }

        if (-not $canPromote) {
            $entry.Status = "Blocked"
            $entry.StatusMessage = $blockReason
            $blocked++
            Write-Log "Blocked: $($entry.UpdateTitle) - $blockReason" -Level "WARNING"
            continue
        }

        # Promote to production groups
        Write-Log "Promoting: $($entry.UpdateTitle)"

        try {
            $update = $wsus.GetUpdate([Microsoft.UpdateServices.Administration.UpdateRevisionId]::New([Guid]$entry.UpdateId))

            foreach ($prodGroupId in $config.ProductionGroupIds) {
                $prodGroup = $wsus.GetComputerTargetGroup([Guid]$prodGroupId)
                if ($prodGroup) {
                    $update.Approve([Microsoft.UpdateServices.Administration.UpdateApprovalAction]::Install, $prodGroup)
                    Write-Log "Approved for production group: $($prodGroup.Name)"
                }
            }

            $entry.Status = "Promoted"
            $entry.PromotedAt = (Get-Date).ToUniversalTime().ToString("o")
            $entry.StatusMessage = "Promoted to production"
            $promotions++

            # Optionally decline superseded updates
            if ($config.DeclineSupersededUpdates) {
                $supersededBy = $update.GetRelatedUpdates([Microsoft.UpdateServices.Administration.UpdateRelationship]::UpdatesThatSupersedeThisUpdate)
                if ($supersededBy.Count -gt 0) {
                    Write-Log "Update is superseded, declining"
                    $update.Decline()
                }
            }
        } catch {
            $entry.StatusMessage = "Promotion failed: $($_.Exception.Message)"
            Write-Log "Failed to promote $($entry.UpdateTitle): $($_.Exception.Message)" -Level "ERROR"
        }
    }

    # Save tracking data
    Save-TrackingData -Data $tracking

    Write-Log "Staged approval check completed. New: $newApprovals, Promoted: $promotions, Blocked: $blocked"

    return [PSCustomObject]@{
        Success      = $true
        NewApprovals = $newApprovals
        Promotions   = $promotions
        Blocked      = $blocked
    }
}
catch {
    Write-Log "Staged approval check failed: $($_.Exception.Message)" -Level "ERROR"
    return [PSCustomObject]@{
        Success = $false
        Error   = @{
            Message = $_.Exception.Message
            Type    = $_.Exception.GetType().Name
        }
    }
}
