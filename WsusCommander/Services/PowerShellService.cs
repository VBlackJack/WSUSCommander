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

using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace WsusCommander.Services;

/// <summary>
/// Service responsible for executing PowerShell scripts from the Scripts folder.
/// </summary>
public sealed class PowerShellService : IPowerShellService
{
    private readonly string _scriptsPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="PowerShellService"/> class.
    /// </summary>
    public PowerShellService()
    {
        _scriptsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
    }

    /// <inheritdoc/>
    public async Task<PSDataCollection<PSObject>> ExecuteScriptAsync(string scriptName, Dictionary<string, object>? parameters = null)
    {
        var scriptPath = Path.Combine(_scriptsPath, scriptName);

        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException(
                string.Format(Properties.Resources.StatusError, $"Script not found: {scriptName}"),
                scriptPath);
        }

        var scriptContent = await File.ReadAllTextAsync(scriptPath);

        var initialSessionState = InitialSessionState.CreateDefault();
        initialSessionState.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Bypass;

        using var runspace = RunspaceFactory.CreateRunspace(initialSessionState);
        runspace.Open();

        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddScript(scriptContent);

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                ps.AddParameter(param.Key, param.Value);
            }
        }

        var results = await Task.Run(() => ps.Invoke());

        if (ps.Streams.Error.Count > 0)
        {
            var errorMessages = string.Join(Environment.NewLine,
                ps.Streams.Error.Select(e => e.Exception?.Message ?? e.ToString()));
            throw new InvalidOperationException(errorMessages);
        }

        var output = new PSDataCollection<PSObject>();
        foreach (var result in results)
        {
            output.Add(result);
        }

        return output;
    }
}
