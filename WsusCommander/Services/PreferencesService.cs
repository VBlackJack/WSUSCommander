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
/// User preferences persistence service.
/// </summary>
public sealed class PreferencesService : IPreferencesService
{
    private readonly string _preferencesPath;
    private readonly ILoggingService _loggingService;
    private UserPreferences _preferences = new();
    private bool _hasSavedPreferences;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreferencesService"/> class.
    /// </summary>
    public PreferencesService(IConfigurationService configService, ILoggingService loggingService)
    {
        _loggingService = loggingService;
        _preferencesPath = Path.Combine(configService.AppSettings.DataPath, "preferences.json");

        // Ensure directory exists
        var directory = Path.GetDirectoryName(_preferencesPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <inheritdoc/>
    public UserPreferences Preferences => _preferences;

    /// <inheritdoc/>
    public bool HasSavedPreferences => _hasSavedPreferences;

    /// <inheritdoc/>
    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_preferencesPath))
            {
                var content = await File.ReadAllTextAsync(_preferencesPath);
                _preferences = JsonSerializer.Deserialize<UserPreferences>(content) ?? new UserPreferences();
                _hasSavedPreferences = true;
                await _loggingService.LogDebugAsync("User preferences loaded.");
            }
        }
        catch (Exception ex)
        {
            await _loggingService.LogWarningAsync($"Failed to load preferences: {ex.Message}");
            _preferences = new UserPreferences();
            _hasSavedPreferences = false;
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync()
    {
        try
        {
            var content = JsonSerializer.Serialize(_preferences, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_preferencesPath, content);
            await _loggingService.LogDebugAsync("User preferences saved.");
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync("Failed to save preferences", ex);
        }
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _preferences = new UserPreferences();
        _hasSavedPreferences = false;
        _loggingService.LogInfoAsync("User preferences reset to defaults.");
    }

    /// <inheritdoc/>
    public T Get<T>(string key, T defaultValue)
    {
        if (_preferences.Custom.TryGetValue(key, out var value))
        {
            try
            {
                if (value is JsonElement element)
                {
                    return JsonSerializer.Deserialize<T>(element.GetRawText()) ?? defaultValue;
                }

                if (value is T typedValue)
                {
                    return typedValue;
                }
            }
            catch
            {
                // Return default on conversion failure
            }
        }

        return defaultValue;
    }

    /// <inheritdoc/>
    public void Set<T>(string key, T value)
    {
        _preferences.Custom[key] = value!;
    }
}
