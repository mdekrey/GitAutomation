using GitAutomation.GitService;
using GitAutomation.Repository;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Newtonsoft.Json.Linq;

namespace GitAutomation.GitHub
{
    class GitHubServiceApi : IGitServiceApi
    {
        private static Regex githubUrlParse = new Regex(@"^/?(?<owner>[^/]+)/(?<repository>[^/]+)");

        private readonly GitRepositoryOptions options;
        private readonly string owner;
        private readonly string repository;
        private readonly string username;
        private readonly HttpClient client;

        public GitHubServiceApi(IOptions<GitRepositoryOptions> options, Func<HttpClient> clientFactory)
        {
            // TODO - ETag/304 the GET requests
            this.options = options.Value;
            var repository = new UriBuilder(this.options.Repository);
            this.username = repository.UserName;
            var match = githubUrlParse.Match(repository.Path);
            this.owner = match.Groups["owner"].Value;
            this.repository = match.Groups["repository"].Value;
            this.client = BuildHttpClient(clientFactory);
        }

        async Task<bool> IGitServiceApi.OpenPullRequest(string title, string targetBranch, string sourceBranch, string body)
        {
            var hasPr = await HasOpenPullRequest(targetBranch: targetBranch, sourceBranch: sourceBranch);

            if (!hasPr)
            {
                return await OpenPullRequest(title: title, targetBranch: targetBranch, sourceBranch: sourceBranch, body: body);
            }

            return true;
        }

        private async Task<bool> OpenPullRequest(string title, string targetBranch, string sourceBranch, string body)
        {
            using (var response = await client.PostAsync($"/repos/{owner}/{repository}/pulls", JsonContent(new
            {
                title,
                @base = targetBranch,
                head = FullBranchRef(sourceBranch),
                body,
                maintainer_can_modify = true,
            })))
            {
                await EnsureSuccessStatusCode(response);
                return true;
            }
        }
        
        public async Task<bool> HasOpenPullRequest(string targetBranch = null, string sourceBranch = null)
        {
            var nvc = new NameValueCollection
            {
                { "state", "open" }
            };
            if (sourceBranch != null)
            {
                nvc.Add("head", FullBranchRef(sourceBranch));
            }
            if (targetBranch != null)
            {
                nvc.Add("base", targetBranch);
            }
            using (var response = await client.GetAsync($"/repos/{owner}/{repository}/pulls{ToQueryString(nvc)}"))
            {
                await EnsureSuccessStatusCode(response);
                var responseString = await response.Content.ReadAsStringAsync();
                var token = JArray.Parse(responseString);
                return token.Count >= 1;
            }
        }

        public Task<ImmutableList<PullRequestReview>> GetPullRequestReviews(string targetBranch)
        {
            throw new NotImplementedException();
        }

        public async Task<ImmutableList<CommitStatus>> GetCommitStatus(string commitSha)
        {

            var nvc = new NameValueCollection
            {
                { "state", "open" }
            };
            using (var response = await client.GetAsync($"/repos/{owner}/{repository}/commits/{commitSha}/status"))
            {
                await EnsureSuccessStatusCode(response);
                var responseString = await response.Content.ReadAsStringAsync();
                var token = JToken.Parse(responseString);
                return (from status in token["statuses"] as JArray
                        select new CommitStatus
                        {
                            Key = status["context"].ToString(),
                            Url = status["url"].ToString(),
                            Description = status["description"].ToString(),
                            State = ToCommitState(status["state"].ToString())
                        }).ToImmutableList();
            }
        }

        private CommitStatus.StatusState ToCommitState(string value)
        {
            switch (value)
            {
                case "success": return CommitStatus.StatusState.Success;
                case "pending": return CommitStatus.StatusState.Pending;
                default: return CommitStatus.StatusState.Error;
            }
        }

        #region Utilities

        private string FullBranchRef(string branchName) =>
            $"{owner}:{branchName}";

        private HttpClient BuildHttpClient(Func<HttpClient> clientFactory)
        {
            var client = clientFactory();
            client.BaseAddress = new Uri("https://api.github.com");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes($"{this.username}:{this.options.Password}"))
            );
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "mdekrey-GitAutomation");
            return client;
        }


        private string ToQueryString(NameValueCollection nvc)
        {
            var array = (from key in nvc.AllKeys
                         from value in nvc.GetValues(key)
                         select string.Format("{0}={1}", WebUtility.UrlEncode(key), WebUtility.UrlEncode(value)))
                .ToArray();
            return "?" + string.Join("&", array);
        }

        public static HttpContent JsonContent<TModel>(TModel model)
        {
            var json = JsonConvert.SerializeObject(model);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private async Task EnsureSuccessStatusCode(HttpResponseMessage response)
        {
            if ((int)response.StatusCode < 200 || (int)response.StatusCode > 299)
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new Exception("Status code: " + response.StatusCode)
                {
                    Data = { { "Response", JsonConvert.DeserializeObject(content) } }
                };
            }
        }

        #endregion
    }
}
