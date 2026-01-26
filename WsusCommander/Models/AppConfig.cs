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

namespace WsusCommander.Models;

/// <summary>
/// Root configuration model mapping the appsettings.json structure.
/// </summary>
public sealed class AppConfig
{
    /// <summary>
    /// Gets or sets the WSUS connection configuration.
    /// </summary>
    public WsusConnectionConfig WsusConnection { get; set; } = new();

    /// <summary>
    /// Gets or sets the application settings configuration.
    /// </summary>
    public AppSettingsConfig AppSettings { get; set; } = new();

    /// <summary>
    /// Gets or sets the security configuration.
    /// </summary>
    public SecurityConfig Security { get; set; } = new();

    /// <summary>
    /// Gets or sets the logging configuration.
    /// </summary>
    public LoggingConfig Logging { get; set; } = new();

    /// <summary>
    /// Gets or sets the performance configuration.
    /// </summary>
    public PerformanceConfig Performance { get; set; } = new();

    /// <summary>
    /// Gets or sets the UI configuration.
    /// </summary>
    public UiConfig UI { get; set; } = new();
}

/// <summary>
/// Configuration model for WSUS server connection settings.
/// </summary>
public sealed class WsusConnectionConfig
{
    /// <summary>
    /// Gets or sets the WSUS server name or IP address.
    /// </summary>
    [Required(ErrorMessage = "Server name is required.")]
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the WSUS server port.
    /// </summary>
    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
    public int Port { get; set; } = 8530;

    /// <summary>
    /// Gets or sets a value indicating whether to use SSL for the connection.
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to validate server certificates.
    /// </summary>
    public bool ValidateCertificate { get; set; } = true;

    /// <summary>
    /// Gets or sets the expected certificate thumbprint for pinning.
    /// </summary>
    public string? CertificateThumbprint { get; set; }

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    [Range(5, 300, ErrorMessage = "Timeout must be between 5 and 300 seconds.")]
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Configuration model for general application settings.
/// </summary>
public sealed class AppSettingsConfig
{
    /// <summary>
    /// Gets or sets the auto-refresh interval in seconds.
    /// </summary>
    [Range(30, 3600, ErrorMessage = "Auto-refresh interval must be between 30 and 3600 seconds.")]
    public int AutoRefreshInterval { get; set; } = 300;

    /// <summary>
    /// Gets or sets the path for application logs.
    /// </summary>
    public string LogPath { get; set; } = @"C:\ProgramData\WsusCommander\logs";

    /// <summary>
    /// Gets or sets the path for application data (cache, preferences).
    /// </summary>
    public string DataPath { get; set; } = @"C:\ProgramData\WsusCommander\data";

    /// <summary>
    /// Gets or sets the maximum number of updates to retrieve per query.
    /// </summary>
    [Range(10, 1000, ErrorMessage = "Max results must be between 10 and 1000.")]
    public int MaxResultsPerQuery { get; set; } = 100;
}

/// <summary>
/// Configuration model for security settings.
/// </summary>
public sealed class SecurityConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether authentication is required.
    /// </summary>
    public bool RequireAuthentication { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of AD groups that grant administrator access.
    /// </summary>
    public List<string> AdministratorGroups { get; set; } = ["WSUS Administrators", "Domain Admins"];

    /// <summary>
    /// Gets or sets the list of AD groups that grant operator access.
    /// </summary>
    public List<string> OperatorGroups { get; set; } = ["WSUS Operators"];

    /// <summary>
    /// Gets or sets a value indicating whether to require confirmation for approvals.
    /// </summary>
    public bool RequireApprovalConfirmation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to require confirmation for declines.
    /// </summary>
    public bool RequireDeclineConfirmation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to require confirmation for sync.
    /// </summary>
    public bool RequireSyncConfirmation { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to audit all operations.
    /// </summary>
    public bool AuditAllOperations { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to encrypt sensitive log data.
    /// </summary>
    public bool EncryptSensitiveLogs { get; set; } = false;
}

/// <summary>
/// Configuration model for logging settings.
/// </summary>
public sealed class LoggingConfig
{
    /// <summary>
    /// Gets or sets the minimum log level.
    /// </summary>
    public string Level { get; set; } = "Info";

    /// <summary>
    /// Gets or sets the log format (Plain or Json).
    /// </summary>
    public string Format { get; set; } = "Plain";

    /// <summary>
    /// Gets or sets the maximum log file size in megabytes.
    /// </summary>
    [Range(1, 100, ErrorMessage = "Max log file size must be between 1 and 100 MB.")]
    public int MaxFileSizeMB { get; set; } = 10;

    /// <summary>
    /// Gets or sets the log retention period in days.
    /// </summary>
    [Range(1, 365, ErrorMessage = "Retention days must be between 1 and 365.")]
    public int RetentionDays { get; set; } = 90;

    /// <summary>
    /// Gets or sets a value indicating whether to include sensitive data in logs.
    /// </summary>
    public bool IncludeSensitiveData { get; set; } = false;
}

/// <summary>
/// Configuration model for performance settings.
/// </summary>
public sealed class PerformanceConfig
{
    /// <summary>
    /// Gets or sets the cache time-to-live in seconds.
    /// </summary>
    [Range(60, 3600, ErrorMessage = "Cache TTL must be between 60 and 3600 seconds.")]
    public int CacheTtlSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the maximum number of concurrent operations.
    /// </summary>
    [Range(1, 10, ErrorMessage = "Max concurrent operations must be between 1 and 10.")]
    public int MaxConcurrentOperations { get; set; } = 3;

    /// <summary>
    /// Gets or sets the operation timeout in seconds.
    /// </summary>
    [Range(10, 600, ErrorMessage = "Operation timeout must be between 10 and 600 seconds.")]
    public int OperationTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the maximum retry attempts for failed operations.
    /// </summary>
    [Range(0, 5, ErrorMessage = "Max retry attempts must be between 0 and 5.")]
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the initial retry delay in milliseconds.
    /// </summary>
    [Range(100, 5000, ErrorMessage = "Initial retry delay must be between 100 and 5000 ms.")]
    public int InitialRetryDelayMs { get; set; } = 500;
}

/// <summary>
/// Configuration model for UI settings.
/// </summary>
public sealed class UiConfig
{
    /// <summary>
    /// Gets or sets the window startup mode (Normal, Maximized, LastState).
    /// </summary>
    public string WindowStartupMode { get; set; } = "Normal";

    /// <summary>
    /// Gets or sets a value indicating whether auto-refresh is enabled by default.
    /// </summary>
    public bool AutoRefreshDefault { get; set; } = false;

    /// <summary>
    /// Gets or sets the application theme (Light or Dark).
    /// </summary>
    public string Theme { get; set; } = "Light";

    /// <summary>
    /// Gets or sets a value indicating whether to show tooltips.
    /// </summary>
    public bool ShowTooltips { get; set; } = true;

    /// <summary>
    /// Gets or sets the default page size for data grids.
    /// </summary>
    [Range(10, 500, ErrorMessage = "Page size must be between 10 and 500.")]
    public int DefaultPageSize { get; set; } = 50;
}
