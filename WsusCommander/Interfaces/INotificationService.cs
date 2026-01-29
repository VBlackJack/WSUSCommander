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
using WsusCommander.Models;

namespace WsusCommander.Interfaces;

/// <summary>
/// Abstraction for user notifications.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Shows a toast notification.
    /// </summary>
    /// <param name="message">Notification message.</param>
    /// <param name="type">Notification type.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    void ShowToast(string message, ToastType type = ToastType.Info, int durationMs = AppConstants.ToastDurations.Info);

    /// <summary>
    /// Shows a balloon notification.
    /// </summary>
    /// <param name="title">Balloon title.</param>
    /// <param name="message">Balloon message.</param>
    void ShowBalloon(string title, string message);

    /// <summary>
    /// Shows a confirmation dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Dialog message.</param>
    /// <returns>True if confirmed.</returns>
    Task<bool> ShowConfirmationAsync(string title, string message);

    /// <summary>
    /// Shows an error dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Dialog message.</param>
    Task ShowErrorAsync(string title, string message);

    /// <summary>
    /// Shows an info dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Dialog message.</param>
    Task ShowInfoAsync(string title, string message);
}
