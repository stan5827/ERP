using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ErpBackend.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ErpBackend.Tests.Services;

public class CacheServiceTests
{
    private readonly MemoryCacheOptions _cacheOptions = new();
    private readonly ILogger<InMemoryCacheService> _logger = NullLogger<InMemoryCacheService>.Instance;

    private InMemoryCacheService CreateService()
    {
        return new InMemoryCacheService(new MemoryCache(_cacheOptions), _logger);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenKeyNotExists()
    {
        // Arrange
        var service = CreateService();
        var key = "nonexistent:key";

        // Act
        var result = await service.GetAsync<string>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_StoresValue_InCache()
    {
        // Arrange
        var service = CreateService();
        var key = "test:key";
        var value = "testvalue";

        // Act
        await service.SetAsync(key, value);

        // Assert
        var result = await service.GetAsync<string>(key);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task SetAsync_WithCustomExpiration_StoresValue()
    {
        // Arrange
        var service = CreateService();
        var key = "test:expiry";
        var value = "expiryvalue";
        var expiration = TimeSpan.FromMinutes(10);

        // Act
        await service.SetAsync(key, value, expiration);

        // Assert
        var result = await service.GetAsync<string>(key);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task RemoveAsync_DeletesValue_FromCache()
    {
        // Arrange
        var service = CreateService();
        var key = "test:remove";
        var value = "removevalue";
        await service.SetAsync(key, value);

        // Act
        await service.RemoveAsync(key);

        // Assert
        var result = await service.GetAsync<string>(key);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetOrSetAsync_ReturnsValue_WhenKeyExists()
    {
        // Arrange
        var service = CreateService();
        var key = "test:getorset";
        var existingValue = "existing";
        await service.SetAsync(key, existingValue);

        // Act
        var result = await service.GetOrSetAsync(key, () => Task.FromResult("newvalue"), TimeSpan.FromMinutes(5));

        // Assert
        Assert.Equal(existingValue, result);
    }

    [Fact]
    public async Task GetOrSetAsync_CallsFactory_WhenKeyNotExists()
    {
        // Arrange
        var service = CreateService();
        var key = "test:getorset:new";

        // Act
        var result = await service.GetOrSetAsync(key, () => Task.FromResult("factoryvalue"), TimeSpan.FromMinutes(5));

        // Assert
        Assert.Equal("factoryvalue", result);
    }

    [Fact]
    public async Task GetOrSetAsync_CachesFactoryResult_WhenKeyNotExists()
    {
        // Arrange
        var service = CreateService();
        var key = "test:getorset:cache";

        // Act
        await service.GetOrSetAsync(key, () => Task.FromResult("cached"), TimeSpan.FromMinutes(5));
        var result = await service.GetAsync<string>(key);

        // Assert
        Assert.Equal("cached", result);
    }

    [Fact]
    public async Task GetOrSetAsync_UsesDefaultExpiration_WhenNotProvided()
    {
        // Arrange
        var service = CreateService();
        var key = "test:defaultexpiry";

        // Act
        await service.GetOrSetAsync(key, () => Task.FromResult("default"), null);

        // Assert - should use default 30 minute expiration
        var result = await service.GetAsync<string>(key);
        Assert.NotNull(result);
    }
}