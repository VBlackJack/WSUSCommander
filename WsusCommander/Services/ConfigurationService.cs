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
    }

    /// <inheritdoc/>
    public AppConfig Config => _config;

    /// <inheritdoc/>
    public WsusConnectionConfig WsusConnection => _config.WsusConnection;

    /// <inheritdoc/>
    public AppSettingsConfig AppSettings => _config.AppSettings;

    /// <inheritdoc/>
    public EmailConfig Email => _config.Email;
}
