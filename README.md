# WSUS Commander

A modern WPF application for managing Windows Server Update Services (WSUS) with an intuitive interface and powerful features.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Platform](https://img.shields.io/badge/Platform-Windows-0078D6?logo=windows)
![License](https://img.shields.io/badge/License-Apache%202.0-blue)
![Tests](https://img.shields.io/badge/Tests-87%20passing-brightgreen)

## Features

### Update Management
- **View Updates** - Browse all updates with filtering and search
- **Approve/Decline** - Approve updates for specific computer groups or decline them
- **Bulk Operations** - Approve or decline multiple updates at once
- **Update Details** - View detailed information including CVEs, supersedence, and file size

### Computer Management
- **Computer Status** - Monitor installed, needed, and failed updates per computer
- **Group Management** - Create, edit, and delete computer groups
- **Compliance Reports** - Generate compliance reports with export capability

### Synchronization
- **Sync Status** - View last sync time and current sync state
- **Manual Sync** - Trigger synchronization on demand

### Export & Reports
- **Multiple Formats** - Export to CSV, TSV, or JSON
- **Compliance Reports** - Overall compliance percentage and details
- **Stale Computers** - Identify computers that haven't reported recently

### User Experience
- **Search & Filter** - Quick search and advanced filtering options
- **Auto-Refresh** - Configurable automatic data refresh
- **Dark/Light Theme** - Respects system theme preferences
- **Keyboard Accessible** - Full keyboard navigation support
- **Multi-language** - English and French localization

## Requirements

- Windows 10/11 or Windows Server 2016+
- .NET 8.0 Runtime
- PowerShell 5.1 or PowerShell 7+
- WSUS Administration Console (for WSUS cmdlets)
- Network access to WSUS server

## Installation

### From Release

1. Download the latest release from [Releases](https://github.com/VBlackJack/WSUSCommander/releases)
2. Extract to your preferred location
3. Run `WsusCommander.exe`

### From Source

```bash
# Clone the repository
git clone https://github.com/VBlackJack/WSUSCommander.git
cd WSUSCommander

# Build the solution
dotnet build

# Run the application
dotnet run --project WsusCommander
```

## Configuration

Edit `appsettings.json` to configure the application:

```json
{
  "WsusConnection": {
    "ServerName": "localhost",
    "Port": 8530,
    "UseSsl": false,
    "TimeoutSeconds": 30
  },
  "Security": {
    "RequireAuthentication": false,
    "AdministratorGroups": ["WSUS Administrators", "Domain Admins"],
    "OperatorGroups": ["WSUS Operators"],
    "RequireApprovalConfirmation": true,
    "RequireDeclineConfirmation": true
  },
  "Performance": {
    "CacheTtlSeconds": 300,
    "MaxRetryAttempts": 3
  }
}
```

### Configuration Sections

| Section | Description |
|---------|-------------|
| `WsusConnection` | WSUS server connection settings |
| `AppSettings` | General application settings (paths, intervals) |
| `Security` | Authentication and authorization settings |
| `Logging` | Log level, format, and retention |
| `Performance` | Cache TTL, retry attempts, timeouts |
| `UI` | Theme, window state, tooltips |

## Architecture

```
WsusCommander/
├── Models/              # Data models
├── ViewModels/          # MVVM ViewModels
├── Views/               # XAML views
├── Services/            # Business logic services
│   ├── I*Service.cs     # Service interfaces
│   └── *Service.cs      # Service implementations
├── Scripts/             # PowerShell scripts for WSUS operations
├── Properties/          # Resources and localization
└── Converters/          # WPF value converters
```

### Key Services

| Service | Purpose |
|---------|---------|
| `PowerShellService` | Executes PowerShell scripts for WSUS operations |
| `AuthenticationService` | Windows authentication and user identity |
| `AuthorizationService` | Role-based access control |
| `CacheService` | In-memory caching with TTL |
| `RetryService` | Retry logic with exponential backoff |
| `ExportService` | Data export to various formats |
| `FilterService` | Update and computer filtering |
| `HealthService` | System health monitoring |

### Design Patterns

- **MVVM** - Model-View-ViewModel with CommunityToolkit.Mvvm
- **Dependency Injection** - Manual DI in composition root
- **Repository Pattern** - PowerShell scripts as data access layer
- **Service Layer** - Business logic encapsulation

## User Roles

| Role | Permissions |
|------|-------------|
| **Administrator** | Full access to all features |
| **Operator** | Approve/decline updates, sync, export |
| **Viewer** | View-only access |

Roles are determined by Active Directory group membership configured in `appsettings.json`.

## Running Tests

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Building for Release

```bash
# Build release version
dotnet publish -c Release -r win-x64 --self-contained false

# Output will be in:
# WsusCommander/bin/Release/net8.0-windows/win-x64/publish/
```

## Troubleshooting

### Cannot connect to WSUS server

1. Verify the server name and port in `appsettings.json`
2. Ensure the WSUS Administration Console is installed
3. Check if the WSUS service is running on the server
4. Verify network connectivity and firewall rules

### PowerShell errors

1. Ensure the WSUS PowerShell module is available:
   ```powershell
   Get-Module -ListAvailable UpdateServices
   ```
2. Run PowerShell as Administrator if required
3. Check execution policy: `Get-ExecutionPolicy`

### Authentication issues

1. Verify your AD group membership
2. Check the configured groups in `appsettings.json`
3. Ensure `RequireAuthentication` is set correctly

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

```
Copyright 2025 Julien Bombled

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0
```

## Acknowledgments

- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - MVVM framework
- [Microsoft WSUS](https://docs.microsoft.com/en-us/windows-server/administration/windows-server-update-services/get-started/windows-server-update-services-wsus) - Windows Server Update Services
