using System.Security.Claims;
using System.Text.Json;
using wallet.Data;
using wallet.Models.Responses;

namespace wallet.Middleware
{
    public class PolicyMiddleware
    {
        private readonly RequestDelegate _next;

        public PolicyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userIdValue = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? context.User.FindFirst("sub")?.Value;

                if (!int.TryParse(userIdValue, out var userId))
                {
                    await WriteForbiddenResponseAsync(context, "Invalid authenticated user.");
                    return;
                }

                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<WalletdbContext>();

                var user = await dbContext.Users.FindAsync(userId);

                if (user == null || !user.IsActive || user.IsSuspended)
                {
                    await WriteForbiddenResponseAsync(context, "Your account is disabled or suspended.");
                    return;
                }
            }

            await _next(context);
        }

        private static async Task WriteForbiddenResponseAsync(HttpContext context, string message)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object?>.Fail(message);
            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsJsonAsync(response, jsonOptions);
        }
    }
}
