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
/// Role-based authorization service for WSUS operations.
/// </summary>
public sealed class AuthorizationService : IAuthorizationService
{
    private readonly IAuthenticationService _authService;
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Permission matrix defining which roles can perform which operations.
    /// </summary>
    private static readonly Dictionary<WsusOperation, UserRole[]> PermissionMatrix = new()
    {
        { WsusOperation.ViewUpdates, [UserRole.Viewer, UserRole.Operator, UserRole.Administrator] },
        { WsusOperation.ViewComputers, [UserRole.Viewer, UserRole.Operator, UserRole.Administrator] },
        { WsusOperation.ViewReports, [UserRole.Viewer, UserRole.Operator, UserRole.Administrator] },
        { WsusOperation.ExportData, [UserRole.Operator, UserRole.Administrator] },
        { WsusOperation.ApproveUpdate, [UserRole.Operator, UserRole.Administrator] },
        { WsusOperation.DeclineUpdate, [UserRole.Operator, UserRole.Administrator] },
        { WsusOperation.StartSync, [UserRole.Operator, UserRole.Administrator] },
        { WsusOperation.ManageGroups, [UserRole.Administrator] },
        { WsusOperation.ConfigureServer, [UserRole.Administrator] }
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationService"/> class.
    /// </summary>
    public AuthorizationService(IAuthenticationService authService, ILoggingService loggingService)
    {
        _authService = authService;
        _loggingService = loggingService;
    }

    /// <inheritdoc/>
    public bool IsAuthorized(WsusOperation operation)
    {
        var user = _authService.CurrentUser;

        if (user is null)
        {
            return false;
        }

        if (!PermissionMatrix.TryGetValue(operation, out var allowedRoles))
        {
            return false;
        }

        return allowedRoles.Contains(user.Role);
    }

    /// <inheritdoc/>
    public bool IsAuthorizedForGroup(WsusOperation operation, Guid groupId)
    {
        var user = _authService.CurrentUser;

        if (user is null)
        {
            return false;
        }

        // Administrators can access all groups
        if (user.IsAdministrator)
        {
            return IsAuthorized(operation);
        }

        // Operators must have the group in their allowed list
        if (user.Role == UserRole.Operator)
        {
            if (user.AllowedWsusGroups.Count == 0)
            {
                // No restrictions, can access all groups
                return IsAuthorized(operation);
            }

            return user.AllowedWsusGroups.Contains(groupId) && IsAuthorized(operation);
        }

        // Viewers can only view, not perform group-specific operations
        return operation is WsusOperation.ViewUpdates or WsusOperation.ViewComputers or WsusOperation.ViewReports;
    }

    /// <inheritdoc/>
    public void EnsureAuthorized(WsusOperation operation)
    {
        if (!IsAuthorized(operation))
        {
            var user = _authService.CurrentUser;
            var userName = user?.AccountName ?? "Anonymous";
            var message = $"User '{userName}' is not authorized to perform operation '{operation}'.";

            _loggingService.LogWarningAsync($"Authorization denied: {message}");

            throw new UnauthorizedAccessException(message);
        }
    }

    /// <inheritdoc/>
    public IEnumerable<WsusOperation> GetAuthorizedOperations()
    {
        return PermissionMatrix
            .Where(kv => IsAuthorized(kv.Key))
            .Select(kv => kv.Key);
    }
}
