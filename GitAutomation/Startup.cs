using System;
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
using GitAutomation.Processes;

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
                .AddJsonFile("git.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();
            services.AddGitUtilities();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            Task.Run(() =>
            {
                if (Directory.Exists("/working"))
                {
                    // starting from an old system? maybe... but I don't want to handle that yet.
                    Directory.Delete("/working", true);
                }
                var info = Directory.CreateDirectory("/working");
                var children = info.GetFileSystemInfos();
                var proc = app.ApplicationServices.GetRequiredService<IReactiveProcessFactory>().BuildProcess(new System.Diagnostics.ProcessStartInfo("git", $"clone {Configuration["git:repository"]} /working"));
                proc.Output.Subscribe(message =>
                {
                    Console.WriteLine($"{message.Channel}: {message.Message}");
                }, onCompleted: () =>
                {
                    Console.WriteLine("Clone completed.");
                });
            });

            if (env.EnvironmentName == "Development")
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseMvc();
        }
    }
}
