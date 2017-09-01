﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Reactive.Linq;
using GitAutomation.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using GitAutomation.BranchSettings;
using GitAutomation.Swagger;
using Swashbuckle.SwaggerGen.Generator;
using GitAutomation.Orchestration;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace GitAutomation
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("/run/secrets/configuration.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
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


            services.AddSwaggerGen(options =>
            {
                options.SingleApiVersion(new Swashbuckle.Swagger.Model.Info
                {
                    Version = "v1",
                    Title = "GitAutomation",
                    Description = "Automate your Git Repository",
                    TermsOfService = "TODO"
                });
                options.DescribeAllEnumsAsStrings();
                //options.OperationFilter<IgnoreCustomBindingOperationFilter>();
                //options.OperationFilter<FixPathOperationFilter>();
                options.OperationFilter<OperationIdFilter>();
                //options.OperationFilter<AddValidationResponseOperationFilter>();
                //options.CustomSchemaIds(t => t.FriendlyId(true));
                //options.SchemaFilter<AdditionalValidationFilter>();
                //options.SchemaFilter<ReferenceEnumFilter>();
                //options.SchemaFilter<ClassAssemblyFilter>();
            });

            services.AddGitUtilities(Configuration.GetSection("persistence").Get<PersistenceOptions>(), Configuration.GetSection("git").Get<GitRepositoryOptions>());
            services.Configure<GitRepositoryOptions>(Configuration.GetSection("git"));
            services.Configure<PersistenceOptions>(Configuration.GetSection("persistence"));
            services.Configure<StaticFileOptions>(options =>
                options.OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers[HeaderNames.CacheControl] =
                        "max-age=0, must-revalidate";
                }
            );

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie()
                .AddOAuth(Auth.Names.OAuthAuthenticationScheme, options =>
                {
                    Configuration.GetSection("authentication:oauth").Bind(options);
                    options.CallbackPath = new PathString("/custom-oauth-signin");
                });
            services.AddAuthorization(options =>
            {
                var bearerOnly = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
                    .Build();
                options.AddPolicy(Auth.Names.DefaultPolicy, bearerOnly);
                options.DefaultPolicy = bearerOnly;
            });
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
            app.UseSwagger();
            app.UseSwaggerUi();

            app.Use((context, next) =>
            {
                context.Request.Path = new PathString("/index.html");
                return next();
            });

            app.UseStaticFiles();
        }
    }
}
