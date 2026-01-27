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

using WsusCommander.Interfaces;
using WsusCommander.Models;

namespace WsusCommander.Services;

/// <summary>
/// Notification service that delegates to the dialog service.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly IDialogService _dialogService;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationService"/> class.
    /// </summary>
    /// <param name="dialogService">Dialog service to display messages.</param>
    public NotificationService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    /// <inheritdoc/>
    public void ShowToast(string message, ToastType type = ToastType.Info, int durationMs = 3000)
    {
        switch (type)
        {
            case ToastType.Success:
                _dialogService.ShowSuccessToast(message, durationMs);
                break;
            case ToastType.Warning:
                _dialogService.ShowWarningToast(message, durationMs);
                break;
            case ToastType.Error:
                _dialogService.ShowErrorToast(message, durationMs);
                break;
            default:
                _dialogService.ShowToast(message, durationMs);
                break;
        }
    }

    /// <inheritdoc/>
    public void ShowBalloon(string title, string message)
    {
        _ = _dialogService.ShowInfoAsync(title, message);
    }

    /// <inheritdoc/>
    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var result = await _dialogService.ShowConfirmationAsync(title, message);
        return result == DialogResult.Confirmed;
    }

    /// <inheritdoc/>
    public Task ShowErrorAsync(string title, string message)
    {
        return _dialogService.ShowErrorAsync(title, message);
    }

    /// <inheritdoc/>
    public Task ShowInfoAsync(string title, string message)
    {
        return _dialogService.ShowInfoAsync(title, message);
    }
}
