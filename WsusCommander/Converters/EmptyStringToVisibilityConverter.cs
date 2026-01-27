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

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WsusCommander.Converters;

/// <summary>
/// Converts an empty/null string to Visibility (empty = Visible, non-empty = Collapsed).
/// Used for showing placeholder text when input is empty.
/// </summary>
[ValueConversion(typeof(string), typeof(Visibility))]
public sealed class EmptyStringToVisibilityConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        var str = value as string;
        return string.IsNullOrEmpty(str) ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
