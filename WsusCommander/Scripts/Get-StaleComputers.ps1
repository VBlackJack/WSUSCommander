# Copyright 2025 Julien Bombled
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

<#
.SYNOPSIS
    Gets computers that haven't reported to WSUS recently.

.PARAMETER StaleDays
    Days threshold for considering a computer stale.

.PARAMETER WsusServer
    The WSUS server name.

.PARAMETER WsusPort
    The WSUS server port.

.PARAMETER UseSsl
    Whether to use SSL connection.
#>

param(
    [Parameter(Mandatory = $false)]
    [int]$StaleDays = 30,

    [Parameter(Mandatory = $false)]
    [string]$WsusServer = "localhost",

    [Parameter(Mandatory = $false)]
    [int]$WsusPort = 8530,

    [Parameter(Mandatory = $false)]
    [bool]$UseSsl = $false
)

try {
    # Import WSUS module
    if (-not (Get-Module -ListAvailable -Name UpdateServices)) {
        throw "WSUS PowerShell module (UpdateServices) is not installed."
    }

    Import-Module UpdateServices -ErrorAction Stop

    # Connect to WSUS server
    $wsus = Get-WsusServer -Name $WsusServer -PortNumber $WsusPort -UseSsl:$UseSsl

    if (-not $wsus) {
        throw "Failed to connect to WSUS server: $WsusServer"
    }

    $staleDate = (Get-Date).AddDays(-$StaleDays)
    $staleComputers = @()

    # Get all computers
    $computers = $wsus.GetComputerTargets()

    foreach ($computer in $computers) {
        $lastReport = $computer.LastReportedStatusTime

        if ($null -eq $lastReport -or $lastReport -lt $staleDate) {
            # Get group memberships
            $groups = $computer.GetComputerTargetGroups()
            $groupNames = @($groups | ForEach-Object { $_.Name })

            $daysSinceReport = if ($null -eq $lastReport) {
                9999
            }
            else {
                [math]::Floor(((Get-Date) - $lastReport).TotalDays)
            }

            $staleComputers += @{
                ComputerId = $computer.Id
                ComputerName = $computer.FullDomainName
                LastReportTime = $lastReport
                DaysSinceLastReport = $daysSinceReport
                GroupNames = $groupNames
            }
        }
    }

    # Sort by days since last report (most stale first)
    $staleComputers | Sort-Object -Property DaysSinceLastReport -Descending
}
catch {
    @{
        Error = $_.Exception.Message
    }
}
