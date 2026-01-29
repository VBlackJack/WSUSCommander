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

using WsusCommander.Constants;

namespace WsusCommander.Services;

/// <summary>
/// High contrast mode options.
/// </summary>
public enum ContrastMode
{
    /// <summary>Normal contrast.</summary>
    Normal,

    /// <summary>High contrast light.</summary>
    HighContrastLight,

    /// <summary>High contrast dark.</summary>
    HighContrastDark
}

/// <summary>
/// Accessibility settings.
/// </summary>
public sealed class AccessibilitySettings
{
    /// <summary>
    /// Gets or sets whether high contrast mode is enabled.
    /// </summary>
    public ContrastMode ContrastMode { get; set; } = ContrastMode.Normal;

    /// <summary>
    /// Gets or sets whether animations are reduced.
    /// </summary>
    public bool ReduceMotion { get; set; }

    /// <summary>
    /// Gets or sets the font scale factor (1.0 = 100%).
    /// </summary>
    public double FontScale { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets whether screen reader support is enabled.
    /// </summary>
    public bool ScreenReaderSupport { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum touch target size in pixels.
    /// </summary>
    public int MinTouchTargetSize { get; set; } = AppConstants.Accessibility.MinTouchTargetSize;

    /// <summary>
    /// Gets or sets whether focus indicators are enhanced.
    /// </summary>
    public bool EnhancedFocusIndicators { get; set; }
}

/// <summary>
/// Interface for accessibility service.
/// </summary>
public interface IAccessibilityService
{
    /// <summary>
    /// Gets the current accessibility settings.
    /// </summary>
    AccessibilitySettings Settings { get; }

    /// <summary>
    /// Gets whether the system is in high contrast mode.
    /// </summary>
    bool IsSystemHighContrast { get; }

    /// <summary>
    /// Gets whether the system prefers reduced motion.
    /// </summary>
    bool SystemPrefersReducedMotion { get; }

    /// <summary>
    /// Gets the system font scale factor.
    /// </summary>
    double SystemFontScale { get; }

    /// <summary>
    /// Updates accessibility settings.
    /// </summary>
    /// <param name="settings">New settings.</param>
    void UpdateSettings(AccessibilitySettings settings);

    /// <summary>
    /// Applies high contrast mode to the application.
    /// </summary>
    /// <param name="mode">Contrast mode.</param>
    void ApplyContrastMode(ContrastMode mode);

    /// <summary>
    /// Gets accessible text for a UI element.
    /// </summary>
    /// <param name="key">Resource key.</param>
    /// <returns>Accessible text.</returns>
    string GetAccessibleText(string key);

    /// <summary>
    /// Announces a message to screen readers.
    /// </summary>
    /// <param name="message">Message to announce.</param>
    /// <param name="isUrgent">Whether the message is urgent.</param>
    void Announce(string message, bool isUrgent = false);

    /// <summary>
    /// Event raised when settings change.
    /// </summary>
    event EventHandler<AccessibilitySettings>? SettingsChanged;
}
