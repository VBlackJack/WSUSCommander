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

/// <summary>
/// ViewModel for the Staging tab displaying computers in "Unassigned Computers" group.
/// </summary>
public sealed partial class StagingViewModel : ObservableObject
{
    private readonly IWsusService _wsusService;
    private readonly ILoggingService _loggingService;
    private readonly INotificationService _notificationService;
    private readonly IBulkOperationService _bulkOperationService;
    private readonly IGroupService _groupService;
    private readonly IDialogService _dialogService;
    private readonly IComputerActionService _computerActionService;

    [ObservableProperty]
    private ObservableCollection<ComputerStatus> _stagingComputers = [];

    [ObservableProperty]
    private ComputerStatus? _selectedComputer;

    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Initializes a new instance of the <see cref="StagingViewModel"/> class.
    /// </summary>
    public StagingViewModel(
        IWsusService wsusService,
        ILoggingService loggingService,
        INotificationService notificationService,
        IBulkOperationService bulkOperationService,
        IGroupService groupService,
        IDialogService dialogService,
        IComputerActionService computerActionService)
    {
        _wsusService = wsusService;
        _loggingService = loggingService;
        _notificationService = notificationService;
        _bulkOperationService = bulkOperationService;
        _groupService = groupService;
        _dialogService = dialogService;
        _computerActionService = computerActionService;
    }

    /// <summary>
    /// Loads computers from the "Unassigned Computers" staging group.
    /// </summary>
    [RelayCommand]
    private async Task LoadStagingComputersAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;

        try
        {
            var computers = await _wsusService.GetStagingComputersAsync(cancellationToken);
            StagingComputers = new ObservableCollection<ComputerStatus>(computers);
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync($"Failed to load staging computers: {ex.Message}", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Moves the selected computer to a different group.
    /// </summary>
    [RelayCommand]
    private async Task MoveComputerToGroupAsync(CancellationToken cancellationToken)
    {
        if (SelectedComputer is null)
            return;

        var groups = await _groupService.GetAllGroupsAsync(true, cancellationToken);
        var groupNames = string.Join(", ", groups
            .Where(g => !string.Equals(g.Name, "Unassigned Computers", StringComparison.OrdinalIgnoreCase))
            .Select(g => g.Name)
            .OrderBy(name => name));

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

        // Remove from staging list since it's no longer unassigned
        StagingComputers.Remove(SelectedComputer);
        _notificationService.ShowToast(
            string.Format(Resources.ToastComputerMoved, SelectedComputer.Name, targetGroup.Name),
            ToastType.Success);
        SelectedComputer = null;
    }

    /// <summary>
    /// Forces a scan on the selected computer.
    /// </summary>
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

    /// <summary>
    /// Removes the selected computer from WSUS.
    /// </summary>
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
        StagingComputers.Remove(SelectedComputer);
        SelectedComputer = null;

        _notificationService.ShowToast(Resources.ToastComputerRemoved, ToastType.Success);
    }
}
