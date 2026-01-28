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

public partial class ConnectionViewModel : ObservableObject
{
    private readonly IWsusService _wsusService;
    private readonly ILoggingService _loggingService;
    private readonly IConfigurationService _configService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private string _serverName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private int _port = 8530;

    [ObservableProperty]
    private bool _useSsl;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isConnecting;

    [ObservableProperty]
    private string _serverVersion = string.Empty;

    [ObservableProperty]
    private string _connectionStatus = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ServerPreset> _serverPresets = new();

    [ObservableProperty]
    private ServerPreset? _selectedPreset;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private UserIdentity? _currentUser;

    public ConnectionViewModel(
        IWsusService wsusService,
        ILoggingService loggingService,
        IConfigurationService configService,
        INotificationService notificationService)
    {
        _wsusService = wsusService;
        _loggingService = loggingService;
        _configService = configService;
        _notificationService = notificationService;

        LoadDefaults();
        LoadPresets();
    }

    private void LoadDefaults()
    {
        var defaults = _configService.WsusConnection;
        ServerName = defaults.ServerName;
        Port = defaults.Port;
        UseSsl = defaults.UseSsl;
    }

    private void LoadPresets()
    {
        // Load saved server presets from preferences
    }

    partial void OnSelectedPresetChanged(ServerPreset? value)
    {
        if (value is not null)
        {
            ServerName = value.ServerName;
            Port = value.Port;
            UseSsl = value.UseSsl;
        }
    }

    private bool CanConnect() => !string.IsNullOrWhiteSpace(ServerName) && Port > 0 && !IsConnected && !IsConnecting;

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        IsConnecting = true;
        ConnectionStatus = Resources.StatusConnecting;

        try
        {
            var result = await _wsusService.ConnectAsync(ServerName, Port, UseSsl, cancellationToken);

            if (result.Success)
            {
                IsConnected = true;
                ServerVersion = result.ServerVersion;
                ConnectionStatus = string.Format(Resources.StatusConnected, ServerName);
                await _loggingService.LogInfoAsync($"Connected to {ServerName}:{Port}");

                OnConnected?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ConnectionStatus = string.Format(Resources.StatusError, result.ErrorMessage);
                await _notificationService.ShowErrorAsync(Resources.DialogError, result.ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            ConnectionStatus = Resources.StatusCancelled;
        }
        catch (Exception ex)
        {
            ConnectionStatus = string.Format(Resources.StatusError, ex.Message);
            await _loggingService.LogErrorAsync($"Connection failed: {ex.Message}", ex);
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private bool CanDisconnect() => IsConnected && !IsConnecting;

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private async Task DisconnectAsync()
    {
        var confirmed = await _notificationService.ShowConfirmationAsync(
            Resources.DialogConfirm,
            Resources.ConfirmDisconnect);

        if (!confirmed) return;

        _wsusService.Disconnect();
        IsConnected = false;
        ServerVersion = string.Empty;
        ConnectionStatus = Resources.StatusDisconnected;

        OnDisconnected?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private Task SavePresetAsync()
    {
        var preset = new ServerPreset
        {
            Name = ServerName,
            ServerName = ServerName,
            Port = Port,
            UseSsl = UseSsl
        };

        ServerPresets.Add(preset);
        // Save to preferences
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void DeletePreset(ServerPreset preset)
    {
        ServerPresets.Remove(preset);
        // Save to preferences
    }

    public event EventHandler? OnConnected;
    public event EventHandler? OnDisconnected;
}
