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

public partial class ComputersViewModel : ObservableObject
{
    private readonly IWsusService _wsusService;
    private readonly ILoggingService _loggingService;
    private readonly INotificationService _notificationService;
    private readonly IExportService _exportService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IBulkOperationService _bulkOperationService;
    private readonly IGroupService _groupService;
    private readonly IDialogService _dialogService;
    private readonly IComputerActionService _computerActionService;

    [ObservableProperty]
    private ObservableCollection<ComputerStatus> _filteredComputerStatuses = [];

    [ObservableProperty]
    private ComputerStatus? _selectedComputer;

    [ObservableProperty]
    private bool _isLoading;

    public ComputersViewModel(
        IWsusService wsusService,
        ILoggingService loggingService,
        INotificationService notificationService,
        IExportService exportService,
        IFileDialogService fileDialogService,
        IBulkOperationService bulkOperationService,
        IGroupService groupService,
        IDialogService dialogService,
        IComputerActionService computerActionService)
    {
        _wsusService = wsusService;
        _loggingService = loggingService;
        _notificationService = notificationService;
        _exportService = exportService;
        _fileDialogService = fileDialogService;
        _bulkOperationService = bulkOperationService;
        _groupService = groupService;
        _dialogService = dialogService;
        _computerActionService = computerActionService;
    }

    [RelayCommand]
    private async Task LoadComputerStatusesAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;

        try
        {
            var computers = await _wsusService.GetComputersAsync(cancellationToken);
            FilteredComputerStatuses = new ObservableCollection<ComputerStatus>(computers);
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync($"Failed to load computers: {ex.Message}", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ViewComputerUpdates()
    {
        if (SelectedComputer is null) return;
        OnViewUpdatesRequested?.Invoke(this, SelectedComputer);
    }

    [RelayCommand]
    private async Task MoveComputerToGroupAsync(CancellationToken cancellationToken)
    {
        if (SelectedComputer is null)
            return;

        var groups = await _groupService.GetAllGroupsAsync(true, cancellationToken);
        var groupNames = string.Join(", ", groups.Select(g => g.Name).OrderBy(name => name));

        var groupName = await _dialogService.ShowInputDialogAsync(
            Resources.DialogMoveComputerTitle,
            string.Format(Resources.DialogMoveComputerPrompt, groupNames));

        if (string.IsNullOrWhiteSpace(groupName))
            return;

        var targetGroup = groups.FirstOrDefault(g => string.Equals(g.Name, groupName, StringComparison.OrdinalIgnoreCase));
        if (targetGroup is null)
        {
            await _notificationService.ShowErrorAsync(
                Resources.DialogError,
                string.Format(Resources.ErrorGroupNotFound, groupName));
            return;
        }

        await _bulkOperationService.MoveComputersToGroupAsync(
            [SelectedComputer.ComputerId],
            targetGroup.Id,
            null,
            cancellationToken);

        SelectedComputer.GroupName = targetGroup.Name;
        _notificationService.ShowToast(
            string.Format(Resources.ToastComputerMoved, SelectedComputer.Name, targetGroup.Name),
            ToastType.Success);
    }

    [RelayCommand]
    private async Task ForceComputerScanAsync(CancellationToken cancellationToken)
    {
        if (SelectedComputer is null)
            return;

        await _computerActionService.ForceComputerScanAsync(SelectedComputer.ComputerId, cancellationToken);
        _notificationService.ShowToast(
            string.Format(Resources.ToastComputerScanQueued, SelectedComputer.Name),
            ToastType.Success);
    }

    [RelayCommand]
    private async Task RemoveComputerAsync(CancellationToken cancellationToken)
    {
        if (SelectedComputer is null)
            return;

        var confirmed = await _notificationService.ShowConfirmationAsync(
            Resources.DialogConfirm,
            string.Format(Resources.ConfirmRemoveComputer, SelectedComputer.Name));

        if (!confirmed)
            return;

        await _computerActionService.RemoveComputerAsync(SelectedComputer.ComputerId, cancellationToken);
        FilteredComputerStatuses.Remove(SelectedComputer);
        SelectedComputer = null;

        _notificationService.ShowToast(Resources.ToastComputerRemoved, ToastType.Success);
    }

    [RelayCommand]
    private async Task ExportComputersAsync(CancellationToken cancellationToken)
    {
        var filePath = _fileDialogService.ShowSaveFileDialog(
            Resources.ExportFilterCsv,
            ".csv",
            "computers_export");

        if (string.IsNullOrEmpty(filePath)) return;

        IsLoading = true;

        try
        {
            var format = filePath.EndsWith(".json") ? ExportFormat.Json :
                        filePath.EndsWith(".tsv") ? ExportFormat.Tsv : ExportFormat.Csv;
            await _exportService.ExportComputersAsync(FilteredComputerStatuses, filePath, format);
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public event EventHandler<ComputerStatus>? OnViewUpdatesRequested;
}
