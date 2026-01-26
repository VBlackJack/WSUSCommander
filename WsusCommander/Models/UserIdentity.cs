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

namespace WsusCommander.Models;

/// <summary>
/// Represents the role of a user in the application.
/// </summary>
public enum UserRole
{
    /// <summary>Read-only access to view updates and statuses.</summary>
    Viewer,

    /// <summary>Can approve/decline updates for assigned groups.</summary>
    Operator,

    /// <summary>Full access to all operations.</summary>
    Administrator
}

/// <summary>
/// Represents an authenticated user's identity.
/// </summary>
public sealed class UserIdentity
{
    /// <summary>
    /// Gets or sets the user's Windows account name (DOMAIN\Username).
    /// </summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's role in the application.
    /// </summary>
    public UserRole Role { get; set; } = UserRole.Viewer;

    /// <summary>
    /// Gets or sets the list of AD groups the user belongs to.
    /// </summary>
    public List<string> Groups { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of WSUS group IDs the user can manage.
    /// </summary>
    public List<Guid> AllowedWsusGroups { get; set; } = [];

    /// <summary>
    /// Gets or sets the authentication timestamp.
    /// </summary>
    public DateTime AuthenticatedAt { get; set; }

    /// <summary>
    /// Gets a value indicating whether the user is an administrator.
    /// </summary>
    public bool IsAdministrator => Role == UserRole.Administrator;

    /// <summary>
    /// Gets a value indicating whether the user can approve updates.
    /// </summary>
    public bool CanApprove => Role is UserRole.Administrator or UserRole.Operator;
}
