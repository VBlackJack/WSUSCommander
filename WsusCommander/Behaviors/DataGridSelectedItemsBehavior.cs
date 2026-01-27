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

using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace WsusCommander.Behaviors;

/// <summary>
/// Enables two-way binding for DataGrid.SelectedItems.
/// </summary>
public static class DataGridSelectedItemsBehavior
{
    /// <summary>
    /// Attached property to bind selected items.
    /// </summary>
    public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.RegisterAttached(
        "SelectedItems",
        typeof(IList),
        typeof(DataGridSelectedItemsBehavior),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemsChanged));

    /// <summary>
    /// Gets the bound selected items collection.
    /// </summary>
    public static IList? GetSelectedItems(DependencyObject obj)
    {
        return (IList?)obj.GetValue(SelectedItemsProperty);
    }

    /// <summary>
    /// Sets the bound selected items collection.
    /// </summary>
    public static void SetSelectedItems(DependencyObject obj, IList? value)
    {
        obj.SetValue(SelectedItemsProperty, value);
    }

    private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid dataGrid)
        {
            return;
        }

        dataGrid.SelectionChanged -= OnDataGridSelectionChanged;
        dataGrid.SelectionChanged += OnDataGridSelectionChanged;

        SyncSelectedItems(dataGrid);
    }

    private static void OnDataGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not DataGrid dataGrid)
        {
            return;
        }

        SyncSelectedItems(dataGrid);
    }

    private static void SyncSelectedItems(DataGrid dataGrid)
    {
        var boundCollection = GetSelectedItems(dataGrid);
        if (boundCollection is null)
        {
            return;
        }

        boundCollection.Clear();
        foreach (var item in dataGrid.SelectedItems)
        {
            boundCollection.Add(item);
        }
    }
}
