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

using System.IO;
using System.Text.Json;
using WsusCommander.Models;

namespace WsusCommander.Services;

/// <summary>
/// JSON-based settings backup service.
/// </summary>
public sealed class SettingsBackupService : ISettingsBackupService
{
    private readonly IConfigurationService _configurationService;
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsBackupService"/> class.
    /// </summary>
    public SettingsBackupService(IConfigurationService configurationService, ILoggingService loggingService)
    {
        _configurationService = configurationService;
        _loggingService = loggingService;
    }

    /// <inheritdoc/>
    public async Task ExportSettingsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var content = JsonSerializer.Serialize(_configurationService.Config, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(filePath, content, cancellationToken);
        await _loggingService.LogInfoAsync($"Exported settings to {filePath}");
    }

    /// <inheritdoc/>
    public async Task ImportSettingsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        var config = JsonSerializer.Deserialize<AppConfig>(content);

        if (config is null)
        {
            throw new InvalidDataException("Settings file is invalid.");
        }

        var targetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        var serialized = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(targetPath, serialized, cancellationToken);
        await _loggingService.LogInfoAsync($"Imported settings from {filePath}");
    }
}
