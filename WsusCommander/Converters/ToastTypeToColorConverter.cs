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
using WsusCommander.Constants;
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
            return new SolidColorBrush(ColorConstants.Toast.Info);
        }

        return type switch
        {
            ToastType.Success => new SolidColorBrush(ColorConstants.Toast.Success),
            ToastType.Warning => new SolidColorBrush(ColorConstants.Toast.Warning),
            ToastType.Error => new SolidColorBrush(ColorConstants.Toast.Error),
            _ => new SolidColorBrush(ColorConstants.Toast.Info)
        };
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
