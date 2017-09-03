using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Linq;

namespace GitAutomation.Auth
{
    public class RolesFromConfiguration : IPrincipalValidation
    {
        private readonly IEnumerable<KeyValuePair<string, ClaimRule[]>> roles;

        public RolesFromConfiguration(IOptions<RolesFromConfigurationOptions> options)
        {
            this.roles = options.Value?.Roles ?? Enumerable.Empty<KeyValuePair<string, ClaimRule[]>>();
        }

        public Task<ClaimsPrincipal> OnValidatePrincipal(HttpContext httpContext, ClaimsPrincipal currentPrincipal)
        {
            var original = currentPrincipal.Identity as ClaimsIdentity;

            var id = new ClaimsIdentity(Auth.Constants.AuthenticationType, original.NameClaimType, Auth.Constants.PermissionType);
            id.AddClaims(original.Claims.Where(claim => claim.Type != Auth.Constants.PermissionType));
            id.AddClaims(
                from role in roles
                where (from rule in role.Value
                       from claim in original.Claims
                       where rule.IsMatch(claim)
                       select rule).Any()
                select new Claim(id.RoleClaimType, role.Key)
            );
            return Task.FromResult(new ClaimsPrincipal(id));
        }
    }
}
