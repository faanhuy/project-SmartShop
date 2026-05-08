using FluentAssertions;
using SmartShop.Infrastructure.RateLimit;
using Xunit;

namespace SmartShop.Application.Tests.RateLimit;

public class InMemoryRateLimitStoreTests
{
    private readonly InMemoryRateLimitStore _store = new();
    private readonly TimeSpan _window = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task IncrementAsync_FirstRequest_ReturnsCountOne()
    {
        var (count, _) = await _store.IncrementAsync("key-new", _window);

        count.Should().Be(1);
    }

    [Fact]
    public async Task IncrementAsync_SecondRequest_SameKey_IncrementsCount()
    {
        await _store.IncrementAsync("key-increment", _window);
        var (count, _) = await _store.IncrementAsync("key-increment", _window);

        count.Should().Be(2);
    }

    [Fact]
    public async Task IncrementAsync_DifferentKeys_CountIndependently()
    {
        await _store.IncrementAsync("key-a", _window);
        await _store.IncrementAsync("key-a", _window);

        var (countB, _) = await _store.IncrementAsync("key-b", _window);

        countB.Should().Be(1);
    }

    [Fact]
    public async Task IncrementAsync_ResetAt_IsApproximatelyWindowAhead()
    {
        var before = DateTimeOffset.UtcNow;
        var (_, resetAt) = await _store.IncrementAsync("key-reset-check", _window);
        var after = DateTimeOffset.UtcNow;

        resetAt.Should().BeOnOrAfter(before.Add(_window).AddSeconds(-1));
        resetAt.Should().BeOnOrBefore(after.Add(_window).AddSeconds(1));
    }

    [Fact]
    public async Task IncrementAsync_ExpiredWindow_ResetsCount()
    {
        var shortWindow = TimeSpan.FromMilliseconds(50);

        await _store.IncrementAsync("key-expire", shortWindow);
        await _store.IncrementAsync("key-expire", shortWindow);

        await Task.Delay(100);

        var (count, _) = await _store.IncrementAsync("key-expire", shortWindow);

        count.Should().Be(1);
    }
}
