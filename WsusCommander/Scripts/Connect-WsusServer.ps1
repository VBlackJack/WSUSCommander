param(
    [Parameter(Mandatory=$true)]
    [string]$ServerName,

    [Parameter(Mandatory=$true)]
    [int]$Port,

    [Parameter(Mandatory=$false)]
    [bool]$UseSsl = $false
)

$ErrorActionPreference = 'Stop'

try {
    # Load the WSUS assembly
    [reflection.assembly]::LoadWithPartialName("Microsoft.UpdateServices.Administration") | Out-Null

    # Attempt connection
    $wsus = [Microsoft.UpdateServices.Administration.AdminProxy]::GetUpdateServer($ServerName, $UseSsl, $Port)

    # Return a simplified object for JSON serialization
    [PSCustomObject]@{
        Name = $wsus.Name
        IsConnected = $true
        Version = $wsus.Version.ToString()
    }
}
catch {
    Write-Error $_.Exception.Message
    exit 1
}
