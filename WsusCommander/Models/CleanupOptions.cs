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
/// Cleanup option flags for WSUS maintenance.
/// </summary>
public sealed class CleanupOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to remove obsolete updates.
    /// </summary>
    public bool RemoveObsoleteUpdates { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to remove obsolete computers.
    /// </summary>
    public bool RemoveObsoleteComputers { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to remove expired updates.
    /// </summary>
    public bool RemoveExpiredUpdates { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to compress update revisions.
    /// </summary>
    public bool CompressUpdateRevisions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to remove unneeded content files.
    /// </summary>
    public bool RemoveUnneededContent { get; set; }
}
