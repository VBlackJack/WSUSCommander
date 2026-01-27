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
using WsusCommander.Properties;

namespace WsusCommander.Services;

/// <summary>
/// Service for managing filter presets.
/// </summary>
public sealed class FilterPresetsService : IFilterPresetsService
{
    private readonly string _presetsPath;
    private readonly ILoggingService _loggingService;
    private readonly List<FilterPreset> _presets = [];
    private readonly List<FilterPreset> _builtInPresets;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterPresetsService"/> class.
    /// </summary>
    public FilterPresetsService(IConfigurationService configService, ILoggingService loggingService)
    {
        _loggingService = loggingService;
        _presetsPath = Path.Combine(configService.AppSettings.DataPath, "filter-presets.json");

        // Ensure directory exists
        var directory = Path.GetDirectoryName(_presetsPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Initialize built-in presets
        _builtInPresets =
        [
            new FilterPreset
            {
                Id = Guid.Parse("00000000-0000-0000-0001-000000000001"),
                Name = Resources.PresetAllUpdates,
                SearchText = string.Empty,
                Classification = string.Empty,
                ApprovalFilter = "All",
                IsBuiltIn = true
            },
            new FilterPreset
            {
                Id = Guid.Parse("00000000-0000-0000-0001-000000000002"),
                Name = Resources.PresetUnapprovedOnly,
                SearchText = string.Empty,
                Classification = string.Empty,
                ApprovalFilter = "Unapproved",
                IsBuiltIn = true
            },
            new FilterPreset
            {
                Id = Guid.Parse("00000000-0000-0000-0001-000000000003"),
                Name = Resources.PresetCriticalUpdates,
                SearchText = string.Empty,
                Classification = "Critical Updates",
                ApprovalFilter = "All",
                IsBuiltIn = true
            },
            new FilterPreset
            {
                Id = Guid.Parse("00000000-0000-0000-0001-000000000004"),
                Name = Resources.PresetSecurityUpdates,
                SearchText = string.Empty,
                Classification = "Security Updates",
                ApprovalFilter = "All",
                IsBuiltIn = true
            },
            new FilterPreset
            {
                Id = Guid.Parse("00000000-0000-0000-0001-000000000005"),
                Name = Resources.PresetDeclinedUpdates,
                SearchText = string.Empty,
                Classification = string.Empty,
                ApprovalFilter = "Declined",
                IsBuiltIn = true
            }
        ];
    }

    /// <inheritdoc/>
    public IReadOnlyList<FilterPreset> GetPresets()
    {
        return [.. _builtInPresets, .. _presets];
    }

    /// <inheritdoc/>
    public IReadOnlyList<FilterPreset> GetBuiltInPresets()
    {
        return _builtInPresets.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_presetsPath))
            {
                var content = await File.ReadAllTextAsync(_presetsPath);
                var collection = JsonSerializer.Deserialize<FilterPresetsCollection>(content);
                if (collection?.Presets != null)
                {
                    _presets.Clear();
                    _presets.AddRange(collection.Presets.Where(p => !p.IsBuiltIn));
                }
                await _loggingService.LogDebugAsync($"Loaded {_presets.Count} custom filter presets.");
            }
        }
        catch (Exception ex)
        {
            await _loggingService.LogWarningAsync($"Failed to load filter presets: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task SavePresetAsync(FilterPreset preset)
    {
        if (preset.IsBuiltIn)
        {
            throw new InvalidOperationException("Cannot modify built-in presets.");
        }

        var existing = _presets.FirstOrDefault(p => p.Id == preset.Id);
        if (existing != null)
        {
            _presets.Remove(existing);
        }

        _presets.Add(preset);
        await SaveToFileAsync();
        await _loggingService.LogInfoAsync($"Filter preset '{preset.Name}' saved.");
    }

    /// <inheritdoc/>
    public async Task DeletePresetAsync(Guid presetId)
    {
        var preset = _presets.FirstOrDefault(p => p.Id == presetId);
        if (preset == null)
        {
            return;
        }

        if (preset.IsBuiltIn)
        {
            throw new InvalidOperationException("Cannot delete built-in presets.");
        }

        _presets.Remove(preset);
        await SaveToFileAsync();
        await _loggingService.LogInfoAsync($"Filter preset '{preset.Name}' deleted.");
    }

    private async Task SaveToFileAsync()
    {
        try
        {
            var collection = new FilterPresetsCollection
            {
                Presets = _presets
            };

            var content = JsonSerializer.Serialize(collection, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_presetsPath, content);
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync("Failed to save filter presets", ex);
        }
    }
}
