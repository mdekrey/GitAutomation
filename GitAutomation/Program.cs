using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace GitAutomation
{
    public class Program
    {
        public static void Main(string[] args)
        {
            new AddonFramework.AddonAssemblyLoader("/extra-bins");

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .Build();

            host.Run();
        }
        
    }
}
