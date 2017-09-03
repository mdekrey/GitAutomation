﻿using GitAutomation.Processes;
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
    class ClearAction : IRepositoryAction
    {
        private readonly Subject<OutputMessage> output = new Subject<OutputMessage>();

        public string ActionType => "Clear";

        public ImmutableDictionary<string, string> Parameters =>
            ImmutableDictionary<string, string>.Empty;

        public IObservable<OutputMessage> DeferredOutput => output;

        public IObservable<OutputMessage> PerformAction(IServiceProvider serviceProvider)
        {
            var checkoutPath = serviceProvider.GetRequiredService<IOptions<GitRepositoryOptions>>()
                .Value.CheckoutPath;
            if (Directory.Exists(checkoutPath))
            {
                // starting from an old system? maybe... but I don't want to handle that yet.
                Directory.Delete(checkoutPath, true);
            }
            Observable.Empty<OutputMessage>()
                .Multicast(output).Connect();
            return output;
        }
    }
}
