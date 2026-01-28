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

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using WsusCommander.Models;

namespace WsusCommander.Services;

/// <summary>
/// Service responsible for loading and providing application configuration.
/// </summary>
public sealed class ConfigurationService : IConfigurationService
{
    private readonly AppConfig _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationService"/> class.
    /// </summary>
    public ConfigurationService()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        _config = new AppConfig();
        configuration.Bind(_config);

        ValidateConfiguration();
    }

    /// <inheritdoc/>
    public AppConfig Config => _config;

    /// <inheritdoc/>
    public WsusConnectionConfig WsusConnection => _config.WsusConnection;

    /// <inheritdoc/>
    public AppSettingsConfig AppSettings => _config.AppSettings;

    /// <inheritdoc/>
    public EmailConfig Email => _config.Email;

    private void ValidateConfiguration()
    {
        var validationResults = new List<ValidationResult>();
        var configSections = new object[]
        {
            _config.WsusConnection,
            _config.AppSettings,
            _config.Security,
            _config.Logging,
            _config.Performance,
            _config.PowerShell,
            _config.UI,
            _config.Email
        };

        foreach (var section in configSections)
        {
            var context = new ValidationContext(section);
            Validator.TryValidateObject(section, context, validationResults, validateAllProperties: true);
        }

        // Validate email config consistency
        if (_config.Email.Enabled)
        {
            if (string.IsNullOrWhiteSpace(_config.Email.SmtpServer))
            {
                validationResults.Add(new ValidationResult(
                    "SMTP server is required when email is enabled.",
                    new[] { nameof(EmailConfig.SmtpServer) }));
            }

            if (string.IsNullOrWhiteSpace(_config.Email.FromAddress))
            {
                validationResults.Add(new ValidationResult(
                    "From address is required when email is enabled.",
                    new[] { nameof(EmailConfig.FromAddress) }));
            }

            if (_config.Email.ToAddresses.Count == 0)
            {
                validationResults.Add(new ValidationResult(
                    "At least one recipient address is required when email is enabled.",
                    new[] { nameof(EmailConfig.ToAddresses) }));
            }
        }

        // Validate PowerShell executable path
        if (!string.IsNullOrWhiteSpace(_config.PowerShell.ExecutablePath) &&
            !System.IO.File.Exists(_config.PowerShell.ExecutablePath))
        {
            validationResults.Add(new ValidationResult(
                $"PowerShell executable not found: {_config.PowerShell.ExecutablePath}",
                new[] { nameof(PowerShellConfig.ExecutablePath) }));
        }

        if (validationResults.Count > 0)
        {
            var errors = string.Join(Environment.NewLine, validationResults.Select(r => r.ErrorMessage));
            throw new InvalidOperationException($"Configuration validation failed:{Environment.NewLine}{errors}");
        }
    }
}
