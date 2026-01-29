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
/// Configuration for staged approval workflows (test group → delay → production).
/// </summary>
public sealed class StagedApprovalConfig
{
    /// <summary>
    /// Gets or sets the test group IDs where updates are first approved.
    /// </summary>
    [Required]
    public List<Guid> TestGroupIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the production group IDs where updates are promoted after testing.
    /// </summary>
    [Required]
    public List<Guid> ProductionGroupIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the test group names for display.
    /// </summary>
    public List<string> TestGroupNames { get; set; } = [];

    /// <summary>
    /// Gets or sets the production group names for display.
    /// </summary>
    public List<string> ProductionGroupNames { get; set; } = [];

    /// <summary>
    /// Gets or sets the number of days to wait in test before promoting to production.
    /// </summary>
    [Range(1, 90)]
    public int PromotionDelayDays { get; set; } = 7;

    /// <summary>
    /// Gets or sets the update classifications to include (empty = all).
    /// </summary>
    public List<string> UpdateClassifications { get; set; } =
        ["Critical Updates", "Security Updates"];

    /// <summary>
    /// Gets or sets whether to require successful installations before promotion.
    /// </summary>
    public bool RequireSuccessfulInstallations { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum number of successful installations required.
    /// </summary>
    [Range(0, 1000)]
    public int MinimumSuccessfulInstallations { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether to abort promotion if failures are detected.
    /// </summary>
    public bool AbortOnFailures { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum allowed failures before aborting promotion.
    /// </summary>
    [Range(0, 100)]
    public int MaxAllowedFailures { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether to auto-decline superseded updates.
    /// </summary>
    public bool DeclineSupersededUpdates { get; set; } = true;
}
