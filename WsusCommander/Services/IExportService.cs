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

using WsusCommander.Models;

namespace WsusCommander.Services;

/// <summary>
/// Export format options.
/// </summary>
public enum ExportFormat
{
    /// <summary>Comma-separated values.</summary>
    Csv,

    /// <summary>Tab-separated values.</summary>
    Tsv,

    /// <summary>JSON format.</summary>
    Json,

    /// <summary>Portable Document Format.</summary>
    Pdf,

    /// <summary>HyperText Markup Language.</summary>
    Html
}

/// <summary>
/// Interface for data export service.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exports updates to a file.
    /// </summary>
    /// <param name="updates">Updates to export.</param>
    /// <param name="filePath">Target file path.</param>
    /// <param name="format">Export format.</param>
    Task ExportUpdatesAsync(IEnumerable<WsusUpdate> updates, string filePath, ExportFormat format);

    /// <summary>
    /// Exports computer statuses to a file.
    /// </summary>
    /// <param name="computers">Computer statuses to export.</param>
    /// <param name="filePath">Target file path.</param>
    /// <param name="format">Export format.</param>
    Task ExportComputersAsync(IEnumerable<ComputerStatus> computers, string filePath, ExportFormat format);

    /// <summary>
    /// Exports computer groups to a file.
    /// </summary>
    /// <param name="groups">Groups to export.</param>
    /// <param name="filePath">Target file path.</param>
    /// <param name="format">Export format.</param>
    Task ExportGroupsAsync(IEnumerable<ComputerGroup> groups, string filePath, ExportFormat format);

    /// <summary>
    /// Gets the file filter for save dialog based on format.
    /// </summary>
    /// <param name="format">Export format.</param>
    /// <returns>File dialog filter string.</returns>
    string GetFileFilter(ExportFormat format);

    /// <summary>
    /// Gets the default file extension for a format.
    /// </summary>
    /// <param name="format">Export format.</param>
    /// <returns>File extension including dot.</returns>
    string GetFileExtension(ExportFormat format);
}
