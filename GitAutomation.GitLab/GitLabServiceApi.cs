using GitAutomation.GitService;
using GitAutomation.Repository;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.WebUtility;

namespace GitAutomation.GitLab
{
    class GitLabServiceApi : IGitServiceApi
    {
        private readonly GitRepositoryOptions repositoryOptions;
        private readonly GitLabServiceApiOptions serviceOptions;
        private readonly HttpClient client;
        private readonly Uri repositoryUri;

        public GitLabServiceApi(IOptions<GitRepositoryOptions> options, IOptions<GitLabServiceApiOptions> serviceOptions, Func<HttpClient> clientFactory)
        {
            this.repositoryOptions = options.Value;
            this.serviceOptions = serviceOptions.Value;

            var repository = new UriBuilder(repositoryOptions.Repository)
            {
                UserName = null,
                Password = null,
            };
            repository.Path = Regex.Replace(repository.Path, @"\.git$", "");
            repositoryUri = repository.Uri;

            this.client = BuildHttpClient(clientFactory);
        }
        
        public Task<string> GetBranchUrl(string name)
        {
            return Task.FromResult<string>(ResourcePathToUrl($"/tree/{name}"));
        }

        private string ResourcePathToUrl(string extraPath)
        {
            return repositoryUri.ToString() + extraPath;
        }

        public Task<ImmutableDictionary<string, ImmutableList<CommitStatus>>> GetCommitStatuses(ImmutableList<string> commitShas)
        {
            // TODO, if we want to support this
            return Task.FromResult(commitShas.Distinct().ToImmutableDictionary(sha => sha, _ => ImmutableList<CommitStatus>.Empty));
        }

        public async Task<ImmutableList<PullRequest>> GetPullRequests(PullRequestState? state = PullRequestState.Open, string targetBranch = null, string sourceBranch = null, bool includeReviews = false, PullRequestAuthorMode authorMode = PullRequestAuthorMode.All)
        {
            var path = $"projects/{serviceOptions.ProjectId}/merge_requests?scope=all";
            if (state != null)
            {
                switch (state.Value) {
                    case PullRequestState.Closed:
                        path += "&state=merged";
                        break;
                    case PullRequestState.Open:
                        path += "&state=opened";
                        break;
                }
            }
            path += targetBranch == null ? "" : $"&target_branch={UrlEncode(targetBranch)}";
            path += sourceBranch == null ? "" : $"&source_branch={UrlEncode(sourceBranch)}";
            var pullRequestsJson = await client.GetStringAsync(path);
            var prs = from entry in JsonConvert.DeserializeObject<JArray>(pullRequestsJson)
                      select new PullRequest
                      {
                          Id = entry.Value<string>("id"),
                          Author = entry["author"].Value<string>("username"),
                          Created = entry.Value<string>("created_at"),
                          Reviews = ImmutableList<PullRequestReview>.Empty,
                          SourceBranch = entry.Value<string>("source_branch"),
                          TargetBranch = entry.Value<string>("target_branch"),
                          State = entry.Value<string>("state") == "opened" ? PullRequestState.Open : PullRequestState.Closed,
                          Url = entry.Value<string>("web_url"),
                          IsSystem = entry["author"].Value<string>("username") == serviceOptions.Username
                      };

            return prs.ToImmutableList();
        }

        public Task MigrateOrClosePullRequests(string fromBranch, string toBranch)
        {
            // TODO - what did this do again? It appears to also be optional in GitHub, so support it if we need to.
            return Task.FromResult<object>(null);
        }

        public async Task<bool> OpenPullRequest(string title, string targetBranch, string sourceBranch, string body = null)
        {
            var response = await client.PostAsync($"projects/{serviceOptions.ProjectId}/merge_requests", new StringContent(
                JsonConvert.SerializeObject(new
                {
                    title,
                    target_branch = targetBranch,
                    source_branch = sourceBranch,
                    description = body
                }), Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }

        private HttpClient BuildHttpClient(Func<HttpClient> clientFactory)
        {
            var client = clientFactory();

            var repository = new UriBuilder(repositoryOptions.Repository)
            {
                UserName = null,
                Password = null,
                Path = "/api/v4/"
            };

            client.DefaultRequestHeaders.Add("Private-Token", serviceOptions.PersonalAccessToken);
            client.BaseAddress = repository.Uri;
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "mdekrey-GitAutomation");
            return client;
        }

    }
}
