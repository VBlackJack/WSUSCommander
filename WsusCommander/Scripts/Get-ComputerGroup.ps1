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
    Gets a specific WSUS computer target group.

.PARAMETER GroupId
    The GUID of the group to retrieve.

.PARAMETER WsusServer
    The WSUS server name.

.PARAMETER WsusPort
    The WSUS server port.

.PARAMETER UseSsl
    Whether to use SSL connection.
#>

param(
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

    # Get the group
    $groupGuid = [Guid]::Parse($GroupId)
    $group = $wsus.GetComputerTargetGroup($groupGuid)

    if (-not $group) {
        throw "Group not found: $GroupId"
    }

    @{
        Id = $group.Id.ToString()
        Name = $group.Name
        Description = $group.Description
        ComputerCount = $group.GetComputerTargets().Count
        ParentGroupId = if ($group.GetParentTargetGroup()) { $group.GetParentTargetGroup().Id.ToString() } else { $null }
    }
}
catch {
    @{
        Error = $_.Exception.Message
        GroupId = $GroupId
    }
}
