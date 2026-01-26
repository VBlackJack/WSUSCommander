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
    Gets critical and security updates summary from WSUS.

.PARAMETER WsusServer
    The WSUS server name.

.PARAMETER WsusPort
    The WSUS server port.

.PARAMETER UseSsl
    Whether to use SSL connection.
#>

param(
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
            $summary = $update.GetSummary()
            $needed = $summary.NotInstalledCount + $summary.DownloadedCount

            $unapprovedUpdatesList += @{
                UpdateId = $update.Id.UpdateId.ToString()
                Title = $update.Title
                KbArticle = ($update.KnowledgebaseArticles -join ", ")
                Severity = $update.MsrcSeverity
                ReleaseDate = $update.CreationDate
                ComputersNeeding = $needed
            }
        }

        # Count computers needing this update
        $summary = $update.GetSummary()
        $computersNeedingCritical += ($summary.NotInstalledCount + $summary.DownloadedCount)
    }

    # Sort unapproved updates by computers needing (most needed first)
    $unapprovedUpdatesList = $unapprovedUpdatesList | Sort-Object -Property ComputersNeeding -Descending

    @{
        TotalCritical = $totalCritical
        ApprovedCritical = $approvedCritical
        UnapprovedCritical = $unapprovedCritical
        ComputersNeedingCritical = $computersNeedingCritical
        UnapprovedUpdates = $unapprovedUpdatesList
    }
}
catch {
    @{
        Error = $_.Exception.Message
    }
}
