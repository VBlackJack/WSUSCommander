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
    Deletes a WSUS computer target group.

.PARAMETER GroupId
    The GUID of the group to delete.

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

    # Check if this is a system group
    $systemGroupNames = @("All Computers", "Unassigned Computers")
    if ($group.Name -in $systemGroupNames) {
        throw "Cannot delete system group: $($group.Name)"
    }

    # Check if group has computers
    $computerCount = $group.GetComputerTargets().Count
    if ($computerCount -gt 0) {
        throw "Cannot delete group with $computerCount computers. Move or remove computers first."
    }

    # Check if group has child groups
    $childGroups = $group.GetChildTargetGroups()
    if ($childGroups.Count -gt 0) {
        throw "Cannot delete group with $($childGroups.Count) child groups. Delete child groups first."
    }

    $groupName = $group.Name

    # Delete the group
    $wsus.DeleteComputerTargetGroup($groupGuid)

    @{
        Success = $true
        GroupId = $GroupId
        GroupName = $groupName
        Message = "Group '$groupName' deleted successfully"
    }
}
catch {
    @{
        Success = $false
        Error = $_.Exception.Message
        GroupId = $GroupId
    }
}
