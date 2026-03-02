namespace IdentityHub.Api.Middleware;

/// <summary>Middleware that adds security headers to all HTTP responses.</summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Prevent browsers from MIME-sniffing the response away from the declared Content-Type
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // Block this page from being embedded in iframes (prevents clickjacking attacks)
        context.Response.Headers["X-Frame-Options"] = "DENY";

        // Disable the legacy browser XSS filter — it's exploitable; CSP is the modern replacement
        context.Response.Headers["X-XSS-Protection"] = "0";

        // Only send the origin (not the full URL path) when navigating to external sites
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Only allow resources from the same origin; block inline scripts and framing
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; frame-ancestors 'none'";

        await _next(context);
    }
}
