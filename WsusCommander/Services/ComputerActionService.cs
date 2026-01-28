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
/// PowerShell-backed computer actions.
/// </summary>
public sealed class ComputerActionService : IComputerActionService
{
    private readonly IPowerShellService _powerShellService;
    private readonly IConfigurationService _configurationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputerActionService"/> class.
    /// </summary>
    public ComputerActionService(IPowerShellService powerShellService, IConfigurationService configurationService)
    {
        _powerShellService = powerShellService;
        _configurationService = configurationService;
    }

    /// <inheritdoc/>
    public Task ForceComputerScanAsync(string computerId, CancellationToken cancellationToken = default)
    {
        return _powerShellService.ExecuteScriptAsync(
            "Force-ComputerScan.ps1",
            GetParameters(computerId),
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task RemoveComputerAsync(string computerId, CancellationToken cancellationToken = default)
    {
        return _powerShellService.ExecuteScriptAsync(
            "Remove-Computer.ps1",
            GetParameters(computerId),
            cancellationToken);
    }

    private Dictionary<string, object> GetParameters(string computerId)
    {
        return new Dictionary<string, object>
        {
            ["ServerName"] = _configurationService.WsusConnection.ServerName,
            ["Port"] = _configurationService.WsusConnection.Port,
            ["UseSsl"] = _configurationService.WsusConnection.UseSsl,
            ["ComputerId"] = computerId
        };
    }
}
