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
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WsusCommander.Interfaces;
using WsusCommander.Models;
using WsusCommander.Properties;
using WsusCommander.Services;

namespace WsusCommander.ViewModels;

public partial class UpdatesViewModel : ObservableObject
{
    private readonly IWsusService _wsusService;
    private readonly ILoggingService _loggingService;
    private readonly INotificationService _notificationService;
    private readonly IExportService _exportService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IFilterPresetsService _filterPresetsService;
    private readonly IDialogService _dialogService;

    private readonly ObservableCollection<WsusUpdate> _allUpdates = new();

    [ObservableProperty]
    private ObservableCollection<WsusUpdate> _filteredUpdates = new();

    [ObservableProperty]
    private WsusUpdate? _selectedUpdate;

    [ObservableProperty]
    private ObservableCollection<WsusUpdate> _selectedUpdates = new();

    [ObservableProperty]
    private UpdateDetails? _selectedUpdateDetails;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedClassification = "All";

    [ObservableProperty]
    private string _selectedApprovalFilter = "All";

    [ObservableProperty]
    private string _selectedSupersededFilter = Resources.FilterSupersededAll;

    [ObservableProperty]
    private ObservableCollection<string> _classifications = new();

    [ObservableProperty]
    private ObservableCollection<FilterPreset> _filterPresets = new();

    [ObservableProperty]
    private FilterPreset? _selectedFilterPreset;

    [ObservableProperty]
    private ObservableCollection<ComputerGroup> _computerGroups = new();

    [ObservableProperty]
    private ComputerGroup? _selectedComputerGroup;

    [ObservableProperty]
    private ObservableCollection<string> _supersededFilters = new(
        new[]
        {
            Resources.FilterSupersededAll,
            Resources.FilterSupersededOnly,
            Resources.FilterHideSuperseded
        });

    [ObservableProperty]
    private ObservableCollection<string> _approvalFilters = new(
        new[]
        {
            "All",
            "Approved",
            "Unapproved",
            "Declined"
        });

    [ObservableProperty]
    private ObservableCollection<int> _pageSizes = new(new[] { 25, 50, 100, 200 });

    [ObservableProperty]
    private int _pageSize = 100;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private bool _isBulkOperationRunning;

    [ObservableProperty]
    private double _bulkProgress;

    [ObservableProperty]
    private string _bulkProgressText = string.Empty;

    [ObservableProperty]
    private bool _canUserApprove = true;

    [ObservableProperty]
    private bool _canUserDecline = true;

    public string PaginationInfo => string.Format(Resources.StatusPagination, CurrentPage, TotalPages, FilteredUpdates.Count);

