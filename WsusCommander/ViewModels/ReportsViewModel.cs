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

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WsusCommander.Interfaces;
using WsusCommander.Models;
using WsusCommander.Properties;
using WsusCommander.Services;

namespace WsusCommander.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly IReportService _reportService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IWsusService _wsusService;
    private readonly ILoggingService _loggingService;

    [ObservableProperty]
    private ComplianceReport? _complianceReport;

    [ObservableProperty]
    private ObservableCollection<StaleComputerInfo> _staleComputers = [];

    [ObservableProperty]
    private List<ComputerGroup> _availableGroups = [];

    [ObservableProperty]
    private ComputerGroup? _selectedGroup;

    [ObservableProperty]
    private bool _isLoading;

    public ReportsViewModel(
        IReportService reportService,
        IFileDialogService fileDialogService,
        IWsusService wsusService,
        ILoggingService loggingService)
    {
        _reportService = reportService;
        _fileDialogService = fileDialogService;
        _wsusService = wsusService;
        _loggingService = loggingService;
    }

    [RelayCommand]
    private async Task LoadReportsAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;

        try
        {
            var selectedGroupId = SelectedGroup?.Id;

            var groups = await _wsusService.GetGroupsAsync(cancellationToken);
            AvailableGroups = [.. groups.OrderBy(g => g.Name)];

            if (selectedGroupId.HasValue)
            {
                SelectedGroup = AvailableGroups.FirstOrDefault(g => g.Id == selectedGroupId.Value);
            }
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync($"Failed to load groups: {ex.Message}", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task GenerateComplianceReportAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;

        try
        {
            var options = new ReportOptions
            {
                GroupId = SelectedGroup?.Id
            };

            ComplianceReport = await _reportService.GenerateComplianceReportAsync(options, cancellationToken);
            var stale = await _reportService.GetStaleComputersAsync(options.StaleDays, cancellationToken);
            StaleComputers = new ObservableCollection<StaleComputerInfo>(stale);
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync($"Failed to generate report: {ex.Message}", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportComplianceReportAsync(CancellationToken cancellationToken)
    {
        if (ComplianceReport is null)
            return;

        var filePath = _fileDialogService.ShowSaveFileDialog(
            Resources.ExportFilterReport,
            ".csv",
            "compliance_report");

        if (string.IsNullOrEmpty(filePath))
            return;

        var format = filePath.EndsWith(".json") ? ExportFormat.Json :
                    filePath.EndsWith(".tsv") ? ExportFormat.Tsv :
                    filePath.EndsWith(".pdf") ? ExportFormat.Pdf :
                    filePath.EndsWith(".html") || filePath.EndsWith(".htm") ? ExportFormat.Html :
                    ExportFormat.Csv;

        await _reportService.ExportReportAsync(ComplianceReport, filePath, format, cancellationToken);
    }
}
