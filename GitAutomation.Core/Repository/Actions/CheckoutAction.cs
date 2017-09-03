using GitAutomation.Processes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace GitAutomation.Repository.Actions
{
    class CheckoutAction : CliAction
    {
        private readonly Subject<OutputMessage> output = new Subject<OutputMessage>();

        public override string ActionType => "Checkout";

        public CheckoutAction(string branch)
        {
            this.Parameters = new Dictionary<string, string>
            {
                { "branch", branch }
            }.ToImmutableDictionary();
        }

        public override ImmutableDictionary<string, string> Parameters { get; }
        
        protected override IReactiveProcess GetCliAction(GitCli gitCli) =>
            gitCli.CheckoutRemote(Parameters["branch"]);
    }
}
