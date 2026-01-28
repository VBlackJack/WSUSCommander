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

using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Text;
using System.Text.Json;

namespace WsusCommander.Services;

/// <summary>
/// Service responsible for executing PowerShell scripts using Windows PowerShell 5.1 out-of-process.
/// This approach ensures compatibility with Windows PowerShell modules like UpdateServices.
/// </summary>
public sealed class PowerShellService : IPowerShellService
{
    private readonly string _scriptsPath;
    private readonly string _powerShellExe;
    private readonly ILoggingService _loggingService;
    private readonly IConfigurationService _configurationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PowerShellService"/> class.
    /// </summary>
    /// <param name="loggingService">The logging service for detailed diagnostics.</param>
    /// <param name="configurationService">The configuration service.</param>
    public PowerShellService(ILoggingService loggingService, IConfigurationService configurationService)
    {
        _loggingService = loggingService;
        _configurationService = configurationService;
        _scriptsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
        _powerShellExe = configurationService.Config.PowerShell.ExecutablePath;
        _loggingService.LogDebugAsync($"PowerShellService initialized. Scripts path: {_scriptsPath}");
        _loggingService.LogDebugAsync($"Using Windows PowerShell: {_powerShellExe}");
    }

    /// <inheritdoc/>
    public async Task<PSDataCollection<PSObject>> ExecuteScriptAsync(
        string scriptName,
        Dictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ValidateScriptName(scriptName);
        var scriptPath = Path.Combine(_scriptsPath, scriptName);
        await _loggingService.LogDebugAsync($"[PS] Executing script: {scriptPath}");

        // Log parameters
        if (parameters != null)
        {
            var paramLog = new StringBuilder();
            paramLog.Append("[PS] Parameters: ");
            foreach (var param in parameters)
            {
                paramLog.Append($"{param.Key}={param.Value} ");
            }
            await _loggingService.LogDebugAsync(paramLog.ToString());
        }

        if (!File.Exists(scriptPath))
        {
            var error = $"Script not found: {scriptPath}";
            await _loggingService.LogErrorAsync($"[PS] {error}");
            throw new FileNotFoundException(
                string.Format(Properties.Resources.StatusError, error),
                scriptPath);
        }

        // Build the PowerShell command with parameters
        var psCommand = new StringBuilder();
        psCommand.Append($"& '{EscapePowerShellString(scriptPath)}'");

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                ValidateParameterName(param.Key);
                var value = param.Value switch
                {
                    bool b => b ? "$true" : "$false",
                    string s => $"'{EscapePowerShellString(s)}'",
                    null => "$null",
                    _ => param.Value.ToString() ?? string.Empty
                };
                psCommand.Append($" -{param.Key} {value}");
            }
        }

        // Wrap to convert output to JSON for easy parsing
        var jsonDepth = _configurationService.Config.PowerShell.JsonDepth;
        var fullCommand = $"{psCommand} | ConvertTo-Json -Depth {jsonDepth} -Compress";
        await _loggingService.LogDebugAsync($"[PS] Command: {fullCommand}");

        try
        {
            var executionPolicy = _configurationService.Config.PowerShell.ExecutionPolicy ?? "RemoteSigned";
            var psi = new ProcessStartInfo
            {
                FileName = _powerShellExe,
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy {executionPolicy} -Command \"{fullCommand}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            await _loggingService.LogDebugAsync("[PS] Starting powershell.exe process...");

            using var process = new Process { StartInfo = psi };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var timeoutSeconds = _configurationService.Config.PowerShell.TimeoutSeconds;
            linkedCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            using var registration = linkedCts.Token.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Process already exited
                }
            });

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null) outputBuilder.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(linkedCts.Token);

            var output = outputBuilder.ToString().Trim();
            var errors = errorBuilder.ToString().Trim();

            await _loggingService.LogDebugAsync($"[PS] Exit code: {process.ExitCode}");
            await _loggingService.LogDebugAsync($"[PS] Output length: {output.Length}");

            if (!string.IsNullOrEmpty(errors))
            {
                await _loggingService.LogWarningAsync($"[PS] Stderr: {errors}");
            }

            if (linkedCts.IsCancellationRequested)
            {
                throw new OperationCanceledException(
                    $"PowerShell execution timed out after {timeoutSeconds} seconds.",
                    linkedCts.Token);
            }

            if (process.ExitCode != 0)
            {
                var errorMessage = !string.IsNullOrEmpty(errors) ? errors : $"PowerShell exited with code {process.ExitCode}";
                await _loggingService.LogErrorAsync($"[PS] Script failed: {errorMessage}");
                throw new InvalidOperationException(errorMessage);
            }

            // Parse JSON output to PSObject collection
            var result = new PSDataCollection<PSObject>();

            if (!string.IsNullOrEmpty(output))
            {
                await _loggingService.LogDebugAsync($"[PS] Raw output: {output}");

                try
                {
                    using var doc = JsonDocument.Parse(output);

                    // Handle both arrays and single objects
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var element in doc.RootElement.EnumerateArray())
                        {
                            var psObj = ConvertJsonToPSObject(element);
                            result.Add(psObj);
                        }
                        await _loggingService.LogDebugAsync($"[PS] Parsed {result.Count} items from array");
                    }
                    else
                    {
                        var psObj = ConvertJsonToPSObject(doc.RootElement);
                        result.Add(psObj);
                        await _loggingService.LogDebugAsync($"[PS] Parsed single object");
                    }
                }
                catch (JsonException ex)
                {
                    await _loggingService.LogWarningAsync($"[PS] Failed to parse JSON, returning raw output: {ex.Message}");
                    var psObj = new PSObject(output);
                    result.Add(psObj);
                }
            }

            await _loggingService.LogInfoAsync($"[PS] Script {scriptName} executed successfully");
            return result;
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync($"[PS] Exception executing {scriptName}", ex);
            throw;
        }
    }

    private static PSObject ConvertJsonToPSObject(JsonElement element)
    {
        var psObj = new PSObject();

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                object? value = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Number => prop.Value.TryGetInt64(out var l) ? (object)l : prop.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => prop.Value.ToString()
                };
                psObj.Properties.Add(new PSNoteProperty(prop.Name, value));
            }
        }

        return psObj;
    }

    private static void ValidateScriptName(string scriptName)
    {
        if (string.IsNullOrWhiteSpace(scriptName))
        {
            throw new ArgumentException(Properties.Resources.ErrorScriptNameRequired, nameof(scriptName));
        }

        if (!string.Equals(scriptName, Path.GetFileName(scriptName), StringComparison.Ordinal))
        {
            throw new ArgumentException(Properties.Resources.ErrorScriptNameNoPath, nameof(scriptName));
        }

        if (!scriptName.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(Properties.Resources.ErrorScriptNameNotPs1, nameof(scriptName));
        }
    }

    private static void ValidateParameterName(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            throw new ArgumentException(Properties.Resources.ErrorPowerShellParameterNameRequired, nameof(parameterName));
        }

        for (var i = 0; i < parameterName.Length; i++)
        {
            var ch = parameterName[i];
            var isValid = i == 0
                ? char.IsLetter(ch)
                : char.IsLetterOrDigit(ch) || ch == '_';

            if (!isValid)
            {
                throw new ArgumentException(
                    string.Format(Properties.Resources.ErrorInvalidPowerShellParameterName, parameterName),
                    nameof(parameterName));
            }
        }
    }

    private static string EscapePowerShellString(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }
}
