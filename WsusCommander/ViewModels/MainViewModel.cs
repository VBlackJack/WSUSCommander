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

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WsusCommander.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    public ConnectionViewModel Connection { get; }
    public DashboardViewModel Dashboard { get; }
    public UpdatesViewModel Updates { get; }
    public ComputersViewModel Computers { get; }
    public GroupsViewModel Groups { get; }
    public ReportsViewModel Reports { get; }
    public RulesViewModel Rules { get; }
    public ActivityViewModel Activity { get; }
    public SettingsViewModel Settings { get; }

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public MainViewModel(
        ConnectionViewModel connection,
        DashboardViewModel dashboard,
        UpdatesViewModel updates,
        ComputersViewModel computers,
        GroupsViewModel groups,
        ReportsViewModel reports,
        RulesViewModel rules,
        ActivityViewModel activity,
        SettingsViewModel settings)
    {
        Connection = connection;
        Dashboard = dashboard;
        Updates = updates;
        Computers = computers;
        Groups = groups;
        Reports = reports;
        Rules = rules;
        Activity = activity;
        Settings = settings;

        Connection.OnConnected += async (_, _) => await OnConnectedAsync();
        Connection.OnDisconnected += (_, _) => OnDisconnected();

        Dashboard.OnNavigateToUpdatesRequested += (_, _) => SelectedTabIndex = 1;
        Dashboard.OnNavigateToComputersRequested += (_, _) => SelectedTabIndex = 2;
        Dashboard.OnOpenReportsRequested += (_, _) => SelectedTabIndex = 3;
    }

    private async Task OnConnectedAsync()
    {
        IsBusy = true;
        StatusMessage = string.Empty;

        await Task.WhenAll(
            Dashboard.LoadDashboardCommand.ExecuteAsync(null),
            Updates.LoadUpdatesCommand.ExecuteAsync(null),
            Computers.LoadComputerStatusesCommand.ExecuteAsync(null),
            Groups.LoadGroupsCommand.ExecuteAsync(null));

        IsBusy = false;
    }

    private void OnDisconnected()
    {
        StatusMessage = string.Empty;
    }

    public void Dispose()
    {
        if (Connection is IDisposable disposableConnection)
        {
            disposableConnection.Dispose();
        }

        if (Dashboard is IDisposable disposableDashboard)
        {
            disposableDashboard.Dispose();
        }

        if (Updates is IDisposable disposableUpdates)
        {
            disposableUpdates.Dispose();
        }

        if (Computers is IDisposable disposableComputers)
        {
            disposableComputers.Dispose();
        }

        if (Groups is IDisposable disposableGroups)
        {
            disposableGroups.Dispose();
        }

        if (Reports is IDisposable disposableReports)
        {
            disposableReports.Dispose();
        }

        if (Rules is IDisposable disposableRules)
        {
            disposableRules.Dispose();
        }

        if (Activity is IDisposable disposableActivity)
        {
            disposableActivity.Dispose();
        }

        if (Settings is IDisposable disposableSettings)
        {
            disposableSettings.Dispose();
        }
    }
}
