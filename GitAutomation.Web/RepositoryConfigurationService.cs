using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;
using GitAutomation.DomainModels;
using GitAutomation.Extensions;
using GitAutomation.Web.Scripts;
using Microsoft.Extensions.Options;
using static GitAutomation.DomainModels.RepositoryStructureReducer;

namespace GitAutomation.Web
{
    internal class RepositoryConfigurationService : IDispatcher
    {
        private readonly ConfigRepositoryOptions options;
        private readonly PowerShellScriptInvoker scriptInvoker;

        private RepositoryConfigurationState state = RepositoryConfigurationState.ZeroState;
        private PowerShellStreams<StandardAction> lastLoadResult;
        private PowerShellStreams<StandardAction> lastPushResult;

        public RepositoryConfigurationService(IOptions<ConfigRepositoryOptions> options, PowerShellScriptInvoker scriptInvoker)
        {
            this.options = options.Value;
            this.scriptInvoker = scriptInvoker;
        }

        internal async void BeginLoad()
        {
            this.lastLoadResult = scriptInvoker.Invoke("$/Config/clone.ps1", new { }, options);
        }

        public void Dispatch(StandardAction action)
        {
            state = (action.Action switch
            {
                "ConfigurationDirectoryNotAccessible" => ConfigurationDirectoryNotAccessible(state),
                "ConfigurationReady" => ConfigurationReady(state),
                "ConfigurationRepositoryCouldNotBeCloned" => ConfigurationRepositoryCouldNotBeCloned(state),
                "ConfigurationRepositoryPasswordIncorrect" => ConfigurationRepositoryPasswordIncorrect(state),
                "ConfigurationRepositoryNoBranch" => ConfigurationRepositoryNoBranch(state),
                _ => state,
            }).With(structure: RepositoryStructureReducer.Reduce(state.Structure, action));
        }

        private RepositoryConfigurationState ConfigurationRepositoryNoBranch(RepositoryConfigurationState original)
        {
            Task.Run(() => CreateDefaultConfiguration());

            return original;
        }

        private RepositoryConfigurationState ConfigurationRepositoryPasswordIncorrect(RepositoryConfigurationState original) =>
            original.With(status: RepositoryConfigurationState.RepositoryConfigurationStatus.Error_PasswordIncorrect);

        private RepositoryConfigurationState ConfigurationRepositoryCouldNotBeCloned(RepositoryConfigurationState original) =>
            original.With(status: RepositoryConfigurationState.RepositoryConfigurationStatus.Error_FailedToClone);

        private RepositoryConfigurationState ConfigurationDirectoryNotAccessible(RepositoryConfigurationState original) =>
            original.With(status: RepositoryConfigurationState.RepositoryConfigurationStatus.Error_DirectoryNotAccessible);

        private RepositoryConfigurationState ConfigurationReady(RepositoryConfigurationState original)
        {
            Task.Run(() => LoadFromDisk());

            return original.With(RepositoryConfigurationState.RepositoryConfigurationStatus.NotReady);
        }

        private async Task CreateDefaultConfiguration()
        {
            try
            {
                var assembly = this.GetType().Assembly;
                var startsWith = $"{assembly.GetName().Name}.Defaults";
                foreach (var resourceName in assembly.GetManifestResourceNames().Where(n => n.StartsWith(startsWith)))
                {
                    var remainingName = resourceName.Substring(startsWith.Length + 1);
                    var outPath = Path.Join(options.CheckoutPath, string.Join('/', remainingName.Split('.', Math.Max(1, remainingName.Count(c => c == '.') - 1))));
                    Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    using (var file = File.Create(outPath))
                    {
                        await stream.CopyToAsync(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            await Task.WhenAll(
                    LoadFromDisk(),
                    PushToRemote()
                );

        }

        private async Task LoadFromDisk()
        {
            // TODO - load from disk
        }

        private async Task PushToRemote()
        {
            lastPushResult = scriptInvoker.Invoke("$/Config/commitAndPush.ps1", new { }, options);
            await lastPushResult;
        }
    }
}