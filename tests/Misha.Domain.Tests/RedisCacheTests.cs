using Misha.Infrastructure.Caching;

namespace Misha.Domain.Tests;

public sealed class RedisCacheTests
{
    [Fact]
    public async Task Set_and_get_returns_value()
    {
        var cache = new InMemoryRedisCache();

        await cache.SetAsync("key", "value");

        var result = await cache.GetAsync("key");

        Assert.Equal("value", result);
    }

    [Fact]
    public async Task Remove_deletes_value()
    {
        var cache = new InMemoryRedisCache();
        await cache.SetAsync("key", "value");

        await cache.RemoveAsync("key");

        Assert.Null(await cache.GetAsync("key"));
    }

    [Fact]
    public async Task Expired_value_is_not_returned()
    {
        var cache = new InMemoryRedisCache();
        await cache.SetAsync("key", "value", TimeSpan.FromMilliseconds(20));

        await Task.Delay(60);

        Assert.Null(await cache.GetAsync("key"));
    }

    [Fact]
    public async Task Cancellation_is_honored()
    {
        var cache = new InMemoryRedisCache();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            cache.GetAsync("key", cancellation.Token));
    }
}
