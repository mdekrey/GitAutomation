using GitAutomation.Auth;
using GitAutomation.EFCore;
using GitAutomation.EFCore.SecurityModel;
using Microsoft.EntityFrameworkCore;
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
            services.AddSingleton<Func<IServiceProvider, SecurityContext>>(topServiceProvider =>
            {
                SecurityContextFactory(topServiceProvider).Database.Migrate();
                return SecurityContextFactory;
            });
            services.AddScoped(sp => sp.GetRequiredService<Func<IServiceProvider, SecurityContext>>()(sp));
            services.AddScoped<ConnectionManagement<SecurityContext>>();
        }

        static SecurityContext SecurityContextFactory(IServiceProvider sp)
        {
            return new SecurityContext(sp.GetRequiredService<ISecurityContextCustomization>());
        }
    }
}
