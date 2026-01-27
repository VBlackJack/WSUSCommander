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
/// Type of approval rule action.
/// </summary>
public enum RuleAction
{
    /// <summary>Approve matching updates.</summary>
    Approve,

    /// <summary>Decline matching updates.</summary>
    Decline
}

/// <summary>
/// Type of rule condition.
/// </summary>
public enum RuleConditionType
{
    /// <summary>Match by update classification.</summary>
    Classification,

    /// <summary>Match if update is superseded.</summary>
    IsSuperseded,

    /// <summary>Match by title pattern.</summary>
    TitleContains,

    /// <summary>Match by KB article number.</summary>
    KbArticle
}

/// <summary>
/// Represents an approval rule for automatic update management.
/// </summary>
public sealed class ApprovalRule
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the rule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the rule is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the condition type.
    /// </summary>
    public RuleConditionType ConditionType { get; set; }

    /// <summary>
    /// Gets or sets the condition value (e.g., classification name, pattern).
    /// </summary>
    public string ConditionValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action to perform.
    /// </summary>
    public RuleAction Action { get; set; }

    /// <summary>
    /// Gets or sets the target group ID for approval actions.
    /// </summary>
    public Guid? TargetGroupId { get; set; }

    /// <summary>
    /// Gets or sets the target group name for display.
    /// </summary>
    public string TargetGroupName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the priority (lower = higher priority).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets the display text for the condition.
    /// </summary>
    public string ConditionDisplay => ConditionType switch
    {
        RuleConditionType.Classification => $"Classification = {ConditionValue}",
        RuleConditionType.IsSuperseded => "Is Superseded",
        RuleConditionType.TitleContains => $"Title contains \"{ConditionValue}\"",
        RuleConditionType.KbArticle => $"KB = {ConditionValue}",
        _ => ConditionValue
    };

    /// <summary>
    /// Gets the display text for the action.
    /// </summary>
    public string ActionDisplay => Action == RuleAction.Approve
        ? $"Approve for {TargetGroupName}"
        : "Decline";
}

/// <summary>
/// Collection of approval rules for serialization.
/// </summary>
public sealed class ApprovalRulesCollection
{
    /// <summary>
    /// Gets or sets the list of rules.
    /// </summary>
    public List<ApprovalRule> Rules { get; set; } = [];
}
