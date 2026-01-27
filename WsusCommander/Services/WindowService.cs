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

using System.Linq;
using System.Windows;
using WsusCommander.Interfaces;
using WsusCommander.Models;
using WsusCommander.Views;

namespace WsusCommander.Services;

/// <summary>
/// WPF implementation of window navigation.
/// </summary>
public sealed class WindowService : IWindowService
{
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowService"/> class.
    /// </summary>
    /// <param name="loggingService">Logging service for diagnostics.</param>
    public WindowService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    /// <inheritdoc/>
    public void ShowComputerUpdates(ComputerStatus computer, IReadOnlyList<ComputerUpdateStatus> updates)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var window = new ComputerUpdatesWindow(computer, updates.ToList())
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        });
    }

    /// <inheritdoc/>
    public void ShowUpdateDetails(WsusUpdate update)
    {
        _ = _loggingService.LogWarningAsync("Update details window not implemented.");
    }

    /// <inheritdoc/>
    public void ShowGroupEditor(ComputerGroup? group = null)
    {
        _ = _loggingService.LogWarningAsync("Group editor window not implemented.");
    }

    /// <inheritdoc/>
    public void ShowRuleEditor(ApprovalRule? rule = null)
    {
        _ = _loggingService.LogWarningAsync("Rule editor window not implemented.");
    }

    /// <inheritdoc/>
    public void ShowSettings()
    {
        _ = _loggingService.LogWarningAsync("Settings window not implemented.");
    }

    /// <inheritdoc/>
    public void ShowAbout()
    {
        _ = _loggingService.LogWarningAsync("About window not implemented.");
    }
}
