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
/// Abstraction for WSUS operations.
/// </summary>
public interface IWsusService
{
    /// <summary>
    /// Connects to a WSUS server.
    /// </summary>
    Task<WsusConnectionResult> ConnectAsync(string serverName, int port, bool useSsl, CancellationToken cancellationToken);

    /// <summary>
    /// Disconnects from the WSUS server.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Gets dashboard statistics.
    /// </summary>
    Task<DashboardStats> GetDashboardStatsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the current health report.
    /// </summary>
    Task<HealthReport> GetHealthReportAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets updates from the server.
    /// </summary>
    Task<IReadOnlyList<WsusUpdate>> GetUpdatesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Approves an update for a group.
    /// </summary>
    Task ApproveUpdateAsync(Guid updateId, Guid groupId, CancellationToken cancellationToken);

    /// <summary>
    /// Declines an update.
    /// </summary>
    Task DeclineUpdateAsync(Guid updateId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets computer statuses.
    /// </summary>
    Task<IReadOnlyList<ComputerStatus>> GetComputersAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets computer groups.
    /// </summary>
    Task<IReadOnlyList<ComputerGroup>> GetGroupsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Starts a synchronization.
    /// </summary>
    Task<SyncStatus> StartSyncAsync(CancellationToken cancellationToken);
}
