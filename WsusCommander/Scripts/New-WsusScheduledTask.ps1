# Copyright 2025 Julien Bombled
# Licensed under the Apache License, Version 2.0

param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$TaskName,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-fA-F-]{36}$')]
    [string]$TaskId,

    [Parameter(Mandatory = $false)]
    [string]$Description = "",

    [Parameter(Mandatory = $true)]
    [ValidateSet("StagedApproval", "Cleanup", "Synchronization")]
    [string]$OperationType,

    [Parameter(Mandatory = $true)]
    [ValidateSet("Once", "Daily", "Weekly", "Monthly")]
    [string]$Frequency,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d{2}:\d{2}$')]
    [string]$TimeOfDay,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d{4}-\d{2}-\d{2}$')]
    [string]$StartDate,

    [Parameter(Mandatory = $false)]
    [string]$EndDate,

    [Parameter(Mandatory = $false)]
    [string]$DaysOfWeek,

    [Parameter(Mandatory = $false)]
    [ValidateRange(1, 31)]
    [int]$DayOfMonth = 1,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$DataPath,

    [Parameter(Mandatory = $true)]
    [bool]$Enabled,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ConfigJson,

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

try {
    # Build the action - execute the autonomous operation script
    $scriptPath = Join-Path $PSScriptRoot "Invoke-WsusScheduledOperation.ps1"

    if (-not (Test-Path $scriptPath)) {
        throw "Operation script not found: $scriptPath"
    }

    # Build arguments for the scheduled script
    $arguments = @(
        "-NoProfile",
        "-NonInteractive",
        "-ExecutionPolicy", "RemoteSigned",
        "-File", "`"$scriptPath`"",
        "-TaskId", $TaskId,
        "-OperationType", $OperationType,
        "-DataPath", "`"$DataPath`"",
        "-WsusServer", $WsusServer,
        "-WsusPort", $WsusPort,
        "-WsusUseSsl", "`$$WsusUseSsl"
    )

    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument ($arguments -join " ")

    # Build the trigger based on frequency
    $startTime = [DateTime]::Parse("$StartDate $TimeOfDay")

    switch ($Frequency) {
        "Once" {
            $trigger = New-ScheduledTaskTrigger -Once -At $startTime
        }
        "Daily" {
            $trigger = New-ScheduledTaskTrigger -Daily -At $startTime
        }
        "Weekly" {
            if ([string]::IsNullOrEmpty($DaysOfWeek)) {
                $daysArray = @([System.DayOfWeek]::Tuesday)
            } else {
                $daysArray = $DaysOfWeek.Split(',') | ForEach-Object {
                    [System.DayOfWeek][int]$_
                }
            }
            $trigger = New-ScheduledTaskTrigger -Weekly -DaysOfWeek $daysArray -At $startTime
        }
        "Monthly" {
            # Monthly trigger requires different approach
            $trigger = New-ScheduledTaskTrigger -Once -At $startTime
            $trigger.Repetition = $null
            # Use a CIM instance for monthly triggers
            $class = Get-CimClass -ClassName MSFT_TaskTrigger -Namespace Root/Microsoft/Windows/TaskScheduler
            $trigger = New-CimInstance -CimClass $class -ClientOnly
            $trigger.Enabled = $true
            $trigger.StartBoundary = $startTime.ToString("yyyy-MM-ddTHH:mm:ss")
            # Note: For proper monthly support, we'd need to use COM interface
            # Falling back to weekly for now
            $trigger = New-ScheduledTaskTrigger -Weekly -DaysOfWeek Monday -At $startTime -WeeksInterval 4
        }
    }

    # Set end date if provided
    if (-not [string]::IsNullOrEmpty($EndDate)) {
        $trigger.EndBoundary = [DateTime]::Parse($EndDate).ToString("yyyy-MM-ddTHH:mm:ss")
    }

    # Task settings
    $settings = New-ScheduledTaskSettingsSet `
        -AllowStartIfOnBatteries `
        -DontStopIfGoingOnBatteries `
        -StartWhenAvailable `
        -RunOnlyIfNetworkAvailable `
        -ExecutionTimeLimit (New-TimeSpan -Hours 4)

    # Principal - run as SYSTEM for reliability
    $principal = New-ScheduledTaskPrincipal `
        -UserId "SYSTEM" `
        -LogonType ServiceAccount `
        -RunLevel Highest

    # Create the task in WsusCommander folder
    $taskPath = "\WsusCommander\"

    # Ensure the folder exists
    $scheduleService = New-Object -ComObject Schedule.Service
    $scheduleService.Connect()
    $rootFolder = $scheduleService.GetFolder("\")
    try {
        $null = $rootFolder.GetFolder("WsusCommander")
    } catch {
        $null = $rootFolder.CreateFolder("WsusCommander")
    }

    # Register the task
    $task = Register-ScheduledTask `
        -TaskName $TaskName `
        -TaskPath $taskPath `
        -Action $action `
        -Trigger $trigger `
        -Settings $settings `
        -Principal $principal `
        -Description $Description `
        -Force

    # Disable if requested
    if (-not $Enabled) {
        Disable-ScheduledTask -TaskName $TaskName -TaskPath $taskPath | Out-Null
    }

    # Get next run time
    $taskInfo = Get-ScheduledTaskInfo -TaskName $TaskName -TaskPath $taskPath

    return [PSCustomObject]@{
        Success     = $true
        TaskName    = $TaskName
        TaskPath    = $taskPath
        State       = $task.State
        NextRunTime = $taskInfo.NextRunTime
    }
}
catch {
    return [PSCustomObject]@{
        Success = $false
        Error   = @{
            Message = $_.Exception.Message
            Type    = $_.Exception.GetType().Name
        }
    }
}
