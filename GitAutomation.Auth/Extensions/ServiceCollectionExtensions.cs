using GitAutomation.Auth;
using GitAutomation.Plugins;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AutomationAuthServiceCollectionExtensions
    {
        public static IServiceCollection AddAutomationAuth(this IServiceCollection services, IConfiguration authenticationSection, IConfiguration authorizationSection)
        {
            var authorizationOptions = authorizationSection.Get<GitAutomation.Auth.AuthorizationOptions>();
            foreach (var plugin in authorizationOptions.Types.Select(PluginActivator.GetPluginOrNull<IRegisterPrincipalValidation>))
            {
                plugin?.RegisterPrincipalValidation(services, authorizationSection);
            }

            var authBuilder = services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnValidatePrincipal = async (context) =>
                        {
                            var principalValidation = context.HttpContext.RequestServices.GetServices<IPrincipalValidation>();
                            var currentPrincipal = context.Principal;
                            foreach (var entry in principalValidation)
                            {
                                currentPrincipal = await entry.OnValidatePrincipal(context.HttpContext, currentPrincipal);
                                if (currentPrincipal == null)
                                {
                                    context.RejectPrincipal();
                                    break;
                                }
                            }
                            if (currentPrincipal != null)
                            {
                                context.ReplacePrincipal(currentPrincipal);
                            }
                        }
                    };
                });
            
            var authenticationOptions = authenticationSection.Get<AuthenticationOptions>();
            services.Configure<AuthenticationOptions>(authenticationSection);
            PluginActivator.GetPlugin<IRegisterAuthentication>(
                typeName: authenticationOptions.Type,
                errorMessage: $"Unknown git service api registry: {authenticationOptions.Type}. Specify a .Net type.`"
            ).RegisterAuthentication(services, authBuilder, authenticationSection);

            services.AddAuthorization(options =>
            {
                options.AddGitAutomationPolicies();
            });

            return services;
        }
    }
}
