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

using System.Management.Automation;

namespace WsusCommander.Services;

/// <summary>
/// Interface for the PowerShell script execution service.
/// </summary>
public interface IPowerShellService
{
    /// <summary>
    /// Executes a PowerShell script asynchronously.
    /// </summary>
    /// <param name="scriptName">The name of the script file (relative to Scripts folder).</param>
    /// <param name="parameters">Optional dictionary of parameters to pass to the script.</param>
    /// <returns>A collection of PSObject results from the script execution.</returns>
    Task<PSDataCollection<PSObject>> ExecuteScriptAsync(string scriptName, Dictionary<string, object>? parameters = null);
}
