using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace GitAutomation.Auth
{
    public interface IPrincipalValidation
    {
        Task<ClaimsPrincipal> OnValidatePrincipal(HttpContext httpContext, ClaimsPrincipal currentPrincipal);
    }
}
