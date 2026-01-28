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
using WsusCommander.Services;

namespace WsusCommander.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigurationService _configService;
    private readonly IPreferencesService _preferencesService;

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
    private int _defaultServerPort = 8530;

    [ObservableProperty]
    private bool _defaultUseSsl;

    [ObservableProperty]
    private bool _enableNotifications = true;

    [ObservableProperty]
    private string _defaultExportFormat = "Csv";

    public SettingsViewModel(IConfigurationService configService, IPreferencesService preferencesService)
    {
        _configService = configService;
        _preferencesService = preferencesService;

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
}
