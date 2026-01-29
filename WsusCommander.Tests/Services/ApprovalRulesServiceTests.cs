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
using FluentAssertions;
using Moq;
using WsusCommander.Models;
using WsusCommander.Services;

namespace WsusCommander.Tests.Services;

public sealed class ApprovalRulesServiceTests : IDisposable
{
    private readonly ApprovalRulesService _sut;
    private readonly Mock<IConfigurationService> _configMock;
    private readonly Mock<ILoggingService> _loggingMock;
    private readonly string _testDataPath;

    public ApprovalRulesServiceTests()
    {
        _testDataPath = Path.Combine(Path.GetTempPath(), $"ApprovalRulesTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDataPath);

        _configMock = new Mock<IConfigurationService>();
        _configMock.Setup(c => c.AppSettings).Returns(new AppSettingsConfig
        {
            DataPath = _testDataPath
        });

        _loggingMock = new Mock<ILoggingService>();
        _loggingMock.Setup(l => l.LogDebugAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _loggingMock.Setup(l => l.LogInfoAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _loggingMock.Setup(l => l.LogWarningAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _loggingMock.Setup(l => l.LogErrorAsync(It.IsAny<string>(), It.IsAny<Exception?>())).Returns(Task.CompletedTask);

        _sut = new ApprovalRulesService(_configMock.Object, _loggingMock.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDataPath))
        {
            Directory.Delete(_testDataPath, true);
        }
        GC.SuppressFinalize(this);
    }

    #region GetRules Tests

    [Fact]
    public void GetRules_WhenNoRulesLoaded_ReturnsEmptyList()
    {
        // Act
        var rules = _sut.GetRules();

        // Assert
        rules.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRules_AfterSaveRule_ReturnsRule()
    {
        // Arrange
        var rule = CreateTestRule("Test Rule");

        // Act
        await _sut.SaveRuleAsync(rule);
        var rules = _sut.GetRules();

        // Assert
        rules.Should().HaveCount(1);
        rules[0].Name.Should().Be("Test Rule");
    }

    [Fact]
    public async Task GetRules_ReturnsRulesOrderedByPriority()
    {
        // Arrange
        var rule1 = CreateTestRule("Low Priority", priority: 10);
        var rule2 = CreateTestRule("High Priority", priority: 1);
        var rule3 = CreateTestRule("Medium Priority", priority: 5);

        await _sut.SaveRuleAsync(rule1);
        await _sut.SaveRuleAsync(rule2);
        await _sut.SaveRuleAsync(rule3);

        // Act
        var rules = _sut.GetRules();

        // Assert
        rules.Should().HaveCount(3);
        rules[0].Name.Should().Be("High Priority");
        rules[1].Name.Should().Be("Medium Priority");
        rules[2].Name.Should().Be("Low Priority");
    }

    #endregion

    #region SaveRuleAsync Tests

    [Fact]
    public async Task SaveRuleAsync_NewRule_AddsToCollection()
    {
        // Arrange
        var rule = CreateTestRule("New Rule");

        // Act
        await _sut.SaveRuleAsync(rule);

        // Assert
        var rules = _sut.GetRules();
        rules.Should().ContainSingle(r => r.Id == rule.Id);
    }

    [Fact]
    public async Task SaveRuleAsync_ExistingRule_UpdatesRule()
    {
        // Arrange
        var rule = CreateTestRule("Original Name");
        await _sut.SaveRuleAsync(rule);

        // Act
        rule.Name = "Updated Name";
        await _sut.SaveRuleAsync(rule);

        // Assert
        var rules = _sut.GetRules();
        rules.Should().ContainSingle();
        rules[0].Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task SaveRuleAsync_PersistsToFile()
    {
        // Arrange
        var rule = CreateTestRule("Persisted Rule");

        // Act
        await _sut.SaveRuleAsync(rule);

        // Assert
        var filePath = Path.Combine(_testDataPath, "approval-rules.json");
        File.Exists(filePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain("Persisted Rule");
    }

    #endregion

    #region DeleteRuleAsync Tests

    [Fact]
    public async Task DeleteRuleAsync_ExistingRule_RemovesFromCollection()
    {
        // Arrange
        var rule = CreateTestRule("Rule to Delete");
        await _sut.SaveRuleAsync(rule);

        // Act
        await _sut.DeleteRuleAsync(rule.Id);

        // Assert
        _sut.GetRules().Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteRuleAsync_NonExistingRule_DoesNotThrow()
    {
        // Act
        var act = () => _sut.DeleteRuleAsync(Guid.NewGuid());

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region LoadAsync Tests

    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_DoesNotThrow()
    {
        // Act
        var act = () => _sut.LoadAsync();

        // Assert
        await act.Should().NotThrowAsync();
        _sut.GetRules().Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_WhenFileExists_LoadsRules()
    {
        // Arrange
        var rule = CreateTestRule("Saved Rule");
        await _sut.SaveRuleAsync(rule);

        // Create a new service instance to simulate restart
        var newService = new ApprovalRulesService(_configMock.Object, _loggingMock.Object);

        // Act
        await newService.LoadAsync();

        // Assert
        var rules = newService.GetRules();
        rules.Should().ContainSingle(r => r.Name == "Saved Rule");
    }

    #endregion

    #region EvaluateRules Tests

    [Fact]
    public async Task EvaluateRules_ClassificationMatch_ReturnsRule()
    {
        // Arrange
        var rule = CreateTestRule("Security Updates Rule",
            conditionType: RuleConditionType.Classification,
            conditionValue: "Security Updates");
        await _sut.SaveRuleAsync(rule);

        var update = new WsusUpdate { Classification = "Security Updates" };

        // Act
        var matchedRule = _sut.EvaluateRules(update);

        // Assert
        matchedRule.Should().NotBeNull();
        matchedRule!.Name.Should().Be("Security Updates Rule");
    }

    [Fact]
    public async Task EvaluateRules_ClassificationNoMatch_ReturnsNull()
    {
        // Arrange
        var rule = CreateTestRule("Security Updates Rule",
            conditionType: RuleConditionType.Classification,
            conditionValue: "Security Updates");
        await _sut.SaveRuleAsync(rule);

        var update = new WsusUpdate { Classification = "Critical Updates" };

        // Act
        var matchedRule = _sut.EvaluateRules(update);

        // Assert
        matchedRule.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateRules_IsSuperseded_ReturnsRule()
    {
        // Arrange
        var rule = CreateTestRule("Superseded Rule",
            conditionType: RuleConditionType.IsSuperseded);
        await _sut.SaveRuleAsync(rule);

        var update = new WsusUpdate { IsSuperseded = true };

        // Act
        var matchedRule = _sut.EvaluateRules(update);

        // Assert
        matchedRule.Should().NotBeNull();
        matchedRule!.Name.Should().Be("Superseded Rule");
    }

    [Fact]
    public async Task EvaluateRules_TitleContains_ReturnsRule()
    {
        // Arrange
        var rule = CreateTestRule("Windows 10 Rule",
            conditionType: RuleConditionType.TitleContains,
            conditionValue: "Windows 10");
        await _sut.SaveRuleAsync(rule);

        var update = new WsusUpdate { Title = "2025-01 Cumulative Update for Windows 10" };

        // Act
        var matchedRule = _sut.EvaluateRules(update);

        // Assert
        matchedRule.Should().NotBeNull();
    }

    [Fact]
    public async Task EvaluateRules_KbArticle_ReturnsRule()
    {
        // Arrange
        var rule = CreateTestRule("KB Rule",
            conditionType: RuleConditionType.KbArticle,
            conditionValue: "KB5001234");
        await _sut.SaveRuleAsync(rule);

        var update = new WsusUpdate { KbArticle = "KB5001234" };

        // Act
        var matchedRule = _sut.EvaluateRules(update);

        // Assert
        matchedRule.Should().NotBeNull();
    }

    [Fact]
    public async Task EvaluateRules_DisabledRule_DoesNotMatch()
    {
        // Arrange
        var rule = CreateTestRule("Disabled Rule",
            conditionType: RuleConditionType.Classification,
            conditionValue: "Security Updates");
        rule.IsEnabled = false;
        await _sut.SaveRuleAsync(rule);

        var update = new WsusUpdate { Classification = "Security Updates" };

        // Act
        var matchedRule = _sut.EvaluateRules(update);

        // Assert
        matchedRule.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateRules_MultipleRules_ReturnsHighestPriority()
    {
        // Arrange
        var lowPriorityRule = CreateTestRule("Low Priority",
            conditionType: RuleConditionType.Classification,
            conditionValue: "Security Updates",
            priority: 10);
        var highPriorityRule = CreateTestRule("High Priority",
            conditionType: RuleConditionType.Classification,
            conditionValue: "Security Updates",
            priority: 1);

        await _sut.SaveRuleAsync(lowPriorityRule);
        await _sut.SaveRuleAsync(highPriorityRule);

        var update = new WsusUpdate { Classification = "Security Updates" };

        // Act
        var matchedRule = _sut.EvaluateRules(update);

        // Assert
        matchedRule.Should().NotBeNull();
        matchedRule!.Name.Should().Be("High Priority");
    }

    #endregion

    private static ApprovalRule CreateTestRule(
        string name,
        RuleConditionType conditionType = RuleConditionType.Classification,
        string conditionValue = "Test",
        int priority = 0)
    {
        return new ApprovalRule
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsEnabled = true,
            ConditionType = conditionType,
            ConditionValue = conditionValue,
            Action = RuleAction.Approve,
            Priority = priority
        };
    }
}
