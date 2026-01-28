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

using System.Windows;
using WsusCommander.Interfaces;
using WsusCommander.Services;
using WsusCommander.ViewModels;
using Res = WsusCommander.Properties.Resources;

namespace WsusCommander;

/// <summary>
/// Application entry point and composition root for dependency injection.
/// </summary>
public partial class App : Application
{
    private MainViewModel? _mainViewModel;
    private readonly List<IDisposable> _disposables = [];

    /// <summary>
    /// Gets the preferences service for window state persistence.
    /// </summary>
    public IPreferencesService? PreferencesService { get; private set; }

    /// <summary>
    /// Gets the configuration service for app defaults.
    /// </summary>
    public IConfigurationService? ConfigService { get; private set; }

    /// <summary>
    /// Called when the application starts. Sets up dependency injection manually.
    /// </summary>
    /// <param name="e">Startup event arguments.</param>
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Composition Root - Manual DI
        IConfigurationService configService = new ConfigurationService();
        ConfigService = configService;
        ILoggingService loggingService = new LoggingService(configService);
        IDialogService dialogService = new DialogService();
        INotificationService notificationService = new NotificationService(dialogService);
        IFileDialogService fileDialogService = new FileDialogService();
        IExportService exportService = new ExportService(loggingService);
        IPreferencesService preferencesService = new PreferencesService(configService, loggingService);
        PreferencesService = preferencesService;
        IApprovalRulesService approvalRulesService = new ApprovalRulesService(configService, loggingService);
        IComplianceHistoryService complianceHistoryService = new ComplianceHistoryService();
        IWsusService wsusService = new WsusService();

        ICacheService cacheService = new CacheService(configService);
        IValidationService validationService = new ValidationService();
        IRetryService retryService = new RetryService(configService, loggingService);
        IPowerShellService psService = new PowerShellService(loggingService, configService);
        IGroupService groupService = new GroupService(
            psService, loggingService, cacheService, validationService, configService, retryService);
        IReportService reportService = new ReportService(
            psService, loggingService, cacheService, configService);
        ICleanupService cleanupService = new CleanupService(
            psService, loggingService, configService);
        IEmailService emailService = new EmailService(configService, loggingService);
        ISchedulerService schedulerService = new SchedulerService(loggingService);
        _ = cleanupService;
        _ = emailService;

        await preferencesService.LoadAsync();

        // Track disposables
        _disposables.Add((IDisposable)cacheService);
        _disposables.Add((IDisposable)schedulerService);

        var connectionViewModel = new ConnectionViewModel(
            wsusService,
            loggingService,
            configService,
            notificationService);
        var dashboardViewModel = new DashboardViewModel(
            wsusService,
            loggingService,
            complianceHistoryService);
        var updatesViewModel = new UpdatesViewModel(
            wsusService,
            loggingService,
            notificationService,
            exportService,
            fileDialogService);
        var computersViewModel = new ComputersViewModel(
            wsusService,
            loggingService,
            notificationService,
            exportService,
            fileDialogService);
        var groupsViewModel = new GroupsViewModel(
            groupService,
            loggingService,
            notificationService);
        var reportsViewModel = new ReportsViewModel(
            reportService,
            fileDialogService);
        var rulesViewModel = new RulesViewModel(approvalRulesService);
        var activityViewModel = new ActivityViewModel(loggingService);
        var settingsViewModel = new SettingsViewModel(configService, preferencesService);
        IWindowService windowService = new WindowService(settingsViewModel);
        _ = windowService;

        _mainViewModel = new MainViewModel(
            connectionViewModel,
            dashboardViewModel,
            updatesViewModel,
            computersViewModel,
            groupsViewModel,
            reportsViewModel,
            rulesViewModel,
            activityViewModel,
            settingsViewModel);

        var mainWindow = new MainWindow
        {
            DataContext = _mainViewModel
        };
        MainWindow = mainWindow;

        await PromptForAuthenticationSetupAsync(preferencesService, dialogService);

        mainWindow.Show();
    }

    /// <summary>
    /// Called when the application exits. Disposes resources.
    /// </summary>
    /// <param name="e">Exit event arguments.</param>
    protected override void OnExit(ExitEventArgs e)
    {
        _mainViewModel?.Dispose();

        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        base.OnExit(e);
    }

    private static async Task PromptForAuthenticationSetupAsync(
        IPreferencesService preferencesService,
        IDialogService dialogService)
    {
        if (preferencesService.Preferences.HasConfiguredAuthentication)
        {
            return;
        }

        var result = await dialogService.ShowConfirmationAsync(
            Res.DialogAuthSetupTitle,
            Res.DialogAuthSetupMessage,
            Res.BtnOk,
            Res.BtnLater);

        preferencesService.Preferences.HasConfiguredAuthentication = true;
        await preferencesService.SaveAsync();

        if (result == DialogResult.Confirmed)
        {
            await dialogService.ShowInfoAsync(
                Res.DialogAuthSetupTitle,
                Res.DialogAuthSetupFollowup);
        }
    }
}
