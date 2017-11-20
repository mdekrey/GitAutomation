using GitAutomation.Processes;
using GitAutomation.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Orchestration.Actions
{
    class ClearAction : ComplexAction<ClearAction.Internal>
    {
        
        public override string ActionType => "Clear";

        public override JToken Parameters => JToken.FromObject(
            ImmutableDictionary<string, string>.Empty
        );
        
        public class Internal : ComplexActionInternal
        {
            private readonly string checkoutPath;

            public Internal(IOptions<GitRepositoryOptions> options)
            {
                this.checkoutPath = options.Value.CheckoutPath;
            }
            
            protected override Task RunProcess()
            {
                if (Directory.Exists(checkoutPath))
                {
                    Directory.Delete(checkoutPath, true);
                }
                return Task.CompletedTask;
            }
        }
    }
}
