using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyModel.Resolution;
using Microsoft.Extensions.DependencyModel;
using System.Runtime.Loader;
using System.Text.RegularExpressions;

namespace GitAutomation
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string rootFolder = "/extra-bins";
            if (Directory.Exists(rootFolder))
            {
                var directories = Directory.EnumerateDirectories(rootFolder);
                var resolvers = from path in directories
                                select new AppBaseCompilationAssemblyResolver(path) as ICompilationAssemblyResolver;

                var assemblyResolver = new CompositeCompilationAssemblyResolver(
                    resolvers.Concat(new ICompilationAssemblyResolver[]
                    {
                        new ReferenceAssemblyPathResolver(),
                        new PackageCompilationAssemblyResolver()
                    }).ToArray()
                );
                var runtimeFallbacks = DependencyContext.Default.RuntimeGraph.FirstOrDefault(v => v.Runtime == Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.OperatingSystem);
                var orderedRuntimes = new[] { runtimeFallbacks.Runtime }.Concat(runtimeFallbacks.Fallbacks).Concat(new[] { "" }).ToArray();

                var allFiles = Directory.EnumerateFiles(rootFolder, "*.deps.json", SearchOption.AllDirectories).ToArray();
                var dependencyContexts = (from depFile in allFiles
                                          let dllPath = Regex.Replace(depFile, ".deps.json$", ".dll")
                                          let assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dllPath)
                                          select new { dir = Path.GetDirectoryName(dllPath), context = DependencyContext.Load(assembly) }).ToArray();
                AssemblyLoadContext.Default.Resolving += (context, name) =>
                {
                    var asm = (from a in AppDomain.CurrentDomain.GetAssemblies()
                               where a.GetName().FullName == name.FullName
                               select a).FirstOrDefault();
                    if (asm != null)
                    {
                        return asm;
                    }

                    var namePart = name.Name;
                    bool NamesMatch(RuntimeLibrary runtime)
                    {
                        return string.Equals(runtime.Name, namePart, StringComparison.OrdinalIgnoreCase);
                    }
                    string ResolveAssemblyPaths(RuntimeLibrary library)
                    {
                        var wrapper = new CompilationLibrary(
                            library.Type,
                            library.Name,
                            library.Version,
                            library.Hash,
                            library.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths),
                            library.Dependencies,
                            library.Serviceable);
                        var assemblyPaths = new List<string>();
                        assemblyResolver.TryResolveAssemblyPaths(wrapper, assemblyPaths);
                        return assemblyPaths.FirstOrDefault();
                    }
                    Assembly TryLoadAssembly(string assemblyPath)
                    {
                        try
                        {
                            return context.LoadFromAssemblyPath(assemblyPath);
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                return Assembly.Load(File.ReadAllBytes(assemblyPath));
                            }
                            catch (Exception ex2)
                            {
                                Console.WriteLine(ex);
                                Console.WriteLine(ex2);
                                return null;
                            }
                        }
                    }

                    var resultAssembly = (from entry in dependencyContexts
                                          from library in entry.context.RuntimeLibraries
                                          where NamesMatch(library)
                                          from runtime in orderedRuntimes
                                          let asmGroup = library.RuntimeAssemblyGroups.FirstOrDefault(g => g.Runtime == runtime)
                                          where asmGroup != null
                                          from finalPath in asmGroup.AssetPaths.Select(assetPath => Path.Combine(entry.dir, assetPath))
                                                               .Concat(new[] { ResolveAssemblyPaths(library) })
                                          where File.Exists(finalPath)
                                          where finalPath != null
                                          let assembly = TryLoadAssembly(finalPath)
                                          where assembly != null
                                          select assembly).FirstOrDefault();
                    return resultAssembly;
                };
            }

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
