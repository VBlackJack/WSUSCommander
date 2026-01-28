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

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using WsusCommander.Models;

namespace WsusCommander.Views;

/// <summary>
/// Window for creating and editing WSUS groups.
/// </summary>
public sealed partial class GroupEditorWindow : Window, INotifyPropertyChanged
{
    private ComputerGroup? _selectedGroup;

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupEditorWindow"/> class.
    /// </summary>
    /// <param name="group">Optional group to edit.</param>
    public GroupEditorWindow(ComputerGroup? group = null)
    {
        Groups = new ObservableCollection<ComputerGroup>();
        GroupMembers = new ObservableCollection<string>();
        SelectedGroup = group;
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>
    /// Gets the available groups.
    /// </summary>
    public ObservableCollection<ComputerGroup> Groups { get; }

    /// <summary>
    /// Gets the group members list.
    /// </summary>
    public ObservableCollection<string> GroupMembers { get; }

    /// <summary>
    /// Gets or sets the selected group.
    /// </summary>
    public ComputerGroup? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            _selectedGroup = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
