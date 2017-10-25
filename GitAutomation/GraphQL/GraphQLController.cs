using GitAutomation.GraphQL.Utilities;
using GraphQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.GraphQL
{
    [Route("api/[controller]")]
    public class GraphQLController : Controller
    {
        private readonly ILogger _logger;

        public GraphQLController(ILogger<GraphQLController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            _logger.LogInformation("Got GET request for GraphiQL.");
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] GraphQLQuery query, [FromServices] Utilities.GraphQLExecutor executor)
        {
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            
            var result = await executor.Execute(query).ConfigureAwait(false);

            if (result.Errors?.Count > 0)
            {
                _logger.LogError("GraphQL errors: {0}", result.Errors);
                return BadRequest(result);
            }

            _logger.LogDebug("GraphQL execution result: {result}", JsonConvert.SerializeObject(result.Data));
            return Ok(result);
        }
    }

}
