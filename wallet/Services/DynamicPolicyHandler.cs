using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using wallet.Data;

namespace wallet.Services
{
    public class DynamicPermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }
        public DynamicPermissionRequirement(string permission) => Permission = permission;
    }

    public class DynamicPolicyHandler : AuthorizationHandler<DynamicPermissionRequirement>
    {
        private readonly IServiceScopeFactory _scopeFactory;
        public DynamicPolicyHandler(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, DynamicPermissionRequirement requirement)
        {
            var roleName = context.User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(roleName)) return;

            if (roleName == "Admin")
            {
                context.Succeed(requirement);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<WalletdbContext>();

            var role = await dbContext.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleName == roleName);
            if (role == null) return;

            var isPermissionEnabled = await dbContext.RoleClaims.AsNoTracking().AnyAsync(x =>
                x.RoleId == role.RoleId &&
                x.ClaimType == requirement.Permission &&
                x.ClaimValue == "Enabled");

            if (isPermissionEnabled)
            {
                context.Succeed(requirement);
            }
        }
    }
}
