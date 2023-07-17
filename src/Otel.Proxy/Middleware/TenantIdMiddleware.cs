using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;

internal class TenantIdMiddleware
{
    private readonly RequestDelegate _next;

    public TenantIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            var httpActivityFeature = context.Features.GetRequiredFeature<IHttpActivityFeature>();
            var tenantId = context.Request.Headers["x-tenant-id"].FirstOrDefault() ?? "";
            if (!string.IsNullOrEmpty(tenantId))
            {
                httpActivityFeature.Activity.SetTag("tenant-id", tenantId);
            }
        }
        catch (Exception ex)
        {
            Activity.Current?.AddEvent(new ActivityEvent("Exception during adding TenantId to middleware",
                tags: new()
                {
                    { "exceptionData", ex.ToString() }
                }));
        }
        finally
        {
            await _next(context);
        }
    }
}

internal static class TenantIdMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantIdMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantIdMiddleware>();
    }
}