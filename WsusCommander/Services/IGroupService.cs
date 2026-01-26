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
/// Options for creating a computer group.
/// </summary>
public sealed class CreateGroupOptions
{
    /// <summary>
    /// Gets or sets the group name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the group description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the parent group ID.
    /// </summary>
    public Guid? ParentGroupId { get; init; }
}

/// <summary>
/// Options for updating a computer group.
/// </summary>
public sealed class UpdateGroupOptions
{
    /// <summary>
    /// Gets or sets the new group name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or sets the new description.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Interface for computer group management service.
/// </summary>
public interface IGroupService
{
    /// <summary>
    /// Gets all computer groups.
    /// </summary>
    /// <param name="includeSystemGroups">Whether to include system groups.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of computer groups.</returns>
    Task<IReadOnlyList<ComputerGroup>> GetAllGroupsAsync(
        bool includeSystemGroups = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific computer group by ID.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Computer group or null if not found.</returns>
    Task<ComputerGroup?> GetGroupAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new computer group.
    /// </summary>
    /// <param name="options">Create options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created computer group.</returns>
    Task<ComputerGroup> CreateGroupAsync(
        CreateGroupOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing computer group.
    /// </summary>
    /// <param name="groupId">Group ID to update.</param>
    /// <param name="options">Update options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated computer group.</returns>
    Task<ComputerGroup> UpdateGroupAsync(
        Guid groupId,
        UpdateGroupOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a computer group.
    /// </summary>
    /// <param name="groupId">Group ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteGroupAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets computers in a group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of computers in the group.</returns>
    Task<IReadOnlyList<ComputerStatus>> GetGroupComputersAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets child groups of a group.
    /// </summary>
    /// <param name="parentGroupId">Parent group ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of child groups.</returns>
    Task<IReadOnlyList<ComputerGroup>> GetChildGroupsAsync(
        Guid parentGroupId,
        CancellationToken cancellationToken = default);
}
