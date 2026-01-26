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

namespace WsusCommander.Services;

/// <summary>
/// Interface for user preferences persistence.
/// </summary>
public interface IPreferencesService
{
    /// <summary>
    /// Gets the current user preferences.
    /// </summary>
    UserPreferences Preferences { get; }

    /// <summary>
    /// Loads preferences from storage.
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Saves preferences to storage.
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// Resets preferences to defaults.
    /// </summary>
    void Reset();

    /// <summary>
    /// Gets a specific preference value.
    /// </summary>
    /// <typeparam name="T">Type of preference.</typeparam>
    /// <param name="key">Preference key.</param>
    /// <param name="defaultValue">Default value if not found.</param>
    /// <returns>The preference value.</returns>
    T Get<T>(string key, T defaultValue);

    /// <summary>
    /// Sets a specific preference value.
    /// </summary>
    /// <typeparam name="T">Type of preference.</typeparam>
    /// <param name="key">Preference key.</param>
    /// <param name="value">Value to set.</param>
    void Set<T>(string key, T value);
}
