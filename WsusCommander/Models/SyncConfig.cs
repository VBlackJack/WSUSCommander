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
/// Configuration for WSUS synchronization scheduled tasks.
/// </summary>
public sealed class SyncConfig
{
    /// <summary>
    /// Gets or sets whether to wait for synchronization to complete.
    /// </summary>
    public bool WaitForCompletion { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum wait time in minutes (0 = no limit).
    /// </summary>
    public int MaxWaitMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets whether to send email notification on completion.
    /// </summary>
    public bool NotifyOnCompletion { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to only notify on errors.
    /// </summary>
    public bool NotifyOnErrorsOnly { get; set; } = true;
}
