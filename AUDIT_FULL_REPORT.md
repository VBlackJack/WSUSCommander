# WSUS Commander - Full Audit Report
Date: 2026-01-27

## Executive Summary
- Overall Score: 62/100
- Critical Issues: 1
- Major Issues: 5
- Minor Issues: 9
- Improvements: 12

## Critical Issues (Fix Immediately)
### [CRIT-001] PowerShell command injection and path traversal risk in script execution
- **File**: WsusCommander/Services/PowerShellService.cs:48-266
- **Description**: Script invocation previously concatenated script paths and parameters directly into a `-Command` string without validating the script name or escaping values, which could allow path traversal and PowerShell injection if any caller supplied crafted values.
- **Impact**: Potential execution of unintended scripts or injection of arbitrary PowerShell commands.
- **Fix Applied**: Yes
- **Fix Details**: Added script name validation (no path segments, `.ps1` only), parameter name validation, and PowerShell string escaping before command construction.

## Major Issues (Fix Soon)
### [MAJ-001] MainViewModel is a god object with excessive responsibilities
- **File**: WsusCommander/ViewModels/MainViewModel.cs:31-55
- **Description**: The main view model coordinates 20+ services and spans 3,000+ lines, mixing connection lifecycle, updates, groups, reporting, settings, and UI behavior in a single class.
- **Impact**: Violates SRP, makes testing difficult, and increases defect risk when modifying unrelated features.
- **Fix Applied**: No

### [MAJ-002] MVVM violations: ViewModel instantiates views and uses UI primitives
- **File**: WsusCommander/ViewModels/MainViewModel.cs:2166-2173, 2430-2441
- **Description**: The view model directly creates `ComputerUpdatesWindow` and uses `Application.Current.MainWindow` / `Clipboard` APIs.
- **Impact**: Tight coupling to UI framework makes the view model hard to test and limits portability.
- **Fix Applied**: No

### [MAJ-003] Authentication disabled by default in configuration
- **File**: WsusCommander/appsettings.json:16-24
- **Description**: `RequireAuthentication` defaults to `false` in the application configuration.
- **Impact**: Operators may run the tool without authentication unless explicitly configured, increasing risk of unauthorized access.
- **Fix Applied**: No (requires product decision)

### [MAJ-004] PowerShell scripts lack parameter validation and consistent error hygiene
- **File**: WsusCommander/Scripts/Approve-WsusUpdate.ps1:4-68 (representative of Scripts/)
- **Description**: Script parameters are not validated (e.g., `ValidateNotNullOrEmpty`, `ValidatePattern`), and errors are rethrown with raw exception text.
- **Impact**: Increased risk of malformed input causing runtime failures; error output may leak internal details.
- **Fix Applied**: No

### [MAJ-005] Limited cancellation support for long-running operations
- **File**: WsusCommander/Services/PowerShellService.cs:48-195
- **Description**: Script execution lacks cancellation token support, preventing graceful cancellation of long-running WSUS operations.
- **Impact**: UI responsiveness and operational control suffer during lengthy tasks.
- **Fix Applied**: No

## Minor Issues (Fix When Possible)
### [MIN-001] Logging pipeline swallows write failures silently
- **File**: WsusCommander/Services/LoggingService.cs:124-164
- **Description**: Exceptions during log flush are ignored, which can hide underlying disk or permission issues.
- **Impact**: Loss of observability when logging fails.

### [MIN-002] Export headers and file filters are hardcoded (i18n gap)
- **File**: WsusCommander/Services/ExportService.cs:94-155
- **Description**: CSV headers and file dialog filters are fixed English strings instead of resource-based strings.
- **Impact**: Incomplete localization coverage for export-related text.

### [MIN-003] Data models lack validation annotations
- **File**: WsusCommander/Models/ComputerGroup.cs:20-48
- **Description**: Core models have no validation attributes for required fields or length constraints.
- **Impact**: Invalid data can flow into services, increasing validation burden at call sites.

### [MIN-004] Execution policy bypass is hardcoded
- **File**: WsusCommander/Services/PowerShellService.cs:101-110
- **Description**: PowerShell launches with `-ExecutionPolicy Bypass` unconditionally.
- **Impact**: Reduces security hardening, and should be configurable or documented.

### [MIN-005] Hardcoded status/log strings exist in services
- **File**: WsusCommander/Services/ExportService.cs:79-90
- **Description**: Export logging text is hardcoded instead of using resource strings.
- **Impact**: Reduces consistency with i18n strategy and centralized message management.

### [MIN-006] Retry/backoff patterns are not consistently applied
- **File**: WsusCommander/Services/GroupService.cs:73-252
- **Description**: Service operations call PowerShell without retry or circuit breaker logic despite a retry service existing.
- **Impact**: Reduced resilience to transient failures.

### [MIN-007] UI dependencies leak into view model namespaces
- **File**: WsusCommander/ViewModels/MainViewModel.cs:17-21
- **Description**: View model imports `System.Windows`, which is not strictly MVVM-compliant.
- **Impact**: Harder to reuse or test view models in isolation.

### [MIN-008] Async exception handling logs but doesn't surface UI feedback
- **File**: WsusCommander/ViewModels/MainViewModel.cs:2443-2446
- **Description**: Clipboard failures are logged but user feedback is not provided.
- **Impact**: User may not know why the action failed.

### [MIN-009] PowerShell output parsing lacks depth configuration
- **File**: WsusCommander/Services/PowerShellService.cs:95-97
- **Description**: `ConvertTo-Json` uses default depth, which may truncate nested objects.
- **Impact**: Potential loss of information in complex WSUS outputs.

## Recommendations
1. Split `MainViewModel` into feature-focused view models (Updates, Computers, Reports, Settings), and introduce coordinators or mediator services.
2. Introduce UI abstraction services (dialog, clipboard, navigation) to remove `System.Windows` dependencies from view models.
3. Add validation attributes to input models and enforce validation in services before invoking PowerShell scripts.
4. Update PowerShell scripts with `ValidateNotNullOrEmpty`, `ValidatePattern`, and consistent error handling; consider returning structured JSON explicitly.
5. Make authentication enabled by default or enforce a first-run configuration step.
6. Add cancellation token support to script execution and long-running service calls.
7. Localize export headers and file filter strings through resources.
8. Document execution policy requirements and provide a configurable setting.

## Files Audited
| File | Lines | Issues | Score |
|------|-------|--------|-------|
| WsusCommander/Services/PowerShellService.cs | 267 | 3 | 70 |
| WsusCommander/ViewModels/MainViewModel.cs | 3075 | 5 | 45 |
| WsusCommander/Services/GroupService.cs | 326 | 2 | 65 |
| WsusCommander/Services/ReportService.cs | 331 | 1 | 70 |
| WsusCommander/Services/HealthService.cs | 401 | 1 | 72 |
| WsusCommander/Services/ExportService.cs | 226 | 2 | 75 |
| WsusCommander/MainWindow.xaml.cs | 159 | 0 | 90 |
| WsusCommander/Scripts/Approve-WsusUpdate.ps1 | 68 | 2 | 70 |
| WsusCommander/appsettings.json | 47 | 1 | 80 |
| WsusCommander/Models/ComputerGroup.cs | 48 | 1 | 80 |
