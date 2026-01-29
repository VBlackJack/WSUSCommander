# Copyright 2025 Julien Bombled
# Licensed under the Apache License, Version 2.0

param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$TaskName,

    [Parameter(Mandatory = $true)]
    [ValidateSet("Update", "Enable", "Disable", "Run", "GetInfo")]
    [string]$Operation,

    # Optional parameters for Update operation
    [Parameter(Mandatory = $false)]
    [string]$Description,

    [Parameter(Mandatory = $false)]
    [ValidateSet("Once", "Daily", "Weekly", "Monthly")]
    [string]$Frequency,

    [Parameter(Mandatory = $false)]
    [string]$TimeOfDay,

    [Parameter(Mandatory = $false)]
    [string]$StartDate,

    [Parameter(Mandatory = $false)]
    [string]$EndDate,

    [Parameter(Mandatory = $false)]
    [string]$DaysOfWeek,

    [Parameter(Mandatory = $false)]
    [int]$DayOfMonth,

    [Parameter(Mandatory = $false)]
    [bool]$Enabled
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$taskPath = "\WsusCommander\"

try {
    switch ($Operation) {
        "GetInfo" {
            $task = Get-ScheduledTask -TaskName $TaskName -TaskPath $taskPath -ErrorAction SilentlyContinue

            if (-not $task) {
                return [PSCustomObject]@{
                    Success = $false
                    Error   = @{
                        Message = "Task not found: $TaskName"
                        Type    = "TaskNotFound"
                    }
                }
            }

            $taskInfo = Get-ScheduledTaskInfo -TaskName $TaskName -TaskPath $taskPath

            return [PSCustomObject]@{
                Success        = $true
                Name           = $task.TaskName
                State          = $task.State.ToString()
                Enabled        = ($task.State -ne 'Disabled')
                LastRunTime    = $taskInfo.LastRunTime
                LastTaskResult = $taskInfo.LastTaskResult
                NextRunTime    = $taskInfo.NextRunTime
                Description    = $task.Description
            }
        }

        "Enable" {
            Enable-ScheduledTask -TaskName $TaskName -TaskPath $taskPath | Out-Null
            return [PSCustomObject]@{
                Success  = $true
                TaskName = $TaskName
                State    = "Enabled"
            }
        }

        "Disable" {
            Disable-ScheduledTask -TaskName $TaskName -TaskPath $taskPath | Out-Null
            return [PSCustomObject]@{
                Success  = $true
                TaskName = $TaskName
                State    = "Disabled"
            }
        }

        "Run" {
            Start-ScheduledTask -TaskName $TaskName -TaskPath $taskPath
            return [PSCustomObject]@{
                Success  = $true
                TaskName = $TaskName
                State    = "Running"
            }
        }

        "Update" {
            $task = Get-ScheduledTask -TaskName $TaskName -TaskPath $taskPath -ErrorAction Stop

            # Update description if provided
            if ($Description) {
                $task.Description = $Description
            }

            # Update trigger if schedule parameters provided
            if ($Frequency -and $TimeOfDay -and $StartDate) {
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
                        # Fallback to 4-week interval
                        $trigger = New-ScheduledTaskTrigger -Weekly -DaysOfWeek Monday -At $startTime -WeeksInterval 4
                    }
                }

                if (-not [string]::IsNullOrEmpty($EndDate)) {
                    $trigger.EndBoundary = [DateTime]::Parse($EndDate).ToString("yyyy-MM-ddTHH:mm:ss")
                }

                $task.Triggers = @($trigger)
            }

            # Apply changes
            Set-ScheduledTask -InputObject $task | Out-Null

            # Handle enable/disable separately
            if ($PSBoundParameters.ContainsKey('Enabled')) {
                if ($Enabled) {
                    Enable-ScheduledTask -TaskName $TaskName -TaskPath $taskPath | Out-Null
                } else {
                    Disable-ScheduledTask -TaskName $TaskName -TaskPath $taskPath | Out-Null
                }
            }

            $taskInfo = Get-ScheduledTaskInfo -TaskName $TaskName -TaskPath $taskPath

            return [PSCustomObject]@{
                Success     = $true
                TaskName    = $TaskName
                NextRunTime = $taskInfo.NextRunTime
            }
        }
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
