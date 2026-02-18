using System.Security.Cryptography;
using System.Text;

namespace ImagenesService.Middlewares
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _apiKey;

        public ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
        {
            _next = next;
            _apiKey = config["InternalApiKey"] ?? string.Empty;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // � Proteger todas las rutas /api
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                // � Validar que el servidor tenga API Key configurada
                if (string.IsNullOrWhiteSpace(_apiKey))
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("API Key no configurada en el servidor");
                    return;
                }

                // � Validar que venga el header
                if (!context.Request.Headers.TryGetValue("X-API-KEY", out var extractedApiKey))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("API Key no proporcionada");
                    return;
                }

                var extractedKey = extractedApiKey.ToString();

                // � Validar longitud antes de comparar (evita excepción)
                if (_apiKey.Length != extractedKey.Length)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("API Key inválida");
                    return;
                }

                // � Comparación segura contra timing attacks
                var valid = CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(_apiKey),
                    Encoding.UTF8.GetBytes(extractedKey)
                );

                if (!valid)
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
