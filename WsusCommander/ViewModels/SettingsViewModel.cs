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
using WsusCommander.Constants;
using WsusCommander.Interfaces;
using WsusCommander.Models;
using WsusCommander.Properties;
using WsusCommander.Services;

namespace WsusCommander.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigurationService _configService;
    private readonly IPreferencesService _preferencesService;
    private readonly IFileDialogService _fileDialogService;
    private readonly INotificationService _notificationService;
    private readonly ISettingsBackupService _settingsBackupService;

    [ObservableProperty]
    private string _selectedTheme = "System";

    [ObservableProperty]
    private string _selectedLanguage = "en";

    [ObservableProperty]
    private bool _isAutoRefreshEnabled;

    [ObservableProperty]
    private int _autoRefreshIntervalSeconds;

    [ObservableProperty]
    private string _defaultServerName = string.Empty;

    [ObservableProperty]
    private int _defaultServerPort = AppConstants.Ports.WsusDefault;

    [ObservableProperty]
    private bool _defaultUseSsl;

    [ObservableProperty]
    private bool _enableNotifications = true;

    [ObservableProperty]
    private string _defaultExportFormat = "Csv";

    public SettingsViewModel(
        IConfigurationService configService,
        IPreferencesService preferencesService,
        IFileDialogService fileDialogService,
        INotificationService notificationService,
        ISettingsBackupService settingsBackupService)
    {
        _configService = configService;
        _preferencesService = preferencesService;
        _fileDialogService = fileDialogService;
        _notificationService = notificationService;
        _settingsBackupService = settingsBackupService;

        _autoRefreshIntervalSeconds = _configService.AppSettings.AutoRefreshInterval;
        _defaultServerName = _configService.WsusConnection.ServerName;
        _defaultServerPort = _configService.WsusConnection.Port;
        _defaultUseSsl = _configService.WsusConnection.UseSsl;
        _defaultExportFormat = _preferencesService.Preferences.LastExportFormat;
    }

    [RelayCommand]
    private void ToggleAutoRefresh()
    {
        IsAutoRefreshEnabled = !IsAutoRefreshEnabled;
    }

    [RelayCommand]
    private async Task BackupSettingsAsync(CancellationToken cancellationToken)
    {
        var filePath = _fileDialogService.ShowSaveFileDialog(
            Resources.ExportFilterJson,
            ".json",
            "wsus-settings-backup");

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        try
        {
            await _settingsBackupService.ExportSettingsAsync(filePath, cancellationToken);
            _notificationService.ShowToast(Resources.ToastSettingsExported, ToastType.Success);
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
    }

    [RelayCommand]
    private async Task RestoreSettingsAsync(CancellationToken cancellationToken)
    {
        var filePath = _fileDialogService.ShowOpenFileDialog(Resources.ExportFilterJson);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var confirmed = await _notificationService.ShowConfirmationAsync(
            Resources.DialogConfirm,
            Resources.ConfirmRestoreSettings);

        if (!confirmed)
        {
            return;
        }

        try
        {
            await _settingsBackupService.ImportSettingsAsync(filePath, cancellationToken);
            _notificationService.ShowToast(Resources.ToastSettingsImported, ToastType.Success);
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
    }
}
