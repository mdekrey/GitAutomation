using GitAutomation.Repository;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.GitHub
{
    [Route("api/[controller]")]
    public class GitHubWebHookController : Controller
    {
        const string refPrefix = "refs/heads/";

        [HttpPost]
        public void Post([FromBody] JObject contents, [FromServices] IGitCli cli, [FromServices] IRemoteRepositoryState repositoryState)
        {
            // TODO - add support to verify signature
            switch (Request.Headers["X-GitHub-Event"])
            {
                case "create":
                case "delete":
                case "push":
                    // refs updated
                    UpdateRef(contents, cli, repositoryState);
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

        private async void UpdateRef(JObject contents, IGitCli cli, IRemoteRepositoryState repositoryState)
        {
            var refName = contents["ref"].ToString();
            var branchName = refName.StartsWith(refPrefix) ? refName.Substring(refPrefix.Length) : refName;
            if (branchName == null)
            {
                return;
            }

            var targetRef = Request.Headers["X-GitHub-Event"] == "create"
                ? await GetRefFor(branchName, cli)
                : Request.Headers["X-GitHub-Event"] == "push"
                    ? contents["after"].ToString()
                    : null;
            repositoryState.BranchUpdated(branchName, targetRef);
        }

        private async Task<string> GetRefFor(string branchName, IGitCli cli)
        {
            await cli.Fetch(branchName).ActiveState;
            return (await cli.ShowRef(branchName).ActiveOutput.FirstOrDefaultAsync()).Message;
        }
    }
}
