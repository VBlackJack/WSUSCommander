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

namespace WsusCommander.Interfaces;

/// <summary>
/// Abstraction for file dialog interactions.
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// Shows a save file dialog.
    /// </summary>
    /// <param name="filter">File filter.</param>
    /// <param name="defaultExtension">Default extension.</param>
    /// <param name="initialFileName">Optional initial file name.</param>
    /// <returns>Selected file path or null if canceled.</returns>
    string? ShowSaveFileDialog(string filter, string defaultExtension, string? initialFileName = null);

    /// <summary>
    /// Shows an open file dialog.
    /// </summary>
    /// <param name="filter">File filter.</param>
    /// <returns>Selected file path or null if canceled.</returns>
    string? ShowOpenFileDialog(string filter);

    /// <summary>
    /// Shows a folder browser dialog.
    /// </summary>
    /// <returns>Selected folder path or null if canceled.</returns>
    string? ShowFolderBrowserDialog();
}
