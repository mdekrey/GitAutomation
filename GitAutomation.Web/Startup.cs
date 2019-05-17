using GitAutomation.DomainModels;
using GitAutomation.Web.Scripts;
using GitAutomation.Web.State;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GitAutomation.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options => options.EnableEndpointRouting = false)
                .AddNewtonsoftJson();

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });

            services.AddOptions<ConfigRepositoryOptions>().Configure(opt => Configuration.GetSection("configurationGit").Bind(opt));
            services.AddOptions<TargetRepositoryOptions>().Configure(opt => Configuration.GetSection("targetGit").Bind(opt));
            services.AddSingleton<PowerShellScriptInvoker>();
            services.AddSingleton<RepositoryConfigurationService>();
            services.AddSingleton<TargetRepositoryService>();

            services.AddSingleton(_ => new StateMachine<AppState>(AppState.Reducer, AppState.ZeroState));
            services.AddSingleton<IDispatcher>(sp => sp.GetRequiredService<StateMachine<AppState>>());
            services.AddSingleton<IStateMachine<AppState>>(sp => sp.GetRequiredService<StateMachine<AppState>>());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                //app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    // FIXME: these scripts are being left around app is terminated
                    //spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });

            app.ApplicationServices.GetRequiredService<RepositoryConfigurationService>().AssertStarted();
            app.ApplicationServices.GetRequiredService<TargetRepositoryService>().AssertStarted();
        }
    }
}
