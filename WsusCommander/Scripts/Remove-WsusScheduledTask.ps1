# Copyright 2025 Julien Bombled
# Licensed under the Apache License, Version 2.0

param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$TaskName
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$taskPath = "\WsusCommander\"

try {
    # Check if task exists
    $task = Get-ScheduledTask -TaskName $TaskName -TaskPath $taskPath -ErrorAction SilentlyContinue

    if (-not $task) {
        return [PSCustomObject]@{
            Success = $true
            Message = "Task does not exist or was already removed."
        }
    }

    # Remove the task
    Unregister-ScheduledTask -TaskName $TaskName -TaskPath $taskPath -Confirm:$false

    return [PSCustomObject]@{
        Success  = $true
        TaskName = $TaskName
        Message  = "Task removed successfully."
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
