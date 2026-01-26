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

using System.Diagnostics;

namespace WsusCommander.Services;

/// <summary>
/// Bulk operations service implementation.
/// </summary>
public sealed class BulkOperationService : IBulkOperationService
{
    private readonly IPowerShellService _powerShellService;
    private readonly ILoggingService _loggingService;
    private readonly int _maxParallelOperations;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkOperationService"/> class.
    /// </summary>
    public BulkOperationService(
        IPowerShellService powerShellService,
        ILoggingService loggingService,
        int maxParallelOperations = 5)
    {
        _powerShellService = powerShellService;
        _loggingService = loggingService;
        _maxParallelOperations = maxParallelOperations;
    }

    /// <inheritdoc/>
    public async Task<BulkOperationResult> ApproveUpdatesAsync(
        IEnumerable<Guid> updateIds,
        Guid targetGroupId,
        IProgress<BulkOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var ids = updateIds.ToList();
        await _loggingService.LogInfoAsync($"Bulk approve: {ids.Count} updates for group {targetGroupId}");

        return await ExecuteBulkOperationAsync(
            ids,
            async (id, ct) =>
            {
                await _powerShellService.ExecuteScriptAsync(
                    "Approve-WsusUpdate.ps1",
                    new Dictionary<string, object>
                    {
                        ["UpdateId"] = id.ToString(),
                        ["GroupId"] = targetGroupId.ToString()
                    });
            },
            id => id.ToString(),
            progress,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<BulkOperationResult> DeclineUpdatesAsync(
        IEnumerable<Guid> updateIds,
        IProgress<BulkOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var ids = updateIds.ToList();
        await _loggingService.LogInfoAsync($"Bulk decline: {ids.Count} updates");

        return await ExecuteBulkOperationAsync(
            ids,
            async (id, ct) =>
            {
                await _powerShellService.ExecuteScriptAsync(
                    "Decline-WsusUpdate.ps1",
                    new Dictionary<string, object>
                    {
                        ["UpdateId"] = id.ToString()
                    });
            },
            id => id.ToString(),
            progress,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<BulkOperationResult> UnapproveUpdatesAsync(
        IEnumerable<Guid> updateIds,
        Guid targetGroupId,
        IProgress<BulkOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var ids = updateIds.ToList();
        await _loggingService.LogInfoAsync($"Bulk unapprove: {ids.Count} updates from group {targetGroupId}");

        return await ExecuteBulkOperationAsync(
            ids,
            async (id, ct) =>
            {
                await _powerShellService.ExecuteScriptAsync(
                    "Unapprove-WsusUpdate.ps1",
                    new Dictionary<string, object>
                    {
                        ["UpdateId"] = id.ToString(),
                        ["GroupId"] = targetGroupId.ToString()
                    });
            },
            id => id.ToString(),
            progress,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<BulkOperationResult> MoveComputersToGroupAsync(
        IEnumerable<string> computerIds,
        Guid targetGroupId,
        IProgress<BulkOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var ids = computerIds.ToList();
        await _loggingService.LogInfoAsync($"Bulk move: {ids.Count} computers to group {targetGroupId}");

        return await ExecuteBulkOperationAsync(
            ids,
            async (id, ct) =>
            {
                await _powerShellService.ExecuteScriptAsync(
                    "Move-ComputerToGroup.ps1",
                    new Dictionary<string, object>
                    {
                        ["ComputerId"] = id,
                        ["TargetGroupId"] = targetGroupId.ToString()
                    });
            },
            id => id,
            progress,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<BulkOperationResult> RemoveComputersFromGroupAsync(
        IEnumerable<string> computerIds,
        Guid groupId,
        IProgress<BulkOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var ids = computerIds.ToList();
        await _loggingService.LogInfoAsync($"Bulk remove: {ids.Count} computers from group {groupId}");

        return await ExecuteBulkOperationAsync(
            ids,
            async (id, ct) =>
            {
                await _powerShellService.ExecuteScriptAsync(
                    "Remove-ComputerFromGroup.ps1",
                    new Dictionary<string, object>
                    {
                        ["ComputerId"] = id,
                        ["GroupId"] = groupId.ToString()
                    });
            },
            id => id,
            progress,
            cancellationToken);
    }

    private async Task<BulkOperationResult> ExecuteBulkOperationAsync<T>(
        List<T> items,
        Func<T, CancellationToken, Task> operation,
        Func<T, string> getItemId,
        IProgress<BulkOperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var errors = new List<BulkOperationError>();
        var completedCount = 0;
        var failedCount = 0;
        var semaphore = new SemaphoreSlim(_maxParallelOperations);

        var tasks = items.Select(async item =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var itemId = getItemId(item);
                progress?.Report(new BulkOperationProgress
                {
                    TotalCount = items.Count,
                    CompletedCount = completedCount,
                    FailedCount = failedCount,
                    CurrentItem = itemId
                });

                await operation(item, cancellationToken);
                Interlocked.Increment(ref completedCount);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failedCount);
                lock (errors)
                {
                    errors.Add(new BulkOperationError
                    {
                        ItemId = getItemId(item),
                        ErrorMessage = ex.Message,
                        Exception = ex
                    });
                }
                await _loggingService.LogWarningAsync($"Bulk operation failed for {getItemId(item)}: {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        });

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            await _loggingService.LogWarningAsync("Bulk operation cancelled");
        }

        sw.Stop();

        var result = new BulkOperationResult
        {
            Success = failedCount == 0,
            TotalCount = items.Count,
            SuccessCount = completedCount,
            FailedCount = failedCount,
            Errors = errors,
            Duration = sw.Elapsed
        };

        progress?.Report(new BulkOperationProgress
        {
            TotalCount = items.Count,
            CompletedCount = completedCount,
            FailedCount = failedCount
        });

        await _loggingService.LogInfoAsync(
            $"Bulk operation completed: {completedCount}/{items.Count} succeeded, {failedCount} failed in {sw.Elapsed.TotalSeconds:F1}s");

        return result;
    }
}
