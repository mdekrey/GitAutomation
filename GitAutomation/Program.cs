using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace GitAutomation
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .Build();

            host.Run();
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var asm = (from a in AppDomain.CurrentDomain.GetAssemblies()
                       where a.GetName().FullName == args.Name
                       select a).FirstOrDefault();

            if (asm == null)
            {
                var assemblyName = args.Name.Split(",")[0];
                if (Directory.Exists("/extra-bins"))
                {
                    var allFiles = Directory.EnumerateFiles("/extra-bins", assemblyName + ".dll", SearchOption.AllDirectories).ToArray();
                    if (allFiles.Length > 0)
                    {
                        return Assembly.LoadFrom(allFiles[0]);
                    }
                }
                throw new FileNotFoundException(args.Name);
            }

            return asm;
        }
    }
}
