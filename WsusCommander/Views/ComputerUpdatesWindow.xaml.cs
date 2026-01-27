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
using System.Windows.Controls;
using WsusCommander.Models;
using Res = WsusCommander.Properties.Resources;

namespace WsusCommander.Views;

/// <summary>
/// Dialog window for viewing and managing updates for a specific computer.
/// </summary>
public partial class ComputerUpdatesWindow : Window, INotifyPropertyChanged
{
    private readonly List<ComputerUpdateStatus> _allUpdates;
    private ObservableCollection<ComputerUpdateStatus> _filteredUpdates;
    private string _currentFilter = "All";

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputerUpdatesWindow"/> class.
    /// </summary>
    public ComputerUpdatesWindow(ComputerStatus computer, List<ComputerUpdateStatus> updates)
    {
        InitializeComponent();
        DataContext = this;

        ComputerName = computer.Name;
        WindowTitle = string.Format(Res.DialogComputerUpdates, computer.Name);
        _allUpdates = updates;
        _filteredUpdates = new ObservableCollection<ComputerUpdateStatus>(updates);

        UpdateSummary();
    }

    /// <summary>
    /// Gets the window title.
    /// </summary>
    public string WindowTitle { get; }

    /// <summary>
    /// Gets the computer name.
    /// </summary>
    public string ComputerName { get; }

    /// <summary>
    /// Gets the summary text.
    /// </summary>
    public string Summary { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the filtered updates collection.
    /// </summary>
    public ObservableCollection<ComputerUpdateStatus> FilteredUpdates
    {
        get => _filteredUpdates;
        private set
        {
            _filteredUpdates = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FilteredCount));
        }
    }

    /// <summary>
    /// Gets the filtered count.
    /// </summary>
    public int FilteredCount => FilteredUpdates.Count;

    /// <summary>
    /// Updates the summary text.
    /// </summary>
    private void UpdateSummary()
    {
        var notApproved = _allUpdates.Count(u => u.ApprovalStatusDisplay == "NotApproved");
        var superseded = _allUpdates.Count(u => u.IsSuperseded);
        var failed = _allUpdates.Count(u => u.IsFailed);

        var parts = new List<string>
        {
            $"{_allUpdates.Count} {Res.FilterNeeded}"
        };

        if (notApproved > 0)
        {
            parts.Add($"{notApproved} {Res.FilterNotApproved}");
        }

        if (superseded > 0)
        {
            parts.Add($"{superseded} {Res.LblSuperseded}");
        }

        if (failed > 0)
        {
            parts.Add($"{failed} {Res.FilterFailed}");
        }

        Summary = string.Join(" | ", parts);
        OnPropertyChanged(nameof(Summary));
    }

    /// <summary>
    /// Handles filter combo selection change.
    /// </summary>
    private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FilterCombo.SelectedItem is ComboBoxItem selectedItem)
        {
            _currentFilter = selectedItem.Content?.ToString() ?? "All";
            ApplyFilter();
        }
    }

    /// <summary>
    /// Applies the current filter to the updates list.
    /// </summary>
    private void ApplyFilter()
    {
        IEnumerable<ComputerUpdateStatus> filtered = _allUpdates;

        if (_currentFilter == Res.FilterNotApproved)
        {
            filtered = _allUpdates.Where(u => u.ApprovalStatusDisplay == "NotApproved");
        }
        else if (_currentFilter == Res.FilterFailed)
        {
            filtered = _allUpdates.Where(u => u.IsFailed);
        }

        FilteredUpdates = new ObservableCollection<ComputerUpdateStatus>(filtered);
    }

    /// <summary>
    /// Handles the close button click.
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Property changed event.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the property changed event.
    /// </summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
