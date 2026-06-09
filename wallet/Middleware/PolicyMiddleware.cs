using System.Security.Claims;
using wallet.Data;
using wallet.Data.Entities;

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

                var userId = int.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);


                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<WalletdbContext>();


                var user = await dbContext.Users.FindAsync(userId);

                if (user == null || !user.IsActive || user.IsSuspended)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync("Your account is disabled or suspended.");
                    return;
                }


            }
            await _next(context);
        }
    }
}
