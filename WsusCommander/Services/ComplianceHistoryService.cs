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
/// In-memory compliance history service.
/// </summary>
public sealed class ComplianceHistoryService : IComplianceHistoryService
{
    private readonly List<ComplianceSnapshot> _snapshots = [];

    /// <inheritdoc/>
    public Task<List<ComplianceSnapshot>> GetHistoryAsync(int days, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.Now.AddDays(-days);
        var history = _snapshots
            .Where(snapshot => snapshot.Timestamp >= cutoff)
            .OrderBy(snapshot => snapshot.Timestamp)
            .ToList();

        return Task.FromResult(history);
    }

    /// <inheritdoc/>
    public Task SaveSnapshotAsync(ComplianceSnapshot snapshot)
    {
        _snapshots.Add(snapshot);
        return Task.CompletedTask;
    }
}
