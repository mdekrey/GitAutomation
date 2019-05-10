using System;
using System.Management.Automation;
using System.Threading.Tasks;
using GitAutomation.Extensions;
using Microsoft.Extensions.Options;

namespace GitAutomation.Web
{
    internal class RepositoryConfigurationService
    {
        private readonly ConfigRepositoryOptions options;

        public RepositoryConfigurationService(IOptions<ConfigRepositoryOptions> options)
        {
            this.options = options.Value;
        }

        internal async Task LoadAsync()
        {

            using (var psInstance = PowerShell.Create())
            {
                psInstance
                    .AddUnrestrictedCommand("./Scripts/Config/Clone.ps1")
                    .BindParametersToPowerShell(options);

                var results = await psInstance.InvokeAsync();
            }

        }
    }
}