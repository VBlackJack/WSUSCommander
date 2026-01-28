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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using WsusCommander.Models;
using WsusCommander.Properties;
using WsusCommander.Services;

namespace WsusCommander.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IWsusService _wsusService;
    private readonly ILoggingService _loggingService;
    private readonly IComplianceHistoryService _complianceService;

    [ObservableProperty]
    private DashboardStats? _dashboardStats;

    [ObservableProperty]
    private List<ComputerGroup> _availableGroups = [];

    [ObservableProperty]
    private ComputerGroup? _selectedGroup;

    [ObservableProperty]
    private string _namePattern = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private DateTime? _lastRefresh;

    [ObservableProperty]
    private List<ComplianceSnapshot> _complianceHistory = new();

    [ObservableProperty]
    private List<ActionItem> _actionItems = new();

    [ObservableProperty]
    private HealthReport? _healthReport;

    [ObservableProperty]
    private SyncStatus? _syncStatus;

    [ObservableProperty]
    private string _dashboardLastSyncDisplay = Resources.StatusReady;

    [ObservableProperty]
    private string _dashboardSyncAgeDisplay = Resources.StatusReady;

    [ObservableProperty]
    private bool _dashboardSyncStale;

    [ObservableProperty]
    private string _dashboardHealthStatusDisplay = Resources.HealthStatusUnknown;

    [ObservableProperty]
    private string _dashboardAutoRefreshDisplay = Resources.StatusReady;

    [ObservableProperty]
    private string _dashboardUserRoleDisplay = Resources.StatusReady;

    [ObservableProperty]
    private string _dashboardAuthStatusDisplay = Resources.StatusReady;

    [ObservableProperty]
    private string _dashboardApprovalConfirmationDisplay = Resources.StatusReady;

    [ObservableProperty]
    private string _dashboardDeclineConfirmationDisplay = Resources.StatusReady;

    [ObservableProperty]
    private string _dashboardSyncConfirmationDisplay = Resources.StatusReady;

    [ObservableProperty]
    private string _dashboardAuditStatusDisplay = Resources.StatusReady;

    [ObservableProperty]
    private string _dashboardSslStatusDisplay = Resources.StatusReady;

    [ObservableProperty]
    private string _dashboardCertificateValidationDisplay = Resources.StatusReady;

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
    private bool _canUserApprove = true;

    [ObservableProperty]
    private bool _canUserDecline = true;

    [ObservableProperty]
    private ISeries[] _complianceTrendSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _complianceTrendXAxis = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _complianceTrendYAxis = Array.Empty<Axis>();

    [ObservableProperty]
    private ISeries[] _classificationSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _computerStatusSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _computerStatusXAxis = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _computerStatusYAxis = Array.Empty<Axis>();

    public DashboardViewModel(
        IWsusService wsusService,
        ILoggingService loggingService,
        IComplianceHistoryService complianceService)
    {
        _wsusService = wsusService;
        _loggingService = loggingService;
        _complianceService = complianceService;
    }

    [RelayCommand]
    private async Task LoadDashboardAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;

        try
        {
            // Preserve current filter values before refreshing groups
            var selectedGroupId = SelectedGroup?.Id;
            var pattern = string.IsNullOrWhiteSpace(NamePattern) ? null : NamePattern;

            // Load available groups for filtering
            var groups = await _wsusService.GetGroupsAsync(cancellationToken);
            AvailableGroups = [.. groups.OrderBy(g => g.Name)];

            // Restore selected group after collection refresh
            if (selectedGroupId.HasValue)
            {
                SelectedGroup = AvailableGroups.FirstOrDefault(g => g.Id == selectedGroupId.Value);
            }

            // Get stats with current filters
            var groupId = selectedGroupId?.ToString();
            DashboardStats = await _wsusService.GetDashboardStatsAsync(groupId, pattern, cancellationToken);
            HealthReport = await _wsusService.GetHealthReportAsync(cancellationToken);
            ComplianceHistory = await _complianceService.GetHistoryAsync(30, cancellationToken);

            UpdateActionItems();
            UpdateCharts();

            LastRefresh = DateTime.Now;
            DashboardLastSyncDisplay = DashboardStats?.LastSyncTime?.ToString("g") ?? Resources.StatusReady;
            DashboardHealthStatusDisplay = HealthReport?.Status switch
            {
                HealthStatus.Healthy => Resources.HealthStatusHealthy,
                HealthStatus.Degraded => Resources.HealthStatusDegraded,
                HealthStatus.Unhealthy => Resources.HealthStatusUnhealthy,
                _ => Resources.HealthStatusUnknown
            };

            await _complianceService.SaveSnapshotAsync(new ComplianceSnapshot
            {
                Timestamp = DateTime.Now,
                CompliancePercent = DashboardStats?.CompliancePercent ?? 0,
                TotalComputers = DashboardStats?.TotalComputers ?? 0,
                CompliantComputers = DashboardStats?.ComputersUpToDate ?? 0
            });
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync($"Dashboard refresh failed: {ex.Message}", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateActionItems()
    {
        ActionItems.Clear();

        if (DashboardStats is null)
        {
            HasDashboardActions = false;
            return;
        }

        HasCriticalAction = DashboardStats.CriticalPending > 0;
        HasSecurityAction = DashboardStats.SecurityPending > 0;
        HasSupersededAction = DashboardStats.SupersededUpdates > 0;
        HasSyncAction = DashboardStats.LastSyncTime is null;
        HasComplianceAction = DashboardStats.TotalComputers > 0;
        HasDashboardActions = HasCriticalAction || HasSecurityAction || HasSupersededAction || HasSyncAction || HasComplianceAction;

        if (HasCriticalAction)
        {
            ActionItems.Add(new ActionItem
            {
                Priority = ActionPriority.Critical,
                Title = Resources.DashboardCriticalPending,
                Description = Resources.DashboardActionRequired,
                ActionCommand = ReviewCriticalUpdatesCommand
            });
        }

        if (HasSecurityAction)
        {
            ActionItems.Add(new ActionItem
            {
                Priority = ActionPriority.High,
                Title = Resources.DashboardSecurityPending,
                Description = Resources.DashboardActionRequired,
                ActionCommand = ReviewSecurityUpdatesCommand
            });
        }

        if (DashboardStats.SupersededUpdates > 0)
        {
            ActionItems.Add(new ActionItem
            {
                Priority = ActionPriority.Medium,
                Title = Resources.DashboardSuperseded,
                Description = Resources.DashboardActionRequired,
                ActionCommand = ReviewSupersededUpdatesCommand
            });
        }

        if (HasComplianceAction)
        {
            ActionItems.Add(new ActionItem
            {
                Priority = ActionPriority.Medium,
                Title = Resources.DashboardComplianceReview,
                Description = Resources.DashboardComplianceAction,
                ActionCommand = NavigateToComputersCommand
            });
        }
    }

    private void UpdateCharts()
    {
        var history = ComplianceHistory
            .OrderBy(snapshot => snapshot.Timestamp)
            .ToList();

        ComplianceTrendSeries = history.Count == 0
            ? Array.Empty<ISeries>()
            : new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = history.Select(snapshot => snapshot.CompliancePercent).ToArray(),
                    Name = Resources.DashboardCompliance,
                    Fill = null
                }
            };

        ComplianceTrendXAxis = history.Count == 0
            ? Array.Empty<Axis>()
            : new[]
            {
                new Axis
                {
                    Labels = history.Select(snapshot => snapshot.Timestamp.ToString("MM-dd")).ToArray()
                }
            };

        ComplianceTrendYAxis =
        [
            new Axis
            {
                MinLimit = 0,
                MaxLimit = 100
            }
        ];

        var stats = DashboardStats;
        ClassificationSeries = stats is null
            ? Array.Empty<ISeries>()
            :
            [
                new PieSeries<int> { Values = new[] { stats.CriticalPending }, Name = Resources.DashboardCriticalPending },
                new PieSeries<int> { Values = new[] { stats.SecurityPending }, Name = Resources.DashboardSecurityPending },
                new PieSeries<int> { Values = new[] { stats.UnapprovedUpdates }, Name = Resources.DashboardUnapproved },
                new PieSeries<int> { Values = new[] { stats.SupersededUpdates }, Name = Resources.DashboardSuperseded }
            ];

        if (stats is null)
        {
            ComputerStatusSeries = Array.Empty<ISeries>();
            ComputerStatusXAxis = Array.Empty<Axis>();
            ComputerStatusYAxis = Array.Empty<Axis>();
            return;
        }

        var unknownCount = Math.Max(0, stats.TotalComputers - stats.ComputersUpToDate - stats.ComputersNeedingUpdates);
        ComputerStatusSeries =
        [
            new ColumnSeries<int> { Values = new[] { stats.ComputersUpToDate }, Name = Resources.DashboardUpToDate },
            new ColumnSeries<int> { Values = new[] { stats.ComputersNeedingUpdates }, Name = Resources.DashboardNeedingUpdates },
            new ColumnSeries<int> { Values = new[] { unknownCount }, Name = Resources.DashboardUnknown }
        ];

        ComputerStatusXAxis =
        [
            new Axis
            {
                Labels = [Resources.DashboardComputerStatus]
            }
        ];

        ComputerStatusYAxis =
        [
            new Axis
            {
                MinLimit = 0
            }
        ];
    }

    [RelayCommand]
    private void ReviewCriticalUpdates()
    {
        OnNavigateToUpdatesWithFilterRequested?.Invoke(this, "Critical");
    }

    [RelayCommand]
    private void ReviewSecurityUpdates()
    {
        OnNavigateToUpdatesWithFilterRequested?.Invoke(this, "Security");
    }

    [RelayCommand]
    private void ReviewSupersededUpdates()
    {
        OnNavigateToUpdatesWithFilterRequested?.Invoke(this, "Superseded");
    }

    [RelayCommand]
    private void NavigateToComputers()
    {
        OnNavigateToComputersRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task StartSyncAsync(CancellationToken cancellationToken)
    {
        SyncStatus = await _wsusService.StartSyncAsync(cancellationToken);
        OnStartSyncRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void OpenComplianceReports()
    {
        OnOpenReportsRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void DeclineAllSuperseded()
    {
        OnDeclineSupersededRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ApproveAllCritical()
    {
        OnApproveCriticalRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ApproveAllSecurity()
    {
        OnApproveSecurityRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? OnApproveCriticalRequested;
    public event EventHandler? OnApproveSecurityRequested;
    public event EventHandler? OnDeclineSupersededRequested;
    public event EventHandler<string>? OnNavigateToUpdatesWithFilterRequested;
    public event EventHandler? OnNavigateToComputersRequested;
    public event EventHandler? OnStartSyncRequested;
    public event EventHandler? OnOpenReportsRequested;
}
