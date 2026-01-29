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

namespace WsusCommander.Constants;

/// <summary>
/// Application-wide constant values.
/// </summary>
/// <remarks>
/// This class centralizes magic numbers and default values to prevent duplication
/// and make configuration auditable. For configurable values, see AppConfig.
/// </remarks>
public static class AppConstants
{
    /// <summary>
    /// Default port constants.
    /// </summary>
    public static class Ports
    {
        /// <summary>
        /// Default WSUS server port (HTTP).
        /// </summary>
        public const int WsusDefault = 8530;

        /// <summary>
        /// Default WSUS server port with SSL (HTTPS).
        /// </summary>
        public const int WsusSsl = 8531;

        /// <summary>
        /// Default SMTP port with TLS.
        /// </summary>
        public const int SmtpTls = 587;

        /// <summary>
        /// Default SMTP port with SSL.
        /// </summary>
        public const int SmtpSsl = 465;
    }

    /// <summary>
    /// Toast notification duration constants in milliseconds.
    /// </summary>
    public static class ToastDurations
    {
        /// <summary>
        /// Duration for informational toast messages.
        /// </summary>
        public const int Info = 3000;

        /// <summary>
        /// Duration for success toast messages.
        /// </summary>
        public const int Success = 3000;

        /// <summary>
        /// Duration for warning toast messages.
        /// </summary>
        public const int Warning = 3000;

        /// <summary>
        /// Duration for error toast messages.
        /// </summary>
        public const int Error = 5000;
    }

    /// <summary>
    /// Search relevance scoring weights.
    /// </summary>
    public static class SearchScoring
    {
        /// <summary>
        /// Score for exact KB article match.
        /// </summary>
        public const int KbExactMatch = 100;

        /// <summary>
        /// Score for partial KB article match.
        /// </summary>
        public const int KbPartialMatch = 50;

        /// <summary>
        /// Score for title contains match.
        /// </summary>
        public const int TitleMatch = 30;

        /// <summary>
        /// Bonus score for title starting with search term.
        /// </summary>
        public const int TitleStartsWithBonus = 20;

        /// <summary>
        /// Score for description match.
        /// </summary>
        public const int DescriptionMatch = 10;

        /// <summary>
        /// Score for product match.
        /// </summary>
        public const int ProductMatch = 15;

        /// <summary>
        /// Score for classification match.
        /// </summary>
        public const int ClassificationMatch = 20;
    }

    /// <summary>
    /// Compliance status threshold percentages.
    /// </summary>
    public static class ComplianceThresholds
    {
        /// <summary>
        /// Minimum percentage for Compliant status.
        /// </summary>
        public const double Compliant = 95.0;

        /// <summary>
        /// Minimum percentage for Partially Compliant status.
        /// </summary>
        public const double PartiallyCompliant = 70.0;
    }

    /// <summary>
    /// Timeout and delay constants in milliseconds.
    /// </summary>
    public static class Timeouts
    {
        /// <summary>
        /// TCP connection check timeout.
        /// </summary>
        public const int TcpConnectionCheck = 5000;

        /// <summary>
        /// Maximum retry delay cap.
        /// </summary>
        public const int MaxRetryDelay = 30000;

        /// <summary>
        /// Log queue processing delay.
        /// </summary>
        public const int LogQueueDelay = 100;
    }

    /// <summary>
    /// Accessibility constants.
    /// </summary>
    public static class Accessibility
    {
        /// <summary>
        /// Minimum touch target size in pixels (WCAG 2.1).
        /// </summary>
        public const int MinTouchTargetSize = 44;
    }

    /// <summary>
    /// Default threshold constants.
    /// </summary>
    public static class Defaults
    {
        /// <summary>
        /// Default number of days before a computer is considered stale.
        /// </summary>
        public const int StaleDays = 30;
    }

    /// <summary>
    /// Cache-related constants.
    /// </summary>
    public static class Cache
    {
        /// <summary>
        /// Cache cleanup interval in minutes.
        /// </summary>
        public const int CleanupIntervalMinutes = 1;

        /// <summary>
        /// Default cache TTL for groups in minutes.
        /// </summary>
        public const int GroupsCacheTtlMinutes = 2;

        /// <summary>
        /// Default cache TTL for reports in minutes.
        /// </summary>
        public const int ReportsCacheTtlMinutes = 5;
    }

    /// <summary>
    /// Health check threshold constants.
    /// </summary>
    public static class HealthThresholds
    {
        /// <summary>
        /// Threshold in milliseconds for slow WSUS response warning.
        /// </summary>
        public const int SlowResponseMs = 2000;

        /// <summary>
        /// Minimum free disk space in GB before warning.
        /// </summary>
        public const int LowDiskSpaceGb = 5;

        /// <summary>
        /// Disk usage percentage threshold for warning.
        /// </summary>
        public const int DiskUsageWarningPercent = 90;

        /// <summary>
        /// Memory usage threshold in MB for high memory warning.
        /// </summary>
        public const int HighMemoryUsageMb = 1024;

        /// <summary>
        /// Standard DPI value for scaling calculations.
        /// </summary>
        public const double StandardDpi = 96.0;
    }

    /// <summary>
    /// Retry delay constants for error recovery.
    /// </summary>
    public static class RetryDelays
    {
        /// <summary>
        /// Retry delay for connection timeout errors in seconds.
        /// </summary>
        public const int ConnectionTimeoutSeconds = 5;

        /// <summary>
        /// Retry delay for server unavailable errors in seconds.
        /// </summary>
        public const int ServerUnavailableSeconds = 30;

        /// <summary>
        /// Retry delay for sync in progress errors in minutes.
        /// </summary>
        public const int SyncInProgressMinutes = 1;

        /// <summary>
        /// Retry delay for script timeout errors in seconds.
        /// </summary>
        public const int ScriptTimeoutSeconds = 10;

        /// <summary>
        /// Retry delay for operation timeout errors in seconds.
        /// </summary>
        public const int OperationTimeoutSeconds = 5;

        /// <summary>
        /// Default dispose wait timeout in seconds.
        /// </summary>
        public const int DisposeWaitSeconds = 5;
    }
}
