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

using System.Windows.Media;

namespace WsusCommander.Constants;

/// <summary>
/// Centralized color constants for theming and accessibility.
/// </summary>
public static class ColorConstants
{
    /// <summary>
    /// High contrast light theme colors.
    /// </summary>
    public static class HighContrastLight
    {
        /// <summary>Dark blue accent color.</summary>
        public static Color Accent => Color.FromRgb(0, 0, 139);

        /// <summary>Dark red error color.</summary>
        public static Color Error => Color.FromRgb(139, 0, 0);

        /// <summary>Dark green success color.</summary>
        public static Color Success => Color.FromRgb(0, 100, 0);

        /// <summary>Dark orange warning color.</summary>
        public static Color Warning => Color.FromRgb(139, 101, 0);

        /// <summary>Dim gray for disabled elements.</summary>
        public static Color Disabled => Color.FromRgb(105, 105, 105);

        /// <summary>Blue focus indicator.</summary>
        public static Color Focus => Color.FromRgb(0, 0, 255);
    }

    /// <summary>
    /// High contrast dark theme colors.
    /// </summary>
    public static class HighContrastDark
    {
        /// <summary>Light sky blue accent color.</summary>
        public static Color Accent => Color.FromRgb(135, 206, 250);

        /// <summary>Tomato red error color.</summary>
        public static Color Error => Color.FromRgb(255, 99, 71);

        /// <summary>Light green success color.</summary>
        public static Color Success => Color.FromRgb(144, 238, 144);

        /// <summary>Gold warning color.</summary>
        public static Color Warning => Color.FromRgb(255, 215, 0);

        /// <summary>Dark gray for disabled elements.</summary>
        public static Color Disabled => Color.FromRgb(169, 169, 169);

        /// <summary>Yellow focus indicator.</summary>
        public static Color Focus => Color.FromRgb(255, 255, 0);
    }

    /// <summary>
    /// Toast notification colors.
    /// </summary>
    public static class Toast
    {
        /// <summary>Info toast blue color.</summary>
        public static Color Info => Color.FromRgb(0x34, 0x98, 0xDB);

        /// <summary>Success toast green color.</summary>
        public static Color Success => Color.FromRgb(0x27, 0xAE, 0x60);

        /// <summary>Warning toast orange color.</summary>
        public static Color Warning => Color.FromRgb(0xF3, 0x9C, 0x12);

        /// <summary>Error toast red color.</summary>
        public static Color Error => Color.FromRgb(0xE7, 0x4C, 0x3C);
    }
}
