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

using WsusCommander.Models;
using WsusCommander.Properties;

namespace WsusCommander.Services;

/// <summary>
/// Cleanup service implementation for WSUS maintenance.
/// </summary>
public sealed class CleanupService : ICleanupService
{
    private readonly IPowerShellService _powerShellService;
    private readonly ILoggingService _loggingService;
    private readonly IConfigurationService _configurationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CleanupService"/> class.
    /// </summary>
    /// <param name="powerShellService">PowerShell execution service.</param>
    /// <param name="loggingService">Logging service.</param>
    /// <param name="configurationService">Configuration service.</param>
    public CleanupService(
        IPowerShellService powerShellService,
        ILoggingService loggingService,
        IConfigurationService configurationService)
    {
        _powerShellService = powerShellService;
        _loggingService = loggingService;
        _configurationService = configurationService;
    }

    /// <inheritdoc/>
    public async Task RunCleanupAsync(CleanupOptions options, CancellationToken cancellationToken = default)
    {
        await _loggingService.LogInfoAsync(Resources.LogCleanupStarted);

        var parameters = new Dictionary<string, object>
        {
            ["ServerName"] = _configurationService.WsusConnection.ServerName,
            ["Port"] = _configurationService.WsusConnection.Port,
            ["UseSsl"] = _configurationService.WsusConnection.UseSsl,
            ["RemoveObsoleteUpdates"] = options.RemoveObsoleteUpdates,
            ["RemoveObsoleteComputers"] = options.RemoveObsoleteComputers,
            ["RemoveExpiredUpdates"] = options.RemoveExpiredUpdates,
            ["CompressUpdateRevisions"] = options.CompressUpdateRevisions,
            ["RemoveUnneededContent"] = options.RemoveUnneededContent
        };

        await _powerShellService.ExecuteScriptAsync(
            "Invoke-WsusCleanup.ps1",
            parameters,
            cancellationToken);

        await _loggingService.LogInfoAsync(Resources.LogCleanupCompleted);
    }
}
