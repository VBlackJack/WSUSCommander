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
        // Core Services
        IConfigurationService configService = new ConfigurationService();
        ConfigService = configService;
        ILoggingService loggingService = new LoggingService(configService);
        IPowerShellService psService = new PowerShellService(loggingService, configService);
        ITimerService timerService = new TimerService();

        // Security Services
        IAuthenticationService authService = new AuthenticationService(configService, loggingService);
        IAuthorizationService authzService = new AuthorizationService(authService, configService, loggingService);

        // Infrastructure Services
        IValidationService validationService = new ValidationService();
        IRetryService retryService = new RetryService(configService, loggingService);
        ICacheService cacheService = new CacheService(configService);
        IDialogService dialogService = new DialogService();
        IClipboardService clipboardService = new ClipboardService();
        INavigationService navigationService = new NavigationService();

        // Data Services
        IExportService exportService = new ExportService(loggingService);
        IPreferencesService preferencesService = new PreferencesService(configService, loggingService);
        PreferencesService = preferencesService;
        IFilterService filterService = new FilterService();
        IFilterPresetsService filterPresetsService = new FilterPresetsService(configService, loggingService);
        IApprovalRulesService approvalRulesService = new ApprovalRulesService(configService, loggingService);

        // Monitoring Services
        IHealthService healthService = new HealthService(configService, psService, loggingService);
        IAccessibilityService accessibilityService = new AccessibilityService(loggingService);
        IThemeService themeService = new ThemeService(loggingService);

        // Business Services
        IBulkOperationService bulkOperationService = new BulkOperationService(
            psService, loggingService, configService);
        IGroupService groupService = new GroupService(
            psService, loggingService, cacheService, validationService, configService, retryService);
        IReportService reportService = new ReportService(
            psService, loggingService, cacheService, configService);

        await preferencesService.LoadAsync();

        // Track disposables
        _disposables.Add((IDisposable)timerService);
        _disposables.Add((IDisposable)cacheService);
        _disposables.Add((IDisposable)healthService);

        // Initialize theme from configuration
        themeService.Initialize(configService.Config.UI.Theme);

        // Create ViewModel with all services
        _mainViewModel = new MainViewModel(
            configService,
            psService,
            loggingService,
            timerService,
            authService,
            authzService,
            validationService,
            retryService,
            cacheService,
            dialogService,
            exportService,
            preferencesService,
            filterService,
            filterPresetsService,
            approvalRulesService,
            healthService,
            accessibilityService,
            bulkOperationService,
            groupService,
            reportService,
            themeService,
            clipboardService,
            navigationService);

        // Initialize ViewModel (authentication, preferences, health check)
        await _mainViewModel.InitializeAsync();

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
