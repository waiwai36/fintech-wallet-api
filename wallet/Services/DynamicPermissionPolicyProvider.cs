using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace wallet.Services
{
    public class DynamicPermissionPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public DynamicPermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) { }

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            var policy = await base.GetPolicyAsync(policyName);
            if (policy == null)
            {
                policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new DynamicPermissionRequirement(policyName))
                    .Build();
            }
            return policy;
        }
    }
}
