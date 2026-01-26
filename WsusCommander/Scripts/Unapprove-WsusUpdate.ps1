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
    Removes approval for a WSUS update from a specific group.

.PARAMETER UpdateId
    The GUID of the update to unapprove.

.PARAMETER GroupId
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
    [string]$UpdateId,

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

    # Get the update
    $updateGuid = [Guid]::Parse($UpdateId)
    $update = $wsus.GetUpdate([Microsoft.UpdateServices.Administration.UpdateRevisionId]::new($updateGuid, 0))

    if (-not $update) {
        throw "Update not found: $UpdateId"
    }

    # Get the target group
    $groupGuid = [Guid]::Parse($GroupId)
    $group = $wsus.GetComputerTargetGroup($groupGuid)

    if (-not $group) {
        throw "Computer group not found: $GroupId"
    }

    # Get current approval for this group and remove it
    $approvals = $update.GetUpdateApprovals()
    $groupApproval = $approvals | Where-Object { $_.ComputerTargetGroupId -eq $groupGuid }

    if ($groupApproval) {
        foreach ($approval in $groupApproval) {
            $approval.Delete()
        }

        @{
            Success = $true
            UpdateId = $UpdateId
            GroupId = $GroupId
            GroupName = $group.Name
            Message = "Update approval removed from group '$($group.Name)'"
        }
    }
    else {
        @{
            Success = $true
            UpdateId = $UpdateId
            GroupId = $GroupId
            GroupName = $group.Name
            Message = "Update was not approved for group '$($group.Name)'"
        }
    }
}
catch {
    @{
        Success = $false
        Error = $_.Exception.Message
        UpdateId = $UpdateId
        GroupId = $GroupId
    }
}
