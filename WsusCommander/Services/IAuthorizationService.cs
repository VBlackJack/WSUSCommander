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

namespace WsusCommander.Services;

/// <summary>
/// Defines the operations that can be authorized.
/// </summary>
public enum WsusOperation
{
    /// <summary>View updates list.</summary>
    ViewUpdates,

    /// <summary>Approve an update for a group.</summary>
    ApproveUpdate,

    /// <summary>Decline an update.</summary>
    DeclineUpdate,

    /// <summary>View computer statuses.</summary>
    ViewComputers,

    /// <summary>Manage computer groups.</summary>
    ManageGroups,

    /// <summary>Start synchronization.</summary>
    StartSync,

    /// <summary>Configure server settings.</summary>
    ConfigureServer,

    /// <summary>Export data.</summary>
    ExportData,

    /// <summary>View compliance reports.</summary>
    ViewReports
}

/// <summary>
/// Interface for role-based authorization service.
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Checks if the current user is authorized to perform the specified operation.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>True if authorized, false otherwise.</returns>
    bool IsAuthorized(WsusOperation operation);

    /// <summary>
    /// Checks if the current user is authorized to perform the operation on a specific group.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <param name="groupId">The target group identifier.</param>
    /// <returns>True if authorized, false otherwise.</returns>
    bool IsAuthorizedForGroup(WsusOperation operation, Guid groupId);

    /// <summary>
    /// Ensures the user is authorized, throwing an exception if not.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown if not authorized.</exception>
    void EnsureAuthorized(WsusOperation operation);

    /// <summary>
    /// Gets all operations the current user is authorized to perform.
    /// </summary>
    /// <returns>List of authorized operations.</returns>
    IEnumerable<WsusOperation> GetAuthorizedOperations();
}
