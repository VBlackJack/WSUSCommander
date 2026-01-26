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

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace WsusCommander.Services;

/// <summary>
/// Accessibility service implementation.
/// </summary>
public sealed class AccessibilityService : IAccessibilityService
{
    private readonly ILoggingService _loggingService;
    private AccessibilitySettings _settings = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessibilityService"/> class.
    /// </summary>
    public AccessibilityService(ILoggingService loggingService)
    {
        _loggingService = loggingService;

        // Initialize with system settings
        _settings.ReduceMotion = SystemPrefersReducedMotion;
        _settings.ContrastMode = IsSystemHighContrast ? ContrastMode.HighContrastDark : ContrastMode.Normal;
        _settings.FontScale = SystemFontScale;
    }

    /// <inheritdoc/>
    public AccessibilitySettings Settings => _settings;

    /// <inheritdoc/>
    public bool IsSystemHighContrast => SystemParameters.HighContrast;

    /// <inheritdoc/>
    public bool SystemPrefersReducedMotion
    {
        get
        {
            try
            {
                var animationInfo = new ANIMATIONINFO { cbSize = (uint)Marshal.SizeOf<ANIMATIONINFO>() };
                if (SystemParametersInfo(SPI_GETANIMATION, animationInfo.cbSize, ref animationInfo, 0))
                {
                    return animationInfo.iMinAnimate == 0;
                }
            }
            catch
            {
                // Ignore errors
            }

            return false;
        }
    }

    /// <inheritdoc/>
    public double SystemFontScale
    {
        get
        {
            try
            {
                var mainWindow = Application.Current?.MainWindow;
                if (mainWindow != null)
                {
                    var dpiScale = VisualTreeHelper.GetDpi(mainWindow);
                    return dpiScale.PixelsPerInchY / 96.0;
                }
            }
            catch
            {
                // Ignore errors
            }

            return 1.0;
        }
    }

    /// <inheritdoc/>
    public event EventHandler<AccessibilitySettings>? SettingsChanged;

    /// <inheritdoc/>
    public void UpdateSettings(AccessibilitySettings settings)
    {
        _settings = settings;
        _loggingService.LogInfoAsync("Accessibility settings updated");
        SettingsChanged?.Invoke(this, settings);
    }

    /// <inheritdoc/>
    public void ApplyContrastMode(ContrastMode mode)
    {
        _settings.ContrastMode = mode;

        try
        {
            var resources = Application.Current.Resources;

            switch (mode)
            {
                case ContrastMode.HighContrastLight:
                    ApplyHighContrastLight(resources);
                    break;

                case ContrastMode.HighContrastDark:
                    ApplyHighContrastDark(resources);
                    break;

                case ContrastMode.Normal:
                default:
                    ApplyNormalContrast(resources);
                    break;
            }

            _loggingService.LogInfoAsync($"Applied contrast mode: {mode}");
        }
        catch (Exception ex)
        {
            _loggingService.LogErrorAsync($"Failed to apply contrast mode: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public string GetAccessibleText(string key)
    {
        try
        {
            var value = Properties.Resources.ResourceManager.GetString(key);
            return value ?? key;
        }
        catch
        {
            return key;
        }
    }

    /// <inheritdoc/>
    public void Announce(string message, bool isUrgent = false)
    {
        // Log the announcement - screen reader integration would require additional platform-specific code
        var logMessage = isUrgent ? $"[URGENT] {message}" : message;
        _loggingService.LogInfoAsync($"Accessibility announcement: {logMessage}");
    }

    private static void ApplyHighContrastLight(ResourceDictionary resources)
    {
        resources["BackgroundBrush"] = new SolidColorBrush(Colors.White);
        resources["ForegroundBrush"] = new SolidColorBrush(Colors.Black);
        resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0, 0, 139));
        resources["BorderBrush"] = new SolidColorBrush(Colors.Black);
        resources["ErrorBrush"] = new SolidColorBrush(Color.FromRgb(139, 0, 0));
        resources["SuccessBrush"] = new SolidColorBrush(Color.FromRgb(0, 100, 0));
        resources["WarningBrush"] = new SolidColorBrush(Color.FromRgb(139, 101, 0));
        resources["DisabledBrush"] = new SolidColorBrush(Color.FromRgb(105, 105, 105));
        resources["FocusBrush"] = new SolidColorBrush(Color.FromRgb(0, 0, 255));
    }

    private static void ApplyHighContrastDark(ResourceDictionary resources)
    {
        resources["BackgroundBrush"] = new SolidColorBrush(Colors.Black);
        resources["ForegroundBrush"] = new SolidColorBrush(Colors.White);
        resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(135, 206, 250));
        resources["BorderBrush"] = new SolidColorBrush(Colors.White);
        resources["ErrorBrush"] = new SolidColorBrush(Color.FromRgb(255, 99, 71));
        resources["SuccessBrush"] = new SolidColorBrush(Color.FromRgb(144, 238, 144));
        resources["WarningBrush"] = new SolidColorBrush(Color.FromRgb(255, 215, 0));
        resources["DisabledBrush"] = new SolidColorBrush(Color.FromRgb(169, 169, 169));
        resources["FocusBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 0));
    }

    private static void ApplyNormalContrast(ResourceDictionary resources)
    {
        resources.Remove("BackgroundBrush");
        resources.Remove("ForegroundBrush");
        resources.Remove("AccentBrush");
        resources.Remove("BorderBrush");
        resources.Remove("ErrorBrush");
        resources.Remove("SuccessBrush");
        resources.Remove("WarningBrush");
        resources.Remove("DisabledBrush");
        resources.Remove("FocusBrush");
    }

    #region Native Methods

    private const uint SPI_GETANIMATION = 0x0048;

    [StructLayout(LayoutKind.Sequential)]
    private struct ANIMATIONINFO
    {
        public uint cbSize;
        public int iMinAnimate;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref ANIMATIONINFO pvParam, uint fWinIni);

    #endregion
}
