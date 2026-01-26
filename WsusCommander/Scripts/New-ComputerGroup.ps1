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
    Creates a new WSUS computer target group.

.PARAMETER GroupName
    The name for the new group.

.PARAMETER Description
    Optional description for the group.

.PARAMETER ParentGroupId
    Optional parent group ID for nested groups.

.PARAMETER WsusServer
    The WSUS server name.

.PARAMETER WsusPort
    The WSUS server port.

.PARAMETER UseSsl
    Whether to use SSL connection.
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$GroupName,

    [Parameter(Mandatory = $false)]
    [string]$Description,

    [Parameter(Mandatory = $false)]
    [string]$ParentGroupId,

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

    # Get parent group if specified
    $parentGroup = $null
    if ($ParentGroupId) {
        $parentGuid = [Guid]::Parse($ParentGroupId)
        $parentGroup = $wsus.GetComputerTargetGroup($parentGuid)
        if (-not $parentGroup) {
            throw "Parent group not found: $ParentGroupId"
        }
    }

    # Create the new group
    if ($parentGroup) {
        $newGroup = $wsus.CreateComputerTargetGroup($GroupName, $parentGroup)
    }
    else {
        $newGroup = $wsus.CreateComputerTargetGroup($GroupName)
    }

    if (-not $newGroup) {
        throw "Failed to create group: $GroupName"
    }

    # Set description if provided
    if ($Description) {
        $newGroup.Description = $Description
        $newGroup.Save()
    }

    @{
        Id = $newGroup.Id.ToString()
        Name = $newGroup.Name
        Description = $newGroup.Description
        ComputerCount = 0
        ParentGroupId = if ($parentGroup) { $parentGroup.Id.ToString() } else { $null }
    }
}
catch {
    @{
        Error = $_.Exception.Message
        GroupName = $GroupName
    }
}
