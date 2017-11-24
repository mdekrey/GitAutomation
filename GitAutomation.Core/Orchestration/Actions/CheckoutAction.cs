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

namespace GitAutomation.Orchestration.Actions
{
    class CheckoutAction : CliAction
    {
        private readonly Subject<OutputMessage> output = new Subject<OutputMessage>();

        public override string ActionType => "Checkout";

        public CheckoutAction(string branch)
        {
            this.branch = branch;
        }

        private readonly string branch;

        public override JToken Parameters => JToken.FromObject(new Dictionary<string, string>
            {
                { "branch", branch }
            }.ToImmutableDictionary());
        
        protected override IReactiveProcess GetCliAction(IGitCli gitCli) =>
            gitCli.CheckoutRemote(branch);
    }
}
