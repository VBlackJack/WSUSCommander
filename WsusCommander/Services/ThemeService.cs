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
using Microsoft.Win32;

namespace WsusCommander.Services;

/// <summary>
/// Service for managing application theming.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private readonly ILoggingService _loggingService;
    private AppTheme _currentTheme = AppTheme.Light;

    /// <inheritdoc />
    public AppTheme CurrentTheme => _currentTheme;

    /// <inheritdoc />
    public event EventHandler<AppTheme>? ThemeChanged;

    /// <summary>
    /// Initializes a new instance of the ThemeService.
    /// </summary>
    /// <param name="loggingService">The logging service.</param>
    public ThemeService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    /// <inheritdoc />
    public void Initialize(string? savedTheme)
    {
        var theme = savedTheme?.ToLowerInvariant() switch
        {
            "dark" => AppTheme.Dark,
            "system" => AppTheme.System,
            _ => AppTheme.Light
        };

        SetTheme(theme);
    }

    /// <inheritdoc />
    public void SetTheme(AppTheme theme)
    {
        var effectiveTheme = theme == AppTheme.System ? GetSystemTheme() : theme;

        var themeUri = effectiveTheme switch
        {
            AppTheme.Dark => new Uri("pack://application:,,,/Themes/DarkTheme.xaml", UriKind.Absolute),
            _ => new Uri("pack://application:,,,/Themes/LightTheme.xaml", UriKind.Absolute)
        };

        try
        {
            var themeDictionary = new ResourceDictionary { Source = themeUri };

            // Find and remove existing theme dictionary
            var existingTheme = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source?.OriginalString.Contains("Theme.xaml") == true);

            if (existingTheme != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(existingTheme);
            }

            // Add new theme
            Application.Current.Resources.MergedDictionaries.Add(themeDictionary);

            _currentTheme = theme;
            ThemeChanged?.Invoke(this, theme);

            _loggingService.LogInfoAsync($"Theme changed to: {theme} (effective: {effectiveTheme})").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _loggingService.LogErrorAsync($"Failed to apply theme: {theme}", ex).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public AppTheme GetSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");

            if (value is int appsUseLightTheme)
            {
                return appsUseLightTheme == 0 ? AppTheme.Dark : AppTheme.Light;
            }
        }
        catch
        {
            // Fallback to light theme if we can't read registry
        }

        return AppTheme.Light;
    }
}
