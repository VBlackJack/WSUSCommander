/*
 * Copyright 2025 Julien Bombled
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WsusCommander.Models;
using WsusCommander.Properties;
using WsusCommander.Services;

namespace WsusCommander.ViewModels;

/// <summary>
/// Main view model for the application with full BMAD compliance.
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    #region Services

    private readonly IConfigurationService _configService;
    private readonly IPowerShellService _psService;
    private readonly ILoggingService _loggingService;
    private readonly ITimerService _timerService;
    private readonly IAuthenticationService _authService;
    private readonly IAuthorizationService _authzService;
    private readonly IValidationService _validationService;
    private readonly IRetryService _retryService;
    private readonly ICacheService _cacheService;
    private readonly IDialogService _dialogService;
    private readonly IExportService _exportService;
    private readonly IPreferencesService _preferencesService;
    private readonly IFilterService _filterService;
    private readonly IHealthService _healthService;
    private readonly IAccessibilityService _accessibilityService;
    private readonly IBulkOperationService _bulkOperationService;
    private readonly IGroupService _groupService;
    private readonly IReportService _reportService;

    #endregion

    #region Observable Properties

    private bool _disposed;

    [ObservableProperty]
    private string _statusText = Resources.StatusReady;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isConnecting;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _serverVersion = string.Empty;

    [ObservableProperty]
    private ObservableCollection<WsusUpdate> _updates = [];

    [ObservableProperty]
    private ObservableCollection<WsusUpdate> _filteredUpdates = [];

    [ObservableProperty]
    private WsusUpdate? _selectedUpdate;

    [ObservableProperty]
    private ObservableCollection<WsusUpdate> _selectedUpdates = [];

    [ObservableProperty]
    private ObservableCollection<ComputerGroup> _computerGroups = [];

    [ObservableProperty]
    private ComputerGroup? _selectedComputerGroup;

    [ObservableProperty]
    private SyncStatus? _syncStatus;

    [ObservableProperty]
    private ObservableCollection<ComputerStatus> _computerStatuses = [];

    [ObservableProperty]
    private ObservableCollection<ComputerStatus> _filteredComputerStatuses = [];

    [ObservableProperty]
    private bool _isAutoRefreshEnabled;

    [ObservableProperty]
    private bool _isSyncing;

    [ObservableProperty]
    private bool _isApproving;

    [ObservableProperty]
    private bool _isDeclining;

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private UserIdentity? _currentUser;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedClassification = string.Empty;

    [ObservableProperty]
    private string _selectedApprovalFilter = "All";

    [ObservableProperty]
    private ObservableCollection<string> _classifications = [];

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private HealthReport? _healthReport;

    [ObservableProperty]
    private ComplianceReport? _complianceReport;

    [ObservableProperty]
    private ObservableCollection<StaleComputerInfo> _staleComputers = [];

    [ObservableProperty]
    private CriticalUpdatesSummary? _criticalUpdatesSummary;

    [ObservableProperty]
    private UpdateDetails? _selectedUpdateDetails;

    [ObservableProperty]
    private bool _isLoadingDetails;

    [ObservableProperty]
    private double _bulkProgress;

    [ObservableProperty]
    private string _bulkProgressText = string.Empty;

    [ObservableProperty]
    private bool _isBulkOperationRunning;

    // Group management
    [ObservableProperty]
    private string _newGroupName = string.Empty;

    [ObservableProperty]
    private string _newGroupDescription = string.Empty;

    [ObservableProperty]
    private ComputerGroup? _selectedGroupForEdit;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    public MainViewModel(
        IConfigurationService configService,
        IPowerShellService psService,
        ILoggingService loggingService,
        ITimerService timerService,
        IAuthenticationService authService,
        IAuthorizationService authzService,
        IValidationService validationService,
        IRetryService retryService,
        ICacheService cacheService,
        IDialogService dialogService,
        IExportService exportService,
        IPreferencesService preferencesService,
        IFilterService filterService,
        IHealthService healthService,
        IAccessibilityService accessibilityService,
        IBulkOperationService bulkOperationService,
        IGroupService groupService,
        IReportService reportService)
    {
        _configService = configService;
        _psService = psService;
        _loggingService = loggingService;
        _timerService = timerService;
        _authService = authService;
        _authzService = authzService;
        _validationService = validationService;
        _retryService = retryService;
        _cacheService = cacheService;
        _dialogService = dialogService;
        _exportService = exportService;
        _preferencesService = preferencesService;
        _filterService = filterService;
        _healthService = healthService;
        _accessibilityService = accessibilityService;
        _bulkOperationService = bulkOperationService;
        _groupService = groupService;
        _reportService = reportService;

        // Configure timer
        _timerService.Interval = _configService.AppSettings.AutoRefreshInterval * 1000;
        _timerService.Tick += OnTimerTick;

        // Subscribe to health changes
        _healthService.HealthStatusChanged += OnHealthStatusChanged;

        // Initialize filter options
        ApprovalFilters = ["All", "Approved", "Unapproved", "Declined"];
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the application title from resources.
    /// </summary>
    public string AppTitle => Resources.AppTitle;

    /// <summary>
    /// Gets the connect button text from resources.
    /// </summary>
    public string ConnectButtonText => Resources.BtnConnect;

    /// <summary>
    /// Gets the configured server name.
    /// </summary>
    public string ServerName => _configService.WsusConnection.ServerName;

    /// <summary>
    /// Gets the configured server port.
    /// </summary>
    public int ServerPort => _configService.WsusConnection.Port;

    /// <summary>
    /// Gets the auto-refresh interval in seconds.
    /// </summary>
    public int AutoRefreshIntervalSeconds => _configService.AppSettings.AutoRefreshInterval;

    /// <summary>
    /// Gets the approval filter options.
    /// </summary>
    public List<string> ApprovalFilters { get; }

    /// <summary>
    /// Gets whether the current user can approve updates.
    /// </summary>
    public bool CanUserApprove => _authzService.IsAuthorized(WsusOperation.ApproveUpdate);

    /// <summary>
    /// Gets whether the current user can decline updates.
    /// </summary>
    public bool CanUserDecline => _authzService.IsAuthorized(WsusOperation.DeclineUpdate);

    /// <summary>
    /// Gets whether the current user can manage groups.
    /// </summary>
    public bool CanUserManageGroups => _authzService.IsAuthorized(WsusOperation.ManageGroups);

    /// <summary>
    /// Gets whether the current user can start sync.
    /// </summary>
    public bool CanUserSync => _authzService.IsAuthorized(WsusOperation.StartSync);

    /// <summary>
    /// Gets whether the current user can export data.
    /// </summary>
    public bool CanUserExport => _authzService.IsAuthorized(WsusOperation.ExportData);

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the ViewModel asynchronously (called after construction).
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            // Load preferences
            await _preferencesService.LoadAsync();
            ApplyPreferences();

            // Authenticate user
            if (_configService.Config.Security.RequireAuthentication)
            {
                CurrentUser = await _authService.AuthenticateAsync();
                IsAuthenticated = _authService.IsAuthenticated;

                if (!IsAuthenticated)
                {
                    StatusText = Resources.ErrorNotAuthenticated;
                    return;
                }

                await _loggingService.LogInfoAsync($"User authenticated: {CurrentUser?.AccountName} ({CurrentUser?.Role})");
            }
            else
            {
                IsAuthenticated = true;
            }

            // Initial health check
            HealthReport = await _healthService.CheckHealthAsync();

            // Notify property changes for authorization
            OnPropertyChanged(nameof(CanUserApprove));
            OnPropertyChanged(nameof(CanUserDecline));
            OnPropertyChanged(nameof(CanUserManageGroups));
            OnPropertyChanged(nameof(CanUserSync));
            OnPropertyChanged(nameof(CanUserExport));
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync("Initialization failed", ex);
            StatusText = string.Format(Resources.StatusError, ex.Message);
        }
    }

    private void ApplyPreferences()
    {
        var prefs = _preferencesService.Preferences;
        if (prefs != null)
        {
            IsAutoRefreshEnabled = prefs.AutoRefreshEnabled;
            SelectedTabIndex = prefs.LastSelectedTabIndex;

            if (IsAutoRefreshEnabled)
            {
                _timerService.Start();
            }
        }
    }

    #endregion

    #region Timer Event

    private async void OnTimerTick(object? sender, EventArgs e)
    {
        if (IsConnected && !IsLoading && !IsSyncing && !IsBulkOperationRunning)
        {
            await _loggingService.LogInfoAsync("Auto-refresh triggered.");
            await RefreshAllDataAsync();
        }
    }

    private void OnHealthStatusChanged(object? sender, HealthReport report)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            HealthReport = report;
        });
    }

    #endregion

    #region Connect Command

    /// <summary>
    /// Command to connect to the WSUS server.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectWsusAsync()
    {
        IsConnecting = true;
        StatusText = Resources.StatusReady;

        try
        {
            await _loggingService.LogInfoAsync($"Connecting to WSUS server: {_configService.WsusConnection.ServerName}");

            var parameters = new Dictionary<string, object>
            {
                { "ServerName", _configService.WsusConnection.ServerName },
                { "Port", _configService.WsusConnection.Port },
                { "UseSsl", _configService.WsusConnection.UseSsl }
            };

            // Use retry service for connection
            var results = await _retryService.ExecuteWithRetryAsync(
                async ct => await _psService.ExecuteScriptAsync("Connect-WsusServer.ps1", parameters),
                "ConnectWsus");

            if (results.Count > 0)
            {
                var wsusInfo = results[0];
                var serverName = wsusInfo.Properties["Name"]?.Value?.ToString() ?? _configService.WsusConnection.ServerName;
                ServerVersion = wsusInfo.Properties["Version"]?.Value?.ToString() ?? string.Empty;

                IsConnected = true;
                StatusText = string.Format(Resources.StatusConnected, serverName);

                await _loggingService.LogInfoAsync($"Connected to WSUS server: {serverName}, Version: {ServerVersion}");

                // Load all initial data
                await LoadAllDataAsync();
            }
        }
        catch (Exception ex)
        {
            IsConnected = false;
            StatusText = string.Format(Resources.StatusError, ex.Message);
            await _loggingService.LogErrorAsync("Failed to connect to WSUS server", ex);
            await _dialogService.ShowErrorAsync(Resources.DialogError, Resources.ErrorConnectionFailed, ex.Message);
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private bool CanConnect() => !IsConnecting && !IsLoading && IsAuthenticated;

    #endregion

    #region Refresh Commands

    /// <summary>
    /// Command to refresh the updates list.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task RefreshUpdatesAsync()
    {
        await LoadUpdatesAsync();
        ApplyFilters();
    }

    private bool CanRefresh() => IsConnected && !IsLoading && !IsBulkOperationRunning;

    private async Task RefreshAllDataAsync()
    {
        await LoadUpdatesAsync();
        await LoadSyncStatusAsync();
        ApplyFilters();
    }

    private async Task LoadAllDataAsync()
    {
        await LoadComputerGroupsAsync();
        await LoadSyncStatusAsync();
        await LoadUpdatesAsync();
        ApplyFilters();
    }

    #endregion

    #region Approve/Decline Commands

    /// <summary>
    /// Command to approve the selected update.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanApproveOrDecline))]
    private async Task ApproveUpdateAsync()
    {
        if (SelectedUpdate is null)
        {
            StatusText = Resources.ErrorNoUpdateSelected;
            return;
        }

        if (SelectedComputerGroup is null)
        {
            StatusText = Resources.ErrorNoGroupSelected;
            return;
        }

        // Authorization check
        if (!_authzService.IsAuthorized(WsusOperation.ApproveUpdate))
        {
            await _dialogService.ShowErrorAsync(Resources.DialogError, Resources.ErrorUnauthorized);
            return;
        }

        // Confirmation dialog
        if (_configService.Config.Security.RequireApprovalConfirmation)
        {
            var result = await _dialogService.ShowConfirmationAsync(
                Resources.DialogConfirm,
                string.Format(Resources.ConfirmApproveUpdate, SelectedUpdate.Title, SelectedComputerGroup.Name));

            if (result != DialogResult.Confirmed)
            {
                return;
            }
        }

        IsApproving = true;
        StatusText = Resources.StatusApproving;

        try
        {
            await _loggingService.LogInfoAsync($"Approving update {SelectedUpdate.Id} for group {SelectedComputerGroup.Name}");

            var parameters = new Dictionary<string, object>
            {
                { "ServerName", _configService.WsusConnection.ServerName },
                { "Port", _configService.WsusConnection.Port },
                { "UseSsl", _configService.WsusConnection.UseSsl },
                { "UpdateId", SelectedUpdate.Id.ToString() },
                { "GroupId", SelectedComputerGroup.Id.ToString() }
            };

            var results = await _retryService.ExecuteWithRetryAsync(
                async ct => await _psService.ExecuteScriptAsync("Approve-WsusUpdate.ps1", parameters),
                "ApproveUpdate");

            if (results.Count > 0)
            {
                var psResult = results[0];
                var success = ParseBool(psResult.Properties["Success"]?.Value);

                if (success)
                {
                    StatusText = string.Format(Resources.StatusApproved, SelectedComputerGroup.Name);
                    await _loggingService.LogInfoAsync($"Update {SelectedUpdate.Id} approved for {SelectedComputerGroup.Name}");
                    _dialogService.ShowToast(string.Format(Resources.StatusApproved, SelectedComputerGroup.Name));

                    // Invalidate cache and refresh
                    _cacheService.Remove("updates");
                    await LoadUpdatesAsync();
                    ApplyFilters();
                }
            }
        }
        catch (Exception ex)
        {
            StatusText = string.Format(Resources.StatusError, ex.Message);
            await _loggingService.LogErrorAsync("Failed to approve update", ex);
            await _dialogService.ShowErrorAsync(Resources.DialogError, Resources.StatusError, ex.Message);
        }
        finally
        {
            IsApproving = false;
        }
    }

    /// <summary>
    /// Command to decline the selected update.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanApproveOrDecline))]
    private async Task DeclineUpdateAsync()
    {
        if (SelectedUpdate is null)
        {
            StatusText = Resources.ErrorNoUpdateSelected;
            return;
        }

        // Authorization check
        if (!_authzService.IsAuthorized(WsusOperation.DeclineUpdate))
        {
            await _dialogService.ShowErrorAsync(Resources.DialogError, Resources.ErrorUnauthorized);
            return;
        }

        // Confirmation dialog
        if (_configService.Config.Security.RequireDeclineConfirmation)
        {
            var result = await _dialogService.ShowConfirmationAsync(
                Resources.DialogConfirm,
                string.Format(Resources.ConfirmDeclineUpdate, SelectedUpdate.Title));

            if (result != DialogResult.Confirmed)
            {
                return;
            }
        }

        IsDeclining = true;
        StatusText = Resources.StatusDeclining;

        try
        {
            await _loggingService.LogInfoAsync($"Declining update {SelectedUpdate.Id}");

            var parameters = new Dictionary<string, object>
            {
                { "ServerName", _configService.WsusConnection.ServerName },
                { "Port", _configService.WsusConnection.Port },
                { "UseSsl", _configService.WsusConnection.UseSsl },
                { "UpdateId", SelectedUpdate.Id.ToString() }
            };

            var results = await _retryService.ExecuteWithRetryAsync(
                async ct => await _psService.ExecuteScriptAsync("Decline-WsusUpdate.ps1", parameters),
                "DeclineUpdate");

            if (results.Count > 0)
            {
                var psResult = results[0];
                var success = ParseBool(psResult.Properties["Success"]?.Value);

                if (success)
                {
                    StatusText = Resources.StatusDeclined;
                    await _loggingService.LogInfoAsync($"Update {SelectedUpdate.Id} declined");
                    _dialogService.ShowToast(Resources.StatusDeclined);

                    _cacheService.Remove("updates");
                    await LoadUpdatesAsync();
                    ApplyFilters();
                }
            }
        }
        catch (Exception ex)
        {
            StatusText = string.Format(Resources.StatusError, ex.Message);
            await _loggingService.LogErrorAsync("Failed to decline update", ex);
            await _dialogService.ShowErrorAsync(Resources.DialogError, Resources.StatusError, ex.Message);
        }
        finally
        {
            IsDeclining = false;
        }
    }

    private bool CanApproveOrDecline() => IsConnected && !IsLoading && !IsApproving && !IsDeclining && !IsBulkOperationRunning && SelectedUpdate is not null;

    #endregion

    #region Bulk Operations Commands

    /// <summary>
    /// Command to approve multiple selected updates.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanBulkOperate))]
    private async Task BulkApproveAsync()
    {
        if (SelectedComputerGroup is null)
        {
            StatusText = Resources.ErrorNoGroupSelected;
            return;
        }

        if (!_authzService.IsAuthorized(WsusOperation.ApproveUpdate))
        {
            await _dialogService.ShowErrorAsync(Resources.DialogError, Resources.ErrorUnauthorized);
            return;
        }

        var updateIds = SelectedUpdates.Select(u => u.Id).ToList();
        if (updateIds.Count == 0)
        {
            StatusText = Resources.ErrorNoUpdateSelected;
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            Resources.DialogConfirm,
            string.Format(Resources.ConfirmBulkApprove, updateIds.Count, SelectedComputerGroup.Name));

        if (confirmed != DialogResult.Confirmed)
        {
            return;
        }

        IsBulkOperationRunning = true;
        BulkProgress = 0;
        BulkProgressText = Resources.StatusBulkOperation;

        try
        {
            var progress = new Progress<BulkOperationProgress>(p =>
            {
                BulkProgress = p.ProgressPercent;
                BulkProgressText = $"{p.CompletedCount}/{p.TotalCount} - {p.CurrentItem}";
            });

            var result = await _bulkOperationService.ApproveUpdatesAsync(
                updateIds,
                SelectedComputerGroup.Id,
                progress);

            StatusText = string.Format(Resources.StatusBulkComplete, result.SuccessCount, result.FailedCount);
            await _loggingService.LogInfoAsync($"Bulk approve completed: {result.SuccessCount} success, {result.FailedCount} failed");

            _cacheService.Remove("updates");
            await LoadUpdatesAsync();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            StatusText = string.Format(Resources.StatusError, ex.Message);
            await _loggingService.LogErrorAsync("Bulk approve failed", ex);
        }
        finally
        {
            IsBulkOperationRunning = false;
            BulkProgress = 0;
            BulkProgressText = string.Empty;
        }
    }

    /// <summary>
    /// Command to decline multiple selected updates.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanBulkOperate))]
    private async Task BulkDeclineAsync()
    {
        if (!_authzService.IsAuthorized(WsusOperation.DeclineUpdate))
        {
            await _dialogService.ShowErrorAsync(Resources.DialogError, Resources.ErrorUnauthorized);
            return;
        }

        var updateIds = SelectedUpdates.Select(u => u.Id).ToList();
        if (updateIds.Count == 0)
        {
            StatusText = Resources.ErrorNoUpdateSelected;
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            Resources.DialogConfirm,
            string.Format(Resources.ConfirmBulkDecline, updateIds.Count));

        if (confirmed != DialogResult.Confirmed)
        {
            return;
        }

        IsBulkOperationRunning = true;
        BulkProgress = 0;

        try
        {
            var progress = new Progress<BulkOperationProgress>(p =>
            {
                BulkProgress = p.ProgressPercent;
                BulkProgressText = $"{p.CompletedCount}/{p.TotalCount} - {p.CurrentItem}";
            });

            var result = await _bulkOperationService.DeclineUpdatesAsync(updateIds, progress);

            StatusText = string.Format(Resources.StatusBulkComplete, result.SuccessCount, result.FailedCount);

            _cacheService.Remove("updates");
            await LoadUpdatesAsync();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            StatusText = string.Format(Resources.StatusError, ex.Message);
            await _loggingService.LogErrorAsync("Bulk decline failed", ex);
        }
        finally
        {
            IsBulkOperationRunning = false;
            BulkProgress = 0;
            BulkProgressText = string.Empty;
        }
    }

    private bool CanBulkOperate() => IsConnected && !IsLoading && !IsBulkOperationRunning && SelectedUpdates.Count > 0;

    #endregion

    #region Sync Command

    /// <summary>
    /// Command to start WSUS synchronization.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStartSync))]
    private async Task StartSyncAsync()
    {
        if (!_authzService.IsAuthorized(WsusOperation.StartSync))
        {
            await _dialogService.ShowErrorAsync(Resources.DialogError, Resources.ErrorUnauthorized);
            return;
        }

        if (_configService.Config.Security.RequireSyncConfirmation)
        {
            var result = await _dialogService.ShowConfirmationAsync(
                Resources.DialogConfirm,
                Resources.ConfirmStartSync);

            if (result != DialogResult.Confirmed)
            {
                return;
            }
        }

        IsSyncing = true;
        StatusText = Resources.StatusSyncing;

        try
        {
            await _loggingService.LogInfoAsync("Starting WSUS synchronization");

            var parameters = new Dictionary<string, object>
            {
                { "ServerName", _configService.WsusConnection.ServerName },
                { "Port", _configService.WsusConnection.Port },
                { "UseSsl", _configService.WsusConnection.UseSsl }
            };

            var results = await _psService.ExecuteScriptAsync("Start-WsusSync.ps1", parameters);

            if (results.Count > 0)
            {
                var psResult = results[0];
                var success = ParseBool(psResult.Properties["Success"]?.Value);
                var message = psResult.Properties["Message"]?.Value?.ToString() ?? string.Empty;

                if (success)
                {
                    StatusText = Resources.StatusSyncStarted;
                    await _loggingService.LogInfoAsync("Synchronization started successfully");
                    _dialogService.ShowToast(Resources.StatusSyncStarted);
                }
                else
                {
                    StatusText = message;
                }

                await LoadSyncStatusAsync();
            }
        }
        catch (Exception ex)
        {
            StatusText = string.Format(Resources.StatusError, ex.Message);
            await _loggingService.LogErrorAsync("Failed to start synchronization", ex);
        }
        finally
        {
            IsSyncing = false;
        }
    }

    private bool CanStartSync() => IsConnected && !IsSyncing && !IsLoading && CanUserSync;

    #endregion

    #region Auto-Refresh Command

    /// <summary>
    /// Command to toggle auto-refresh functionality.
    /// </summary>
    [RelayCommand]
    private async Task ToggleAutoRefreshAsync()
    {
        IsAutoRefreshEnabled = !IsAutoRefreshEnabled;

        if (IsAutoRefreshEnabled)
        {
            _timerService.Start();
            StatusText = string.Format(Resources.StatusAutoRefreshOn, AutoRefreshIntervalSeconds);
            await _loggingService.LogInfoAsync($"Auto-refresh enabled with {AutoRefreshIntervalSeconds}s interval");
        }
        else
        {
            _timerService.Stop();
            StatusText = Resources.StatusAutoRefreshOff;
            await _loggingService.LogInfoAsync("Auto-refresh disabled");
        }

        // Save preference
        await SavePreferencesAsync();
    }

    #endregion

    #region Export Commands

    /// <summary>
    /// Command to export updates to file.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportUpdatesAsync()
    {
        if (!_authzService.IsAuthorized(WsusOperation.ExportData))
        {
            await _dialogService.ShowErrorAsync(Resources.DialogError, Resources.ErrorUnauthorized);
            return;
        }

        var filter = _exportService.GetFileFilter(ExportFormat.Csv);
        var filePath = await _dialogService.ShowSaveFileDialogAsync("wsus_updates", filter);

        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        IsExporting = true;
        StatusText = Resources.StatusExporting;

        try
        {
            var format = GetExportFormat(filePath);
            await _exportService.ExportUpdatesAsync(Updates, filePath, format);

            StatusText = Resources.StatusExportComplete;
            _dialogService.ShowToast(Resources.StatusExportComplete);
            await _loggingService.LogInfoAsync($"Exported {Updates.Count} updates to {filePath}");
        }
        catch (Exception ex)
        {
            StatusText = string.Format(Resources.StatusError, ex.Message);
            await _loggingService.LogErrorAsync("Export failed", ex);
            await _dialogService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
        finally
        {
            IsExporting = false;
        }
    }

    /// <summary>
    /// Command to export computers to file.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportComputersAsync()
    {
        if (!_authzService.IsAuthorized(WsusOperation.ExportData))
        {
            await _dialogService.ShowErrorAsync(Resources.DialogError, Resources.ErrorUnauthorized);
            return;
        }

        var filter = _exportService.GetFileFilter(ExportFormat.Csv);
        var filePath = await _dialogService.ShowSaveFileDialogAsync("wsus_computers", filter);

        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        IsExporting = true;
        StatusText = Resources.StatusExporting;

        try
        {
            var format = GetExportFormat(filePath);
            await _exportService.ExportComputersAsync(ComputerStatuses, filePath, format);

            StatusText = Resources.StatusExportComplete;
            _dialogService.ShowToast(Resources.StatusExportComplete);
        }
        catch (Exception ex)
        {
            StatusText = string.Format(Resources.StatusError, ex.Message);
            await _dialogService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
        finally
        {
            IsExporting = false;
        }
    }

    private bool CanExport() => IsConnected && !IsExporting && !IsLoading;

    private static ExportFormat GetExportFormat(string filePath)
    {
        var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".json" => ExportFormat.Json,
            ".tsv" => ExportFormat.Tsv,
            _ => ExportFormat.Csv
        };
    }

    #endregion

    #region Filter Commands

    /// <summary>
    /// Command to apply search filter.
    /// </summary>
    [RelayCommand]
    private void ApplySearch()
    {
        ApplyFilters();
    }

    /// <summary>
    /// Command to clear all filters.
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedClassification = string.Empty;
        SelectedApprovalFilter = "All";
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var criteria = new UpdateFilterCriteria
        {
            SearchText = SearchText,
            Classification = string.IsNullOrEmpty(SelectedClassification) ? null : SelectedClassification
        };

        // Apply approval filter
        criteria.IsApproved = SelectedApprovalFilter switch
        {
            "Approved" => true,
            "Unapproved" => false,
            _ => null
        };

        criteria.IsDeclined = SelectedApprovalFilter == "Declined" ? true : null;

        var filtered = _filterService.FilterUpdates(Updates, criteria);
        FilteredUpdates = new ObservableCollection<WsusUpdate>(filtered);

        // Update classifications list
        var distinctClassifications = _filterService.GetDistinctClassifications(Updates).ToList();
        Classifications = new ObservableCollection<string>(distinctClassifications);
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSelectedClassificationChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSelectedApprovalFilterChanged(string value)
    {
        ApplyFilters();
    }

    #endregion

    #region Computer Status Commands

    /// <summary>
    /// Command to load computer statuses.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task LoadComputerStatusesAsync()
    {
        IsLoading = true;
        StatusText = Resources.StatusLoadingComputers;

        try
        {
            await _loggingService.LogInfoAsync("Loading computer statuses");

            var parameters = new Dictionary<string, object>
            {
                { "ServerName", _configService.WsusConnection.ServerName },
                { "Port", _configService.WsusConnection.Port },
                { "UseSsl", _configService.WsusConnection.UseSsl }
            };

            if (SelectedComputerGroup is not null)
            {
                parameters.Add("GroupId", SelectedComputerGroup.Id.ToString());
            }

            var results = await _retryService.ExecuteWithRetryAsync(
                async ct => await _psService.ExecuteScriptAsync("Get-ComputerStatus.ps1", parameters),
                "LoadComputerStatuses");

            ComputerStatuses.Clear();

            foreach (var psObject in results)
            {
                var computerStatus = new ComputerStatus
                {
                    ComputerId = psObject.Properties["ComputerId"]?.Value?.ToString() ?? string.Empty,
                    Name = psObject.Properties["Name"]?.Value?.ToString() ?? string.Empty,
                    IpAddress = psObject.Properties["IpAddress"]?.Value?.ToString() ?? string.Empty,
                    LastReportedTime = ParseNullableDateTime(psObject.Properties["LastReportedTime"]?.Value),
                    InstalledCount = ParseInt(psObject.Properties["InstalledCount"]?.Value),
                    NeededCount = ParseInt(psObject.Properties["NeededCount"]?.Value),
                    FailedCount = ParseInt(psObject.Properties["FailedCount"]?.Value),
                    GroupName = psObject.Properties["GroupName"]?.Value?.ToString() ?? string.Empty
                };

                ComputerStatuses.Add(computerStatus);
            }

            FilteredComputerStatuses = new ObservableCollection<ComputerStatus>(ComputerStatuses);
            StatusText = string.Format(Resources.StatusComputersLoaded, ComputerStatuses.Count);
            await _loggingService.LogInfoAsync($"Loaded {ComputerStatuses.Count} computer statuses");
        }
        catch (Exception ex)
        {
            StatusText = string.Format(Resources.StatusError, ex.Message);
            await _loggingService.LogErrorAsync("Failed to load computer statuses", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Reports Commands

    /// <summary>
    /// Command to generate compliance report.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task GenerateComplianceReportAsync()
    {
        if (!_authzService.IsAuthorized(WsusOperation.ViewReports))
        {
            await _dialogService.ShowErrorAsync(Resources.DialogError, Resources.ErrorUnauthorized);
            return;
        }

        IsLoading = true;
        StatusText = Resources.StatusGeneratingReport;

        try
        {
            var options = new ReportOptions
            {
                GroupId = SelectedComputerGroup?.Id
            };

            ComplianceReport = await _reportService.GenerateComplianceReportAsync(options);
            StaleComputers = new ObservableCollection<StaleComputerInfo>(ComplianceReport.StaleComputers);
            CriticalUpdatesSummary = ComplianceReport.CriticalUpdates;

            StatusText = string.Format(Resources.ReportGenerated, ComplianceReport.CompliancePercent);
            await _loggingService.LogInfoAsync($"Compliance report generated: {ComplianceReport.CompliancePercent:F1}%");
        }
        catch (Exception ex)
        {
            StatusText = string.Format(Resources.StatusError, ex.Message);
            await _loggingService.LogErrorAsync("Failed to generate compliance report", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Command to export compliance report.
    /// </summary>
    [RelayCommand]
    private async Task ExportComplianceReportAsync()
    {
        if (ComplianceReport is null)
        {
            StatusText = Resources.ErrorNoReport;
            return;
        }

        var filter = _exportService.GetFileFilter(ExportFormat.Csv);
        var filePath = await _dialogService.ShowSaveFileDialogAsync("compliance_report", filter);

        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        try
        {
            var format = GetExportFormat(filePath);
            await _reportService.ExportReportAsync(ComplianceReport, filePath, format);
            _dialogService.ShowToast(Resources.StatusExportComplete);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
    }

    #endregion

    #region Group Management Commands

    /// <summary>
    /// Command to create a new computer group.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanManageGroups))]
    private async Task CreateGroupAsync()
    {
        if (!_authzService.IsAuthorized(WsusOperation.ManageGroups))
        {
            await _dialogService.ShowErrorAsync(Resources.DialogError, Resources.ErrorUnauthorized);
            return;
        }

        var sanitizedName = _validationService.Sanitize(NewGroupName);
        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            StatusText = Resources.ValidationRequired;
            return;
        }

        try
        {
            var options = new CreateGroupOptions
            {
                Name = sanitizedName,
                Description = _validationService.Sanitize(NewGroupDescription)
            };

            var newGroup = await _groupService.CreateGroupAsync(options);

            StatusText = string.Format(Resources.StatusGroupCreated, newGroup.Name);
            _dialogService.ShowToast(string.Format(Resources.StatusGroupCreated, newGroup.Name));

            NewGroupName = string.Empty;
            NewGroupDescription = string.Empty;

            await LoadComputerGroupsAsync();
        }
        catch (Exception ex)
        {
            StatusText = string.Format(Resources.StatusError, ex.Message);
            await _dialogService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
    }

    /// <summary>
    /// Command to delete the selected computer group.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteGroup))]
    private async Task DeleteGroupAsync()
    {
        if (SelectedGroupForEdit is null)
        {
            return;
        }

        if (!_authzService.IsAuthorized(WsusOperation.ManageGroups))
        {
            await _dialogService.ShowErrorAsync(Resources.DialogError, Resources.ErrorUnauthorized);
            return;
        }

        // Check for system groups
        if (SelectedGroupForEdit.Name == "All Computers" || SelectedGroupForEdit.Name == "Unassigned Computers")
        {
            await _dialogService.ShowErrorAsync(Resources.DialogError, Resources.ErrorCannotDeleteSystemGroup);
            return;
        }

        var confirmed = await _dialogService.ShowWarningAsync(
            Resources.DialogWarning,
            string.Format(Resources.ConfirmDeleteGroup, SelectedGroupForEdit.Name));

        if (!confirmed)
        {
            return;
        }

        try
        {
            await _groupService.DeleteGroupAsync(SelectedGroupForEdit.Id);

            StatusText = string.Format(Resources.StatusGroupDeleted, SelectedGroupForEdit.Name);
            _dialogService.ShowToast(string.Format(Resources.StatusGroupDeleted, SelectedGroupForEdit.Name));

            SelectedGroupForEdit = null;
            await LoadComputerGroupsAsync();
        }
        catch (Exception ex)
        {
            StatusText = string.Format(Resources.StatusError, ex.Message);
            await _dialogService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
    }

    private bool CanManageGroups() => IsConnected && !IsLoading && CanUserManageGroups;
    private bool CanDeleteGroup() => IsConnected && !IsLoading && CanUserManageGroups && SelectedGroupForEdit is not null;

    #endregion

    #region Update Details Command

    /// <summary>
    /// Command to view update details.
    /// </summary>
    [RelayCommand]
    private async Task ViewUpdateDetailsAsync()
    {
        if (SelectedUpdate is null)
        {
            return;
        }

        IsLoadingDetails = true;

        try
        {
            var parameters = new Dictionary<string, object>
            {
                { "ServerName", _configService.WsusConnection.ServerName },
                { "Port", _configService.WsusConnection.Port },
                { "UseSsl", _configService.WsusConnection.UseSsl },
                { "UpdateId", SelectedUpdate.Id.ToString() }
            };

            var results = await _psService.ExecuteScriptAsync("Get-UpdateDetails.ps1", parameters);

            if (results.Count > 0)
            {
                var psObject = results[0];
                SelectedUpdateDetails = new UpdateDetails
                {
                    Id = SelectedUpdate.Id,
                    Title = SelectedUpdate.Title,
                    Description = psObject.Properties["Description"]?.Value?.ToString() ?? string.Empty,
                    KbArticle = SelectedUpdate.KbArticle,
                    Classification = SelectedUpdate.Classification,
                    Severity = psObject.Properties["Severity"]?.Value?.ToString() ?? string.Empty,
                    CreationDate = SelectedUpdate.CreationDate,
                    ArrivalDate = ParseNullableDateTime(psObject.Properties["ArrivalDate"]?.Value),
                    IsApproved = SelectedUpdate.IsApproved,
                    IsDeclined = SelectedUpdate.IsDeclined,
                    IsSuperseded = ParseBool(psObject.Properties["IsSuperseded"]?.Value),
                    RequiresReboot = ParseBool(psObject.Properties["RequiresReboot"]?.Value),
                    ReleaseNotesUrl = psObject.Properties["ReleaseNotesUrl"]?.Value?.ToString(),
                    SupportUrl = psObject.Properties["SupportUrl"]?.Value?.ToString()
                };
            }
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync("Failed to load update details", ex);
        }
        finally
        {
            IsLoadingDetails = false;
        }
    }

    #endregion

    #region Health Command

    /// <summary>
    /// Command to check system health.
    /// </summary>
    [RelayCommand]
    private async Task CheckHealthAsync()
    {
        try
        {
            HealthReport = await _healthService.CheckHealthAsync();
            await _loggingService.LogInfoAsync($"Health check complete: {HealthReport.Status}");
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync("Health check failed", ex);
        }
    }

    #endregion

    #region Data Loading Methods

    /// <summary>
    /// Loads computer groups from the WSUS server.
    /// </summary>
    private async Task LoadComputerGroupsAsync()
    {
        try
        {
            StatusText = Resources.StatusLoadingGroups;
            await _loggingService.LogInfoAsync("Loading computer groups");

            var groups = await _cacheService.GetOrCreateAsync(
                "computer_groups",
                async () =>
                {
                    var parameters = new Dictionary<string, object>
                    {
                        { "ServerName", _configService.WsusConnection.ServerName },
                        { "Port", _configService.WsusConnection.Port },
                        { "UseSsl", _configService.WsusConnection.UseSsl }
                    };

                    var results = await _psService.ExecuteScriptAsync("Get-ComputerGroups.ps1", parameters);
                    var groupList = new List<ComputerGroup>();

                    foreach (var psObject in results)
                    {
                        var group = new ComputerGroup
                        {
                            Id = ParseGuid(psObject.Properties["Id"]?.Value),
                            Name = psObject.Properties["Name"]?.Value?.ToString() ?? string.Empty,
                            Description = psObject.Properties["Description"]?.Value?.ToString() ?? string.Empty,
                            ComputerCount = ParseInt(psObject.Properties["ComputerCount"]?.Value)
                        };

                        groupList.Add(group);
                    }

                    return groupList.AsReadOnly();
                },
                TimeSpan.FromMinutes(2));

            ComputerGroups.Clear();
            foreach (var group in groups)
            {
                ComputerGroups.Add(group);
            }

            await _loggingService.LogInfoAsync($"Loaded {ComputerGroups.Count} computer groups");
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync("Failed to load computer groups", ex);
        }
    }

    /// <summary>
    /// Loads synchronization status from the WSUS server.
    /// </summary>
    private async Task LoadSyncStatusAsync()
    {
        try
        {
            await _loggingService.LogDebugAsync("Loading sync status");

            var parameters = new Dictionary<string, object>
            {
                { "ServerName", _configService.WsusConnection.ServerName },
                { "Port", _configService.WsusConnection.Port },
                { "UseSsl", _configService.WsusConnection.UseSsl }
            };

            var results = await _psService.ExecuteScriptAsync("Get-SyncStatus.ps1", parameters);

            if (results.Count > 0)
            {
                var psObject = results[0];
                SyncStatus = new SyncStatus
                {
                    Status = psObject.Properties["Status"]?.Value?.ToString() ?? string.Empty,
                    LastSyncTime = ParseNullableDateTime(psObject.Properties["LastSyncTime"]?.Value),
                    NextSyncTime = ParseNullableDateTime(psObject.Properties["NextSyncTime"]?.Value),
                    IsSyncing = ParseBool(psObject.Properties["IsSyncing"]?.Value),
                    LastSyncResult = psObject.Properties["LastSyncResult"]?.Value?.ToString() ?? string.Empty
                };

                await _loggingService.LogDebugAsync($"Sync status: {SyncStatus.Status}, Last: {SyncStatus.LastSyncTime}");
            }
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync("Failed to load sync status", ex);
        }
    }

    /// <summary>
    /// Loads updates from the WSUS server.
    /// </summary>
    private async Task LoadUpdatesAsync()
    {
        IsLoading = true;
        StatusText = Resources.StatusLoading;

        try
        {
            var updates = await _cacheService.GetOrCreateAsync(
                "updates",
                async () =>
                {
                    var parameters = new Dictionary<string, object>
                    {
                        { "ServerName", _configService.WsusConnection.ServerName },
                        { "Port", _configService.WsusConnection.Port },
                        { "UseSsl", _configService.WsusConnection.UseSsl },
                        { "UpdateScope", "All" }
                    };

                    var results = await _psService.ExecuteScriptAsync("Get-WsusUpdates.ps1", parameters);
                    var updateList = new List<WsusUpdate>();

                    foreach (var psObject in results)
                    {
                        var update = new WsusUpdate
                        {
                            Id = ParseGuid(psObject.Properties["Id"]?.Value),
                            Title = psObject.Properties["Title"]?.Value?.ToString() ?? string.Empty,
                            KbArticle = psObject.Properties["KbArticle"]?.Value?.ToString() ?? string.Empty,
                            Classification = psObject.Properties["Classification"]?.Value?.ToString() ?? string.Empty,
                            CreationDate = ParseDateTime(psObject.Properties["CreationDate"]?.Value),
                            IsApproved = ParseBool(psObject.Properties["IsApproved"]?.Value),
                            IsDeclined = ParseBool(psObject.Properties["IsDeclined"]?.Value),
                            IsSuperseded = ParseBool(psObject.Properties["IsSuperseded"]?.Value),
                            Description = psObject.Properties["Description"]?.Value?.ToString()
                        };

                        updateList.Add(update);
                    }

                    return updateList.AsReadOnly();
                },
                TimeSpan.FromMinutes(2));

            Updates.Clear();
            foreach (var update in updates)
            {
                Updates.Add(update);
            }

            StatusText = string.Format(Resources.StatusUpdatesLoaded, Updates.Count);
        }
        catch (Exception ex)
        {
            StatusText = string.Format(Resources.StatusError, ex.Message);
            await _loggingService.LogErrorAsync("Failed to load updates", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Preferences

    private async Task SavePreferencesAsync()
    {
        try
        {
            var prefs = _preferencesService.Preferences;
            prefs.AutoRefreshEnabled = IsAutoRefreshEnabled;
            prefs.LastSelectedTabIndex = SelectedTabIndex;
            prefs.LastSelectedGroupId = SelectedComputerGroup?.Id;

            await _preferencesService.SaveAsync();
        }
        catch (Exception ex)
        {
            await _loggingService.LogWarningAsync($"Failed to save preferences: {ex.Message}");
        }
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        _ = SavePreferencesAsync();
    }

    #endregion

    #region Parse Helpers

    private static Guid ParseGuid(object? value)
    {
        if (value is null)
            return Guid.Empty;

        if (value is Guid guid)
            return guid;

        if (Guid.TryParse(value.ToString(), out var parsedGuid))
            return parsedGuid;

        return Guid.Empty;
    }

    private static DateTime ParseDateTime(object? value)
    {
        if (value is null)
            return DateTime.MinValue;

        if (value is DateTime dateTime)
            return dateTime;

        if (DateTime.TryParse(value.ToString(), out var parsedDateTime))
            return parsedDateTime;

        return DateTime.MinValue;
    }

    private static DateTime? ParseNullableDateTime(object? value)
    {
        if (value is null)
            return null;

        if (value is DateTime dateTime)
            return dateTime;

        if (DateTime.TryParse(value.ToString(), out var parsedDateTime))
            return parsedDateTime;

        return null;
    }

    private static bool ParseBool(object? value)
    {
        if (value is null)
            return false;

        if (value is bool boolValue)
            return boolValue;

        if (bool.TryParse(value.ToString(), out var parsedBool))
            return parsedBool;

        return false;
    }

    private static int ParseInt(object? value)
    {
        if (value is null)
            return 0;

        if (value is int intValue)
            return intValue;

        if (int.TryParse(value.ToString(), out var parsedInt))
            return parsedInt;

        return 0;
    }

    #endregion

    #region Dispose

    /// <summary>
    /// Disposes resources used by the view model.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        // Save preferences before disposing
        _ = SavePreferencesAsync();

        _timerService.Tick -= OnTimerTick;
        _healthService.HealthStatusChanged -= OnHealthStatusChanged;

        if (_timerService is IDisposable disposableTimer)
        {
            disposableTimer.Dispose();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    #endregion
}
