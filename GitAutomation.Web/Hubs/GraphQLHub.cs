using GitAutomation.State;
using GitAutomation.Web.GraphQL;
using GitAutomation.Web.State;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GitAutomation.Web.Hubs
{

    public class GraphQLHub : Hub
    {
        private readonly IStateMachine<AppState> stateMachine;
        private readonly ILogger logger;

        public GraphQLHub(IStateMachine<AppState> stateMachine, ILogger<GraphQLHub> logger)
        {
            this.stateMachine = stateMachine;
            this.logger = logger;
        }

        public ChannelReader<string> Query(string graphql, CancellationToken cancellation)
        {
            var ast = graphql.AsGraphQlAst();
            var observable = stateMachine.StateUpdates.Select(e => ast.ToJson(e.Payload).ToString())
                .DistinctUntilChanged();

            var allCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation, Context.ConnectionAborted);
            return observable.AsChannelReader(allCancellation.Token);
        }
    }
}
