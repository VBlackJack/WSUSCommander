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

using System.IO;
using System.Text;
using System.Text.Json;
using WsusCommander.Models;
using WsusCommander.Properties;

namespace WsusCommander.Services;

/// <summary>
/// Data export service implementation.
/// </summary>
public sealed class ExportService : IExportService
{
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportService"/> class.
    /// </summary>
    public ExportService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    /// <inheritdoc/>
    public async Task ExportUpdatesAsync(IEnumerable<WsusUpdate> updates, string filePath, ExportFormat format)
    {
        var data = updates.ToList();
        await _loggingService.LogInfoAsync(
            string.Format(Resources.LogExportingUpdates, data.Count, filePath));

        var content = format switch
        {
            ExportFormat.Csv => GenerateUpdatesCsv(data, ","),
            ExportFormat.Tsv => GenerateUpdatesCsv(data, "\t"),
            ExportFormat.Json => GenerateUpdatesJson(data),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
        await _loggingService.LogInfoAsync(string.Format(Resources.LogExportCompleted, filePath));
    }

    /// <inheritdoc/>
    public async Task ExportComputersAsync(IEnumerable<ComputerStatus> computers, string filePath, ExportFormat format)
    {
        var data = computers.ToList();
        await _loggingService.LogInfoAsync(
            string.Format(Resources.LogExportingComputers, data.Count, filePath));

        var content = format switch
        {
            ExportFormat.Csv => GenerateComputersCsv(data, ","),
            ExportFormat.Tsv => GenerateComputersCsv(data, "\t"),
            ExportFormat.Json => GenerateComputersJson(data),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
        await _loggingService.LogInfoAsync(string.Format(Resources.LogExportCompleted, filePath));
    }

    /// <inheritdoc/>
    public async Task ExportGroupsAsync(IEnumerable<ComputerGroup> groups, string filePath, ExportFormat format)
    {
        var data = groups.ToList();
        await _loggingService.LogInfoAsync(
            string.Format(Resources.LogExportingGroups, data.Count, filePath));

        var content = format switch
        {
            ExportFormat.Csv => GenerateGroupsCsv(data, ","),
            ExportFormat.Tsv => GenerateGroupsCsv(data, "\t"),
            ExportFormat.Json => GenerateGroupsJson(data),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
        await _loggingService.LogInfoAsync(string.Format(Resources.LogExportCompleted, filePath));
    }

    /// <inheritdoc/>
    public string GetFileFilter(ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Csv => Resources.ExportFilterCsv,
            ExportFormat.Tsv => Resources.ExportFilterTsv,
            ExportFormat.Json => Resources.ExportFilterJson,
            _ => Resources.ExportFilterAll
        };
    }

    /// <inheritdoc/>
    public string GetFileExtension(ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Csv => ".csv",
            ExportFormat.Tsv => ".tsv",
            ExportFormat.Json => ".json",
            _ => ".txt"
        };
    }

    private static string GenerateUpdatesCsv(List<WsusUpdate> updates, string separator)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine(string.Join(separator,
            Resources.ExportHeaderUpdateId,
            Resources.ExportHeaderUpdateTitle,
            Resources.ExportHeaderKBArticle,
            Resources.ExportHeaderClassification,
            Resources.ExportHeaderCreationDate,
            Resources.ExportHeaderApprovalStatus,
            Resources.ExportHeaderDeclineStatus));

        // Data
        foreach (var update in updates)
        {
            sb.AppendLine(string.Join(separator,
                EscapeCsv(update.Id.ToString()),
                EscapeCsv(update.Title),
                EscapeCsv(update.KbArticle),
                EscapeCsv(update.Classification),
                update.CreationDate.ToString("yyyy-MM-dd"),
                update.IsApproved,
                update.IsDeclined));
        }

        return sb.ToString();
    }

    private static string GenerateUpdatesJson(List<WsusUpdate> updates)
    {
        return JsonSerializer.Serialize(updates, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private static string GenerateComputersCsv(List<ComputerStatus> computers, string separator)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine(string.Join(separator,
            Resources.ExportHeaderComputerId,
            Resources.ExportHeaderComputerName,
            Resources.ExportHeaderIpAddress,
            Resources.ExportHeaderGroupName,
            Resources.ExportHeaderInstalledCount,
            Resources.ExportHeaderNeededCount,
            Resources.ExportHeaderFailedCount,
            Resources.ExportHeaderLastContact));

        // Data
        foreach (var computer in computers)
        {
            sb.AppendLine(string.Join(separator,
                EscapeCsv(computer.ComputerId),
                EscapeCsv(computer.Name),
                EscapeCsv(computer.IpAddress),
                EscapeCsv(computer.GroupName),
                computer.InstalledCount,
                computer.NeededCount,
                computer.FailedCount,
                computer.LastReportedTime?.ToString("yyyy-MM-dd HH:mm") ?? ""));
        }

        return sb.ToString();
    }

    private static string GenerateComputersJson(List<ComputerStatus> computers)
    {
        return JsonSerializer.Serialize(computers, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private static string GenerateGroupsCsv(List<ComputerGroup> groups, string separator)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine(string.Join(separator,
            Resources.ExportHeaderGroupId,
            Resources.ExportHeaderGroupName,
            Resources.ExportHeaderGroupDescription,
            Resources.ExportHeaderGroupComputerCount));

        // Data
        foreach (var group in groups)
        {
            sb.AppendLine(string.Join(separator,
                EscapeCsv(group.Id.ToString()),
                EscapeCsv(group.Name),
                EscapeCsv(group.Description),
                group.ComputerCount));
        }

        return sb.ToString();
    }

    private static string GenerateGroupsJson(List<ComputerGroup> groups)
    {
        return JsonSerializer.Serialize(groups, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // Escape quotes and wrap in quotes if contains special characters
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
