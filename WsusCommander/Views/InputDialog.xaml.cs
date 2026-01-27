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

namespace WsusCommander.Views;

/// <summary>
/// Simple input dialog for text entry.
/// </summary>
public partial class InputDialog : Window
{
    /// <summary>
    /// Gets or sets the dialog title.
    /// </summary>
    public string DialogTitle
    {
        get => Title;
        set => Title = value;
    }

    /// <summary>
    /// Gets or sets the prompt text.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the input value.
    /// </summary>
    public string InputValue { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputDialog"/> class.
    /// </summary>
    public InputDialog()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        };
    }

    /// <summary>
    /// Shows the dialog and returns the user input.
    /// </summary>
    /// <param name="owner">Owner window.</param>
    /// <param name="title">Dialog title.</param>
    /// <param name="prompt">Prompt text.</param>
    /// <param name="defaultValue">Default value.</param>
    /// <returns>User input or null if cancelled.</returns>
    public static string? Show(Window? owner, string title, string prompt, string defaultValue = "")
    {
        var dialog = new InputDialog
        {
            Owner = owner,
            DialogTitle = title,
            Prompt = prompt,
            InputValue = defaultValue
        };

        return dialog.ShowDialog() == true ? dialog.InputValue : null;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
