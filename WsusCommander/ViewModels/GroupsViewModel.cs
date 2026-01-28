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

public partial class GroupsViewModel : ObservableObject
{
    private readonly IGroupService _groupService;
    private readonly ILoggingService _loggingService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private ObservableCollection<ComputerGroup> _computerGroups = [];

    [ObservableProperty]
    private ComputerGroup? _selectedGroupForEdit;

    [ObservableProperty]
    private string _newGroupName = string.Empty;

    [ObservableProperty]
    private string _newGroupDescription = string.Empty;

    public GroupsViewModel(
        IGroupService groupService,
        ILoggingService loggingService,
        INotificationService notificationService)
    {
        _groupService = groupService;
        _loggingService = loggingService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    private async Task LoadGroupsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var groups = await _groupService.GetAllGroupsAsync(true, cancellationToken);
            ComputerGroups = new ObservableCollection<ComputerGroup>(groups);
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync($"Failed to load groups: {ex.Message}", ex);
        }
    }

    [RelayCommand]
    private async Task CreateGroupAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(NewGroupName))
            return;

        try
        {
            var group = await _groupService.CreateGroupAsync(
                new CreateGroupOptions { Name = NewGroupName, Description = NewGroupDescription },
                cancellationToken);
            ComputerGroups.Add(group);
            NewGroupName = string.Empty;
            NewGroupDescription = string.Empty;
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteGroupAsync(CancellationToken cancellationToken)
    {
        if (SelectedGroupForEdit is null)
            return;

        try
        {
            await _groupService.DeleteGroupAsync(SelectedGroupForEdit.Id, cancellationToken);
            ComputerGroups.Remove(SelectedGroupForEdit);
            SelectedGroupForEdit = null;
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
    }
}
