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
using System.Windows.Data;
using System.Windows.Media;
using WsusCommander.Models;

namespace WsusCommander.Converters;

/// <summary>
/// Converts a ToastType to a brush color.
/// </summary>
[ValueConversion(typeof(ToastType), typeof(Brush))]
public sealed class ToastTypeToColorConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ToastType type)
        {
            return new SolidColorBrush(Color.FromRgb(0x34, 0x98, 0xDB)); // Default blue
        }

        return type switch
        {
            ToastType.Success => new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60)),
            ToastType.Warning => new SolidColorBrush(Color.FromRgb(0xF3, 0x9C, 0x12)),
            ToastType.Error => new SolidColorBrush(Color.FromRgb(0xE7, 0x4C, 0x3C)),
            _ => new SolidColorBrush(Color.FromRgb(0x34, 0x98, 0xDB))
        };
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
