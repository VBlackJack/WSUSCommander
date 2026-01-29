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
using System.Windows.Threading;
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
    private ILoggingService? _loggingService;

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
        _loggingService = loggingService;
        IDialogService dialogService = new DialogService();
        INotificationService notificationService = new NotificationService(dialogService);
        IFileDialogService fileDialogService = new FileDialogService();
        IExportService exportService = new ExportService(loggingService);
        IPreferencesService preferencesService = new PreferencesService(configService, loggingService);
        PreferencesService = preferencesService;
        IApprovalRulesService approvalRulesService = new ApprovalRulesService(configService, loggingService);
        IComplianceHistoryService complianceHistoryService = new ComplianceHistoryService();

        ICacheService cacheService = new CacheService(configService);
        IValidationService validationService = new ValidationService();
        IRetryService retryService = new RetryService(configService, loggingService);
        IPowerShellService psService = new PowerShellService(loggingService, configService);

        IWsusService wsusService = new WsusService(psService, configService, loggingService);

        IBulkOperationService bulkOperationService = new BulkOperationService(psService, loggingService, configService);
        IGroupService groupService = new GroupService(
            psService, loggingService, cacheService, validationService, configService, retryService);
        IReportService reportService = new ReportService(
            psService, loggingService, cacheService, configService);
        ICleanupService cleanupService = new CleanupService(
            psService, loggingService, configService);
        IComputerActionService computerActionService = new ComputerActionService(psService, configService);
        ISmtpClientFactory smtpClientFactory = new SmtpClientFactory();
        IEmailService emailService = new EmailService(configService, loggingService, smtpClientFactory);
        ISchedulerService schedulerService = new SchedulerService(loggingService);
        ISettingsBackupService settingsBackupService = new SettingsBackupService(configService, loggingService);

        ITaskSchedulerService taskSchedulerService = new TaskSchedulerService(psService, loggingService, configService);
        IScheduledTasksService scheduledTasksService = new ScheduledTasksService(configService, loggingService, taskSchedulerService);

        await preferencesService.LoadAsync();
        await scheduledTasksService.LoadAsync();

        // Track disposables
        _disposables.Add((IDisposable)cacheService);
        _disposables.Add((IDisposable)schedulerService);

        IFilterPresetsService filterPresetsService = new FilterPresetsService(configService, loggingService);
        await filterPresetsService.LoadAsync();

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
            fileDialogService,
            filterPresetsService,
            dialogService);
        var computersViewModel = new ComputersViewModel(
            wsusService,
            loggingService,
            notificationService,
            exportService,
            fileDialogService,
            bulkOperationService,
            groupService,
            dialogService,
            computerActionService);
        var stagingViewModel = new StagingViewModel(
            wsusService,
            loggingService,
            notificationService,
            bulkOperationService,
            groupService,
            dialogService,
            computerActionService,
            configService);
        var groupsViewModel = new GroupsViewModel(
            groupService,
            loggingService,
            notificationService);
        var reportsViewModel = new ReportsViewModel(
            reportService,
            fileDialogService,
            wsusService,
            loggingService);
        var rulesViewModel = new RulesViewModel(approvalRulesService, fileDialogService, notificationService);
        var activityViewModel = new ActivityViewModel(loggingService);
        var settingsViewModel = new SettingsViewModel(
            configService,
            preferencesService,
            fileDialogService,
            notificationService,
            settingsBackupService);
        IWindowService windowService = new WindowService(
            settingsViewModel,
            cleanupService,
            scheduledTasksService,
            taskSchedulerService,
            groupService,
            dialogService,
            notificationService,
            loggingService);

        _mainViewModel = new MainViewModel(
            connectionViewModel,
            dashboardViewModel,
            updatesViewModel,
            computersViewModel,
            stagingViewModel,
            groupsViewModel,
            reportsViewModel,
            rulesViewModel,
            activityViewModel,
            settingsViewModel,
            windowService);

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

    /// <summary>
    /// Handles unhandled exceptions to prevent crashes and log errors.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Exception event arguments.</param>
    private async void Application_DispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;

        var message = e.Exception.InnerException?.Message ?? e.Exception.Message;

        if (_loggingService is not null)
        {
            await _loggingService.LogErrorAsync(
                $"Unhandled exception: {message}",
                e.Exception);
        }

        MessageBox.Show(
            string.Format(Res.ErrorUnhandledException, message),
            Res.DialogError,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
