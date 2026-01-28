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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WsusCommander.Models;
using WsusCommander.Properties;
using WsusCommander.Services;

namespace WsusCommander.Views;

/// <summary>
/// Cleanup window for WSUS maintenance.
/// </summary>
public sealed partial class CleanupWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CleanupWindow"/> class.
    /// </summary>
    /// <param name="cleanupService">Cleanup service.</param>
    public CleanupWindow(ICleanupService cleanupService)
    {
        InitializeComponent();
        DataContext = new CleanupViewModel(cleanupService);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private sealed partial class CleanupViewModel : ObservableObject
    {
        private readonly ICleanupService _cleanupService;

        [ObservableProperty]
        private bool _removeObsoleteUpdates = true;

        [ObservableProperty]
        private bool _removeObsoleteComputers = true;

        [ObservableProperty]
        private bool _removeExpiredUpdates = true;

        [ObservableProperty]
        private bool _compressUpdateRevisions = true;

        [ObservableProperty]
        private bool _removeUnneededContent = true;

        [ObservableProperty]
        private bool _isRunning;

        public CleanupViewModel(ICleanupService cleanupService)
        {
            _cleanupService = cleanupService;
        }

        public bool CanInteract => !IsRunning;

        partial void OnIsRunningChanged(bool value)
        {
            OnPropertyChanged(nameof(CanInteract));
        }

        [RelayCommand]
        private async Task RunCleanupAsync()
        {
            IsRunning = true;

            try
            {
                var options = new CleanupOptions
                {
                    RemoveObsoleteUpdates = RemoveObsoleteUpdates,
                    RemoveObsoleteComputers = RemoveObsoleteComputers,
                    RemoveExpiredUpdates = RemoveExpiredUpdates,
                    CompressUpdateRevisions = CompressUpdateRevisions,
                    RemoveUnneededContent = RemoveUnneededContent
                };

                await _cleanupService.RunCleanupAsync(options);

                MessageBox.Show(
                    Properties.Resources.CleanupCompletedMessage,
                    Properties.Resources.CleanupWindowTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(Properties.Resources.CleanupFailedMessage, ex.Message),
                    Properties.Resources.CleanupWindowTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsRunning = false;
            }
        }
    }
}
