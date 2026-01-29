# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

```bash
# Build (debug)
dotnet build

# Run application
dotnet run --project WsusCommander

# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity normal

# Run a single test by name
dotnet test --filter "FullyQualifiedName~CacheServiceTests.GetOrAdd"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Build portable release (ALWAYS use this command for testing)
dotnet publish WsusCommander/WsusCommander.csproj -c Release -r win-x64 --self-contained true -o "publish"
```

## Portable Build Location

**IMPORTANT**: Always publish portable builds to the `publish/` folder at the project root:
- Path: `G:\_dev\WSUSCommander\publish\`
- Executable: `publish\WsusCommander.exe`

This is the ONLY location for portable builds. Never use `bin/Release/*/publish/` or other locations.

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
- **Manual DI**: Services created in `App.OnStartup()`, disposed in `App.OnExit()`. All services have interface/implementation pairs (e.g., `IWsusService`/`WsusService`)
- **Resource management**: Services implement `IDisposable` (CacheService, SchedulerService)

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

Scripts are organized by domain:
- **Connection**: `Connect-WsusServer.ps1`
- **Updates**: `Get-WsusUpdates.ps1`, `Approve-WsusUpdate.ps1`, `Decline-WsusUpdate.ps1`, `Unapprove-WsusUpdate.ps1`, `Get-UpdateDetails.ps1`, `Get-UnapprovedUpdates.ps1`, `Get-CriticalUpdates.ps1`, `Decline-SupersededUpdates.ps1`
- **Computers**: `Get-ComputerStatus.ps1`, `Get-ComputerUpdates.ps1`, `Get-StaleComputers.ps1`, `Force-ComputerScan.ps1`, `Remove-Computer.ps1`
- **Groups**: `Get-ComputerGroups.ps1`, `Get-ComputerGroup.ps1`, `New-ComputerGroup.ps1`, `Set-ComputerGroup.ps1`, `Remove-ComputerGroup.ps1`, `Move-ComputerToGroup.ps1`, `Remove-ComputerFromGroup.ps1`, `Get-ChildGroups.ps1`
- **Reports**: `Get-ComplianceReport.ps1`, `Get-DashboardStats.ps1`, `Get-ActivityLog.ps1`
- **Maintenance**: `Get-SyncStatus.ps1`, `Start-WsusSync.ps1`, `Invoke-WsusCleanup.ps1`

## Requirements

- Windows 10/11 or Windows Server 2016+
- .NET 8.0 Runtime
- PowerShell 5.1+ with WSUS Administration Console installed (provides UpdateServices module)

## BMAD Method

This project uses the BMAD (Breakthrough Method of Agile AI Driven Development) framework for structured improvements.

### Key Files

| File | Purpose |
|------|---------|
| `_bmad-output/project-context.md` | Current project state and patterns |
| `_bmad-output/sprint-status.yaml` | Backlog and sprint tracking |
| `_bmad/docs/checklists.md` | All review checklists |
| `_bmad/docs/quick-start.md` | BMAD usage guide |

### Agent Personas

- **Supervisor**: Orchestrates improvements, coordinates other agents
- **PM**: Defines requirements, prioritizes backlog
- **Architect**: Makes and documents technical decisions (ADRs)
- **Dev**: Implements features and tests
- **QA**: Reviews code, validates quality
- **SM**: Manages sprints and tracks progress

### Workflows

1. **Quick Flow**: Simple fixes (1-5 stories) - specify, implement, review
2. **Standard Flow**: Features (5-15 stories) - PRD, architecture, stories, implement, review
3. **Full Flow**: Major features (15+ stories) - includes research, epics, retrospectives

### Common Tasks

- **Analyze codebase**: Review for improvement opportunities
- **Plan improvement**: Create PRD and stories for a feature
- **Implement story**: Build a planned story following patterns
- **Review changes**: Adversarial code review
- **Sprint status**: Check current progress
