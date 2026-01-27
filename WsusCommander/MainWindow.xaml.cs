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

namespace WsusCommander;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IPreferencesService? _preferencesService;
    private readonly IConfigurationService? _configService;

    /// <summary>
    /// Initializes a new instance of MainWindow.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        // Get preferences service from App
        _preferencesService = (Application.Current as App)?.PreferencesService;
        _configService = (Application.Current as App)?.ConfigService;

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        RestoreWindowState();
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        SaveWindowState();
    }

    private void RestoreWindowState()
    {
        var hasSavedPreferences = _preferencesService?.HasSavedPreferences == true;
        if (!hasSavedPreferences)
        {
            ApplyStartupMode(_configService?.Config.UI.WindowStartupMode);
            return;
        }

        if (_preferencesService is null)
            return;

        var prefs = _preferencesService.Preferences;

        // Restore window position if valid
        if (prefs.WindowLeft.HasValue && prefs.WindowTop.HasValue)
        {
            // Ensure window is on screen
            var left = prefs.WindowLeft.Value;
            var top = prefs.WindowTop.Value;

            // Check if position is within any screen bounds
            if (IsPositionOnScreen(left, top))
            {
                Left = left;
                Top = top;
            }
        }

        // Restore window size if valid
        if (prefs.WindowWidth.HasValue && prefs.WindowHeight.HasValue)
        {
            var width = prefs.WindowWidth.Value;
            var height = prefs.WindowHeight.Value;

            // Clamp to minimum size
            Width = Math.Max(width, MinWidth);
            Height = Math.Max(height, MinHeight);
        }

        // Restore maximized state
        if (prefs.WindowMaximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void ApplyStartupMode(string? mode)
    {
        var startupMode = mode?.ToLowerInvariant();

        switch (startupMode)
        {
            case "maximized":
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                WindowState = WindowState.Maximized;
                break;
            case "laststate":
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                WindowState = WindowState.Normal;
                break;
            default:
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                WindowState = WindowState.Normal;
                break;
        }
    }

    private void SaveWindowState()
    {
        if (_preferencesService is null)
            return;

        var prefs = _preferencesService.Preferences;

        // Save maximized state
        prefs.WindowMaximized = WindowState == WindowState.Maximized;

        // Save position and size only if not maximized
        if (WindowState == WindowState.Normal)
        {
            prefs.WindowLeft = Left;
            prefs.WindowTop = Top;
            prefs.WindowWidth = Width;
            prefs.WindowHeight = Height;
        }

        // Save preferences (fire and forget)
        _ = _preferencesService.SaveAsync();
    }

    private static bool IsPositionOnScreen(double left, double top)
    {
        // Check if the position is within the virtual screen bounds
        var virtualLeft = SystemParameters.VirtualScreenLeft;
        var virtualTop = SystemParameters.VirtualScreenTop;
        var virtualWidth = SystemParameters.VirtualScreenWidth;
        var virtualHeight = SystemParameters.VirtualScreenHeight;

        return left >= virtualLeft &&
               left < virtualLeft + virtualWidth &&
               top >= virtualTop &&
               top < virtualTop + virtualHeight;
    }
}
