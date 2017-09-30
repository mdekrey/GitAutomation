using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation
{
    public static class ConfigurationSingleton
    {
        static ConfigurationSingleton()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("configuration.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();
        }

        public static IConfiguration Configuration { get; }
    }
}
