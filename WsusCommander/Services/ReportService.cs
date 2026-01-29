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
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using WsusCommander.Constants;
using WsusCommander.Models;
using WsusCommander.Properties;

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
            parameters,
            cancellationToken);

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
                    },
                    cancellationToken);

                return ParseStaleComputers(result);
            },
            TimeSpan.FromMinutes(AppConstants.Cache.ReportsCacheTtlMinutes));
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
                    },
                    cancellationToken);

                return ParseCriticalUpdatesSummary(result);
            },
            TimeSpan.FromMinutes(AppConstants.Cache.ReportsCacheTtlMinutes));
    }

    /// <inheritdoc/>
    public async Task ExportReportAsync(
        ComplianceReport report,
        string filePath,
        ExportFormat format,
        CancellationToken cancellationToken = default)
    {
        await _loggingService.LogInfoAsync($"Exporting compliance report to {filePath}");

        if (format == ExportFormat.Pdf)
        {
            var pdfBytes = GeneratePdfReport(report);
            await File.WriteAllBytesAsync(filePath, pdfBytes, cancellationToken);
            await _loggingService.LogInfoAsync($"Report exported: {filePath}");
            return;
        }

        var content = format switch
        {
            ExportFormat.Json => GenerateJsonReport(report),
            ExportFormat.Csv => GenerateCsvReport(report),
            ExportFormat.Tsv => GenerateTsvReport(report),
            ExportFormat.Html => GenerateHtmlReport(report),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8, cancellationToken);
        await _loggingService.LogInfoAsync($"Report exported: {filePath}");
    }

    private static ComplianceReport ParseComplianceReport(PSDataCollection<PSObject>? data)
    {
        var report = new ComplianceReport
        {
            Title = Resources.ReportCompliance
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

        // Parse group compliance
        var groupComplianceValue = item.Properties["GroupCompliance"]?.Value;
        if (groupComplianceValue is object[] groupArray)
        {
            foreach (var groupItem in groupArray)
            {
                if (groupItem is PSObject groupObj)
                {
                    report.GroupCompliance.Add(new GroupComplianceInfo
                    {
                        GroupId = Guid.TryParse(GetPropertyString(groupObj, "GroupId"), out var gid) ? gid : Guid.Empty,
                        GroupName = GetPropertyString(groupObj, "GroupName"),
                        TotalComputers = GetPropertyInt(groupObj, "TotalComputers"),
                        CompliantComputers = GetPropertyInt(groupObj, "CompliantComputers"),
                        CompliancePercent = GetPropertyDouble(groupObj, "CompliancePercent"),
                        TotalNeededUpdates = GetPropertyInt(groupObj, "TotalNeededUpdates"),
                        TotalFailedUpdates = GetPropertyInt(groupObj, "TotalFailedUpdates")
                    });
                }
            }
        }

        // Parse computer details
        var computerDetailsValue = item.Properties["ComputerDetails"]?.Value;
        if (computerDetailsValue is object[] computerArray)
        {
            foreach (var computerItem in computerArray)
            {
                if (computerItem is PSObject compObj)
                {
                    report.ComputerDetails.Add(new ComputerComplianceInfo
                    {
                        ComputerId = GetPropertyString(compObj, "ComputerId"),
                        ComputerName = GetPropertyString(compObj, "ComputerName"),
                        IpAddress = GetPropertyString(compObj, "IpAddress"),
                        LastReportedTime = GetPropertyDateTime(compObj, "LastReportedTime"),
                        LastSyncTime = GetPropertyDateTime(compObj, "LastSyncTime"),
                        OsDescription = GetPropertyString(compObj, "OSDescription"),
                        IsCompliant = GetPropertyBool(compObj, "IsCompliant"),
                        CompliancePercent = GetPropertyDouble(compObj, "CompliancePercent"),
                        InstalledCount = GetPropertyInt(compObj, "InstalledCount"),
                        NeededCount = GetPropertyInt(compObj, "NeededCount"),
                        DownloadedCount = GetPropertyInt(compObj, "DownloadedCount"),
                        NotInstalledCount = GetPropertyInt(compObj, "NotInstalledCount"),
                        FailedCount = GetPropertyInt(compObj, "FailedCount"),
                        Groups = GetPropertyStringList(compObj, "Groups")
                    });
                }
            }
        }

        // Determine overall status
        report.OverallStatus = report.CompliancePercent switch
        {
            >= AppConstants.ComplianceThresholds.Compliant => ComplianceStatus.Compliant,
            >= AppConstants.ComplianceThresholds.PartiallyCompliant => ComplianceStatus.PartiallyCompliant,
            > 0 => ComplianceStatus.NonCompliant,
            _ => ComplianceStatus.Unknown
        };

        return report;
    }

    private static string GenerateHtmlReport(ComplianceReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\"/>");
        sb.AppendLine($"<title>{report.Title}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#2c3e50;}");
        sb.AppendLine("h1{margin-bottom:4px;}");
        sb.AppendLine(".summary{display:flex;gap:16px;margin:16px 0;}");
        sb.AppendLine(".card{border:1px solid #e0e0e0;border-radius:8px;padding:12px;min-width:180px;}");
        sb.AppendLine(".chart{margin-top:16px;}");
        sb.AppendLine(".bar{height:16px;border-radius:4px;background:#3498db;}");
        sb.AppendLine("table{border-collapse:collapse;width:100%;margin-top:12px;}");
        sb.AppendLine("th,td{border:1px solid #e0e0e0;padding:8px;text-align:left;}");
        sb.AppendLine("th{background:#f5f5f5;}");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine($"<h1>{report.Title}</h1>");
        sb.AppendLine($"<div>{string.Format(Resources.ReportGeneratedOn, report.GeneratedAt.ToLocalTime())}</div>");
        sb.AppendLine("<div class=\"summary\">");
        sb.AppendLine($"<div class=\"card\"><strong>{Resources.ReportTotalComputers}</strong><div>{report.TotalComputers}</div></div>");
        sb.AppendLine($"<div class=\"card\"><strong>{Resources.ReportCompliantComputers}</strong><div>{report.CompliantComputers}</div></div>");
        sb.AppendLine($"<div class=\"card\"><strong>{Resources.ReportNonCompliantComputers}</strong><div>{report.NonCompliantComputers}</div></div>");
        sb.AppendLine($"<div class=\"card\"><strong>{Resources.ReportCompliancePercent}</strong><div>{report.CompliancePercent:F1}%</div></div>");
        sb.AppendLine("</div>");

        sb.AppendLine($"<h2>{Resources.ReportComplianceByGroup}</h2>");
        sb.AppendLine(BuildGroupComplianceChart(report.GroupCompliance));
        sb.AppendLine(BuildGroupComplianceTable(report.GroupCompliance));

        sb.AppendLine(BuildComputerDetailsTable(report.ComputerDetails));

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static string BuildGroupComplianceChart(IReadOnlyList<GroupComplianceInfo> groups)
    {
        if (groups.Count == 0)
        {
            return $"<div>{Resources.ReportNoGroupData}</div>";
        }

        var sb = new StringBuilder();
        sb.AppendLine("<div class=\"chart\">");
        foreach (var group in groups.OrderByDescending(g => g.CompliancePercent))
        {
            sb.AppendLine($"<div><strong>{group.GroupName}</strong> ({group.CompliancePercent:F1}%)</div>");
            sb.AppendLine($"<div class=\"bar\" style=\"width:{Math.Min(100, group.CompliancePercent):F1}%\"></div>");
        }
        sb.AppendLine("</div>");
        return sb.ToString();
    }

    private static string BuildGroupComplianceTable(IReadOnlyList<GroupComplianceInfo> groups)
    {
        if (groups.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr>");
        sb.AppendLine($"<th>{Resources.ReportGroupName}</th>");
        sb.AppendLine($"<th>{Resources.ReportTotalComputers}</th>");
        sb.AppendLine($"<th>{Resources.ReportCompliantComputers}</th>");
        sb.AppendLine($"<th>{Resources.ReportCompliancePercent}</th>");
        sb.AppendLine($"<th>{Resources.ReportNeededUpdates}</th>");
        sb.AppendLine($"<th>{Resources.ReportFailedUpdates}</th>");
        sb.AppendLine("</tr></thead>");
        sb.AppendLine("<tbody>");
        foreach (var group in groups)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{group.GroupName}</td>");
            sb.AppendLine($"<td>{group.TotalComputers}</td>");
            sb.AppendLine($"<td>{group.CompliantComputers}</td>");
            sb.AppendLine($"<td>{group.CompliancePercent:F1}%</td>");
            sb.AppendLine($"<td>{group.TotalNeededUpdates}</td>");
            sb.AppendLine($"<td>{group.TotalFailedUpdates}</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</tbody></table>");
        return sb.ToString();
    }

    private static string BuildComputerDetailsTable(IReadOnlyList<ComputerComplianceInfo> computers)
    {
        if (computers.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"<h2>{Resources.ReportComputerDetails}</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr>");
        sb.AppendLine($"<th>{Resources.ColumnComputerName}</th>");
        sb.AppendLine($"<th>{Resources.ColumnIpAddress}</th>");
        sb.AppendLine($"<th>{Resources.ReportOS}</th>");
        sb.AppendLine($"<th>{Resources.ReportCompliancePercent}</th>");
        sb.AppendLine($"<th>{Resources.ColumnInstalled}</th>");
        sb.AppendLine($"<th>{Resources.ColumnNeeded}</th>");
        sb.AppendLine($"<th>{Resources.ColumnFailed}</th>");
        sb.AppendLine($"<th>{Resources.ColumnLastReported}</th>");
        sb.AppendLine($"<th>{Resources.ReportGroupName}</th>");
        sb.AppendLine("</tr></thead>");
        sb.AppendLine("<tbody>");

        // Sort by non-compliant first (most needed + failed updates)
        foreach (var computer in computers.OrderByDescending(c => c.NeededCount + c.FailedCount))
        {
            var statusColor = computer.IsCompliant ? "#27ae60" : (computer.FailedCount > 0 ? "#e74c3c" : "#f39c12");
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{computer.ComputerName}</td>");
            sb.AppendLine($"<td>{computer.IpAddress}</td>");
            sb.AppendLine($"<td>{computer.OsDescription}</td>");
            sb.AppendLine($"<td style=\"color:{statusColor};font-weight:bold\">{computer.CompliancePercent:F1}%</td>");
            sb.AppendLine($"<td>{computer.InstalledCount}</td>");
            sb.AppendLine($"<td>{computer.NeededCount}</td>");
            sb.AppendLine($"<td style=\"color:{(computer.FailedCount > 0 ? "#e74c3c" : "inherit")}\">{computer.FailedCount}</td>");
            sb.AppendLine($"<td>{computer.LastReportedTime?.ToString("yyyy-MM-dd HH:mm") ?? "-"}</td>");
            sb.AppendLine($"<td>{string.Join(", ", computer.Groups)}</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</tbody></table>");
        return sb.ToString();
    }

    private static byte[] GeneratePdfReport(ComplianceReport report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header()
                    .Text(report.Title)
                    .FontSize(20)
                    .SemiBold();

                page.Content().Column(column =>
                {
                    column.Item().Text(string.Format(Resources.ReportGeneratedOn, report.GeneratedAt.ToLocalTime()));
                    column.Item().Text(string.Format(Resources.ReportCompliancePercentDisplay, report.CompliancePercent));
                    column.Item().Text(string.Format(Resources.ReportTotalComputersDisplay, report.TotalComputers));
                    column.Item().Text(string.Format(Resources.ReportCompliantComputersDisplay, report.CompliantComputers));
                    column.Item().Text(string.Format(Resources.ReportNonCompliantComputersDisplay, report.NonCompliantComputers));

                    column.Item().PaddingTop(10).Text(Resources.ReportComplianceByGroup).FontSize(14).SemiBold();
                    if (report.GroupCompliance.Count == 0)
                    {
                        column.Item().Text(Resources.ReportNoGroupData);
                    }
                    else
                    {
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text(Resources.ReportGroupName).SemiBold();
                                header.Cell().Text(Resources.ReportTotalComputers).SemiBold();
                                header.Cell().Text(Resources.ReportCompliantComputers).SemiBold();
                                header.Cell().Text(Resources.ReportCompliancePercent).SemiBold();
                            });

                            foreach (var group in report.GroupCompliance)
                            {
                                table.Cell().Text(group.GroupName);
                                table.Cell().Text(group.TotalComputers.ToString());
                                table.Cell().Text(group.CompliantComputers.ToString());
                                table.Cell().Text($"{group.CompliancePercent:F1}%");
                            }
                        });
                    }

                    // Computer Details section
                    if (report.ComputerDetails.Count > 0)
                    {
                        column.Item().PaddingTop(10).Text(Resources.ReportComputerDetails).FontSize(14).SemiBold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text(Resources.ColumnComputerName).SemiBold();
                                header.Cell().Text(Resources.ReportCompliancePercent).SemiBold();
                                header.Cell().Text(Resources.ColumnInstalled).SemiBold();
                                header.Cell().Text(Resources.ColumnNeeded).SemiBold();
                                header.Cell().Text(Resources.ColumnFailed).SemiBold();
                            });

                            foreach (var computer in report.ComputerDetails.OrderByDescending(c => c.NeededCount + c.FailedCount))
                            {
                                table.Cell().Text(computer.ComputerName);
                                table.Cell().Text($"{computer.CompliancePercent:F1}%");
                                table.Cell().Text(computer.InstalledCount.ToString());
                                table.Cell().Text(computer.NeededCount.ToString());
                                table.Cell().Text(computer.FailedCount.ToString());
                            }
                        });
                    }
                });
            });
        });

        return document.GeneratePdf();
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
            if (lrtValue is DateTime dt && dt != DateTime.MinValue)
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

    private static bool GetPropertyBool(PSObject item, string propertyName)
    {
        var value = item.Properties[propertyName]?.Value;
        if (value is bool b) return b;
        if (bool.TryParse(value?.ToString(), out var result)) return result;
        return false;
    }

    private static DateTime? GetPropertyDateTime(PSObject item, string propertyName)
    {
        var value = item.Properties[propertyName]?.Value;
        if (value is DateTime dt && dt != DateTime.MinValue) return dt;

        var str = value?.ToString();
        if (string.IsNullOrEmpty(str)) return null;

        // Handle WCF JSON date format: /Date(milliseconds)/
        if (str.StartsWith("/Date(") && str.EndsWith(")/"))
        {
            var msStr = str[6..^2];
            if (long.TryParse(msStr, out var ms) && ms > 0)
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(ms).LocalDateTime;
            }
        }

        if (DateTime.TryParse(str, out var result) && result != DateTime.MinValue) return result;
        return null;
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
            sb.AppendLine();
        }

        if (report.ComputerDetails.Count > 0)
        {
            sb.AppendLine("Computer Details");
            sb.AppendLine("Computer Name,IP Address,OS,Compliant,Compliance %,Installed,Needed,Downloaded,Failed,Last Report,Last Sync,Groups");
            foreach (var computer in report.ComputerDetails.OrderByDescending(c => c.NeededCount + c.FailedCount))
            {
                sb.AppendLine($"{EscapeCsv(computer.ComputerName)},{computer.IpAddress},{EscapeCsv(computer.OsDescription)},{(computer.IsCompliant ? "Yes" : "No")},{computer.CompliancePercent:F1},{computer.InstalledCount},{computer.NeededCount},{computer.DownloadedCount},{computer.FailedCount},{computer.LastReportedTime?.ToString("yyyy-MM-dd HH:mm") ?? "Never"},{computer.LastSyncTime?.ToString("yyyy-MM-dd HH:mm") ?? "Never"},{EscapeCsv(string.Join("; ", computer.Groups))}");
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
