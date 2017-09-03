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
            services.AddMvc();

            services.AddSwaggerGen(options =>
            {
                options.SingleApiVersion(new Swashbuckle.Swagger.Model.Info
                {
                    Version = "v1",
                    Title = "Woosti API",
                    Description = "Create your own personal Radio",
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

            services.AddGitUtilities(Configuration.GetSection("persistence").Get<PersistenceOptions>());
            services.Configure<GitRepositoryOptions>(Configuration.GetSection("git"));
            services.Configure<StaticFileOptions>(options =>
                options.OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers[HeaderNames.CacheControl] =
                        "max-age=0, must-revalidate";
                }
            );
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            var repositoryState = app.ApplicationServices.GetRequiredService<IRepositoryState>();

            app.UseDefaultFiles(new DefaultFilesOptions
            {
                DefaultFileNames =
                {
                    "index.html"
                }
            });

            if (env.IsDevelopment())
            {
                repositoryState.DeleteRepository().Subscribe();

                app.UseDeveloperExceptionPage();
            }
            repositoryState.ProcessActions().Subscribe(
                onNext: _ =>
                {
                    Console.WriteLine(_);
                },
                onCompleted: () =>
                {
                    Console.WriteLine("COMPLETED - This shouldn't happen!");
                },
                onError: _ =>
                {
                    Console.WriteLine(_);
                }
            );
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
