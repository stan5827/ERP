namespace ErpBackend.Middleware;

public class ApiVersionMiddleware
{
    private readonly RequestDelegate _next;

    public ApiVersionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        if (path.StartsWith("/api/"))
        {
            var version = "v1";
            var segments = path.Split('/').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            if (segments.Length >= 2 && segments[1].StartsWith("v"))
            {
                version = segments[1];
                var newPath = "/api/" + string.Join("/", segments.Skip(2));
                context.Request.Path = newPath;
            }

            context.Items["ApiVersion"] = version;
        }

        await _next(context);
    }
}

public static class ApiVersionMiddlewareExtensions
{
    public static IApplicationBuilder UseApiVersioning(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiVersionMiddleware>();
    }
}