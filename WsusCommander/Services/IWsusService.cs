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
    /// Gets dashboard statistics with optional filters.
    /// </summary>
    /// <param name="groupId">Optional group ID to filter computers.</param>
    /// <param name="namePattern">Optional wildcard pattern to filter computer names (e.g., "*SRV*", "PADSEC*").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<DashboardStats> GetDashboardStatsAsync(string? groupId, string? namePattern, CancellationToken cancellationToken);

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
    /// Gets computer statuses, optionally excluding unassigned computers.
    /// </summary>
    /// <param name="excludeUnassigned">If true, excludes computers in the "Unassigned Computers" group.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<ComputerStatus>> GetComputersAsync(bool excludeUnassigned, CancellationToken cancellationToken);

    /// <summary>
    /// Gets staging computers (computers in the "Unassigned Computers" group).
    /// </summary>
    Task<IReadOnlyList<ComputerStatus>> GetStagingComputersAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets computer groups.
    /// </summary>
    Task<IReadOnlyList<ComputerGroup>> GetGroupsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Starts a synchronization.
    /// </summary>
    Task<SyncStatus> StartSyncAsync(CancellationToken cancellationToken);
}
