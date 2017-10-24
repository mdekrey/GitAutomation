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

namespace GitAutomation
{
    public class Startup
    {
        private readonly IHostingEnvironment env;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("/run/secrets/configuration.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
            this.env = env;
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc().AddJsonOptions(options =>
            {
                options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            });
            
            services.AddGitUtilities(Configuration.GetSection("persistence"), Configuration.GetSection("git"));
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
                var repositoryState = app.ApplicationServices.GetRequiredService<IRepositoryState>();
                repositoryState.DeleteRepository();

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
