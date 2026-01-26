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
    Gets child groups of a WSUS computer target group.

.PARAMETER ParentGroupId
    The GUID of the parent group.

.PARAMETER WsusServer
    The WSUS server name.

.PARAMETER WsusPort
    The WSUS server port.

.PARAMETER UseSsl
    Whether to use SSL connection.
#>

param(
    [Parameter(Mandatory = $true)]
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

    # Get the parent group
    $parentGuid = [Guid]::Parse($ParentGroupId)
    $parentGroup = $wsus.GetComputerTargetGroup($parentGuid)

    if (-not $parentGroup) {
        throw "Parent group not found: $ParentGroupId"
    }

    # Get child groups
    $childGroups = $parentGroup.GetChildTargetGroups()

    $childGroups | ForEach-Object {
        @{
            Id = $_.Id.ToString()
            Name = $_.Name
            Description = $_.Description
            ComputerCount = $_.GetComputerTargets().Count
            ParentGroupId = $ParentGroupId
        }
    }
}
catch {
    @{
        Error = $_.Exception.Message
        ParentGroupId = $ParentGroupId
    }
}
