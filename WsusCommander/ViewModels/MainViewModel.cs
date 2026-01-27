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
using System.Linq;
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
    private readonly IFilterPresetsService _filterPresetsService;
    private readonly IApprovalRulesService _approvalRulesService;
    private readonly IHealthService _healthService;
    private readonly IAccessibilityService _accessibilityService;
    private readonly IBulkOperationService _bulkOperationService;
    private readonly IGroupService _groupService;
    private readonly IReportService _reportService;
    private readonly IThemeService _themeService;

    #endregion

    #region Observable Properties

    private bool _disposed;

    [ObservableProperty]
    private string _statusText = Resources.StatusReady;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshUpdatesCommand))]
    [NotifyCanExecuteChangedFor(nameof(GenerateComplianceReportCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadComputerStatusesCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportUpdatesCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportComputersCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApproveUpdateCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeclineUpdateCommand))]
    [NotifyCanExecuteChangedFor(nameof(BulkApproveCommand))]
    [NotifyCanExecuteChangedFor(nameof(BulkDeclineCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartSyncCommand))]
    [NotifyCanExecuteChangedFor(nameof(CreateGroupCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteGroupCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeclineAllSupersededCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApproveAllCriticalCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApproveAllSecurityCommand))]
    private bool _isConnected;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectWsusCommand))]
    private bool _isConnecting;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshUpdatesCommand))]
    [NotifyCanExecuteChangedFor(nameof(GenerateComplianceReportCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadComputerStatusesCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportUpdatesCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportComputersCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApproveUpdateCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeclineUpdateCommand))]
    [NotifyCanExecuteChangedFor(nameof(BulkApproveCommand))]
    [NotifyCanExecuteChangedFor(nameof(BulkDeclineCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartSyncCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConnectWsusCommand))]
    [NotifyCanExecuteChangedFor(nameof(CreateGroupCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteGroupCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeclineAllSupersededCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApproveAllCriticalCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApproveAllSecurityCommand))]
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
    [NotifyCanExecuteChangedFor(nameof(ApproveAllCriticalCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApproveAllSecurityCommand))]
    private ComputerGroup? _selectedComputerGroup;

    [ObservableProperty]
    private SyncStatus? _syncStatus;

    [ObservableProperty]
    private ObservableCollection<ComputerStatus> _computerStatuses = [];

    [ObservableProperty]
    private ObservableCollection<ComputerStatus> _filteredComputerStatuses = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ViewComputerUpdatesCommand))]
    private ComputerStatus? _selectedComputer;

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
    private string _selectedSupersededFilter = Resources.FilterSupersededAll;

    [ObservableProperty]
    private ObservableCollection<string> _classifications = [];

    [ObservableProperty]
    private ObservableCollection<FilterPreset> _filterPresets = [];

    [ObservableProperty]
    private FilterPreset? _selectedFilterPreset;

    [ObservableProperty]
    private int _selectedTabIndex;

    // Pagination
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(FirstPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(LastPageCommand))]
    private int _currentPage = 1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(FirstPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(LastPageCommand))]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _pageSize = 50;

    [ObservableProperty]
    private int _totalFilteredCount;

    /// <summary>
    /// Gets the available page sizes.
    /// </summary>
    public ObservableCollection<int> PageSizes { get; } = [25, 50, 100, 200];

    /// <summary>
    /// Gets the pagination info text.
    /// </summary>
    public string PaginationInfo => TotalFilteredCount > 0
        ? string.Format(Resources.StatusPagination, CurrentPage, TotalPages, TotalFilteredCount)
        : string.Empty;

    [ObservableProperty]
    private HealthReport? _healthReport;

    [ObservableProperty]
    private ComplianceReport? _complianceReport;

    [ObservableProperty]
    private ObservableCollection<StaleComputerInfo> _staleComputers = [];

    [ObservableProperty]
    private CriticalUpdatesSummary? _criticalUpdatesSummary;

    [ObservableProperty]
    private DashboardStats? _dashboardStats;

    [ObservableProperty]
    private string _dashboardLastSyncDisplay = Resources.DashboardLastSyncNever;

    [ObservableProperty]
    private string _dashboardSyncAgeDisplay = Resources.DashboardSyncAgeUnknown;

    [ObservableProperty]
    private bool _dashboardSyncStale;

    [ObservableProperty]
    private string _dashboardHealthStatusDisplay = Resources.DashboardHealthUnknown;

    [ObservableProperty]
    private string _dashboardAutoRefreshDisplay = string.Empty;

    [ObservableProperty]
    private string _dashboardUserRoleDisplay = string.Empty;

    [ObservableProperty]
    private string _dashboardAuthStatusDisplay = string.Empty;

    [ObservableProperty]
    private string _dashboardApprovalConfirmationDisplay = string.Empty;

    [ObservableProperty]
    private string _dashboardDeclineConfirmationDisplay = string.Empty;

    [ObservableProperty]
    private string _dashboardSyncConfirmationDisplay = string.Empty;

    [ObservableProperty]
    private string _dashboardAuditStatusDisplay = string.Empty;

    [ObservableProperty]
    private string _dashboardSslStatusDisplay = string.Empty;

    [ObservableProperty]
    private string _dashboardCertificateValidationDisplay = string.Empty;

    [ObservableProperty]
    private bool _hasDashboardActions;

    [ObservableProperty]
    private bool _hasCriticalAction;

    [ObservableProperty]
    private bool _hasSecurityAction;

    [ObservableProperty]
    private bool _hasSupersededAction;

    [ObservableProperty]
    private bool _hasSyncAction;

    [ObservableProperty]
    private bool _hasComplianceAction;

    [ObservableProperty]
    private ObservableCollection<ActivityLogEntry> _activityLog = [];

    [ObservableProperty]
    private ObservableCollection<ApprovalRule> _approvalRules = [];

    [ObservableProperty]
    private ApprovalRule? _selectedRule;

    [ObservableProperty]
    private UpdateDetails? _selectedUpdateDetails;

    [ObservableProperty]
    private bool _isLoadingDetails;

    [ObservableProperty]
    private double _bulkProgress;

    [ObservableProperty]
    private string _bulkProgressText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshUpdatesCommand))]
    [NotifyCanExecuteChangedFor(nameof(GenerateComplianceReportCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadComputerStatusesCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApproveUpdateCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeclineUpdateCommand))]
    [NotifyCanExecuteChangedFor(nameof(BulkApproveCommand))]
    [NotifyCanExecuteChangedFor(nameof(BulkDeclineCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeclineAllSupersededCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApproveAllCriticalCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApproveAllSecurityCommand))]
    private bool _isBulkOperationRunning;

    /// <summary>
    /// Gets the collection of active toast notifications.
    /// </summary>
    public ObservableCollection<Models.ToastNotification> ToastNotifications { get; } = [];

    // Group management
    [ObservableProperty]
    private string _newGroupName = string.Empty;

    [ObservableProperty]
    private string _newGroupDescription = string.Empty;

    [ObservableProperty]
    private ComputerGroup? _selectedGroupForEdit;

    // Connection settings (editable)
    [ObservableProperty]
    private string _inputServerName = string.Empty;

    [ObservableProperty]
    private string _inputServerPort = string.Empty;

    [ObservableProperty]
    private bool _inputUseSsl;

    #endregion

    private const string PageSizePreferenceKey = "Updates.PageSize";

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
        IFilterPresetsService filterPresetsService,
        IApprovalRulesService approvalRulesService,
        IHealthService healthService,
        IAccessibilityService accessibilityService,
        IBulkOperationService bulkOperationService,
        IGroupService groupService,
        IReportService reportService,
        IThemeService themeService)
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
        _filterPresetsService = filterPresetsService;
        _approvalRulesService = approvalRulesService;
        _healthService = healthService;
        _accessibilityService = accessibilityService;
        _bulkOperationService = bulkOperationService;
        _groupService = groupService;
        _reportService = reportService;
        _themeService = themeService;

        // Configure timer
        _timerService.Interval = _configService.AppSettings.AutoRefreshInterval * 1000;
        _timerService.Tick += OnTimerTick;

        // Subscribe to health changes
        _healthService.HealthStatusChanged += OnHealthStatusChanged;

        // Subscribe to toast notifications
        _dialogService.ToastRequested += OnToastRequested;

        // Initialize filter options
        ApprovalFilters = ["All", "Approved", "Unapproved", "Declined"];
        SupersededFilters =
        [
            Resources.FilterSupersededAll,
            Resources.FilterSupersededOnly,
            Resources.FilterHideSuperseded
        ];
        _selectedSupersededFilter = Resources.FilterSupersededAll;

        // Apply configured default page size
        _pageSize = _configService.Config.UI.DefaultPageSize;
        EnsurePageSizeOption(_pageSize);

        // Initialize connection settings from config
        _inputServerName = _configService.WsusConnection.ServerName;
        _inputServerPort = _configService.WsusConnection.Port.ToString();
        _inputUseSsl = _configService.WsusConnection.UseSsl;

        UpdateSecuritySummary();
        UpdateAutoRefreshSummary();
        UpdateUserSummary();
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
    /// Gets the superseded filter options.
    /// </summary>
    public List<string> SupersededFilters { get; }

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

    /// <summary>
    /// Gets the current application theme.
    /// </summary>
    public AppTheme CurrentTheme => _themeService.CurrentTheme;

    /// <summary>
    /// Gets the current theme display name.
    /// </summary>
    public string CurrentThemeDisplayName => CurrentTheme switch
    {
        AppTheme.Dark => Resources.ThemeDark,
        AppTheme.System => Resources.ThemeSystem,
        _ => Resources.ThemeLight
    };

    /// <summary>
    /// Gets whether dark theme is currently active.
    /// </summary>
    public bool IsDarkTheme => CurrentTheme == AppTheme.Dark ||
        (CurrentTheme == AppTheme.System && _themeService.GetSystemTheme() == AppTheme.Dark);

    #endregion

    #region Theme Commands

    /// <summary>
    /// Command to toggle between light and dark theme.
    /// </summary>
    [RelayCommand]
    private void ToggleTheme()
    {
        var newTheme = CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
        _themeService.SetTheme(newTheme);
        OnPropertyChanged(nameof(CurrentTheme));
        OnPropertyChanged(nameof(CurrentThemeDisplayName));
        OnPropertyChanged(nameof(IsDarkTheme));
    }

    /// <summary>
    /// Command to set a specific theme.
    /// </summary>
    [RelayCommand]
    private void SetTheme(string themeName)
    {
        var theme = themeName?.ToLowerInvariant() switch
        {
            "dark" => AppTheme.Dark,
            "system" => AppTheme.System,
            _ => AppTheme.Light
        };

        _themeService.SetTheme(theme);
        OnPropertyChanged(nameof(CurrentTheme));
        OnPropertyChanged(nameof(CurrentThemeDisplayName));
        OnPropertyChanged(nameof(IsDarkTheme));
    }

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

            // Load filter presets
            await LoadFilterPresetsAsync();

            // Load approval rules
            await LoadApprovalRulesAsync();

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
            IsAutoRefreshEnabled = _preferencesService.HasSavedPreferences
                ? prefs.AutoRefreshEnabled
                : _configService.Config.UI.AutoRefreshDefault;
            SelectedTabIndex = prefs.LastSelectedTabIndex;
            PageSize = Math.Clamp(
                _preferencesService.Get(PageSizePreferenceKey, _configService.Config.UI.DefaultPageSize),
                10,
                500);
            EnsurePageSizeOption(PageSize);

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

    private async void OnToastRequested(object? sender, Models.ToastNotification notification)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ToastNotifications.Add(notification);
        });

        // Auto-dismiss after duration
        await Task.Delay(notification.Duration);

        Application.Current.Dispatcher.Invoke(() =>
        {
            ToastNotifications.Remove(notification);
        });
    }

    /// <summary>
    /// Command to manually dismiss a toast notification.
    /// </summary>
    [RelayCommand]
    private void DismissToast(Models.ToastNotification? notification)
    {
        if (notification is null)
            return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            ToastNotifications.Remove(notification);
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
        // Validate input
        var serverNameError = _validationService.ValidateServerName(InputServerName);
        if (serverNameError is not null)
        {
            await _dialogService.ShowErrorAsync(Resources.DialogError, Resources.ValidationInvalidHostname, serverNameError.Message);
            return;
        }

        if (!int.TryParse(InputServerPort, out var port) || port < 1 || port > 65535)
        {
            await _dialogService.ShowErrorAsync(Resources.DialogError, Resources.ValidationInvalidPort, string.Empty);
            return;
        }

        IsConnecting = true;
        StatusText = Resources.StatusReady;

        try
        {
            await _loggingService.LogInfoAsync($"Connecting to WSUS server: {InputServerName}:{port}");

            var parameters = new Dictionary<string, object>
            {
                { "ServerName", InputServerName },
                { "Port", port },
                { "UseSsl", InputUseSsl }
            };

            // Use retry service for connection
            var results = await _retryService.ExecuteWithRetryAsync(
                async ct => await _psService.ExecuteScriptAsync("Connect-WsusServer.ps1", parameters),
                "ConnectWsus");

            if (results.Count > 0)
            {
                var wsusInfo = results[0];
                var serverName = wsusInfo.Properties["Name"]?.Value?.ToString() ?? InputServerName;
                ServerVersion = wsusInfo.Properties["Version"]?.Value?.ToString() ?? string.Empty;

                // Update the configuration with the actual connection values
                _configService.WsusConnection.ServerName = InputServerName;
                _configService.WsusConnection.Port = port;
                _configService.WsusConnection.UseSsl = InputUseSsl;

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
            var details = $"{ex.Message}\n\nLog file: {_loggingService.CurrentLogFilePath}";
            await _dialogService.ShowErrorAsync(Resources.DialogError, Resources.ErrorConnectionFailed, details);
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private bool CanConnect() => !IsConnecting && !IsLoading && IsAuthenticated && !string.IsNullOrWhiteSpace(InputServerName);

    /// <summary>
    /// Command to disconnect from the WSUS server.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private async Task DisconnectAsync()
    {
        if (!IsConnected) return;

        var result = await _dialogService.ShowConfirmationAsync(
            Resources.DialogConfirm,
            Resources.ConfirmDisconnect);

        if (result != DialogResult.Confirmed)
        {
            return;
        }

        await _loggingService.LogInfoAsync("Disconnecting from WSUS server");

        // Stop auto-refresh timer
        if (IsAutoRefreshEnabled)
        {
            IsAutoRefreshEnabled = false;
        }

        // Clear all data
        Updates.Clear();
        FilteredUpdates.Clear();
        ComputerStatuses.Clear();
        ComputerGroups.Clear();
        DashboardStats = null;
        HealthReport = null;
        ActivityLog.Clear();

        IsConnected = false;
        ServerVersion = string.Empty;
        StatusText = Resources.StatusDisconnected;

        _cacheService.Clear();

        await _loggingService.LogInfoAsync("Disconnected from WSUS server");
    }

    private bool CanDisconnect() => IsConnected && !IsLoading && !IsSyncing && !IsBulkOperationRunning;

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
        await LoadComputerStatusesAsync();
        await LoadDashboardAsync();
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

    #region One-Click Action Commands

    /// <summary>
    /// Command to decline all superseded updates.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOneClickAction))]
    private async Task DeclineAllSupersededAsync()
    {
        if (!_authzService.IsAuthorized(WsusOperation.DeclineUpdate))
        {
            await _dialogService.ShowErrorAsync(Resources.DialogError, Resources.ErrorUnauthorized);
            return;
        }

        IsBulkOperationRunning = true;
        StatusText = Resources.StatusDecliningSuperseded;

        try
        {
            // First, get the count
            var countParams = new Dictionary<string, object>
            {
                { "ServerName", _configService.WsusConnection.ServerName },
                { "Port", _configService.WsusConnection.Port },
                { "UseSsl", _configService.WsusConnection.UseSsl },
                { "CountOnly", true }
            };

            var countResult = await _psService.ExecuteScriptAsync("Decline-SupersededUpdates.ps1", countParams);
            var count = 0;

            if (countResult.Count > 0)
            {
                count = ParseInt(countResult[0].Properties["Count"]?.Value);
            }

            if (count == 0)
            {
                StatusText = Resources.ErrorNoSupersededUpdates;
                _dialogService.ShowToast(Resources.ErrorNoSupersededUpdates);
                return;
            }

            // Confirm with user
            var confirmed = await _dialogService.ShowConfirmationAsync(
                Resources.DialogConfirm,
                string.Format(Resources.ConfirmDeclineSuperseded, count));

            if (confirmed != DialogResult.Confirmed)
            {
                StatusText = Resources.StatusReady;
                return;
            }

            // Execute the decline
            var parameters = new Dictionary<string, object>
            {
                { "ServerName", _configService.WsusConnection.ServerName },
                { "Port", _configService.WsusConnection.Port },
                { "UseSsl", _configService.WsusConnection.UseSsl }
            };

            var results = await _psService.ExecuteScriptAsync("Decline-SupersededUpdates.ps1", parameters);

            if (results.Count > 0)
            {
                var successCount = ParseInt(results[0].Properties["SuccessCount"]?.Value);
                var failedCount = ParseInt(results[0].Properties["FailedCount"]?.Value);

                StatusText = string.Format(Resources.StatusDeclinedSuperseded, successCount, failedCount);
                await _loggingService.LogInfoAsync($"Declined {successCount} superseded updates, {failedCount} failed");
                _dialogService.ShowSuccessToast(string.Format(Resources.StatusDeclinedSuperseded, successCount, failedCount));

                // Refresh updates
                _cacheService.Remove("updates");
                await LoadUpdatesAsync();
                ApplyFilters();
            }
        }
        catch (Exception ex)
        {
            StatusText = string.Format(Resources.StatusError, ex.Message);
            await _loggingService.LogErrorAsync("Failed to decline superseded updates", ex);
            await _dialogService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
        finally
        {
            IsBulkOperationRunning = false;
        }
    }

    /// <summary>
    /// Command to approve all unapproved critical updates.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOneClickApprove))]
    private async Task ApproveAllCriticalAsync()
    {
        await ApproveUpdatesByClassificationAsync("Critical");
    }

    /// <summary>
    /// Command to approve all unapproved security updates.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOneClickApprove))]
    private async Task ApproveAllSecurityAsync()
    {
        await ApproveUpdatesByClassificationAsync("Security");
    }

    /// <summary>
    /// Approves all unapproved updates of the specified classification.
    /// </summary>
    private async Task ApproveUpdatesByClassificationAsync(string classification)
    {
        if (!_authzService.IsAuthorized(WsusOperation.ApproveUpdate))
        {
            await _dialogService.ShowErrorAsync(Resources.DialogError, Resources.ErrorUnauthorized);
            return;
        }

        if (SelectedComputerGroup is null)
        {
            StatusText = Resources.ErrorNoGroupSelected;
            return;
        }

        IsBulkOperationRunning = true;
        StatusText = classification == "Critical" ? Resources.StatusApprovingCritical : Resources.StatusApprovingSecurity;

        try
        {
            // First, get the unapproved updates
            var getParams = new Dictionary<string, object>
            {
                { "ServerName", _configService.WsusConnection.ServerName },
                { "Port", _configService.WsusConnection.Port },
                { "UseSsl", _configService.WsusConnection.UseSsl },
                { "Classification", classification },
                { "CountOnly", true }
            };

            var countResult = await _psService.ExecuteScriptAsync("Get-UnapprovedUpdates.ps1", getParams);
            var count = 0;

            if (countResult.Count > 0)
            {
                count = ParseInt(countResult[0].Properties["Count"]?.Value);
            }

            if (count == 0)
            {
                var errorMsg = classification == "Critical" ? Resources.ErrorNoCriticalUpdates : Resources.ErrorNoSecurityUpdates;
                StatusText = errorMsg;
                _dialogService.ShowWarningToast(errorMsg);
                return;
            }

            // Confirm with user
            var confirmMsg = classification == "Critical"
                ? string.Format(Resources.ConfirmApproveCritical, count, SelectedComputerGroup.Name)
                : string.Format(Resources.ConfirmApproveSecurity, count, SelectedComputerGroup.Name);

            var confirmed = await _dialogService.ShowConfirmationAsync(Resources.DialogConfirm, confirmMsg);

            if (confirmed != DialogResult.Confirmed)
            {
                StatusText = Resources.StatusReady;
                return;
            }

            // Get the actual update IDs
            getParams["CountOnly"] = false;
            var updatesResult = await _psService.ExecuteScriptAsync("Get-UnapprovedUpdates.ps1", getParams);

            var updateIds = new List<string>();
            foreach (var psObject in updatesResult)
            {
                var id = psObject.Properties["Id"]?.Value?.ToString();
                if (!string.IsNullOrEmpty(id))
                {
                    updateIds.Add(id);
                }
            }

            if (updateIds.Count == 0)
            {
                var errorMsg = classification == "Critical" ? Resources.ErrorNoCriticalUpdates : Resources.ErrorNoSecurityUpdates;
                StatusText = errorMsg;
                return;
            }

            // Approve the updates
            var approveParams = new Dictionary<string, object>
            {
                { "ServerName", _configService.WsusConnection.ServerName },
                { "Port", _configService.WsusConnection.Port },
                { "UseSsl", _configService.WsusConnection.UseSsl },
                { "UpdateIds", updateIds.ToArray() },
                { "GroupId", SelectedComputerGroup.Id.ToString() }
            };

            var results = await _psService.ExecuteScriptAsync("Approve-Updates.ps1", approveParams);

            if (results.Count > 0)
            {
                var successCount = ParseInt(results[0].Properties["SuccessCount"]?.Value);
                var failedCount = ParseInt(results[0].Properties["FailedCount"]?.Value);

                var statusMsg = classification == "Critical"
                    ? string.Format(Resources.StatusApprovedCritical, successCount, failedCount)
                    : string.Format(Resources.StatusApprovedSecurity, successCount, failedCount);

                StatusText = statusMsg;
                await _loggingService.LogInfoAsync($"Approved {successCount} {classification.ToLower()} updates for {SelectedComputerGroup.Name}, {failedCount} failed");
                _dialogService.ShowSuccessToast(statusMsg);

                // Refresh updates
                _cacheService.Remove("updates");
                await LoadUpdatesAsync();
                ApplyFilters();
            }
        }
        catch (Exception ex)
        {
            StatusText = string.Format(Resources.StatusError, ex.Message);
            await _loggingService.LogErrorAsync($"Failed to approve {classification.ToLower()} updates", ex);
            await _dialogService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
        finally
        {
            IsBulkOperationRunning = false;
        }
    }

    private bool CanOneClickAction() => IsConnected && !IsLoading && !IsBulkOperationRunning;
    private bool CanOneClickApprove() => IsConnected && !IsLoading && !IsBulkOperationRunning && SelectedComputerGroup is not null;

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
                    _dialogService.ShowSuccessToast(Resources.StatusSyncStarted);
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

    #region Dashboard Insights

    private const double ComplianceWarningThreshold = 90.0;
    private static readonly TimeSpan SyncStaleThreshold = TimeSpan.FromHours(24);

    private void UpdateDashboardInsights(DashboardStats? stats)
    {
        UpdateSyncInsights(stats);
        UpdateHealthSummary();
        UpdateAutoRefreshSummary();
        UpdateSecuritySummary();
        UpdateUserSummary();
        UpdateActionFlags(stats);
    }

    private void UpdateSyncInsights(DashboardStats? stats)
    {
        if (stats?.LastSyncTime is DateTime lastSync)
        {
            DashboardLastSyncDisplay = lastSync.ToString("g");
            var age = DateTime.Now - lastSync;
            DashboardSyncAgeDisplay = FormatDuration(age);
            DashboardSyncStale = age > SyncStaleThreshold;
        }
        else
        {
            DashboardLastSyncDisplay = Resources.DashboardLastSyncNever;
            DashboardSyncAgeDisplay = Resources.DashboardSyncAgeUnknown;
            DashboardSyncStale = true;
        }
    }

    private void UpdateActionFlags(DashboardStats? stats)
    {
        if (stats == null)
        {
            HasCriticalAction = false;
            HasSecurityAction = false;
            HasSupersededAction = false;
            HasComplianceAction = false;
            HasSyncAction = false;
            HasDashboardActions = false;
            return;
        }

        HasCriticalAction = stats?.CriticalPending > 0;
        HasSecurityAction = stats?.SecurityPending > 0;
        HasSupersededAction = stats?.SupersededUpdates > 0;
        HasComplianceAction = stats.CompliancePercent < ComplianceWarningThreshold;
        HasSyncAction = DashboardSyncStale;

        HasDashboardActions = HasCriticalAction
            || HasSecurityAction
            || HasSupersededAction
            || HasComplianceAction
            || HasSyncAction;
    }

    private void UpdateHealthSummary()
    {
        DashboardHealthStatusDisplay = HealthReport?.Status switch
        {
            HealthStatus.Healthy => Resources.HealthStatusHealthy,
            HealthStatus.Degraded => Resources.HealthStatusDegraded,
            HealthStatus.Unhealthy => Resources.HealthStatusUnhealthy,
            _ => Resources.DashboardHealthUnknown
        };
    }

    private void UpdateAutoRefreshSummary()
    {
        if (IsAutoRefreshEnabled)
        {
            var minutes = Math.Max(1, (int)Math.Round(AutoRefreshIntervalSeconds / 60.0));
            DashboardAutoRefreshDisplay = string.Format(Resources.DashboardAutoRefreshEnabled, minutes);
            return;
        }

        DashboardAutoRefreshDisplay = Resources.DashboardAutoRefreshDisabled;
    }

    private void UpdateUserSummary()
    {
        if (CurrentUser == null)
        {
            DashboardUserRoleDisplay = Resources.DashboardUserUnknown;
            return;
        }

        var displayName = string.IsNullOrWhiteSpace(CurrentUser.DisplayName)
            ? CurrentUser.AccountName
            : CurrentUser.DisplayName;
        DashboardUserRoleDisplay = $"{displayName} ({CurrentUser.Role})";
    }

    private void UpdateSecuritySummary()
    {
        var security = _configService.Config.Security;
        DashboardAuthStatusDisplay = FormatEnabled(security.RequireAuthentication);
        DashboardApprovalConfirmationDisplay = FormatEnabled(security.RequireApprovalConfirmation);
        DashboardDeclineConfirmationDisplay = FormatEnabled(security.RequireDeclineConfirmation);
        DashboardSyncConfirmationDisplay = FormatEnabled(security.RequireSyncConfirmation);
        DashboardAuditStatusDisplay = FormatEnabled(security.AuditAllOperations);

        var connection = _configService.WsusConnection;
        DashboardSslStatusDisplay = FormatEnabled(connection.UseSsl);
        DashboardCertificateValidationDisplay = FormatEnabled(connection.ValidateCertificate);
    }

    private static string FormatEnabled(bool enabled)
    {
        return enabled ? Resources.StatusEnabled : Resources.StatusDisabled;
    }

    private static string FormatDuration(TimeSpan span)
    {
        if (span.TotalDays >= 1)
        {
            return $"{(int)span.TotalDays}d {span.Hours}h";
        }

        if (span.TotalHours >= 1)
        {
            return $"{(int)span.TotalHours}h {span.Minutes}m";
        }

        return $"{Math.Max(0, span.Minutes)}m";
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
        SelectedSupersededFilter = Resources.FilterSupersededAll;
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
        criteria.IsSuperseded = SelectedSupersededFilter == Resources.FilterSupersededOnly ? true : null;
        criteria.HideSuperseded = SelectedSupersededFilter == Resources.FilterHideSuperseded;

        var filtered = _filterService.FilterUpdates(Updates, criteria).ToList();

        // Update total count and pages
        TotalFilteredCount = filtered.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalFilteredCount / PageSize));

        // Ensure current page is valid (use backing field to avoid re-triggering ApplyFilters)
#pragma warning disable MVVMTK0034
        if (CurrentPage > TotalPages)
        {
            _currentPage = TotalPages;
            OnPropertyChanged(nameof(CurrentPage));
        }
#pragma warning restore MVVMTK0034

        // Apply pagination
        var paginatedUpdates = filtered
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        FilteredUpdates = new ObservableCollection<WsusUpdate>(paginatedUpdates);

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

    partial void OnSelectedSupersededFilterChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnDashboardStatsChanged(DashboardStats? value)
    {
        UpdateDashboardInsights(value);
    }

    partial void OnHealthReportChanged(HealthReport? value)
    {
        UpdateHealthSummary();
    }

    partial void OnCurrentUserChanged(UserIdentity? value)
    {
        UpdateUserSummary();
    }

    partial void OnIsAutoRefreshEnabledChanged(bool value)
    {
        UpdateAutoRefreshSummary();
    }

    partial void OnSelectedFilterPresetChanged(FilterPreset? value)
    {
        if (value != null)
        {
            ApplyFilterPreset(value);
        }
    }

    partial void OnCurrentPageChanged(int value)
    {
        ApplyFilters();
        OnPropertyChanged(nameof(PaginationInfo));
    }

    partial void OnTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(PaginationInfo));
    }

    partial void OnTotalFilteredCountChanged(int value)
    {
        OnPropertyChanged(nameof(PaginationInfo));
    }

    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 1;
        ApplyFilters();
    }

    private void ApplyFilterPreset(FilterPreset preset)
    {
        SearchText = preset.SearchText;
        SelectedClassification = preset.Classification;
        SelectedApprovalFilter = preset.ApprovalFilter;
        SelectedSupersededFilter = Resources.FilterSupersededAll;
        // ApplyFilters() is called automatically by the property changed handlers
    }

    #endregion

    #region Pagination Commands

    /// <summary>
    /// Command to go to the first page.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private void FirstPage()
    {
        CurrentPage = 1;
    }

    /// <summary>
    /// Command to go to the previous page.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
        }
    }

    /// <summary>
    /// Command to go to the next page.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private void NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
        }
    }

    /// <summary>
    /// Command to go to the last page.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private void LastPage()
    {
        CurrentPage = TotalPages;
    }

    private bool CanGoToPreviousPage() => CurrentPage > 1;
    private bool CanGoToNextPage() => CurrentPage < TotalPages;

    /// <summary>
    /// Loads filter presets.
    /// </summary>
    private async Task LoadFilterPresetsAsync()
    {
        try
        {
            await _filterPresetsService.LoadAsync();
            var presets = _filterPresetsService.GetPresets();
            FilterPresets = new ObservableCollection<FilterPreset>(presets);
            await _loggingService.LogDebugAsync($"Loaded {FilterPresets.Count} filter presets");
        }
        catch (Exception ex)
        {
            await _loggingService.LogWarningAsync($"Failed to load filter presets: {ex.Message}");
        }
    }

    /// <summary>
    /// Command to save the current filter as a preset.
    /// </summary>
    [RelayCommand]
    private async Task SaveFilterPresetAsync()
    {
        var presetName = await _dialogService.ShowInputDialogAsync(
            Resources.DialogSavePreset,
            Resources.LblPresetName);

        if (string.IsNullOrWhiteSpace(presetName))
        {
            return;
        }

        var preset = new FilterPreset
        {
            Name = presetName.Trim(),
            SearchText = SearchText,
            Classification = SelectedClassification,
            ApprovalFilter = SelectedApprovalFilter,
            IsBuiltIn = false
        };

        try
        {
            await _filterPresetsService.SavePresetAsync(preset);
            FilterPresets = new ObservableCollection<FilterPreset>(_filterPresetsService.GetPresets());
            StatusText = Resources.StatusPresetSaved;
            _dialogService.ShowToast(Resources.StatusPresetSaved);
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync("Failed to save filter preset", ex);
            await _dialogService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
    }

    /// <summary>
    /// Command to delete the selected filter preset.
    /// </summary>
    [RelayCommand]
    private async Task DeleteFilterPresetAsync()
    {
        if (SelectedFilterPreset == null)
        {
            return;
        }

        if (SelectedFilterPreset.IsBuiltIn)
        {
            await _dialogService.ShowErrorAsync(Resources.DialogError, Resources.ErrorCannotDeleteBuiltIn);
            return;
        }

        try
        {
            await _filterPresetsService.DeletePresetAsync(SelectedFilterPreset.Id);
            FilterPresets = new ObservableCollection<FilterPreset>(_filterPresetsService.GetPresets());
            SelectedFilterPreset = null;
            StatusText = Resources.StatusPresetDeleted;
            _dialogService.ShowToast(Resources.StatusPresetDeleted);
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync("Failed to delete filter preset", ex);
            await _dialogService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
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

    /// <summary>
    /// Gets whether a computer is selected.
    /// </summary>
    private bool CanViewComputerUpdates() => SelectedComputer is not null && IsConnected;

    /// <summary>
    /// Command to view updates for the selected computer.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanViewComputerUpdates))]
    private async Task ViewComputerUpdatesAsync()
    {
        if (SelectedComputer is null)
        {
            return;
        }

        await _loggingService.LogInfoAsync($"Viewing updates for computer: {SelectedComputer.Name}");
        StatusText = Resources.StatusLoading;
        IsLoading = true;

        try
        {
            var parameters = new Dictionary<string, object>
            {
                ["ServerName"] = _configService.WsusConnection.ServerName,
                ["Port"] = _configService.WsusConnection.Port,
                ["UseSsl"] = _configService.WsusConnection.UseSsl,
                ["ComputerId"] = SelectedComputer.ComputerId,
                ["StatusFilter"] = "Needed"
            };

            var result = await _psService.ExecuteScriptAsync("Get-ComputerUpdates.ps1", parameters);

            var updates = new List<ComputerUpdateStatus>();
            foreach (var item in result)
            {
                updates.Add(new ComputerUpdateStatus
                {
                    UpdateId = ParseGuid(item.Properties["UpdateId"]?.Value),
                    Title = item.Properties["Title"]?.Value?.ToString() ?? string.Empty,
                    KbArticle = item.Properties["KbArticle"]?.Value?.ToString() ?? string.Empty,
                    Classification = item.Properties["Classification"]?.Value?.ToString() ?? string.Empty,
                    InstallationStateDisplay = item.Properties["InstallationState"]?.Value?.ToString() ?? string.Empty,
                    ApprovalStatusDisplay = item.Properties["ApprovalStatus"]?.Value?.ToString() ?? string.Empty,
                    IsSuperseded = ParseBool(item.Properties["IsSuperseded"]?.Value),
                    SupersededBy = item.Properties["SupersededBy"]?.Value?.ToString() ?? string.Empty,
                    Severity = item.Properties["Severity"]?.Value?.ToString() ?? string.Empty
                });
            }

            await _loggingService.LogInfoAsync($"Loaded {updates.Count} updates for {SelectedComputer.Name}");
            StatusText = string.Format(Resources.StatusUpdatesLoaded, updates.Count);

            // Open the dialog with the updates
            await ShowComputerUpdatesDialogAsync(SelectedComputer, updates);
        }
        catch (Exception ex)
        {
            StatusText = string.Format(Resources.StatusError, ex.Message);
            await _loggingService.LogErrorAsync($"Failed to load updates for computer {SelectedComputer.Name}", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Shows the computer updates dialog.
    /// </summary>
    private Task ShowComputerUpdatesDialogAsync(ComputerStatus computer, List<ComputerUpdateStatus> updates)
    {
        var dialog = new Views.ComputerUpdatesWindow(computer, updates)
        {
            Owner = Application.Current.MainWindow
        };
        dialog.ShowDialog();
        return Task.CompletedTask;
    }

    #endregion

    #region Reports Commands

    /// <summary>
    /// Command to generate compliance report.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task GenerateComplianceReportAsync()
    {
        await _loggingService.LogInfoAsync("GenerateComplianceReportAsync called");

        if (!_authzService.IsAuthorized(WsusOperation.ViewReports))
        {
            await _loggingService.LogWarningAsync("User not authorized for ViewReports");
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

            var report = await _reportService.GenerateComplianceReportAsync(options);
            await _loggingService.LogDebugAsync($"Report received: TotalComputers={report.TotalComputers}, Compliant={report.CompliantComputers}, Percent={report.CompliancePercent}");

            // Fetch stale computers separately
            var staleComputersList = await _reportService.GetStaleComputersAsync(options.StaleDays);
            report.StaleComputers = staleComputersList.ToList();
            await _loggingService.LogDebugAsync($"Stale computers loaded: {staleComputersList.Count}");

            ComplianceReport = report;
            await _loggingService.LogDebugAsync($"ComplianceReport property set, IsNull={ComplianceReport is null}");

            StaleComputers = new ObservableCollection<StaleComputerInfo>(report.StaleComputers);
            CriticalUpdatesSummary = report.CriticalUpdates;

            StatusText = string.Format(Resources.ReportGenerated, report.CompliancePercent);
            await _loggingService.LogInfoAsync($"Compliance report generated: {report.CompliancePercent:F1}%");
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

    /// <summary>
    /// Command to copy KB article number to clipboard.
    /// </summary>
    [RelayCommand]
    private void CopyKbArticle()
    {
        if (SelectedUpdate is null || string.IsNullOrEmpty(SelectedUpdate.KbArticle))
        {
            return;
        }

        try
        {
            System.Windows.Clipboard.SetText(SelectedUpdate.KbArticle);
            _dialogService.ShowSuccessToast(string.Format(Resources.StatusCopied, SelectedUpdate.KbArticle));
        }
        catch (Exception ex)
        {
            _loggingService.LogErrorAsync("Failed to copy to clipboard", ex).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Command to search KB article online.
    /// </summary>
    [RelayCommand]
    private void SearchKbOnline()
    {
        if (SelectedUpdate is null || string.IsNullOrEmpty(SelectedUpdate.KbArticle))
        {
            return;
        }

        try
        {
            var searchUrl = $"https://support.microsoft.com/search/results?query={SelectedUpdate.KbArticle}";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = searchUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _loggingService.LogErrorAsync("Failed to open browser", ex).ConfigureAwait(false);
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

    #region Approval Rules Commands

    /// <summary>
    /// Loads approval rules.
    /// </summary>
    private async Task LoadApprovalRulesAsync()
    {
        try
        {
            await _approvalRulesService.LoadAsync();
            var rules = _approvalRulesService.GetRules();
            ApprovalRules = new ObservableCollection<ApprovalRule>(rules);
            await _loggingService.LogDebugAsync($"Loaded {ApprovalRules.Count} approval rules");
        }
        catch (Exception ex)
        {
            await _loggingService.LogWarningAsync($"Failed to load approval rules: {ex.Message}");
        }
    }

    /// <summary>
    /// Command to delete the selected rule.
    /// </summary>
    [RelayCommand]
    private async Task DeleteRuleAsync()
    {
        if (SelectedRule == null)
        {
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            Resources.DialogConfirm,
            string.Format(Resources.ConfirmDeleteRule, SelectedRule.Name));

        if (confirmed != DialogResult.Confirmed)
        {
            return;
        }

        try
        {
            await _approvalRulesService.DeleteRuleAsync(SelectedRule.Id);
            ApprovalRules = new ObservableCollection<ApprovalRule>(_approvalRulesService.GetRules());
            SelectedRule = null;
            StatusText = Resources.StatusRuleDeleted;
            _dialogService.ShowToast(Resources.StatusRuleDeleted);
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync("Failed to delete approval rule", ex);
            await _dialogService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
    }

    /// <summary>
    /// Command to toggle a rule's enabled state.
    /// </summary>
    [RelayCommand]
    private async Task ToggleRuleEnabledAsync(ApprovalRule rule)
    {
        if (rule == null)
        {
            return;
        }

        rule.IsEnabled = !rule.IsEnabled;

        try
        {
            await _approvalRulesService.SaveRuleAsync(rule);
            ApprovalRules = new ObservableCollection<ApprovalRule>(_approvalRulesService.GetRules());
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync("Failed to toggle rule state", ex);
        }
    }

    #endregion

    #region Activity Monitor Command

    /// <summary>
    /// Command to load the activity log.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task LoadActivityLogAsync()
    {
        StatusText = Resources.StatusLoadingActivity;

        try
        {
            await _loggingService.LogInfoAsync("Loading activity log");

            var parameters = new Dictionary<string, object>
            {
                { "ServerName", _configService.WsusConnection.ServerName },
                { "Port", _configService.WsusConnection.Port },
                { "UseSsl", _configService.WsusConnection.UseSsl },
                { "MaxEntries", 100 }
            };

            var results = await _psService.ExecuteScriptAsync("Get-ActivityLog.ps1", parameters);

            var activities = new List<ActivityLogEntry>();
            foreach (var psObject in results)
            {
                activities.Add(new ActivityLogEntry
                {
                    Timestamp = ParseDateTime(psObject.Properties["Timestamp"]?.Value),
                    ActivityType = psObject.Properties["ActivityType"]?.Value?.ToString() ?? string.Empty,
                    Description = psObject.Properties["Description"]?.Value?.ToString() ?? string.Empty,
                    User = psObject.Properties["User"]?.Value?.ToString() ?? string.Empty,
                    Target = psObject.Properties["Target"]?.Value?.ToString() ?? string.Empty,
                    Status = psObject.Properties["Status"]?.Value?.ToString() ?? string.Empty
                });
            }

            ActivityLog = new ObservableCollection<ActivityLogEntry>(activities);
            await _loggingService.LogInfoAsync($"Loaded {ActivityLog.Count} activity log entries");
            StatusText = string.Format(Resources.StatusActivityLoaded, ActivityLog.Count);
        }
        catch (Exception ex)
        {
            StatusText = string.Format(Resources.StatusError, ex.Message);
            await _loggingService.LogErrorAsync("Failed to load activity log", ex);
        }
    }

    #endregion

    #region Dashboard Command

    /// <summary>
    /// Command to load dashboard statistics.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task LoadDashboardAsync()
    {
        StatusText = Resources.StatusLoading;

        try
        {
            await _loggingService.LogInfoAsync("Loading dashboard statistics");

            var parameters = new Dictionary<string, object>
            {
                { "ServerName", _configService.WsusConnection.ServerName },
                { "Port", _configService.WsusConnection.Port },
                { "UseSsl", _configService.WsusConnection.UseSsl }
            };

            var results = await _psService.ExecuteScriptAsync("Get-DashboardStats.ps1", parameters);

            if (results.Count > 0)
            {
                var psObject = results[0];
                DashboardStats = new DashboardStats
                {
                    TotalUpdates = ParseInt(psObject.Properties["TotalUpdates"]?.Value),
                    UnapprovedUpdates = ParseInt(psObject.Properties["UnapprovedUpdates"]?.Value),
                    SupersededUpdates = ParseInt(psObject.Properties["SupersededUpdates"]?.Value),
                    CriticalPending = ParseInt(psObject.Properties["CriticalPending"]?.Value),
                    SecurityPending = ParseInt(psObject.Properties["SecurityPending"]?.Value),
                    TotalComputers = ParseInt(psObject.Properties["TotalComputers"]?.Value),
                    ComputersNeedingUpdates = ParseInt(psObject.Properties["ComputersNeedingUpdates"]?.Value),
                    ComputersUpToDate = ParseInt(psObject.Properties["ComputersUpToDate"]?.Value),
                    CompliancePercent = ParseDouble(psObject.Properties["CompliancePercent"]?.Value),
                    LastSyncTime = ParseNullableDateTime(psObject.Properties["LastSyncTime"]?.Value)
                };

                await _loggingService.LogInfoAsync($"Dashboard loaded: {DashboardStats.TotalUpdates} updates, {DashboardStats.TotalComputers} computers");
                StatusText = Resources.StatusReady;
            }
        }
        catch (Exception ex)
        {
            StatusText = string.Format(Resources.StatusError, ex.Message);
            await _loggingService.LogErrorAsync("Failed to load dashboard statistics", ex);
        }
    }

    [RelayCommand]
    private void ReviewCriticalUpdates()
    {
        NavigateToUpdatesWithFilters("Critical Updates", "Unapproved", Resources.FilterSupersededAll);
    }

    [RelayCommand]
    private void ReviewSecurityUpdates()
    {
        NavigateToUpdatesWithFilters("Security Updates", "Unapproved", Resources.FilterSupersededAll);
    }

    [RelayCommand]
    private void ReviewSupersededUpdates()
    {
        NavigateToUpdatesWithFilters(string.Empty, "All", Resources.FilterSupersededOnly);
    }

    [RelayCommand]
    private void OpenComplianceReports()
    {
        SelectedTabIndex = 3;
    }

    private void NavigateToUpdatesWithFilters(string classification, string approvalFilter, string supersededFilter)
    {
        SelectedTabIndex = 1;
        SearchText = string.Empty;
        SelectedClassification = classification;
        SelectedApprovalFilter = approvalFilter;
        SelectedSupersededFilter = supersededFilter;
        CurrentPage = 1;
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
            _preferencesService.Set(PageSizePreferenceKey, PageSize);

            await _preferencesService.SaveAsync();
        }
        catch (Exception ex)
        {
            await _loggingService.LogWarningAsync($"Failed to save preferences: {ex.Message}");
        }
    }

    private void EnsurePageSizeOption(int pageSize)
    {
        if (!PageSizes.Contains(pageSize))
        {
            PageSizes.Add(pageSize);
            var ordered = PageSizes.OrderBy(size => size).ToList();
            PageSizes.Clear();
            foreach (var size in ordered)
            {
                PageSizes.Add(size);
            }
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

        var str = value.ToString();
        if (string.IsNullOrEmpty(str))
            return DateTime.MinValue;

        // Handle Microsoft JSON date format: /Date(milliseconds)/
        if (str.StartsWith("/Date(") && str.EndsWith(")/"))
        {
            var ticksStr = str[6..^2]; // Remove "/Date(" and ")/"
            if (long.TryParse(ticksStr, out var milliseconds))
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).LocalDateTime;
            }
        }

        if (DateTime.TryParse(str, out var parsedDateTime))
            return parsedDateTime;

        return DateTime.MinValue;
    }

    private static DateTime? ParseNullableDateTime(object? value)
    {
        if (value is null)
            return null;

        if (value is DateTime dateTime)
            return dateTime;

        var str = value.ToString();
        if (string.IsNullOrEmpty(str))
            return null;

        // Handle Microsoft JSON date format: /Date(milliseconds)/
        if (str.StartsWith("/Date(") && str.EndsWith(")/"))
        {
            var ticksStr = str[6..^2]; // Remove "/Date(" and ")/"
            if (long.TryParse(ticksStr, out var milliseconds))
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).LocalDateTime;
            }
        }

        if (DateTime.TryParse(str, out var parsedDateTime))
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

        // Handle long values from JSON parsing
        if (value is long longValue)
            return (int)longValue;

        if (int.TryParse(value.ToString(), out var parsedInt))
            return parsedInt;

        return 0;
    }

    private static double ParseDouble(object? value)
    {
        if (value is null)
            return 0.0;

        if (value is double doubleValue)
            return doubleValue;

        if (value is float floatValue)
            return floatValue;

        if (value is decimal decimalValue)
            return (double)decimalValue;

        if (double.TryParse(value.ToString(), out var parsedDouble))
            return parsedDouble;

        return 0.0;
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
        _dialogService.ToastRequested -= OnToastRequested;

        if (_timerService is IDisposable disposableTimer)
        {
            disposableTimer.Dispose();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    #endregion
}
