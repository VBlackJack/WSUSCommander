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

namespace WsusCommander.Services;

/// <summary>
/// Defines the available application themes.
/// </summary>
public enum AppTheme
{
    /// <summary>
    /// Light theme with bright backgrounds.
    /// </summary>
    Light,

    /// <summary>
    /// Dark theme with dark backgrounds.
    /// </summary>
    Dark,

    /// <summary>
    /// Follows the Windows system theme setting.
    /// </summary>
    System
}

/// <summary>
/// Service for managing application theming.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the currently applied theme.
    /// </summary>
    AppTheme CurrentTheme { get; }

    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    event EventHandler<AppTheme>? ThemeChanged;

    /// <summary>
    /// Applies the specified theme to the application.
    /// </summary>
    /// <param name="theme">The theme to apply.</param>
    void SetTheme(AppTheme theme);

    /// <summary>
    /// Gets the system's current theme preference.
    /// </summary>
    /// <returns>Light or Dark based on system settings.</returns>
    AppTheme GetSystemTheme();

    /// <summary>
    /// Initializes the theme service with the user's preference.
    /// </summary>
    /// <param name="savedTheme">The saved theme preference.</param>
    void Initialize(string? savedTheme);
}
