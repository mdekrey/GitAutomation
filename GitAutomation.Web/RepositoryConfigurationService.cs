using System;
using System.Management.Automation;
using System.Threading.Tasks;
using GitAutomation.DomainModels;
using GitAutomation.Extensions;
using GitAutomation.Web.Scripts;
using Microsoft.Extensions.Options;
using static GitAutomation.DomainModels.RepositoryStructureReducer;

namespace GitAutomation.Web
{
    internal class RepositoryConfigurationService
    {
        private readonly ConfigRepositoryOptions options;
        private readonly PowerShellScriptInvoker scriptInvoker;

        public RepositoryConfigurationService(IOptions<ConfigRepositoryOptions> options, PowerShellScriptInvoker scriptInvoker)
        {
            this.options = options.Value;
            this.scriptInvoker = scriptInvoker;
        }

        internal async Task LoadAsync()
        {
            var streams = scriptInvoker.Invoke("$/Config/clone.ps1", new { }, options);

            try
            {
                await streams.Completion;
                var result = streams.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}