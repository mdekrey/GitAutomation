using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.GitHub
{
    [Route("api/[controller]")]
    public class GitHubWebHookController : Controller
    {
        [HttpPost]
        public void Post([FromBody] JObject contents)
        {
            // TODO - add support to verify signature
            switch (Request.Headers["X-GitHub-Event"])
            {
                case "create":
                case "delete":
                case "push":
                    // refs updated
                    // TODO
                    break;
                case "pull_request":
                case "pull_request_review":
                    // pull request updates
                    // TODO
                    break;
                case "status":
                    // status checks
                    // TODO
                    break;
            }
        }
    }
}
