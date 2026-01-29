# Copyright 2025 Julien Bombled
# Licensed under the Apache License, Version 2.0
#
# Autonomous WSUS operation script - executes scheduled tasks independently of the GUI.
# This script is called by Windows Task Scheduler.

param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-fA-F-]{36}$')]
    [string]$TaskId,

    [Parameter(Mandatory = $true)]
    [ValidateSet("StagedApproval", "Cleanup", "Synchronization")]
    [string]$OperationType,

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
    [bool]$WsusUseSsl
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Setup logging
$logPath = Join-Path $DataPath "task-logs"
if (-not (Test-Path $logPath)) {
    New-Item -ItemType Directory -Path $logPath -Force | Out-Null
}
$logFile = Join-Path $logPath "$(Get-Date -Format 'yyyy-MM-dd')_$TaskId.log"

function Write-TaskLog {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"
    Add-Content -Path $logFile -Value $logMessage
    if ($Level -eq "ERROR") {
        Write-Error $Message
    }
}

function Get-TaskConfig {
    $tasksFile = Join-Path $DataPath "scheduled-tasks.json"
    if (-not (Test-Path $tasksFile)) {
        throw "Tasks configuration not found: $tasksFile"
    }
    $content = Get-Content $tasksFile -Raw | ConvertFrom-Json
    $task = $content.Tasks | Where-Object { $_.Id -eq $TaskId }
    if (-not $task) {
        throw "Task not found in configuration: $TaskId"
    }
    return $task
}

function Update-TaskStatus {
    param(
        [string]$Status,
        [string]$Message
    )
    try {
        $tasksFile = Join-Path $DataPath "scheduled-tasks.json"
        $content = Get-Content $tasksFile -Raw | ConvertFrom-Json
        $task = $content.Tasks | Where-Object { $_.Id -eq $TaskId }
        if ($task) {
            $task.LastRunAt = (Get-Date).ToUniversalTime().ToString("o")
            $task.LastRunStatus = $Status
            $task.LastRunMessage = $Message
            $content | ConvertTo-Json -Depth 10 | Set-Content $tasksFile
        }
    } catch {
        Write-TaskLog "Failed to update task status: $($_.Exception.Message)" -Level "WARNING"
    }
}

try {
    Write-TaskLog "Starting scheduled operation: $OperationType for task $TaskId"

    # Load task configuration
    $taskConfig = Get-TaskConfig
    Write-TaskLog "Loaded task configuration: $($taskConfig.Name)"

    # Check if module is available
    if (-not (Get-Module -ListAvailable -Name UpdateServices)) {
        throw "WSUS Module (UpdateServices) is not installed on this machine."
    }

    # Connect to WSUS server
    Write-TaskLog "Connecting to WSUS server: $WsusServer`:$WsusPort (SSL: $WsusUseSsl)"
    $wsus = Get-WsusServer -Name $WsusServer -PortNumber $WsusPort -UseSsl:$WsusUseSsl

    if (-not $wsus) {
        throw "Failed to connect to WSUS server"
    }
    Write-TaskLog "Connected successfully"

    $result = $null
    $statusMessage = ""

    switch ($OperationType) {
        "StagedApproval" {
            Write-TaskLog "Executing staged approval workflow"
            $stagedConfig = $taskConfig.StagedApprovalSettings

            # Call the staged approval check script
            $checkScript = Join-Path $PSScriptRoot "Invoke-StagedApprovalCheck.ps1"
            if (Test-Path $checkScript) {
                $result = & $checkScript `
                    -TaskId $TaskId `
                    -DataPath $DataPath `
                    -WsusServer $WsusServer `
                    -WsusPort $WsusPort `
                    -WsusUseSsl $WsusUseSsl `
                    -ConfigJson ($stagedConfig | ConvertTo-Json -Compress)

                $statusMessage = "Approved: $($result.NewApprovals), Promoted: $($result.Promotions), Blocked: $($result.Blocked)"
            } else {
                throw "Staged approval check script not found"
            }
        }

        "Cleanup" {
            Write-TaskLog "Executing WSUS cleanup"
            $cleanupSettings = $taskConfig.CleanupSettings

            $cleanupScope = New-Object Microsoft.UpdateServices.Administration.CleanupScope
            $cleanupScope.CleanupObsoleteUpdates = $cleanupSettings.RemoveObsoleteUpdates
            $cleanupScope.CleanupObsoleteComputers = $cleanupSettings.RemoveObsoleteComputers
            $cleanupScope.CleanupExpiredUpdates = $cleanupSettings.RemoveExpiredUpdates
            $cleanupScope.CompressUpdates = $cleanupSettings.CompressUpdateRevisions
            $cleanupScope.CleanupUnneededContentFiles = $cleanupSettings.RemoveUnneededContent

            $cleanupManager = $wsus.GetCleanupManager()
            $result = $cleanupManager.PerformCleanup($cleanupScope)

            $statusMessage = "Deleted updates: $($result.SupersededUpdatesDeclined), Removed computers: $($result.ObsoleteComputersDeleted)"
            Write-TaskLog $statusMessage
        }

        "Synchronization" {
            Write-TaskLog "Starting WSUS synchronization"
            $syncConfig = $taskConfig.SyncSettings

            $subscription = $wsus.GetSubscription()
            $subscription.StartSynchronization()

            if ($syncConfig.WaitForCompletion) {
                $maxWait = [TimeSpan]::FromMinutes($syncConfig.MaxWaitMinutes)
                $startTime = Get-Date
                $timeout = $false

                Write-TaskLog "Waiting for synchronization to complete (max: $($syncConfig.MaxWaitMinutes) minutes)"

                while ($subscription.GetSynchronizationStatus() -eq [Microsoft.UpdateServices.Administration.SynchronizationStatus]::Running) {
                    if ($syncConfig.MaxWaitMinutes -gt 0 -and ((Get-Date) - $startTime) -gt $maxWait) {
                        $timeout = $true
                        break
                    }
                    Start-Sleep -Seconds 30
                }

                $syncStatus = $subscription.GetLastSynchronizationInfo()

                if ($timeout) {
                    $statusMessage = "Synchronization still running after $($syncConfig.MaxWaitMinutes) minutes"
                    Write-TaskLog $statusMessage -Level "WARNING"
                } elseif ($syncStatus.Result -eq [Microsoft.UpdateServices.Administration.SynchronizationResult]::Succeeded) {
                    $statusMessage = "Synchronization completed successfully. New updates: $($syncStatus.NumberOfNewUpdates)"
                    Write-TaskLog $statusMessage
                } else {
                    $statusMessage = "Synchronization completed with result: $($syncStatus.Result)"
                    Write-TaskLog $statusMessage -Level "WARNING"
                }
            } else {
                $statusMessage = "Synchronization started (not waiting for completion)"
                Write-TaskLog $statusMessage
            }
        }
    }

    Write-TaskLog "Operation completed successfully"
    Update-TaskStatus -Status "Success" -Message $statusMessage

    return [PSCustomObject]@{
        Success = $true
        TaskId  = $TaskId
        Message = $statusMessage
    }
}
catch {
    $errorMessage = $_.Exception.Message
    Write-TaskLog "Operation failed: $errorMessage" -Level "ERROR"
    Update-TaskStatus -Status "Failed" -Message $errorMessage

    return [PSCustomObject]@{
        Success = $false
        TaskId  = $TaskId
        Error   = @{
            Message = $errorMessage
            Type    = $_.Exception.GetType().Name
        }
    }
}
