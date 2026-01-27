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
using System.Windows;
using Microsoft.Win32;
using WsusCommander.Interfaces;

namespace WsusCommander.Services;

/// <summary>
/// WPF implementation of file dialog access.
/// </summary>
public sealed class FileDialogService : IFileDialogService
{
    /// <inheritdoc/>
    public string? ShowSaveFileDialog(string filter, string defaultExtension, string? initialFileName = null)
    {
        return Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new SaveFileDialog
            {
                Filter = filter,
                DefaultExt = defaultExtension,
                FileName = initialFileName ?? string.Empty,
                AddExtension = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        });
    }

    /// <inheritdoc/>
    public string? ShowOpenFileDialog(string filter)
    {
        return Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                CheckFileExists = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        });
    }

    /// <inheritdoc/>
    public string? ShowFolderBrowserDialog()
    {
        return Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Folders|*.",
                FileName = "SelectFolder",
                CheckFileExists = false,
                ValidateNames = false
            };

            if (dialog.ShowDialog() != true)
            {
                return null;
            }

            return Path.GetDirectoryName(dialog.FileName);
        });
    }
}
