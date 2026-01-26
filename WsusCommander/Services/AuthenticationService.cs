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

using System.DirectoryServices.AccountManagement;
using System.Security.Principal;
using WsusCommander.Models;

namespace WsusCommander.Services;

/// <summary>
/// Windows-based authentication service using current user identity.
/// </summary>
public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IConfigurationService _configService;
    private readonly ILoggingService _loggingService;
    private UserIdentity? _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationService"/> class.
    /// </summary>
    public AuthenticationService(IConfigurationService configService, ILoggingService loggingService)
    {
        _configService = configService;
        _loggingService = loggingService;
    }

    /// <inheritdoc/>
    public UserIdentity? CurrentUser => _currentUser;

    /// <inheritdoc/>
    public bool IsAuthenticated => _currentUser is not null;

    /// <inheritdoc/>
    public async Task<UserIdentity> AuthenticateAsync()
    {
        return await Task.Run(() =>
        {
            var windowsIdentity = WindowsIdentity.GetCurrent();

            if (windowsIdentity is null || !windowsIdentity.IsAuthenticated)
            {
                throw new InvalidOperationException("No authenticated Windows user found.");
            }

            var user = new UserIdentity
            {
                AccountName = windowsIdentity.Name,
                AuthenticatedAt = DateTime.UtcNow
            };

            // Get user details from Active Directory
            try
            {
                using var context = new PrincipalContext(ContextType.Domain);
                using var principal = UserPrincipal.FindByIdentity(context, windowsIdentity.Name);

                if (principal is not null)
                {
                    user.DisplayName = principal.DisplayName ?? windowsIdentity.Name;
                    user.Email = principal.EmailAddress ?? string.Empty;

                    // Get group memberships
                    var groups = principal.GetAuthorizationGroups();
                    foreach (var group in groups)
                    {
                        if (!string.IsNullOrEmpty(group.Name))
                        {
                            user.Groups.Add(group.Name);
                        }
                    }
                }
            }
            catch (PrincipalServerDownException)
            {
                // Domain controller not available, use local identity
                user.DisplayName = windowsIdentity.Name;
                _loggingService.LogWarningAsync("Domain controller unavailable, using local identity.");
            }
            catch (Exception ex)
            {
                _loggingService.LogWarningAsync($"Could not retrieve AD details: {ex.Message}");
                user.DisplayName = windowsIdentity.Name;
            }

            // Determine user role based on configuration
            user.Role = DetermineUserRole(user);

            _currentUser = user;
            _loggingService.LogInfoAsync($"User authenticated: {user.AccountName} as {user.Role}");

            return user;
        });
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateGroupMembershipAsync(IEnumerable<string> requiredGroups)
    {
        if (_currentUser is null)
        {
            return false;
        }

        return await Task.Run(() =>
        {
            var requiredSet = new HashSet<string>(requiredGroups, StringComparer.OrdinalIgnoreCase);
            return _currentUser.Groups.Any(g => requiredSet.Contains(g));
        });
    }

    /// <inheritdoc/>
    public void SignOut()
    {
        if (_currentUser is not null)
        {
            _loggingService.LogInfoAsync($"User signed out: {_currentUser.AccountName}");
            _currentUser = null;
        }
    }

    /// <summary>
    /// Determines the user role based on group membership and configuration.
    /// </summary>
    private UserRole DetermineUserRole(UserIdentity user)
    {
        var security = _configService.Config.Security;

        // Check for administrator groups
        if (security.AdministratorGroups.Any(g =>
            user.Groups.Contains(g, StringComparer.OrdinalIgnoreCase)))
        {
            return UserRole.Administrator;
        }

        // Check for operator groups
        if (security.OperatorGroups.Any(g =>
            user.Groups.Contains(g, StringComparer.OrdinalIgnoreCase)))
        {
            return UserRole.Operator;
        }

        // Default to viewer
        return UserRole.Viewer;
    }
}
