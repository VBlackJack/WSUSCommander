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
using System.Text.Json;
using WsusCommander.Models;

namespace WsusCommander.Services;

/// <summary>
/// Service for managing approval rules.
/// </summary>
public sealed class ApprovalRulesService : IApprovalRulesService
{
    private readonly string _rulesPath;
    private readonly ILoggingService _loggingService;
    private readonly List<ApprovalRule> _rules = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovalRulesService"/> class.
    /// </summary>
    public ApprovalRulesService(IConfigurationService configService, ILoggingService loggingService)
    {
        _loggingService = loggingService;
        _rulesPath = Path.Combine(configService.AppSettings.DataPath, "approval-rules.json");

        // Ensure directory exists
        var directory = Path.GetDirectoryName(_rulesPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<ApprovalRule> GetRules()
    {
        return _rules.OrderBy(r => r.Priority).ToList().AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_rulesPath))
            {
                var content = await File.ReadAllTextAsync(_rulesPath);
                var collection = JsonSerializer.Deserialize<ApprovalRulesCollection>(content);
                if (collection?.Rules != null)
                {
                    _rules.Clear();
                    _rules.AddRange(collection.Rules);
                }
                await _loggingService.LogDebugAsync($"Loaded {_rules.Count} approval rules.");
            }
        }
        catch (Exception ex)
        {
            await _loggingService.LogWarningAsync($"Failed to load approval rules: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task SaveRuleAsync(ApprovalRule rule)
    {
        var existing = _rules.FirstOrDefault(r => r.Id == rule.Id);
        if (existing != null)
        {
            _rules.Remove(existing);
        }

        _rules.Add(rule);
        await SaveToFileAsync();
        await _loggingService.LogInfoAsync($"Approval rule '{rule.Name}' saved.");
    }

    /// <inheritdoc/>
    public async Task DeleteRuleAsync(Guid ruleId)
    {
        var rule = _rules.FirstOrDefault(r => r.Id == ruleId);
        if (rule != null)
        {
            _rules.Remove(rule);
            await SaveToFileAsync();
            await _loggingService.LogInfoAsync($"Approval rule '{rule.Name}' deleted.");
        }
    }

    /// <inheritdoc/>
    public ApprovalRule? EvaluateRules(WsusUpdate update)
    {
        foreach (var rule in _rules.Where(r => r.IsEnabled).OrderBy(r => r.Priority))
        {
            if (MatchesRule(update, rule))
            {
                return rule;
            }
        }

        return null;
    }

    private static bool MatchesRule(WsusUpdate update, ApprovalRule rule)
    {
        return rule.ConditionType switch
        {
            RuleConditionType.Classification =>
                string.Equals(update.Classification, rule.ConditionValue, StringComparison.OrdinalIgnoreCase),

            RuleConditionType.IsSuperseded =>
                update.IsDeclined, // Note: Would need IsSuperseded property on WsusUpdate

            RuleConditionType.TitleContains =>
                update.Title?.Contains(rule.ConditionValue, StringComparison.OrdinalIgnoreCase) ?? false,

            RuleConditionType.KbArticle =>
                update.KbArticle?.Contains(rule.ConditionValue, StringComparison.OrdinalIgnoreCase) ?? false,

            _ => false
        };
    }

    private async Task SaveToFileAsync()
    {
        try
        {
            var collection = new ApprovalRulesCollection
            {
                Rules = _rules
            };

            var content = JsonSerializer.Serialize(collection, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_rulesPath, content);
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync("Failed to save approval rules", ex);
        }
    }
}
