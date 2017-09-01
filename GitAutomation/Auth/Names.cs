using Microsoft.AspNetCore.Authentication.Cookies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Auth
{
    public static class Names
    {
        public static string DefaultPolicy = CookieAuthenticationDefaults.AuthenticationScheme;
        public static string OAuthAuthenticationScheme = "CustomOAuthScheme";
        public static string OAuthAuthorizationPolicy = "OAuthPolicy";
        public static string AuthenticationType = "uri:GitAutomation";
        public static string RoleType = "uri:GitAutomation:Role";
    }
}
