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
using WsusCommander.Models;
using WsusCommander.Properties;
using WsusCommander.Services;
using WsusCommander.Interfaces;

namespace WsusCommander.ViewModels;

public partial class RulesViewModel : ObservableObject
{
    private readonly IApprovalRulesService _rulesService;
    private readonly IFileDialogService _fileDialogService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private ObservableCollection<ApprovalRule> _approvalRules = [];

    [ObservableProperty]
    private ApprovalRule? _selectedRule;

    public RulesViewModel(
        IApprovalRulesService rulesService,
        IFileDialogService fileDialogService,
        INotificationService notificationService)
    {
        _rulesService = rulesService;
        _fileDialogService = fileDialogService;
        _notificationService = notificationService;
        LoadRules();
    }

    private void LoadRules()
    {
        ApprovalRules = new ObservableCollection<ApprovalRule>(_rulesService.GetRules());
    }

    [RelayCommand]
    private Task CreateRuleAsync()
    {
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task EditRuleAsync()
    {
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task DeleteRuleAsync()
    {
        if (SelectedRule is null)
            return;

        await _rulesService.DeleteRuleAsync(SelectedRule.Id);
        ApprovalRules.Remove(SelectedRule);
        SelectedRule = null;
    }

    [RelayCommand]
    private async Task ImportRulesAsync(CancellationToken cancellationToken)
    {
        var filePath = _fileDialogService.ShowOpenFileDialog(Resources.ExportFilterJson);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        try
        {
            await _rulesService.ImportRulesAsync(filePath);
            LoadRules();
            _notificationService.ShowToast(Resources.ToastRulesImported, ToastType.Success);
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
    }

    [RelayCommand]
    private Task ApplyRulesAsync()
    {
        return Task.CompletedTask;
    }
}