    public UpdatesViewModel(
        IWsusService wsusService,
        ILoggingService loggingService,
        INotificationService notificationService,
        IExportService exportService,
        IFileDialogService fileDialogService,
        IFilterPresetsService filterPresetsService,
        IDialogService dialogService)
    {
        _wsusService = wsusService;
        _loggingService = loggingService;
        _notificationService = notificationService;
        _exportService = exportService;
        _fileDialogService = fileDialogService;
        _filterPresetsService = filterPresetsService;
        _dialogService = dialogService;

        LoadFilterPresets();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedClassificationChanged(string value) => ApplyFilters();
    partial void OnSelectedApprovalFilterChanged(string value) => ApplyFilters();
    partial void OnSelectedSupersededFilterChanged(string value) => ApplyFilters();

    partial void OnSelectedFilterPresetChanged(FilterPreset? value)
    {
        if (value == null) return;

        SearchText = value.SearchText;
        SelectedClassification = string.IsNullOrEmpty(value.Classification)
            ? Resources.FilterAll
            : value.Classification;
        SelectedApprovalFilter = value.ApprovalFilter;
        SelectedSupersededFilter = string.IsNullOrEmpty(value.SupersededFilter) || value.SupersededFilter == "All"
            ? Resources.FilterSupersededAll
            : value.SupersededFilter;

        ApplyFilters();
    }

    private void LoadFilterPresets()
    {
        var presets = _filterPresetsService.GetPresets();
        FilterPresets = new ObservableCollection<FilterPreset>(presets);

        // Default to "All Updates" preset
        SelectedFilterPreset = FilterPresets.FirstOrDefault(p =>
            p.Name == Resources.PresetAllUpdates) ?? FilterPresets.FirstOrDefault();
    }

    [RelayCommand]
    private async Task LoadUpdatesAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        StatusMessage = Resources.StatusLoading;

        try
        {
            var updates = await _wsusService.GetUpdatesAsync(cancellationToken);

            _allUpdates.Clear();
            foreach (var update in updates)
            {
                _allUpdates.Add(update);
            }

            ApplyFilters();

            Classifications = new ObservableCollection<string>(
                new[] { Resources.FilterAll }.Concat(updates.Select(u => u.Classification).Distinct().OrderBy(c => c)));

            // Load computer groups for target group selection
            var groups = await _wsusService.GetGroupsAsync(cancellationToken);
            ComputerGroups = new ObservableCollection<ComputerGroup>(groups.OrderBy(g => g.Name));

            StatusMessage = string.Format(Resources.StatusUpdatesLoaded, _allUpdates.Count);

            await _loggingService.LogInfoAsync($"Loaded {_allUpdates.Count} updates");
        }
        catch (OperationCanceledException)
        {
            StatusMessage = Resources.StatusCancelled;
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Resources.StatusError, ex.Message);
            await _loggingService.LogErrorAsync($"Failed to load updates: {ex.Message}", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allUpdates.Where(FilterUpdate).ToList();
        FilteredUpdates = new ObservableCollection<WsusUpdate>(filtered);
        CurrentPage = 1;
        TotalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PageSize));
        OnPropertyChanged(nameof(PaginationInfo));
    }

    private bool FilterUpdate(WsusUpdate update)
    {
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLowerInvariant();
            if (!update.Title.ToLowerInvariant().Contains(search) &&
                !update.KbArticle.ToLowerInvariant().Contains(search) &&
                !(update.Description?.ToLowerInvariant().Contains(search) ?? false))
            {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(SelectedClassification) &&
            SelectedClassification != Resources.FilterAll &&
            update.Classification != SelectedClassification)
            return false;

        if (SelectedApprovalFilter == "Approved" && !update.IsApproved) return false;
        if (SelectedApprovalFilter == "Unapproved" && update.IsApproved) return false;
        if (SelectedApprovalFilter == "Declined" && !update.IsDeclined) return false;

        if (SelectedSupersededFilter == Resources.FilterSupersededOnly && !update.IsSuperseded) return false;
        if (SelectedSupersededFilter == Resources.FilterHideSuperseded && update.IsSuperseded) return false;

        return true;
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedClassification = Resources.FilterAll;
        SelectedApprovalFilter = "All";
        SelectedSupersededFilter = Resources.FilterSupersededAll;
        SelectedFilterPreset = FilterPresets.FirstOrDefault(p =>
            p.Name == Resources.PresetAllUpdates) ?? FilterPresets.FirstOrDefault();
        ApplyFilters();
    }

    [RelayCommand]
    private void ApplySearch()
    {
        ApplyFilters();
    }

    [RelayCommand]
    private async Task SaveFilterPresetAsync()
    {
        var presetName = await _dialogService.ShowInputDialogAsync(
            Resources.DialogSavePreset,
            Resources.LblPresetName);

        if (string.IsNullOrWhiteSpace(presetName)) return;

        var preset = new FilterPreset
        {
            Name = presetName,
            SearchText = SearchText,
            Classification = SelectedClassification,
            ApprovalFilter = SelectedApprovalFilter,
            SupersededFilter = SelectedSupersededFilter,
            IsBuiltIn = false
        };

        await _filterPresetsService.SavePresetAsync(preset);
        LoadFilterPresets();
        _notificationService.ShowToast(Resources.StatusPresetSaved, ToastType.Success);
    }

    [RelayCommand]
    private void ApplyPreset(string presetName)
    {
        var preset = FilterPresets.FirstOrDefault(p =>
            p.Name.Contains(presetName, StringComparison.OrdinalIgnoreCase));
        if (preset != null)
        {
            SelectedFilterPreset = preset;
        }
    }

    private bool CanApproveUpdate() => SelectedUpdate is not null && SelectedComputerGroup is not null && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanApproveUpdate))]
    private async Task ApproveUpdateAsync(CancellationToken cancellationToken)
    {
        if (SelectedUpdate is null || SelectedComputerGroup is null) return;

        var confirmed = await _notificationService.ShowConfirmationAsync(
            Resources.DialogConfirm,
            string.Format(Resources.ConfirmApproveUpdate, SelectedUpdate.Title, SelectedComputerGroup.Name));

        if (!confirmed) return;

        IsLoading = true;
        StatusMessage = Resources.StatusApproving;

        try
        {
            await _wsusService.ApproveUpdateAsync(SelectedUpdate.Id, SelectedComputerGroup.Id, cancellationToken);
            SelectedUpdate.IsApproved = true;
            StatusMessage = string.Format(Resources.StatusApproved, SelectedComputerGroup.Name);

            _notificationService.ShowToast(
                string.Format(Resources.ToastApproveSuccess, 1),
                ToastType.Success);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Resources.StatusError, ex.Message);
            await _notificationService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanDeclineUpdate() => SelectedUpdate is not null && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanDeclineUpdate))]
    private async Task DeclineUpdateAsync(CancellationToken cancellationToken)
    {
        if (SelectedUpdate is null) return;

        var confirmed = await _notificationService.ShowConfirmationAsync(
            Resources.DialogConfirm,
            string.Format(Resources.ConfirmDeclineUpdate, SelectedUpdate.Title));

        if (!confirmed) return;

        IsLoading = true;
        StatusMessage = Resources.StatusDeclining;

        try
        {
            await _wsusService.DeclineUpdateAsync(SelectedUpdate.Id, cancellationToken);
            SelectedUpdate.IsDeclined = true;
            StatusMessage = Resources.StatusDeclined;

            _notificationService.ShowToast(
                string.Format(Resources.ToastDeclineSuccess, 1),
                ToastType.Success);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Resources.StatusError, ex.Message);
            await _notificationService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanBulkApprove() => SelectedUpdates.Count > 0 && SelectedComputerGroup is not null && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanBulkApprove))]
    private async Task BulkApproveAsync(CancellationToken cancellationToken)
    {
        if (SelectedComputerGroup is null) return;

        var confirmed = await _notificationService.ShowConfirmationAsync(
            Resources.DialogConfirm,
            string.Format(Resources.ConfirmBulkApprove, SelectedUpdates.Count, SelectedComputerGroup.Name));

        if (!confirmed) return;

        IsBulkOperationRunning = true;
        var succeeded = 0;
        var failed = 0;

        try
        {
            var total = SelectedUpdates.Count;
            var current = 0;

            foreach (var update in SelectedUpdates.ToList())
            {
                cancellationToken.ThrowIfCancellationRequested();

                current++;
                BulkProgress = current * 100d / total;
                BulkProgressText = string.Format(Resources.StatusBulkOperation, current, total);

                try
                {
                    await _wsusService.ApproveUpdateAsync(update.Id, SelectedComputerGroup.Id, cancellationToken);
                    update.IsApproved = true;
                    succeeded++;
                }
                catch
                {
                    failed++;
                }
            }

            StatusMessage = string.Format(Resources.StatusBulkComplete, succeeded, failed);
            _notificationService.ShowToast(
                string.Format(Resources.ToastApproveSuccess, succeeded),
                failed == 0 ? ToastType.Success : ToastType.Warning);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = Resources.StatusCancelled;
        }
        finally
        {
            IsBulkOperationRunning = false;
        }
    }

    private bool CanBulkDecline() => SelectedUpdates.Count > 0 && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanBulkDecline))]
    private async Task BulkDeclineAsync(CancellationToken cancellationToken)
    {
        var confirmed = await _notificationService.ShowConfirmationAsync(
            Resources.DialogConfirm,
            string.Format(Resources.ConfirmBulkDecline, SelectedUpdates.Count));

        if (!confirmed) return;

        IsBulkOperationRunning = true;
        var succeeded = 0;
        var failed = 0;

        try
        {
            var total = SelectedUpdates.Count;
            var current = 0;

            foreach (var update in SelectedUpdates.ToList())
            {
                cancellationToken.ThrowIfCancellationRequested();

                current++;
                BulkProgress = current * 100d / total;
                BulkProgressText = string.Format(Resources.StatusBulkOperation, current, total);

                try
                {
                    await _wsusService.DeclineUpdateAsync(update.Id, cancellationToken);
                    update.IsDeclined = true;
                    succeeded++;
                }
                catch
                {
                    failed++;
                }
            }

            StatusMessage = string.Format(Resources.StatusBulkComplete, succeeded, failed);
            _notificationService.ShowToast(
                string.Format(Resources.ToastDeclineSuccess, succeeded),
                failed == 0 ? ToastType.Success : ToastType.Warning);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = Resources.StatusCancelled;
        }
        finally
        {
            IsBulkOperationRunning = false;
        }
    }

    [RelayCommand]
    private async Task DeclineAllSupersededAsync(CancellationToken cancellationToken)
    {
        var superseded = _allUpdates.Where(u => u.IsSuperseded && !u.IsDeclined).ToList();

        if (superseded.Count == 0)
        {
            await _notificationService.ShowInfoAsync(Resources.DialogInfo, Resources.ErrorNoSupersededUpdates);
            return;
        }

        var confirmed = await _notificationService.ShowConfirmationAsync(
            Resources.DialogConfirm,
            string.Format(Resources.ConfirmDeclineSuperseded, superseded.Count));

        if (!confirmed) return;

        IsBulkOperationRunning = true;
        var succeeded = 0;
        var failed = 0;

        try
        {
            var total = superseded.Count;
            var current = 0;

            foreach (var update in superseded)
            {
                cancellationToken.ThrowIfCancellationRequested();

                current++;
                BulkProgress = current * 100d / total;
                BulkProgressText = string.Format(Resources.ProgressDecliningCount, current, total);

                try
                {
                    await _wsusService.DeclineUpdateAsync(update.Id, cancellationToken);
                    update.IsDeclined = true;
                    succeeded++;
                }
                catch
                {
                    failed++;
                }
            }

            StatusMessage = string.Format(Resources.StatusDeclinedSuperseded, succeeded, failed);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = Resources.StatusCancelled;
        }
        finally
        {
            IsBulkOperationRunning = false;
        }
    }

    [RelayCommand]
    private Task ApproveAllCriticalAsync()
    {
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task ApproveAllSecurityAsync()
    {
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ExportAsync(CancellationToken cancellationToken)
    {
        var filePath = _fileDialogService.ShowSaveFileDialog(
            Resources.ExportFilterCsv,
            ".csv",
            "updates_export");

        if (string.IsNullOrEmpty(filePath)) return;

        IsLoading = true;
        StatusMessage = Resources.StatusExporting;

        try
        {
            var updates = FilteredUpdates.ToList();
            var format = filePath.EndsWith(".json") ? ExportFormat.Json :
                        filePath.EndsWith(".tsv") ? ExportFormat.Tsv : ExportFormat.Csv;

            await _exportService.ExportUpdatesAsync(updates, filePath, format);

            StatusMessage = string.Format(Resources.StatusExportComplete, filePath);
            _notificationService.ShowToast(Resources.StatusExportComplete, ToastType.Success);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Resources.StatusError, ex.Message);
            await _notificationService.ShowErrorAsync(Resources.DialogError, ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ViewUpdateDetails()
    {
        if (SelectedUpdate is null) return;
        SelectedUpdateDetails = new UpdateDetails
        {
            Description = SelectedUpdate.Description ?? string.Empty
        };
        OnViewDetailsRequested?.Invoke(this, SelectedUpdate);
    }

    [RelayCommand]
    private void CopyKbArticle()
    {
        if (SelectedUpdate is null) return;
        OnCopyToClipboardRequested?.Invoke(this, SelectedUpdate.KbArticle);
    }

    [RelayCommand]
    private void SearchKbOnline()
    {
        if (SelectedUpdate is null || string.IsNullOrEmpty(SelectedUpdate.KbArticle)) return;
        var url = $"https://support.microsoft.com/kb/{SelectedUpdate.KbArticle.Replace("KB", "")}";
        OnOpenUrlRequested?.Invoke(this, url);
    }

    [RelayCommand]
    private void FirstPage()
    {
        CurrentPage = 1;
        OnPropertyChanged(nameof(PaginationInfo));
    }

    [RelayCommand]
    private void PreviousPage()
    {
        CurrentPage = Math.Max(1, CurrentPage - 1);
        OnPropertyChanged(nameof(PaginationInfo));
    }

    [RelayCommand]
    private void NextPage()
    {
        CurrentPage = Math.Min(TotalPages, CurrentPage + 1);
        OnPropertyChanged(nameof(PaginationInfo));
    }

    [RelayCommand]
    private void LastPage()
    {
        CurrentPage = TotalPages;
        OnPropertyChanged(nameof(PaginationInfo));
    }

    public event EventHandler<WsusUpdate>? OnViewDetailsRequested;
    public event EventHandler<string>? OnCopyToClipboardRequested;
    public event EventHandler<string>? OnOpenUrlRequested;
}
