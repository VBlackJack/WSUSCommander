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

using FluentAssertions;
using WsusCommander.Models;
using WsusCommander.Services;

namespace WsusCommander.Tests.Services;

public class FilterServiceTests
{
    private readonly FilterService _sut;
    private readonly List<WsusUpdate> _testUpdates;

    public FilterServiceTests()
    {
        _sut = new FilterService();
        _testUpdates = CreateTestUpdates();
    }

    private static List<WsusUpdate> CreateTestUpdates()
    {
        return
        [
            new WsusUpdate
            {
                Id = Guid.NewGuid(),
                Title = "Security Update for Windows 10",
                KbArticle = "KB5001234",
                Classification = "Security Updates",
                IsApproved = true,
                IsDeclined = false,
                CreationDate = DateTime.Now.AddDays(-10)
            },
            new WsusUpdate
            {
                Id = Guid.NewGuid(),
                Title = "Critical Update for .NET Framework",
                KbArticle = "KB5005678",
                Classification = "Critical Updates",
                IsApproved = false,
                IsDeclined = false,
                CreationDate = DateTime.Now.AddDays(-5)
            },
            new WsusUpdate
            {
                Id = Guid.NewGuid(),
                Title = "Definition Update for Windows Defender",
                KbArticle = "KB5009012",
                Classification = "Definition Updates",
                IsApproved = true,
                IsDeclined = false,
                CreationDate = DateTime.Now.AddDays(-1)
            },
            new WsusUpdate
            {
                Id = Guid.NewGuid(),
                Title = "Feature Update for Office 365",
                KbArticle = "KB5003456",
                Classification = "Feature Packs",
                IsApproved = false,
                IsDeclined = true,
                CreationDate = DateTime.Now.AddDays(-30)
            }
        ];
    }

    #region FilterUpdates Tests

    [Fact]
    public void FilterUpdates_WithNoCriteria_ReturnsAllUpdates()
    {
        // Arrange
        var criteria = new UpdateFilterCriteria();

        // Act
        var result = _sut.FilterUpdates(_testUpdates, criteria).ToList();

        // Assert
        result.Should().HaveCount(_testUpdates.Count);
    }

    [Fact]
    public void FilterUpdates_WithSearchText_FiltersOnTitle()
    {
        // Arrange
        var criteria = new UpdateFilterCriteria { SearchText = "Windows" };

        // Act
        var result = _sut.FilterUpdates(_testUpdates, criteria).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(u => u.Title.Contains("Windows"));
    }

    [Fact]
    public void FilterUpdates_WithSearchText_FiltersOnKbArticle()
    {
        // Arrange
        var criteria = new UpdateFilterCriteria { SearchText = "KB5001234" };

        // Act
        var result = _sut.FilterUpdates(_testUpdates, criteria).ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].KbArticle.Should().Be("KB5001234");
    }

    [Fact]
    public void FilterUpdates_WithClassification_FiltersCorrectly()
    {
        // Arrange
        var criteria = new UpdateFilterCriteria { Classification = "Security Updates" };

        // Act
        var result = _sut.FilterUpdates(_testUpdates, criteria).ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].Classification.Should().Be("Security Updates");
    }

    [Fact]
    public void FilterUpdates_WithIsApprovedTrue_ReturnsOnlyApproved()
    {
        // Arrange
        var criteria = new UpdateFilterCriteria { IsApproved = true };

        // Act
        var result = _sut.FilterUpdates(_testUpdates, criteria).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(u => u.IsApproved);
    }

    [Fact]
    public void FilterUpdates_WithIsApprovedFalse_ReturnsOnlyUnapproved()
    {
        // Arrange
        var criteria = new UpdateFilterCriteria { IsApproved = false };

        // Act
        var result = _sut.FilterUpdates(_testUpdates, criteria).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(u => !u.IsApproved);
    }

    [Fact]
    public void FilterUpdates_WithIsDeclinedTrue_ReturnsOnlyDeclined()
    {
        // Arrange
        var criteria = new UpdateFilterCriteria { IsDeclined = true };

        // Act
        var result = _sut.FilterUpdates(_testUpdates, criteria).ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].IsDeclined.Should().BeTrue();
    }

    [Fact]
    public void FilterUpdates_WithMultipleCriteria_AppliesAllFilters()
    {
        // Arrange
        var criteria = new UpdateFilterCriteria
        {
            SearchText = "Update",
            IsApproved = true
        };

        // Act
        var result = _sut.FilterUpdates(_testUpdates, criteria).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(u => u.Title.Contains("Update") && u.IsApproved);
    }

    [Fact]
    public void FilterUpdates_CaseInsensitiveSearch()
    {
        // Arrange
        var criteria = new UpdateFilterCriteria { SearchText = "WINDOWS" };

        // Act
        var result = _sut.FilterUpdates(_testUpdates, criteria).ToList();

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetDistinctClassifications Tests

    [Fact]
    public void GetDistinctClassifications_ReturnsUniqueClassifications()
    {
        // Act
        var result = _sut.GetDistinctClassifications(_testUpdates).ToList();

        // Assert
        result.Should().HaveCount(4);
        result.Should().Contain("Security Updates");
        result.Should().Contain("Critical Updates");
        result.Should().Contain("Definition Updates");
        result.Should().Contain("Feature Packs");
    }

    [Fact]
    public void GetDistinctClassifications_ReturnsSortedList()
    {
        // Act
        var result = _sut.GetDistinctClassifications(_testUpdates).ToList();

        // Assert
        result.Should().BeInAscendingOrder();
    }

    [Fact]
    public void GetDistinctClassifications_WithEmptyList_ReturnsEmpty()
    {
        // Act
        var result = _sut.GetDistinctClassifications([]).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}
