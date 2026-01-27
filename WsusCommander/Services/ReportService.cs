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
using System.Management.Automation;
using System.Text;
using System.Text.Json;
using WsusCommander.Models;

namespace WsusCommander.Services;

/// <summary>
/// Report generation service implementation.
/// </summary>
public sealed class ReportService : IReportService
{
    private readonly IPowerShellService _powerShellService;
    private readonly ILoggingService _loggingService;
    private readonly ICacheService _cacheService;
    private readonly IConfigurationService _configService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportService"/> class.
    /// </summary>
    public ReportService(
        IPowerShellService powerShellService,
        ILoggingService loggingService,
        ICacheService cacheService,
        IConfigurationService configService)
    {
        _powerShellService = powerShellService;
        _loggingService = loggingService;
        _cacheService = cacheService;
        _configService = configService;
    }

    /// <inheritdoc/>
    public async Task<ComplianceReport> GenerateComplianceReportAsync(
        ReportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new ReportOptions();
        await _loggingService.LogInfoAsync("Generating compliance report");

        var parameters = new Dictionary<string, object>
        {
            ["ServerName"] = _configService.WsusConnection.ServerName,
            ["Port"] = _configService.WsusConnection.Port,
            ["UseSsl"] = _configService.WsusConnection.UseSsl,
            ["StaleDays"] = options.StaleDays,
            ["IncludeSuperseded"] = options.IncludeSuperseded,
            ["IncludeDeclined"] = options.IncludeDeclined
        };

        if (options.GroupId.HasValue)
        {
            parameters["GroupId"] = options.GroupId.Value.ToString();
        }

        var result = await _powerShellService.ExecuteScriptAsync(
            "Get-ComplianceReport.ps1",
            parameters);

        var report = ParseComplianceReport(result);
        await _loggingService.LogInfoAsync($"Compliance report generated: {report.CompliancePercent:F1}% compliant");

        return report;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<StaleComputerInfo>> GetStaleComputersAsync(
        int staleDays = 30,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"stale_computers_{staleDays}";
        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                await _loggingService.LogInfoAsync($"Getting stale computers (>{staleDays} days)");

                var result = await _powerShellService.ExecuteScriptAsync(
                    "Get-StaleComputers.ps1",
                    new Dictionary<string, object>
                    {
                        ["ServerName"] = _configService.WsusConnection.ServerName,
                        ["Port"] = _configService.WsusConnection.Port,
                        ["UseSsl"] = _configService.WsusConnection.UseSsl,
                        ["StaleDays"] = staleDays
                    });

                return ParseStaleComputers(result);
            },
            TimeSpan.FromMinutes(5));
    }

    /// <inheritdoc/>
    public async Task<CriticalUpdatesSummary> GetCriticalUpdatesSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        return await _cacheService.GetOrCreateAsync(
            "critical_updates_summary",
            async () =>
            {
                await _loggingService.LogInfoAsync("Getting critical updates summary");

                var result = await _powerShellService.ExecuteScriptAsync(
                    "Get-CriticalUpdates.ps1",
                    new Dictionary<string, object>
                    {
                        ["ServerName"] = _configService.WsusConnection.ServerName,
                        ["Port"] = _configService.WsusConnection.Port,
                        ["UseSsl"] = _configService.WsusConnection.UseSsl
                    });

                return ParseCriticalUpdatesSummary(result);
            },
            TimeSpan.FromMinutes(5));
    }

    /// <inheritdoc/>
    public async Task ExportReportAsync(
        ComplianceReport report,
        string filePath,
        ExportFormat format,
        CancellationToken cancellationToken = default)
    {
        await _loggingService.LogInfoAsync($"Exporting compliance report to {filePath}");

        var content = format switch
        {
            ExportFormat.Json => GenerateJsonReport(report),
            ExportFormat.Csv => GenerateCsvReport(report),
            ExportFormat.Tsv => GenerateTsvReport(report),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8, cancellationToken);
        await _loggingService.LogInfoAsync($"Report exported: {filePath}");
    }

    private static ComplianceReport ParseComplianceReport(PSDataCollection<PSObject>? data)
    {
        var report = new ComplianceReport
        {
            Title = "WSUS Compliance Report"
        };

        if (data == null || !data.Any())
        {
            return report;
        }

        var item = data.First();

        report.TotalComputers = GetPropertyInt(item, "TotalComputers");
        report.CompliantComputers = GetPropertyInt(item, "CompliantComputers");
        report.NonCompliantComputers = GetPropertyInt(item, "NonCompliantComputers");
        report.TotalApprovedUpdates = GetPropertyInt(item, "TotalApprovedUpdates");
        report.CompliancePercent = GetPropertyDouble(item, "CompliancePercent");

        // Determine overall status
        report.OverallStatus = report.CompliancePercent switch
        {
            >= 95 => ComplianceStatus.Compliant,
            >= 70 => ComplianceStatus.PartiallyCompliant,
            > 0 => ComplianceStatus.NonCompliant,
            _ => ComplianceStatus.Unknown
        };

        return report;
    }

    private static IReadOnlyList<StaleComputerInfo> ParseStaleComputers(PSDataCollection<PSObject>? data)
    {
        var staleComputers = new List<StaleComputerInfo>();

        if (data == null)
        {
            return staleComputers;
        }

        foreach (var item in data)
        {
            DateTime? lastReport = null;
            var lrtValue = item.Properties["LastReportTime"]?.Value;
            if (lrtValue is DateTime dt)
            {
                lastReport = dt;
            }

            staleComputers.Add(new StaleComputerInfo
            {
                ComputerId = GetPropertyString(item, "ComputerId"),
                ComputerName = GetPropertyString(item, "ComputerName"),
                LastReportTime = lastReport,
                DaysSinceLastReport = GetPropertyInt(item, "DaysSinceLastReport"),
                GroupNames = GetPropertyStringList(item, "GroupNames")
            });
        }

        return staleComputers;
    }

    private static CriticalUpdatesSummary ParseCriticalUpdatesSummary(PSDataCollection<PSObject>? data)
    {
        var summary = new CriticalUpdatesSummary();

        if (data == null || !data.Any())
        {
            return summary;
        }

        var item = data.First();

        summary.TotalCritical = GetPropertyInt(item, "TotalCritical");
        summary.ApprovedCritical = GetPropertyInt(item, "ApprovedCritical");
        summary.UnapprovedCritical = GetPropertyInt(item, "UnapprovedCritical");
        summary.ComputersNeedingCritical = GetPropertyInt(item, "ComputersNeedingCritical");

        return summary;
    }

    private static int GetPropertyInt(PSObject item, string propertyName)
    {
        var value = item.Properties[propertyName]?.Value;
        return value != null ? Convert.ToInt32(value) : 0;
    }

    private static double GetPropertyDouble(PSObject item, string propertyName)
    {
        var value = item.Properties[propertyName]?.Value;
        return value != null ? Convert.ToDouble(value) : 0;
    }

    private static string GetPropertyString(PSObject item, string propertyName)
    {
        return item.Properties[propertyName]?.Value?.ToString() ?? string.Empty;
    }

    private static List<string> GetPropertyStringList(PSObject item, string propertyName)
    {
        var value = item.Properties[propertyName]?.Value;
        if (value is object[] arr)
        {
            return arr.Select(x => x?.ToString() ?? string.Empty).ToList();
        }

        return [];
    }

    private static string GenerateJsonReport(ComplianceReport report)
    {
        return JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private static string GenerateCsvReport(ComplianceReport report)
    {
        var sb = new StringBuilder();

        sb.AppendLine("WSUS Compliance Report Summary");
        sb.AppendLine($"Generated,{report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Overall Status,{report.OverallStatus}");
        sb.AppendLine($"Compliance %,{report.CompliancePercent:F1}");
        sb.AppendLine($"Total Computers,{report.TotalComputers}");
        sb.AppendLine($"Compliant Computers,{report.CompliantComputers}");
        sb.AppendLine($"Non-Compliant Computers,{report.NonCompliantComputers}");
        sb.AppendLine($"Total Approved Updates,{report.TotalApprovedUpdates}");
        sb.AppendLine();

        if (report.GroupCompliance.Count > 0)
        {
            sb.AppendLine("Group Compliance");
            sb.AppendLine("Group Name,Total Computers,Compliant Computers,Compliance %,Needed Updates,Failed Updates");
            foreach (var group in report.GroupCompliance)
            {
                sb.AppendLine($"{EscapeCsv(group.GroupName)},{group.TotalComputers},{group.CompliantComputers},{group.CompliancePercent:F1},{group.TotalNeededUpdates},{group.TotalFailedUpdates}");
            }
            sb.AppendLine();
        }

        if (report.StaleComputers.Count > 0)
        {
            sb.AppendLine("Stale Computers");
            sb.AppendLine("Computer Name,Last Report,Days Since Report,Groups");
            foreach (var computer in report.StaleComputers)
            {
                sb.AppendLine($"{EscapeCsv(computer.ComputerName)},{computer.LastReportTime?.ToString("yyyy-MM-dd") ?? "Never"},{computer.DaysSinceLastReport},{EscapeCsv(string.Join("; ", computer.GroupNames))}");
            }
        }

        return sb.ToString();
    }

    private static string GenerateTsvReport(ComplianceReport report)
    {
        return GenerateCsvReport(report).Replace(",", "\t");
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
