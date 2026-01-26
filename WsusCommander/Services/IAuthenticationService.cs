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
/// Interface for user authentication service.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Gets the currently authenticated user.
    /// </summary>
    UserIdentity? CurrentUser { get; }

    /// <summary>
    /// Gets a value indicating whether a user is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Authenticates the current Windows user.
    /// </summary>
    /// <returns>The authenticated user identity.</returns>
    Task<UserIdentity> AuthenticateAsync();

    /// <summary>
    /// Validates that the current user has the required group membership.
    /// </summary>
    /// <param name="requiredGroups">List of AD groups (any match is sufficient).</param>
    /// <returns>True if user is member of at least one required group.</returns>
    Task<bool> ValidateGroupMembershipAsync(IEnumerable<string> requiredGroups);

    /// <summary>
    /// Signs out the current user.
    /// </summary>
    void SignOut();
}
