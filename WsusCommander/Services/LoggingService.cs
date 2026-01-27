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

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace WsusCommander.Services;

/// <summary>
/// Service responsible for logging application events to file asynchronously.
/// </summary>
public sealed class LoggingService : ILoggingService, IDisposable
{
    private readonly string _logPath;
    private readonly string _logFileName;
    private readonly ConcurrentQueue<string> _logQueue = new();
    private readonly SemaphoreSlim _writeSemaphore = new(1, 1);
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _writeTask;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingService"/> class.
    /// </summary>
    /// <param name="configService">The configuration service to get log path settings.</param>
    public LoggingService(IConfigurationService configService)
    {
        // Use app directory for easier debugging, fallback to configured path
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var appLogPath = Path.Combine(appDir, "logs");
        _logPath = Directory.Exists(Path.GetDirectoryName(configService.AppSettings.LogPath))
            ? configService.AppSettings.LogPath
            : appLogPath;
        _logFileName = $"wsuscommander_{DateTime.Now:yyyyMMdd}.log";

        EnsureLogDirectoryExists();
        _writeTask = ProcessLogQueueAsync(_cts.Token);

        // Log startup info
        LogAsync("INFO", $"=== WSUS Commander Started ===");
        LogAsync("INFO", $"Log file: {Path.Combine(_logPath, _logFileName)}");
        LogAsync("INFO", $"App directory: {appDir}");
    }

    /// <summary>
    /// Gets the current log file path.
    /// </summary>
    public string CurrentLogFilePath => Path.Combine(_logPath, _logFileName);

    /// <inheritdoc/>
    public event EventHandler<LoggingFailedEventArgs>? LoggingFailed;

    /// <inheritdoc/>
    public Task LogInfoAsync(string message)
    {
        return LogAsync("INFO", message);
    }

    /// <inheritdoc/>
    public Task LogWarningAsync(string message)
    {
        return LogAsync("WARN", message);
    }

    /// <inheritdoc/>
    public Task LogErrorAsync(string message, Exception? exception = null)
    {
        var fullMessage = exception is not null
            ? $"{message} | Exception: {exception.Message} | StackTrace: {exception.StackTrace}"
            : message;

        return LogAsync("ERROR", fullMessage);
    }

    /// <inheritdoc/>
    public Task LogDebugAsync(string message)
    {
        return LogAsync("DEBUG", message);
    }

    /// <summary>
    /// Queues a log entry for asynchronous writing.
    /// </summary>
    private Task LogAsync(string level, string message)
    {
        var logEntry = FormatLogEntry(level, message);
        _logQueue.Enqueue(logEntry);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Formats a log entry with timestamp and level.
    /// </summary>
    private static string FormatLogEntry(string level, string message)
    {
        return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level,-5}] {message}";
    }

    /// <summary>
    /// Ensures the log directory exists.
    /// </summary>
    private void EnsureLogDirectoryExists()
    {
        if (!Directory.Exists(_logPath))
        {
            Directory.CreateDirectory(_logPath);
        }
    }

    /// <summary>
    /// Background task that processes the log queue and writes to file.
    /// </summary>
    private async Task ProcessLogQueueAsync(CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(_logPath, _logFileName);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_logQueue.IsEmpty)
                {
                    await Task.Delay(100, cancellationToken);
                    continue;
                }

                await _writeSemaphore.WaitAsync(cancellationToken);
                try
                {
                    var sb = new StringBuilder();
                    while (_logQueue.TryDequeue(out var entry))
                    {
                        sb.AppendLine(entry);
                    }

                    if (sb.Length > 0)
                    {
                        await File.AppendAllTextAsync(filePath, sb.ToString(), cancellationToken);
                    }
                }
                finally
                {
                    _writeSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Write to Windows Event Log as fallback
                try
                {
                    EventLog.WriteEntry(
                        "WSUSCommander",
                        $"Logging failure: {ex.Message}",
                        EventLogEntryType.Warning);
                }
                catch
                {
                    // Last resort - ignore
                }

                LoggingFailed?.Invoke(this, new LoggingFailedEventArgs(ex));
            }
        }

        // Flush remaining entries on shutdown
        await FlushRemainingEntriesAsync(filePath);
    }

    /// <summary>
    /// Flushes any remaining log entries on shutdown.
    /// </summary>
    private async Task FlushRemainingEntriesAsync(string filePath)
    {
        if (_logQueue.IsEmpty)
            return;

        var sb = new StringBuilder();
        while (_logQueue.TryDequeue(out var entry))
        {
            sb.AppendLine(entry);
        }

        if (sb.Length > 0)
        {
            await File.AppendAllTextAsync(filePath, sb.ToString());
        }
    }

    /// <summary>
    /// Disposes resources used by the logging service.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _cts.Cancel();

        try
        {
            _writeTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // Ignore cancellation exceptions
        }

        _cts.Dispose();
        _writeSemaphore.Dispose();
        _disposed = true;
    }
}
