param(
    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [ValidatePattern('^[a-zA-Z0-9\-_.]+$')]
    [string]$ServerName,

    [Parameter(Mandatory=$true)]
    [ValidateRange(1, 65535)]
    [int]$Port,

    [Parameter(Mandatory=$false)]
    [bool]$UseSsl = $false
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

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
