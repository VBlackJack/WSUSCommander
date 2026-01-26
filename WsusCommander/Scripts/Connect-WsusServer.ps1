param(
    [string]$ServerName,
    [int]$Port,
    [bool]$UseSsl
)

# Defensive coding: Check if module exists
if (-not (Get-Module -ListAvailable -Name UpdateServices)) {
    Write-Error "WSUS Module (UpdateServices) is not installed on this machine." -ErrorAction Stop
}

# Attempt connection (This returns a IUpdateServer object)
try {
    $wsus = [Microsoft.UpdateServices.Administration.AdminProxy]::GetUpdateServer($ServerName, $UseSsl, $Port)
    # Return a simplified object for C#
    return [PSCustomObject]@{
        Name = $wsus.Name
        IsConnected = $true
        Version = $wsus.Version.ToString()
    }
}
catch {
    throw $_
}
