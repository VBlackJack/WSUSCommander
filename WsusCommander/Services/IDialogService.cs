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
/// Result of a dialog interaction.
/// </summary>
public enum DialogResult
{
    /// <summary>User confirmed the action.</summary>
    Confirmed,

    /// <summary>User cancelled the action.</summary>
    Cancelled,

    /// <summary>User selected an alternative action.</summary>
    Alternative
}

/// <summary>
/// Interface for dialog and notification service.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a confirmation dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Dialog message.</param>
    /// <param name="confirmText">Text for confirm button.</param>
    /// <param name="cancelText">Text for cancel button.</param>
    /// <returns>The dialog result.</returns>
    Task<DialogResult> ShowConfirmationAsync(
        string title,
        string message,
        string confirmText = "OK",
        string cancelText = "Cancel");

    /// <summary>
    /// Shows a warning dialog with confirmation.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Warning message.</param>
    /// <returns>True if user confirmed.</returns>
    Task<bool> ShowWarningAsync(string title, string message);

    /// <summary>
    /// Shows an error dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Error message.</param>
    /// <param name="details">Optional error details.</param>
    Task ShowErrorAsync(string title, string message, string? details = null);

    /// <summary>
    /// Shows an information dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Information message.</param>
    Task ShowInfoAsync(string title, string message);

    /// <summary>
    /// Shows a toast notification.
    /// </summary>
    /// <param name="message">Notification message.</param>
    /// <param name="duration">Duration in milliseconds.</param>
    void ShowToast(string message, int duration = 3000);

    /// <summary>
    /// Shows a save file dialog.
    /// </summary>
    /// <param name="defaultFileName">Default file name.</param>
    /// <param name="filter">File filter (e.g., "CSV files|*.csv").</param>
    /// <returns>Selected file path or null if cancelled.</returns>
    Task<string?> ShowSaveFileDialogAsync(string defaultFileName, string filter);

    /// <summary>
    /// Shows an open file dialog.
    /// </summary>
    /// <param name="filter">File filter.</param>
    /// <returns>Selected file path or null if cancelled.</returns>
    Task<string?> ShowOpenFileDialogAsync(string filter);
}
