using Microsoft.AspNetCore.Authorization;
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

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DynamicPermissionRequirement requirement)
        {
           
            var roleName = context.User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(roleName)) return Task.CompletedTask;

            if (roleName == "Admin")
            {
                context.Succeed(requirement);
                return Task.CompletedTask;  
            }

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<WalletdbContext>();

            
            var role = dbContext.Roles.FirstOrDefault(r => r.RoleName == roleName);
            if (role == null) return Task.CompletedTask;

           
            var isPermissionEnabled = dbContext.RoleClaims.Any(x =>
                x.RoleId == role.RoleId &&
                x.ClaimType == requirement.Permission &&
                x.ClaimValue == "Enabled");

            if (isPermissionEnabled)
            {
                context.Succeed(requirement); 
            }

            return Task.CompletedTask;
        }
    }
}
