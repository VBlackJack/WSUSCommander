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

using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;
using WsusCommander.Properties;

namespace WsusCommander.Views;

/// <summary>
/// About dialog window.
/// </summary>
public sealed partial class AboutWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AboutWindow"/> class.
    /// </summary>
    public AboutWindow()
    {
        InitializeComponent();
        DataContext = new AboutWindowViewModel();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private sealed class AboutWindowViewModel
    {
        /// <summary>
        /// Gets the application name.
        /// </summary>
        public string AppName => Resources.AppTitle;

        /// <summary>
        /// Gets the application version display string.
        /// </summary>
        public string AppVersion
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version is null ? string.Empty : string.Format(Resources.AboutVersionLabel, version);
            }
        }

        /// <summary>
        /// Gets the author name.
        /// </summary>
        public string Author => Resources.AboutAuthorValue;
    }
}
