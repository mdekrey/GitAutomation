using GitAutomation.Auth;
using GitAutomation.BranchSettings;
using GitAutomation.EFCore;
using GitAutomation.EFCore.BranchingModel;
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

        public static void AddEfBranchingContext<T>(this IServiceCollection services)
            where T : class, IBranchingContextCustomization
        {
            services.AddSingleton<IBranchingContextCustomization, T>();

            services.AddSingleton<IBranchSettings, EfBranchSettings>();
            services.AddSingleton<Func<IServiceProvider, BranchingContext>>(topServiceProvider =>
            {
                BranchingContextFactory(topServiceProvider).Database.Migrate();
                return BranchingContextFactory;
            });
            services.AddScoped(sp => sp.GetRequiredService<Func<IServiceProvider, BranchingContext>>()(sp));
            services.AddScoped<ConnectionManagement<BranchingContext>>();
        }

        static SecurityContext SecurityContextFactory(IServiceProvider sp)
        {
            return new SecurityContext(sp.GetRequiredService<ISecurityContextCustomization>());
        }

        static BranchingContext BranchingContextFactory(IServiceProvider sp)
        {
            return new BranchingContext(sp.GetRequiredService<IBranchingContextCustomization>());
        }
    }
}
