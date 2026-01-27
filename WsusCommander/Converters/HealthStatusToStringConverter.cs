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
using WsusCommander.Properties;

namespace WsusCommander.Converters;

/// <summary>
/// Converts a health status string to a localized display string.
/// </summary>
[ValueConversion(typeof(string), typeof(string))]
public sealed class HealthStatusToStringConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = value as string;
        return status switch
        {
            "Healthy" => Resources.HealthStatusHealthy,
            "Degraded" => Resources.HealthStatusDegraded,
            "Unhealthy" => Resources.HealthStatusUnhealthy,
            _ => Resources.HealthStatusUnknown
        };
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
