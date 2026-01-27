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
    [string]$GroupId
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

    # Get the group
    $groupGuid = [Guid]$GroupId
    $group = $wsus.GetComputerTargetGroup($groupGuid)

    if (-not $group) {
        Write-Error "Group not found: $GroupId" -ErrorAction Stop
    }

    # Check if this is a system group
    $systemGroupNames = @("All Computers", "Unassigned Computers")
    if ($group.Name -in $systemGroupNames) {
        Write-Error "Cannot remove computers from system group '$($group.Name)'" -ErrorAction Stop
    }

    # Remove computer from group
    $group.RemoveComputerTarget($computer)

    return [PSCustomObject]@{
        Success      = $true
        ComputerId   = $ComputerId
        ComputerName = $computer.FullDomainName
        GroupId      = $GroupId
        GroupName    = $group.Name
        Message      = "Computer '$($computer.FullDomainName)' removed from group '$($group.Name)'"
    }
}
catch {
    Write-Error "Failed to remove computer from group: $_" -ErrorAction Stop
    throw $_
}
