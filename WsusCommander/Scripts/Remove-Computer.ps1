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
    [string]$ComputerId
)

try {
    if (-not (Get-Module -ListAvailable -Name UpdateServices)) {
        Write-Error "WSUS Module (UpdateServices) is not installed on this machine." -ErrorAction Stop
    }

    [reflection.assembly]::LoadWithPartialName("Microsoft.UpdateServices.Administration") | Out-Null
    $wsus = [Microsoft.UpdateServices.Administration.AdminProxy]::GetUpdateServer($ServerName, $UseSsl, $Port)

    if (-not $wsus) {
        Write-Error "Failed to connect to WSUS server: $ServerName" -ErrorAction Stop
    }

    $computer = $wsus.GetComputerTarget($ComputerId)

    if (-not $computer) {
        Write-Error "Computer not found: $ComputerId" -ErrorAction Stop
    }

    $wsus.DeleteComputerTarget($computer)

    return [PSCustomObject]@{
        Success      = $true
        ComputerId   = $ComputerId
        ComputerName = $computer.FullDomainName
        Message      = "Removed computer $($computer.FullDomainName)"
    }
}
catch {
    Write-Error "Failed to remove computer: $_" -ErrorAction Stop
    throw $_
}
