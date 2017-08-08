using GitAutomation.Processes;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Text;

namespace GitAutomation.Repository
{
    class RepositoryState : IRepositoryState
    {
        private readonly string checkoutPath;
        private readonly IReactiveProcessFactory reactiveProcessFactory;
        private readonly string repository;

        public RepositoryState(IReactiveProcessFactory factory, IOptions<GitRepositoryOptions> options)
        {
            this.reactiveProcessFactory = factory;
            this.repository = options.Value.Repository;
            this.checkoutPath = options.Value.CheckoutPath;
        }

        public IObservable<string> Reset()
        {

            if (Directory.Exists(checkoutPath))
            {
                // starting from an old system? maybe... but I don't want to handle that yet.
                Directory.Delete(checkoutPath, true);
            }
            return Observable.Empty<string>();
        }

        public IObservable<string> Initialize()
        {
            var info = Directory.CreateDirectory(checkoutPath);
            var children = info.GetFileSystemInfos();
            var proc = reactiveProcessFactory.BuildProcess(new System.Diagnostics.ProcessStartInfo("git", $"clone {repository} \"{checkoutPath.Replace(@"\", @"\\").Replace("\"", "\\\"")}\""));
            return proc.Output.Select(msg => msg.Message);
        }
    }
}
