using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Cors.Internal;
using GitAutomation.BranchSettings;
using GitAutomation.Orchestration;
using GitAutomation.Repository;
using GitAutomation.AddonFramework;

namespace GitAutomation
{
    public class Startup
    {
        private readonly IHostingEnvironment env;
        private readonly AddonAssemblyLoader addonLoader;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            var cfgPath = builder.Build().GetValue<string>("OS:additional-configuration");
            if (!string.IsNullOrEmpty(cfgPath))
            {
                builder = builder.AddJsonFile(cfgPath, optional: false, reloadOnChange: true);
            }

            builder = builder.AddEnvironmentVariables();

            var addonPath = builder.Build().GetValue<string>("OS:addon-path");
            if (addonPath != null)
            {
                addonLoader = new AddonAssemblyLoader(addonPath);
            }

            Configuration = builder.Build();
            this.env = env;
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var mvcBuilder = services.AddMvc().AddJsonOptions(options =>
            {
                options.SerializerSettings.DateParseHandling = Newtonsoft.Json.DateParseHandling.DateTimeOffset;
                options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            });
            services.AddMvcExtensions(Configuration.GetSection("mvcExtensions"), mvcBuilder);

            services.AddGitUtilities(Configuration.GetSection("persistence"), Configuration.GetSection("git"), Configuration.GetSection("app"));
            services.Configure<GitRepositoryOptions>(Configuration.GetSection("git"));
            services.Configure<PersistenceOptions>(Configuration.GetSection("persistence"));
            services.Configure<StaticFileOptions>(options =>
                options.OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers[HeaderNames.CacheControl] =
                        "max-age=0, must-revalidate";
                }
            );

            services.AddAutomationAuth(
                authenticationSection: Configuration.GetSection("authentication"),
                authorizationSection: Configuration.GetSection("authorization")
            );

            if (env.IsDevelopment())
            {
                services.AddCors(options =>
                {
                    options.AddPolicy("Allow",
                        builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().AllowCredentials());
                });
                services.Configure<MvcOptions>(options =>
                {
                    options.Filters.Add(new CorsAuthorizationFilterFactory("Allow"));
                });
            }

            services.AddGraphQLServices<GraphQL.GitAutomationQuery>();
            services.AddScoped<GraphQL.Loaders>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            var repositoryStateRunner = app.ApplicationServices.GetRequiredService<IRepositoryStateDriver>();

            if (env.IsDevelopment())
            {
                var actions = app.ApplicationServices.GetRequiredService<IOrchestrationActions>();
                actions.DeleteRepository();

                app.UseDeveloperExceptionPage();
                app.UseCors("Allow");
            }

            var forwardHeaders = Configuration.GetValue<string>("forwardedHeaders");
            if (forwardHeaders != null)
            {
                var forwardingOptions = new ForwardedHeadersOptions
                {
                    ForwardedHeaders = Enum.Parse<ForwardedHeaders>(forwardHeaders),
                };
                forwardingOptions.KnownNetworks.Clear();
                forwardingOptions.KnownProxies.Clear();
                app.UseForwardedHeaders(forwardingOptions);
            }

            app.UseAuthentication();

            app.UseDefaultFiles(new DefaultFilesOptions
            {
                DefaultFileNames =
                {
                    "index.html"
                }
            });

            repositoryStateRunner.Start();
            app.UseStaticFiles();
            app.UseMvc();

            app.Use((context, next) =>
            {
                context.Request.Path = new PathString("/index.html");
                return next();
            });

            app.UseStaticFiles();
        }
    }
}
