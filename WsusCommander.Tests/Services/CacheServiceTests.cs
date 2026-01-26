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
using Moq;
using WsusCommander.Models;
using WsusCommander.Services;

namespace WsusCommander.Tests.Services;

public class CacheServiceTests : IDisposable
{
    private readonly CacheService _sut;
    private readonly Mock<IConfigurationService> _configMock;

    public CacheServiceTests()
    {
        _configMock = new Mock<IConfigurationService>();
        _configMock.Setup(c => c.Config).Returns(CreateConfig());

        _sut = new CacheService(_configMock.Object);
    }

    private static AppConfig CreateConfig()
    {
        return new AppConfig
        {
            Performance = new PerformanceConfig
            {
                CacheTtlSeconds = 300
            }
        };
    }

    public void Dispose()
    {
        _sut.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Set and Get Tests

    [Fact]
    public void Set_AndGet_ReturnsCachedValue()
    {
        // Arrange
        const string key = "test-key";
        const string value = "test-value";

        // Act
        _sut.Set(key, value);
        var cachedValue = _sut.Get<string>(key);

        // Assert
        cachedValue.Should().Be(value);
    }

    [Fact]
    public void Get_WithMissingKey_ReturnsNull()
    {
        // Act
        var cachedValue = _sut.Get<string>("non-existent-key");

        // Assert
        cachedValue.Should().BeNull();
    }

    [Fact]
    public void Set_OverwritesExistingValue()
    {
        // Arrange
        const string key = "test-key";

        // Act
        _sut.Set(key, "first-value");
        _sut.Set(key, "second-value");
        var cachedValue = _sut.Get<string>(key);

        // Assert
        cachedValue.Should().Be("second-value");
    }

    [Fact]
    public void Set_WithCustomTtl_StoresValue()
    {
        // Arrange
        const string key = "test-key";
        const string value = "test-value";

        // Act
        _sut.Set(key, value, TimeSpan.FromMinutes(5));
        var cachedValue = _sut.Get<string>(key);

        // Assert
        cachedValue.Should().Be(value);
    }

    #endregion

    #region GetOrCreateAsync Tests

    [Fact]
    public async Task GetOrCreateAsync_WhenCacheMiss_CallsFactory()
    {
        // Arrange
        const string key = "test-key";
        var factoryCalled = false;

        // Act
        var result = await _sut.GetOrCreateAsync(key, async () =>
        {
            factoryCalled = true;
            return await Task.FromResult("factory-value");
        });

        // Assert
        factoryCalled.Should().BeTrue();
        result.Should().Be("factory-value");
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenCacheHit_DoesNotCallFactory()
    {
        // Arrange
        const string key = "test-key";
        _sut.Set(key, "cached-value");
        var factoryCalled = false;

        // Act
        var result = await _sut.GetOrCreateAsync(key, async () =>
        {
            factoryCalled = true;
            return await Task.FromResult("factory-value");
        });

        // Assert
        factoryCalled.Should().BeFalse();
        result.Should().Be("cached-value");
    }

    [Fact]
    public async Task GetOrCreateAsync_CachesFactoryResult()
    {
        // Arrange
        const string key = "test-key";
        var callCount = 0;

        // Act
        await _sut.GetOrCreateAsync(key, async () =>
        {
            callCount++;
            return await Task.FromResult("value");
        });

        await _sut.GetOrCreateAsync(key, async () =>
        {
            callCount++;
            return await Task.FromResult("value");
        });

        // Assert
        callCount.Should().Be(1);
    }

    #endregion

    #region Remove Tests

    [Fact]
    public void Remove_RemovesCachedValue()
    {
        // Arrange
        const string key = "test-key";
        _sut.Set(key, "value");

        // Act
        _sut.Remove(key);
        var result = _sut.Get<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Remove_WithNonExistentKey_DoesNotThrow()
    {
        // Act
        var act = () => _sut.Remove("non-existent-key");

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllCachedValues()
    {
        // Arrange
        _sut.Set("key1", "value1");
        _sut.Set("key2", "value2");
        _sut.Set("key3", "value3");

        // Act
        _sut.Clear();

        // Assert
        _sut.Get<string>("key1").Should().BeNull();
        _sut.Get<string>("key2").Should().BeNull();
        _sut.Get<string>("key3").Should().BeNull();
    }

    #endregion

    #region GetStatistics Tests

    [Fact]
    public void GetStatistics_ReturnsValidStats()
    {
        // Arrange
        _sut.Set("key1", "value1");

        // Act
        var stats = _sut.GetStatistics();

        // Assert
        stats.Should().NotBeNull();
        stats.ItemCount.Should().BeGreaterOrEqualTo(1);
    }

    #endregion

    #region Complex Type Tests

    [Fact]
    public void Set_AndGet_WithComplexType_Works()
    {
        // Arrange
        const string key = "update-key";
        var update = new WsusUpdate
        {
            Id = Guid.NewGuid(),
            Title = "Test Update",
            KbArticle = "KB123456"
        };

        // Act
        _sut.Set(key, update);
        var cachedUpdate = _sut.Get<WsusUpdate>(key);

        // Assert
        cachedUpdate.Should().NotBeNull();
        cachedUpdate!.Title.Should().Be("Test Update");
        cachedUpdate.KbArticle.Should().Be("KB123456");
    }

    [Fact]
    public void Set_AndGet_WithCollection_Works()
    {
        // Arrange
        const string key = "list-key";
        var list = new List<string> { "item1", "item2", "item3" };

        // Act
        _sut.Set(key, list);
        var cachedList = _sut.Get<List<string>>(key);

        // Assert
        cachedList.Should().BeEquivalentTo(list);
    }

    #endregion
}
