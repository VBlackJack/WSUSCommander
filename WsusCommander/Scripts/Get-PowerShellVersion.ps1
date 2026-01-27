# Copyright 2025 Julien Bombled
# Licensed under the Apache License, Version 2.0

# Simple health check script to verify PowerShell is working
return [PSCustomObject]@{
    Version     = $PSVersionTable.PSVersion.ToString()
    Edition     = $PSVersionTable.PSEdition
    Platform    = [Environment]::OSVersion.Platform.ToString()
    IsHealthy   = $true
}
