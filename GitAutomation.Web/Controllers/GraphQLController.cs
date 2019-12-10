using GitAutomation.Web.GraphQL;
using GitAutomation.State;
using GitAutomation.Web.State;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Web.Controllers
{
    // TODO - replace this with GraphLinqQL
    [Route("api/[controller]")]
    public class GraphQLController : Controller
    {
        private readonly IStateMachine<AppState> stateMachine;

        public GraphQLController(IStateMachine<AppState> stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        [HttpPost]
        public IActionResult Post([FromBody] GraphQlBody body)
        {
            return Ok(new { data = body.Query.AsGraphQlAst().ToJson(stateMachine.State) });
        }

        public class GraphQlBody
        {
#nullable disable warnings

            public string OperationName { get; set; }
            public string Query { get; set; }
            public object Variables { get; set; }
        }
    }
}
