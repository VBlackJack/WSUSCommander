# Copyright 2025 Julien Bombled
# Licensed under the Apache License, Version 2.0
#
# Gets installation status for an update in specified groups.

param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-fA-F-]{36}$')]
    [string]$UpdateId,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$GroupIds,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$WsusServer,

    [Parameter(Mandatory = $true)]
    [ValidateRange(1, 65535)]
    [int]$WsusPort,

    [Parameter(Mandatory = $true)]
    [bool]$WsusUseSsl
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

try {
    # Connect to WSUS
    $wsus = Get-WsusServer -Name $WsusServer -PortNumber $WsusPort -UseSsl:$WsusUseSsl

    # Get the update
    $updateRevisionId = New-Object Microsoft.UpdateServices.Administration.UpdateRevisionId([Guid]$UpdateId)
    $update = $wsus.GetUpdate($updateRevisionId)

    if (-not $update) {
        throw "Update not found: $UpdateId"
    }

    $installed = 0
    $failed = 0
    $pending = 0
    $notApplicable = 0

    # Process each group
    $groupIdArray = $GroupIds.Split(',')

    foreach ($groupId in $groupIdArray) {
        $group = $wsus.GetComputerTargetGroup([Guid]$groupId.Trim())
        if (-not $group) {
            continue
        }

        # Get computers in this group
        $computerScope = New-Object Microsoft.UpdateServices.Administration.ComputerTargetScope
        $computerScope.ComputerTargetGroups.Add($group) | Out-Null

        $computers = $wsus.GetComputerTargets($computerScope)

        foreach ($computer in $computers) {
            # Get update status for this computer
            $updateScope = New-Object Microsoft.UpdateServices.Administration.UpdateScope
            $updateScope.UpdateIds.Add($update.Id.UpdateId) | Out-Null

            $updateInfoCollection = $computer.GetUpdateInstallationInfoPerUpdate($updateScope)

            foreach ($updateInfo in $updateInfoCollection) {
                switch ($updateInfo.UpdateInstallationState) {
                    "Installed" { $installed++ }
                    "InstalledPendingReboot" { $installed++ }
                    "Downloaded" { $pending++ }
                    "NotInstalled" { $pending++ }
                    "Failed" { $failed++ }
                    "NotApplicable" { $notApplicable++ }
                    default { $pending++ }
                }
            }
        }
    }

    return [PSCustomObject]@{
        Success       = $true
        UpdateId      = $UpdateId
        Installed     = $installed
        Failed        = $failed
        Pending       = $pending
        NotApplicable = $notApplicable
        Total         = $installed + $failed + $pending + $notApplicable
    }
}
catch {
    return [PSCustomObject]@{
        Success = $false
        Error   = @{
            Message = $_.Exception.Message
            Type    = $_.Exception.GetType().Name
        }
        Installed     = 0
        Failed        = 0
        Pending       = 0
        NotApplicable = 0
        Total         = 0
    }
}
