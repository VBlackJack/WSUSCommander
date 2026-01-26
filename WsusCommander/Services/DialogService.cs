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

using System.Windows;
using Microsoft.Win32;

namespace WsusCommander.Services;

/// <summary>
/// WPF-based dialog service implementation.
/// </summary>
public sealed class DialogService : IDialogService
{
    /// <inheritdoc/>
    public Task<DialogResult> ShowConfirmationAsync(
        string title,
        string message,
        string confirmText = "OK",
        string cancelText = "Cancel")
    {
        return Task.FromResult(Application.Current.Dispatcher.Invoke(() =>
        {
            var result = MessageBox.Show(
                Application.Current.MainWindow,
                message,
                title,
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);

            return result == MessageBoxResult.OK ? DialogResult.Confirmed : DialogResult.Cancelled;
        }));
    }

    /// <inheritdoc/>
    public Task<bool> ShowWarningAsync(string title, string message)
    {
        return Task.FromResult(Application.Current.Dispatcher.Invoke(() =>
        {
            var result = MessageBox.Show(
                Application.Current.MainWindow,
                message,
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return result == MessageBoxResult.Yes;
        }));
    }

    /// <inheritdoc/>
    public Task ShowErrorAsync(string title, string message, string? details = null)
    {
        return Task.Run(() => Application.Current.Dispatcher.Invoke(() =>
        {
            var fullMessage = details is null ? message : $"{message}\n\nDetails:\n{details}";

            MessageBox.Show(
                Application.Current.MainWindow,
                fullMessage,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }));
    }

    /// <inheritdoc/>
    public Task ShowInfoAsync(string title, string message)
    {
        return Task.Run(() => Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(
                Application.Current.MainWindow,
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }));
    }

    /// <inheritdoc/>
    public void ShowToast(string message, int duration = 3000)
    {
        // Toast notifications would require custom implementation
        // For now, we log the message - can be enhanced with custom toast control
        Application.Current.Dispatcher.Invoke(() =>
        {
            // Could implement as a Snackbar-style notification in the UI
            System.Diagnostics.Debug.WriteLine($"Toast: {message}");
        });
    }

    /// <inheritdoc/>
    public Task<string?> ShowSaveFileDialogAsync(string defaultFileName, string filter)
    {
        return Task.FromResult(Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new SaveFileDialog
            {
                FileName = defaultFileName,
                Filter = filter,
                DefaultExt = GetDefaultExtension(filter),
                AddExtension = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }));
    }

    /// <inheritdoc/>
    public Task<string?> ShowOpenFileDialogAsync(string filter)
    {
        return Task.FromResult(Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                CheckFileExists = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }));
    }

    private static string GetDefaultExtension(string filter)
    {
        // Extract first extension from filter like "CSV files|*.csv"
        var parts = filter.Split('|');
        if (parts.Length >= 2)
        {
            var ext = parts[1].Replace("*", "").Trim();
            return ext.StartsWith('.') ? ext : $".{ext}";
        }
        return ".txt";
    }
}
