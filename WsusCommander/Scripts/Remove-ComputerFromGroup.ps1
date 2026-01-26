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
    Removes a computer from a WSUS target group.

.PARAMETER ComputerId
    The computer target ID.

.PARAMETER GroupId
    The GUID of the group to remove from.

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
    [string]$GroupId,

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

    # Get the group
    $groupGuid = [Guid]::Parse($GroupId)
    $group = $wsus.GetComputerTargetGroup($groupGuid)

    if (-not $group) {
        throw "Group not found: $GroupId"
    }

    # Check if this is a system group
    $systemGroupNames = @("All Computers", "Unassigned Computers")
    if ($group.Name -in $systemGroupNames) {
        throw "Cannot remove computers from system group '$($group.Name)'"
    }

    # Remove computer from group
    $group.RemoveComputerTarget($computer)

    @{
        Success = $true
        ComputerId = $ComputerId
        ComputerName = $computer.FullDomainName
        GroupId = $GroupId
        GroupName = $group.Name
        Message = "Computer '$($computer.FullDomainName)' removed from group '$($group.Name)'"
    }
}
catch {
    @{
        Success = $false
        Error = $_.Exception.Message
        ComputerId = $ComputerId
        GroupId = $GroupId
    }
}
