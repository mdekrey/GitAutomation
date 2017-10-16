using GitAutomation.Auth;
using GitAutomation.EFCore;
using GitAutomation.EFCore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddEfSecurityContext<T>(this IServiceCollection services)
            where T : class, ISecurityContextCustomization
        {
            services.AddSingleton<ISecurityContextCustomization, T>();

            services.AddSingleton<IPrincipalValidation, EfPermissionManagement>();
            services.AddSingleton<IManageUserPermissions, EfPermissionManagement>();
            services.AddSingleton<IContextFactory<SecurityContext>>(sp =>
                new ContextFactory<SecurityContext>(() => new SecurityContext(sp.GetRequiredService<ISecurityContextCustomization>())));
            services.AddScoped<ConnectionManagement<SecurityContext>>();
        }
    }
}
