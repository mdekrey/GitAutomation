using GitAutomation.GitService;
using GitAutomation.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
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
        public void Post([FromBody] JObject contents, [FromServices] IServiceProvider serviceProvider)
        {
            // TODO - add support to verify signature
            switch (Request.Headers["X-GitHub-Event"])
            {
                case "create":
                case "delete":
                case "push":
                    // refs updated
                    UpdateRef(contents, serviceProvider.GetRequiredService<IGitCli>(), serviceProvider.GetRequiredService<IRemoteRepositoryState>());
                    break;
                case "pull_request":
                    UpdatePullRequest(contents, serviceProvider.GetRequiredService<IGitHubPullRequestChanges>());
                    break;
                case "pull_request_review":
                    UpdatePullRequestReview(contents, serviceProvider.GetRequiredService<IGitHubPullRequestChanges>());
                    break;
                case "status":
                    // status checks
                    UpdateStatus(contents, serviceProvider.GetRequiredService<IGitHubStatusChanges>());
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


        private void UpdateStatus(JObject contents, IGitHubStatusChanges gitHubStatusChanges)
        {
            gitHubStatusChanges.ReceiveCommitStatus(contents["sha"].ToString(),
                new GitService.CommitStatus
                {
                    Key = contents["context"].ToString(),
                    Description = contents["description"].ToString(),
                    Url = contents["target_url"].ToString(),
                    State = GitHubConverters.ToCommitState(contents["state"].ToString()),
                });
        }


        private void UpdatePullRequest(JObject contents, IGitHubPullRequestChanges gitHubPullRequestChanges)
        {
            var entry = contents["pull_request"];
            gitHubPullRequestChanges.ReceivePullRequestUpdate(new GitService.PullRequest
            {
                Id = entry["number"].ToString(),
                Created = entry["created_at"].Value<DateTimeOffset>().ToString("s", System.Globalization.CultureInfo.InvariantCulture),
                Author = entry["user"]["login"].ToString(),
                State = entry["state"].ToString().ToUpper() == "OPEN" ? PullRequestState.Open : PullRequestState.Closed,
                TargetBranch = entry["base"]["ref"].ToString(),
                SourceBranch = entry["head"]["ref"].ToString(),
                Url = entry["_links"]["html"]["href"].ToString(),
                Reviews = null
            });
        }

        private void UpdatePullRequestReview(JObject contents, IGitHubPullRequestChanges gitHubPullRequestChanges)
        {
            var targetPrId = contents["pull_request"]["number"].ToString();
            var state = GitHubConverters.ToApprovalState(contents["review"]["state"].ToString());
            if (state.HasValue)
            {
                gitHubPullRequestChanges.ReceivePullRequestReview(id: targetPrId, review: new GitService.PullRequestReview
                {
                    Author = contents["review"]["user"]["login"].ToString(),
                    State = state.Value,
                    SubmittedDate = contents["review"].Value<DateTimeOffset>("submitted_at").ToString("s", System.Globalization.CultureInfo.InvariantCulture),
                    Url = contents["review"]["_links"]["html"]["href"].ToString()

                }, remove: contents["action"].ToString() == "dismissed");
            }
        }

    }
}
