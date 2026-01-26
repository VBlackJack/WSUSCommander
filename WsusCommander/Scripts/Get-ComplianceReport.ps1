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
    Generates a compliance report from WSUS.

.PARAMETER GroupId
    Optional group ID to filter by.

.PARAMETER StaleDays
    Days threshold for stale computers.

.PARAMETER IncludeSuperseded
    Whether to include superseded updates.

.PARAMETER IncludeDeclined
    Whether to include declined updates.

.PARAMETER WsusServer
    The WSUS server name.

.PARAMETER WsusPort
    The WSUS server port.

.PARAMETER UseSsl
    Whether to use SSL connection.
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$GroupId,

    [Parameter(Mandatory = $false)]
    [int]$StaleDays = 30,

    [Parameter(Mandatory = $false)]
    [bool]$IncludeSuperseded = $false,

    [Parameter(Mandatory = $false)]
    [bool]$IncludeDeclined = $false,

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

    # Get target group
    $targetGroup = $null
    if ($GroupId) {
        $groupGuid = [Guid]::Parse($GroupId)
        $targetGroup = $wsus.GetComputerTargetGroup($groupGuid)
    }

    # Get all computer target groups
    $groups = $wsus.GetComputerTargetGroups()

    # Filter to specific group if requested
    if ($targetGroup) {
        $groups = @($targetGroup)
    }

    # Get computers
    $allComputers = $wsus.GetComputerTargets()
    $totalComputers = $allComputers.Count

    # Calculate compliance per group
    $groupCompliance = @()
    $totalNeeded = 0
    $totalFailed = 0
    $compliantComputers = 0

    foreach ($group in $groups) {
        # Skip "All Computers" group in per-group stats
        if ($group.Name -eq "All Computers") {
            continue
        }

        $groupComputers = $group.GetComputerTargets()
        $groupTotal = $groupComputers.Count

        if ($groupTotal -eq 0) {
            continue
        }

        $groupNeeded = 0
        $groupFailed = 0
        $groupCompliant = 0

        foreach ($computer in $groupComputers) {
            $status = $computer.GetUpdateInstallationSummary()
            $needed = $status.NotInstalledCount + $status.DownloadedCount
            $failed = $status.FailedCount

            $groupNeeded += $needed
            $groupFailed += $failed

            if ($needed -eq 0 -and $failed -eq 0) {
                $groupCompliant++
            }
        }

        $compliancePercent = if ($groupTotal -gt 0) { ($groupCompliant / $groupTotal) * 100 } else { 0 }

        $groupCompliance += @{
            GroupId = $group.Id.ToString()
            GroupName = $group.Name
            TotalComputers = $groupTotal
            CompliantComputers = $groupCompliant
            CompliancePercent = [math]::Round($compliancePercent, 1)
            TotalNeededUpdates = $groupNeeded
            TotalFailedUpdates = $groupFailed
        }

        $totalNeeded += $groupNeeded
        $totalFailed += $groupFailed
    }

    # Calculate overall compliance
    foreach ($computer in $allComputers) {
        $status = $computer.GetUpdateInstallationSummary()
        $needed = $status.NotInstalledCount + $status.DownloadedCount
        $failed = $status.FailedCount

        if ($needed -eq 0 -and $failed -eq 0) {
            $compliantComputers++
        }
    }

    $overallCompliancePercent = if ($totalComputers -gt 0) { ($compliantComputers / $totalComputers) * 100 } else { 0 }

    # Get approved updates count
    $updateScope = New-Object Microsoft.UpdateServices.Administration.UpdateScope
    $updateScope.ApprovedStates = [Microsoft.UpdateServices.Administration.ApprovedStates]::LatestRevisionApproved
    $approvedUpdates = $wsus.GetUpdates($updateScope)

    @{
        TotalComputers = $totalComputers
        CompliantComputers = $compliantComputers
        NonCompliantComputers = $totalComputers - $compliantComputers
        TotalApprovedUpdates = $approvedUpdates.Count
        CompliancePercent = [math]::Round($overallCompliancePercent, 1)
        GroupCompliance = $groupCompliance
        TotalNeededUpdates = $totalNeeded
        TotalFailedUpdates = $totalFailed
    }
}
catch {
    @{
        Error = $_.Exception.Message
    }
}
