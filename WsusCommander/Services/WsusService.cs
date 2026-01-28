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

using WsusCommander.Models;

namespace WsusCommander.Services;

/// <summary>
/// Placeholder WSUS service implementation.
/// </summary>
public sealed class WsusService : IWsusService
{
    /// <inheritdoc/>
    public Task<WsusConnectionResult> ConnectAsync(
        string serverName,
        int port,
        bool useSsl,
        CancellationToken cancellationToken)
    {
        var result = new WsusConnectionResult
        {
            Success = true,
            ServerVersion = "Unknown"
        };

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public void Disconnect()
    {
    }

    /// <inheritdoc/>
    public Task<DashboardStats> GetDashboardStatsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new DashboardStats());
    }

    /// <inheritdoc/>
    public Task<HealthReport> GetHealthReportAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new HealthReport { Status = HealthStatus.Healthy });
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<WsusUpdate>> GetUpdatesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<WsusUpdate>>(Array.Empty<WsusUpdate>());
    }

    /// <inheritdoc/>
    public Task ApproveUpdateAsync(Guid updateId, Guid groupId, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DeclineUpdateAsync(Guid updateId, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ComputerStatus>> GetComputersAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<ComputerStatus>>(Array.Empty<ComputerStatus>());
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ComputerGroup>> GetGroupsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<ComputerGroup>>(Array.Empty<ComputerGroup>());
    }

    /// <inheritdoc/>
    public Task<SyncStatus> StartSyncAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new SyncStatus
        {
            Status = "Syncing",
            LastSyncTime = DateTime.Now,
            IsSyncing = true
        });
    }
}
