using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.DotNet.PlatformAbstractions;

namespace GitAutomation.AddonFramework
{
    public class AddonAssemblyLoader : IDisposable
    {
        private const string dependencyWildcard = "*.deps.json";
        private static readonly Regex dependencyExtension = new Regex(".deps.json$");

        private readonly CompositeCompilationAssemblyResolver assemblyResolver;
        private readonly IReadOnlyList<string> orderedRuntimes;
        private readonly IReadOnlyList<(string dir, DependencyContext context)> dependencyContexts;

        public AddonAssemblyLoader(string rootFolder)
        {
            if (Directory.Exists(rootFolder))
            {
                // Build the assembly resolver, which finds paths based on a RuntimeLibrary alone. Doesn't seem to check runtime dirs, though.
                this.assemblyResolver = new CompositeCompilationAssemblyResolver(
                    (from path in Directory.EnumerateDirectories(rootFolder)
                     select new AppBaseCompilationAssemblyResolver(path) as ICompilationAssemblyResolver
                    ).Concat(new ICompilationAssemblyResolver[]
                    {
                        new ReferenceAssemblyPathResolver(),
                        new PackageCompilationAssemblyResolver()
                    }).ToArray()
                );

                // Figure out our runtime monikers, including fallbacks. We want to use our specific runtime first, of course, but then the
                // fallbacks are ordered in priority order. Lastly, if no runtime matches, we'll use the empty-string version.
                var runtimeFallbacks = DependencyContext.Default.RuntimeGraph.FirstOrDefault(v => v.Runtime == RuntimeEnvironment.OperatingSystem);
                this.orderedRuntimes = new[] { runtimeFallbacks.Runtime }.Concat(runtimeFallbacks.Fallbacks).Concat(new[] { "" }).ToList().AsReadOnly();

                // Next, we find all the *.deps.json files. These say what depends on what, and gives us a dependency context. This is handy
                // for a faster way to find the DLLs than scanning the folders. This could also be used to pre-load dependencies, but remember
                // that not all dependencies are required for all code paths; lazy loading will be fine in case of this application.
                var allFiles = Directory.EnumerateFiles(rootFolder, dependencyWildcard, SearchOption.AllDirectories).ToArray();
                this.dependencyContexts =
                    (from depFile in allFiles
                     let dllPath = dependencyExtension.Replace(depFile, ".dll")
                     let assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dllPath)
                     let context = DependencyContext.Load(assembly)
                     where context != null
                     select (dir: Path.GetDirectoryName(dllPath), context)).ToArray();

                // Register our resolution handler
                AssemblyLoadContext.Default.Resolving += HandleResolving;
            }
        }

        /// <summary>
        /// Unregisters the resolution handler in case we no longer want to load new add-ons. Remember that all the old DLLs are still in memory;
        /// we didn't put them into a separate AppContext in this implementation.
        /// </summary>
        void IDisposable.Dispose()
        {
            AssemblyLoadContext.Default.Resolving -= HandleResolving;
        }
        
        private Assembly HandleResolving(AssemblyLoadContext context, AssemblyName name)
        {
            return
                // either an already loaded assembly...
                (from a in AppDomain.CurrentDomain.GetAssemblies()
                 where a.GetName().FullName == name.FullName
                 select a).FirstOrDefault()
                // or loaded from our add-ons folder
                ?? (from entry in dependencyContexts
                    from library in entry.context.RuntimeLibraries
                    where NamesMatch(library)
                    from finalPath in PossibleAssemblyPaths(entry.dir, library)
                    where File.Exists(finalPath)
                    let assembly = TryLoadAssembly(finalPath)
                    where assembly != null
                    select assembly).FirstOrDefault();

            bool NamesMatch(RuntimeLibrary runtime)
            {
                return string.Equals(runtime.Name, name.Name, StringComparison.OrdinalIgnoreCase);
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
                        // An AssemblyLoadContext can cache that it couldn't find a file so it doesn't try again.
                        // However, since we're specifying a path that we know already exists, we can trick it by
                        // loading the bytes directly.
                        // See https://stackoverflow.com/q/4483429/195653 for more information
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
        }

        private IEnumerable<string> PossibleAssemblyPaths(string withinDirectory, RuntimeLibrary library)
        {
            return (from paths in PathsPrioritizedByRuntime()
                    from assetPath in paths
                    select Path.Combine(withinDirectory, assetPath))
                .Concat(ResolveAssemblyPaths());

            IEnumerable<IEnumerable<string>> PathsPrioritizedByRuntime()
            {
                return from runtime in orderedRuntimes
                       let assetPaths = FindPathsMatchingRuntime(runtime)
                       where assetPaths != null
                       select assetPaths;
            }

            IEnumerable<string> FindPathsMatchingRuntime(string runtime)
            {
                return library.RuntimeAssemblyGroups.FirstOrDefault(g => g.Runtime == runtime)?.AssetPaths;
            }

            IReadOnlyList<string> ResolveAssemblyPaths()
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
                return assemblyPaths.AsReadOnly();
            }
        }
    }
}
