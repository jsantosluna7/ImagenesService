using Microsoft.AspNetCore.Diagnostics;

namespace ImagenesService.Middlewares
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _apiKey;

        public ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
        {
            _next = next;
            _apiKey = config["InternalApiKey"] ?? "";
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Solo proteger rutas de subida y borrado
            if (context.Request.Path.StartsWithSegments("/api/images"))
            {
                if (!context.Request.Headers.TryGetValue("X-API-KEY", out var extractedApiKey))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("API Key no proporcionada");
                    return;
                }

                if (!_apiKey.Equals(extractedApiKey))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("API Key inválida");
                    return;
                }
            }

            await _next(context);
        }
    }
}
