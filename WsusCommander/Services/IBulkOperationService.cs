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
/// Progress information for bulk operations.
/// </summary>
public sealed class BulkOperationProgress
{
    /// <summary>
    /// Gets or sets the total items count.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Gets or sets the completed items count.
    /// </summary>
    public int CompletedCount { get; init; }

    /// <summary>
    /// Gets or sets the failed items count.
    /// </summary>
    public int FailedCount { get; init; }

    /// <summary>
    /// Gets or sets the current item being processed.
    /// </summary>
    public string? CurrentItem { get; init; }

    /// <summary>
    /// Gets the progress percentage.
    /// </summary>
    public double ProgressPercent => TotalCount > 0 ? (double)CompletedCount / TotalCount * 100 : 0;
}

/// <summary>
/// Result of a bulk operation.
/// </summary>
public sealed class BulkOperationResult
{
    /// <summary>
    /// Gets or sets whether the operation succeeded overall.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets or sets the total items processed.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Gets or sets the successful items count.
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// Gets or sets the failed items count.
    /// </summary>
    public int FailedCount { get; init; }

    /// <summary>
    /// Gets or sets the errors for failed items.
    /// </summary>
    public IReadOnlyList<BulkOperationError> Errors { get; init; } = [];

    /// <summary>
    /// Gets or sets the operation duration.
    /// </summary>
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Error information for a failed bulk operation item.
/// </summary>
public sealed class BulkOperationError
{
    /// <summary>
    /// Gets or sets the item identifier.
    /// </summary>
    public required string ItemId { get; init; }

    /// <summary>
    /// Gets or sets the item name.
    /// </summary>
    public string? ItemName { get; init; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Gets or sets the exception if available.
    /// </summary>
    public Exception? Exception { get; init; }
}

/// <summary>
/// Interface for bulk operations service.
/// </summary>
public interface IBulkOperationService
{
    /// <summary>
    /// Approves multiple updates for a group.
    /// </summary>
    /// <param name="updateIds">Update IDs to approve.</param>
    /// <param name="targetGroupId">Target group ID.</param>
    /// <param name="progress">Progress callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Bulk operation result.</returns>
    Task<BulkOperationResult> ApproveUpdatesAsync(
        IEnumerable<Guid> updateIds,
        Guid targetGroupId,
        IProgress<BulkOperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Declines multiple updates.
    /// </summary>
    /// <param name="updateIds">Update IDs to decline.</param>
    /// <param name="progress">Progress callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Bulk operation result.</returns>
    Task<BulkOperationResult> DeclineUpdatesAsync(
        IEnumerable<Guid> updateIds,
        IProgress<BulkOperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes approval for multiple updates from a group.
    /// </summary>
    /// <param name="updateIds">Update IDs to unapprove.</param>
    /// <param name="targetGroupId">Target group ID.</param>
    /// <param name="progress">Progress callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Bulk operation result.</returns>
    Task<BulkOperationResult> UnapproveUpdatesAsync(
        IEnumerable<Guid> updateIds,
        Guid targetGroupId,
        IProgress<BulkOperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves computers to a different group.
    /// </summary>
    /// <param name="computerIds">Computer IDs to move.</param>
    /// <param name="targetGroupId">Target group ID.</param>
    /// <param name="progress">Progress callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Bulk operation result.</returns>
    Task<BulkOperationResult> MoveComputersToGroupAsync(
        IEnumerable<string> computerIds,
        Guid targetGroupId,
        IProgress<BulkOperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes computers from a group.
    /// </summary>
    /// <param name="computerIds">Computer IDs to remove.</param>
    /// <param name="groupId">Group ID to remove from.</param>
    /// <param name="progress">Progress callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Bulk operation result.</returns>
    Task<BulkOperationResult> RemoveComputersFromGroupAsync(
        IEnumerable<string> computerIds,
        Guid groupId,
        IProgress<BulkOperationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
