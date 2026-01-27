# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

```bash
# Build
dotnet build

# Run application
dotnet run --project WsusCommander

# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity normal

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Build release
dotnet publish -c Release -r win-x64 --self-contained false
```

## Architecture Overview

WSUS Commander is a WPF application (.NET 8.0, C# 12) for managing Windows Server Update Services. It uses MVVM pattern with CommunityToolkit.Mvvm and manual dependency injection.

### Data Flow

```
UI (XAML) → ViewModel → Services → PowerShell Scripts → WSUS API
```

- **Composition root**: `App.xaml.cs` instantiates all services and injects them into `MainViewModel`
- **PowerShell scripts** (`Scripts/*.ps1`) are the data access layer - all WSUS operations go through them
- **Services** (`Services/`) encapsulate business logic with interface/implementation pairs (all implementations are sealed)

### Key Patterns

- **MVVM**: `[ObservableProperty]` and `[RelayCommand]` attributes from CommunityToolkit.Mvvm
- **Manual DI**: Services created in `App.OnStartup()`, disposed in `App.OnExit()`
- **Resource management**: Services implement `IDisposable` (CacheService, HealthService, TimerService)

### Localization

Uses .NET resource files in `Properties/`:
- `Resources.resx` (English)
- `Resources.fr.resx` (French)

Access via: `WsusCommander.Properties.Resources.KeyName`

### Configuration

All settings in `appsettings.json` with validation via `AppConfig.cs`. Key sections:
- `WsusConnection`: Server, port, SSL settings
- `Security`: AD groups for role-based access (Administrator/Operator/Viewer)
- `Performance`: Cache TTL, retry attempts, timeouts

## Code Standards

- **License header**: Apache 2.0 with author "Julien Bombled" on all new files
- **Language**: English only for code, comments, and documentation
- **Namespaces**: `WsusCommander`, `WsusCommander.Models`, `WsusCommander.Services`, `WsusCommander.ViewModels`
- **Types**: All classes are sealed; use file-scoped namespaces
- **Documentation**: XML docs (`/// <summary>`) on all public members

## Testing

- Framework: xUnit with Moq and FluentAssertions
- Location: `WsusCommander.Tests/Services/`
- Naming convention: `[MethodName]_[Scenario]_[Expected]`

## PowerShell Scripts

Located in `WsusCommander/Scripts/`. Each script:
- Has Apache 2.0 header
- Uses `param()` with `[Parameter(Mandatory)]` attributes
- Returns `PSCustomObject` for C# deserialization
- Has proper error handling with `try/catch` and `-ErrorAction Stop`

Key scripts: `Connect-WsusServer.ps1`, `Get-WsusUpdates.ps1`, `Approve-WsusUpdate.ps1`, `Get-ComputerGroups.ps1`, `Get-ComplianceReport.ps1`

## Requirements

- Windows 10/11 or Windows Server 2016+
- .NET 8.0 Runtime
- PowerShell 5.1+ with WSUS Administration Console installed (provides UpdateServices module)
