using IdentityHub.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace IdentityHub.Tests;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task Middleware_SetsAllSecurityHeaders()
    {
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
        Assert.Equal("0", context.Response.Headers["X-XSS-Protection"]);
        Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"]);
        Assert.Equal("default-src 'self'; frame-ancestors 'none'", context.Response.Headers["Content-Security-Policy"]);
    }

    [Fact]
    public async Task Middleware_CallsNextDelegate()
    {
        var nextCalled = false;
        var middleware = new SecurityHeadersMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(new DefaultHttpContext());

        Assert.True(nextCalled);
    }
}
