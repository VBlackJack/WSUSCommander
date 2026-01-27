# Copyright 2025 Julien Bombled
# Licensed under the Apache License, Version 2.0

param(
    [Parameter(Mandatory = $true)]
    [string]$ServerName,

    [Parameter(Mandatory = $true)]
    [int]$Port,

    [Parameter(Mandatory = $true)]
    [bool]$UseSsl,

    [Parameter(Mandatory = $true)]
    [string]$ComputerId,

    [Parameter(Mandatory = $true)]
    [string]$TargetGroupId
)

try {
    # Defensive coding: Check if module exists
    if (-not (Get-Module -ListAvailable -Name UpdateServices)) {
        Write-Error "WSUS Module (UpdateServices) is not installed on this machine." -ErrorAction Stop
    }

    # Load the WSUS assembly
    [reflection.assembly]::LoadWithPartialName("Microsoft.UpdateServices.Administration") | Out-Null

    # Connect to WSUS server
    $wsus = [Microsoft.UpdateServices.Administration.AdminProxy]::GetUpdateServer($ServerName, $UseSsl, $Port)

    if (-not $wsus) {
        Write-Error "Failed to connect to WSUS server: $ServerName" -ErrorAction Stop
    }

    # Get the computer
    $computer = $wsus.GetComputerTarget($ComputerId)

    if (-not $computer) {
        Write-Error "Computer not found: $ComputerId" -ErrorAction Stop
    }

    # Get the target group
    $groupGuid = [Guid]$TargetGroupId
    $targetGroup = $wsus.GetComputerTargetGroup($groupGuid)

    if (-not $targetGroup) {
        Write-Error "Target group not found: $TargetGroupId" -ErrorAction Stop
    }

    # Get current group memberships
    $currentGroups = $computer.GetComputerTargetGroups()

    # Remove from non-automatic groups (keep "All Computers" and "Unassigned Computers")
    $systemGroupNames = @("All Computers", "Unassigned Computers")

    foreach ($group in $currentGroups) {
        if ($group.Name -notin $systemGroupNames -and $group.Id -ne $groupGuid) {
            $groupToRemove = $wsus.GetComputerTargetGroup($group.Id)
            if ($groupToRemove) {
                $groupToRemove.RemoveComputerTarget($computer)
            }
        }
    }

    # Add to new group
    $targetGroup.AddComputerTarget($computer)

    return [PSCustomObject]@{
        Success         = $true
        ComputerId      = $ComputerId
        ComputerName    = $computer.FullDomainName
        TargetGroupId   = $TargetGroupId
        TargetGroupName = $targetGroup.Name
        Message         = "Computer '$($computer.FullDomainName)' moved to group '$($targetGroup.Name)'"
    }
}
catch {
    Write-Error "Failed to move computer to group: $_" -ErrorAction Stop
    throw $_
}
