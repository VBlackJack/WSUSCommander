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

    $computerName = $computer.FullDomainName
    if ([string]::IsNullOrWhiteSpace($computerName)) {
        Write-Error "Computer name could not be resolved for $ComputerId" -ErrorAction Stop
    }

    Invoke-Command -ComputerName $computerName -ScriptBlock {
        if (Get-Command -Name usoclient -ErrorAction SilentlyContinue) {
            usoclient StartScan | Out-Null
        }
        elseif (Test-Path "$env:SystemRoot\System32\wuauclt.exe") {
            wuauclt /detectnow /reportnow | Out-Null
        }
        else {
            throw "No supported update client found."
        }
    } -ErrorAction Stop

    return [PSCustomObject]@{
        Success      = $true
        ComputerId   = $ComputerId
        ComputerName = $computerName
        Message      = "Triggered update scan on $computerName"
    }
}
catch {
    Write-Error "Failed to trigger computer scan: $_" -ErrorAction Stop
    throw $_
}
