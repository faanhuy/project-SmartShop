using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SmartShop.Application.Common.Interfaces;
using SmartShop.WebAPI.Middleware;
using SmartShop.WebAPI.Options;
using Xunit;

namespace SmartShop.Application.Tests.RateLimit;

public class RateLimitingMiddlewareTests
{
    private readonly Mock<IRateLimitStore> _storeMock = new();
    private bool _nextCalled;

    private RequestDelegate BuildNext()
    {
        _nextCalled = false;
        return ctx =>
        {
            _nextCalled = true;
            return Task.CompletedTask;
        };
    }

    private RateLimitingMiddleware BuildMiddleware(RateLimitOptions opts)
    {
        var options = Options.Create(opts);
        var logger = NullLogger<RateLimitingMiddleware>.Instance;
        return new RateLimitingMiddleware(BuildNext(), _storeMock.Object, options, logger);
    }

    private static DefaultHttpContext BuildContext(
        string path,
        string method = "GET",
        string? userId = null,
        string? remoteIp = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        ctx.Request.Method = method;

        if (userId is not null)
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }

        if (remoteIp is not null)
            ctx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(remoteIp);

        return ctx;
    }

    private static RateLimitOptions EnabledOpts(string policy = "Auth", int limit = 10, int window = 60) =>
        new()
        {
            Enabled = true,
            Rules = new Dictionary<string, RateLimitRule>
            {
                [policy] = new RateLimitRule { PermitLimit = limit, WindowSeconds = window }
            }
        };

    private void SetupStore(long count, DateTimeOffset? resetAt = null)
    {
        var reset = resetAt ?? DateTimeOffset.UtcNow.AddSeconds(60);
        _storeMock
            .Setup(s => s.IncrementAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((count, reset));
    }

    [Fact]
    public async Task InvokeAsync_DisabledOption_SkipsRateLimit_CallsNext()
    {
        var opts = new RateLimitOptions { Enabled = false };
        var middleware = BuildMiddleware(opts);
        var ctx = BuildContext("/api/auth/login");

        await middleware.InvokeAsync(ctx);

        _nextCalled.Should().BeTrue();
        _storeMock.Verify(
            s => s.IncrementAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ExemptPath_VNPayCallback_CallsNext()
    {
        var middleware = BuildMiddleware(EnabledOpts());
        var ctx = BuildContext("/api/payments/vnpay/callback");

        await middleware.InvokeAsync(ctx);

        _nextCalled.Should().BeTrue();
        _storeMock.Verify(
            s => s.IncrementAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_UnknownPath_NoPolicy_CallsNext()
    {
        var middleware = BuildMiddleware(EnabledOpts());
        var ctx = BuildContext("/api/products");

        await middleware.InvokeAsync(ctx);

        _nextCalled.Should().BeTrue();
        ctx.Response.StatusCode.Should().NotBe(429);
    }

    [Fact]
    public async Task InvokeAsync_WithinLimit_SetsHeaders_CallsNext()
    {
        var opts = EnabledOpts("Auth", limit: 10, window: 60);
        var resetAt = DateTimeOffset.UtcNow.AddSeconds(60);
        SetupStore(count: 1, resetAt: resetAt);

        var middleware = BuildMiddleware(opts);
        var ctx = BuildContext("/api/auth/login");
        ctx.Response.Body = new System.IO.MemoryStream();

        await middleware.InvokeAsync(ctx);

        _nextCalled.Should().BeTrue();
        ctx.Response.StatusCode.Should().NotBe(429);
        ctx.Response.Headers["X-RateLimit-Limit"].ToString().Should().Be("10");
        ctx.Response.Headers["X-RateLimit-Remaining"].ToString().Should().Be("9");
    }

    [Fact]
    public async Task InvokeAsync_ExceedsLimit_Returns429_WithRetryAfter()
    {
        var opts = EnabledOpts("Auth", limit: 10, window: 60);
        var resetAt = DateTimeOffset.UtcNow.AddSeconds(30);
        SetupStore(count: 11, resetAt: resetAt);

        var middleware = BuildMiddleware(opts);
        var ctx = BuildContext("/api/auth/login");
        ctx.Response.Body = new System.IO.MemoryStream();

        await middleware.InvokeAsync(ctx);

        _nextCalled.Should().BeFalse();
        ctx.Response.StatusCode.Should().Be(429);
        ctx.Response.Headers["Retry-After"].ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task InvokeAsync_PostOrders_AppliesOrdersPolicy()
    {
        var opts = new RateLimitOptions
        {
            Enabled = true,
            Rules = new Dictionary<string, RateLimitRule>
            {
                ["Orders"] = new RateLimitRule { PermitLimit = 5, WindowSeconds = 60 }
            }
        };
        SetupStore(count: 1);

        var middleware = BuildMiddleware(opts);
        var ctx = BuildContext("/api/orders", method: "POST");
        ctx.Response.Body = new System.IO.MemoryStream();

        await middleware.InvokeAsync(ctx);

        _storeMock.Verify(
            s => s.IncrementAsync(
                It.Is<string>(k => k.StartsWith("Orders:")),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_GetOrders_NoPolicy_CallsNext()
    {
        var middleware = BuildMiddleware(EnabledOpts());
        var ctx = BuildContext("/api/orders", method: "GET");

        await middleware.InvokeAsync(ctx);

        _nextCalled.Should().BeTrue();
        _storeMock.Verify(
            s => s.IncrementAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_AuthenticatedUser_UsesUserIdAsKey()
    {
        var opts = EnabledOpts("Auth", limit: 10, window: 60);
        SetupStore(count: 1);

        var middleware = BuildMiddleware(opts);
        var ctx = BuildContext("/api/auth/login", userId: "user-42");
        ctx.Response.Body = new System.IO.MemoryStream();

        await middleware.InvokeAsync(ctx);

        _storeMock.Verify(
            s => s.IncrementAsync(
                It.Is<string>(k => k.StartsWith("Auth:user:")),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_AnonymousUser_UsesIpAsKey()
    {
        var opts = EnabledOpts("Auth", limit: 10, window: 60);
        SetupStore(count: 1);

        var middleware = BuildMiddleware(opts);
        var ctx = BuildContext("/api/auth/login", remoteIp: "192.168.1.1");
        ctx.Response.Body = new System.IO.MemoryStream();

        await middleware.InvokeAsync(ctx);

        _storeMock.Verify(
            s => s.IncrementAsync(
                It.Is<string>(k => k.StartsWith("Auth:ip:")),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
