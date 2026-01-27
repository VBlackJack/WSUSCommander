# Copyright 2025 Julien Bombled
# Licensed under the Apache License, Version 2.0

param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [ValidatePattern('^[a-zA-Z0-9\-_.]+$')]
    [string]$ServerName,

    [Parameter(Mandatory = $true)]
    [ValidateRange(1, 65535)]
    [int]$Port,

    [Parameter(Mandatory = $true)]
    [bool]$UseSsl,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [ValidatePattern('^[0-9a-fA-F-]{36}$')]
    [string]$UpdateId
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

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

    # Get the update
    $updateGuid = [Guid]$UpdateId
    $updateRevisionId = New-Object Microsoft.UpdateServices.Administration.UpdateRevisionId($updateGuid)
    $update = $wsus.GetUpdate($updateRevisionId)

    if (-not $update) {
        Write-Error "Update not found: $UpdateId" -ErrorAction Stop
    }

    # Decline the update
    $update.Decline()

    return [PSCustomObject]@{
        Success  = $true
        UpdateId = $UpdateId
        Action   = "Declined"
    }
}
catch {
    $errorResult = @{
        Success = $false
        Error   = @{
            Message = $_.Exception.Message
            Type    = $_.Exception.GetType().Name
        }
    }
    Write-Output ($errorResult | ConvertTo-Json -Depth 5 -Compress)
    exit 1
}
