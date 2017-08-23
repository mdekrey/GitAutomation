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

namespace GitAutomation.GitService
{
    class GitHubServiceApi : IGitServiceApi
    {
        private static Regex githubUrlParse = new Regex(@"^/?(?<owner>[^/]+)/(?<repository>[^/]+)");

        private readonly GitRepositoryOptions options;
        private readonly Func<HttpClient> clientFactory;
        private readonly string owner;
        private readonly string repository;
        private readonly string username;

        public GitHubServiceApi(IOptions<GitRepositoryOptions> options, Func<HttpClient> clientFactory)
        {
            this.options = options.Value;
            var repository = new UriBuilder(this.options.Repository);
            this.username = repository.UserName;
            var match = githubUrlParse.Match(repository.Path);
            this.owner = match.Groups["owner"].Value;
            this.repository = match.Groups["repository"].Value;
            this.clientFactory = clientFactory;
        }

        public async Task<bool> OpenPullRequest(string title, string targetBranch, string sourceBranch, string body = null)
        {
            using (var client = BuildHttpClient())
            {
                var hasPr = await HasOpenPullRequest(client, targetBranch: targetBranch, sourceBranch: sourceBranch);

                if (!hasPr)
                {
                    return await OpenPullRequest(client, title: title, targetBranch: targetBranch, sourceBranch: sourceBranch, body: body);
                }

                return true;
            }
        }

        private async Task<bool> OpenPullRequest(HttpClient client, string title, string targetBranch, string sourceBranch, string body)
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
            using (var client = BuildHttpClient())
            {
                return await HasOpenPullRequest(client, targetBranch: targetBranch, sourceBranch: sourceBranch);
            }
        }

        private async Task<bool> HasOpenPullRequest(HttpClient client, string targetBranch = null, string sourceBranch = null)
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
                nvc.Add("base", FullBranchRef(targetBranch));
            }
            using (var response = await client.GetAsync($"/repos/{owner}/{repository}/pulls{ToQueryString(nvc)}"))
            {
                await EnsureSuccessStatusCode(response);
                var responseString = await response.Content.ReadAsStringAsync();
                var token = Newtonsoft.Json.Linq.JArray.Parse(responseString);
                return token.Count >= 1;
            }
        }

        private string FullBranchRef(string branchName) =>
            $"{owner}:{branchName}";

        private HttpClient BuildHttpClient()
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

    }
}
