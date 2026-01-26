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
    Moves a computer to a different WSUS target group.

.PARAMETER ComputerId
    The computer target ID.

.PARAMETER TargetGroupId
    The GUID of the target group.

.PARAMETER WsusServer
    The WSUS server name.

.PARAMETER WsusPort
    The WSUS server port.

.PARAMETER UseSsl
    Whether to use SSL connection.
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ComputerId,

    [Parameter(Mandatory = $true)]
    [string]$TargetGroupId,

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

    # Get the computer
    $computer = $wsus.GetComputerTarget($ComputerId)

    if (-not $computer) {
        throw "Computer not found: $ComputerId"
    }

    # Get the target group
    $groupGuid = [Guid]::Parse($TargetGroupId)
    $targetGroup = $wsus.GetComputerTargetGroup($groupGuid)

    if (-not $targetGroup) {
        throw "Target group not found: $TargetGroupId"
    }

    # Get current group memberships
    $currentGroups = $computer.GetComputerTargetGroups()

    # Remove from non-automatic groups (keep "All Computers" and "Unassigned Computers")
    $systemGroupNames = @("All Computers", "Unassigned Computers")

    foreach ($group in $currentGroups) {
        if ($group.Name -notin $systemGroupNames -and $group.Id -ne $groupGuid) {
            $targetGroup = $wsus.GetComputerTargetGroup($group.Id)
            if ($targetGroup) {
                $targetGroup.RemoveComputerTarget($computer)
            }
        }
    }

    # Add to new group
    $targetGroup.AddComputerTarget($computer)

    @{
        Success = $true
        ComputerId = $ComputerId
        ComputerName = $computer.FullDomainName
        TargetGroupId = $TargetGroupId
        TargetGroupName = $targetGroup.Name
        Message = "Computer '$($computer.FullDomainName)' moved to group '$($targetGroup.Name)'"
    }
}
catch {
    @{
        Success = $false
        Error = $_.Exception.Message
        ComputerId = $ComputerId
        TargetGroupId = $TargetGroupId
    }
}
