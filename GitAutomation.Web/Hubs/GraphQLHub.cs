using GitAutomation.State;
using GitAutomation.Web.GraphQL;
using GitAutomation.Web.State;
using Microsoft.AspNetCore.SignalR;
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

        public GraphQLHub(IStateMachine<AppState> stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        public ChannelReader<string> Query(string graphql)
        {
            var ast = graphql.AsGraphQlAst();
            var observable = stateMachine.StateUpdates.Select(e => ast.ToJson(e.State).ToString());

            return observable.AsChannelReader(Context.ConnectionAborted);
        }
    }
}
