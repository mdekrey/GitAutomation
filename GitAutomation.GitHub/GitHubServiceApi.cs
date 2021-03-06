﻿using GitAutomation.GitService;
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
using System.Collections.Concurrent;
using System.Threading;

namespace GitAutomation.GitHub
{
    class GitHubServiceApi : IGitServiceApi, IGitHubPullRequestChanges, IGitHubStatusChanges
    {
        struct AsyncData<T>
        {

        }

        private const string pageInfoFragment = @"
fragment pageInfo on PageInfo {
  hasNextPage
  endCursor
}";
        private const string rateLimitFragment = @"
fragment rateLimit on Query {
    rateLimit {
        cost
        limit
        nodeCount
        remaining
        resetAt
    }
}";
        private const string pullRequestFragment = @"
fragment pullRequest on PullRequest {
  number
  createdAt
  title
  author {
    ...user
  }
  state
  mergeable
  baseRefName
  headRefName
  resourcePath
}";
        private const string reviewFragment = @"
fragment review on PullRequestReview {
  author {
    ...user
  }
  state
  resourcePath
  createdAt
}";
        private const string userFragment = @"
fragment user on User {
  login
  avatarUrl
}";

        private static Regex githubUrlParse = new Regex(@"^/?(?<owner>[^/]+)/(?<repository>[^/]+)");

        private readonly ConcurrentDictionary<string, ImmutableList<CommitStatus>> commitStatus = new ConcurrentDictionary<string, ImmutableList<CommitStatus>>();
        private readonly AsyncLazy<ConcurrentDictionary<string, PullRequest>> pullRequests;

        private readonly GitRepositoryOptions repositoryOptions;
        private readonly GithubServiceApiOptions serviceOptions;

        private readonly string owner;
        private readonly string repository;
        private readonly string username;
        private readonly HttpClient client;
        private readonly GraphQLClient graphqlClient;

        public GitHubServiceApi(IOptions<GitRepositoryOptions> options, IOptions<GithubServiceApiOptions> serviceOptions, Func<HttpClient> clientFactory)
        {
            // TODO - ETag/304 the GET requests
            this.repositoryOptions = options.Value;
            this.serviceOptions = serviceOptions.Value;
            var repository = new UriBuilder(this.repositoryOptions.Repository);
            this.username = repository.UserName;
            var match = githubUrlParse.Match(repository.Path);
            this.owner = match.Groups["owner"].Value;
            this.repository = match.Groups["repository"].Value;
            this.client = BuildHttpClient(clientFactory);
            this.graphqlClient = BuildGraphqlClient(clientFactory);
            pullRequests = new AsyncLazy<ConcurrentDictionary<string, PullRequest>>(async () => await PullRequests());
        }

        async Task<ConcurrentDictionary<string, PullRequest>> PullRequests()
        {
            var query = @"
query($owner: String!, $repository: String!, $states: [PullRequestState!], $targetBranch: String, $sourceBranch: String, $includeReviews: Boolean!, $after: String) {
  repository(owner: $owner, name: $repository) {
    pullRequests(
      first: 100,
      after: $after,
      states: $states,
      orderBy: { field: CREATED_AT, direction: DESC },
      baseRefName: $targetBranch,
      headRefName: $sourceBranch
    ) {
      pageInfo {
        ...pageInfo
      }
      nodes {
        ...pullRequest
        reviews(first: 10) @include(if: $includeReviews) {
          nodes {
        	...review
          }
        }
      }
    }
  }
  ...rateLimit
}" + pageInfoFragment + rateLimitFragment + pullRequestFragment + reviewFragment + userFragment;
            var pullRequests = new List<PullRequest>();

            string after = null;

            do
            {

                var data = await graphqlClient.Query(query, new
                {
                    owner,
                    repository,
                    after,
                    states = new[] { "CLOSED", "OPEN", "MERGED" },
                    includeReviews = true
                });

                pullRequests.AddRange(
                    from entry in data["repository"]["pullRequests"]["nodes"] as JArray
                    select new PullRequest
                    {
                        Id = entry["number"].ToString(),
                        Created = entry["createdAt"].ToString(),
                        Author = entry["author"]["login"].ToString(),
                        IsSystem = entry["author"]["login"].ToString() == username,
                        State = entry["state"].ToString() == "OPEN" ? PullRequestState.Open : PullRequestState.Closed,
                        TargetBranch = entry["baseRefName"].ToString(),
                        SourceBranch = entry["headRefName"].ToString(),
                        Url = ResourcePathToUrl(entry["resourcePath"].ToString()),
                        Reviews = serviceOptions.CheckPullRequestReviews
                        ? ToPullRequestReviews(entry["reviews"]["nodes"] as JArray)
                        : ImmutableList<PullRequestReview>.Empty
                    });

                after = data["repository"]["pullRequests"]["pageInfo"].Value<bool>("hasNextPage")
                    ? data["repository"]["pullRequests"]["pageInfo"]["endCursor"].ToString()
                    : null;
            } while (after != null);

            return new ConcurrentDictionary<string, PullRequest>(pullRequests.ToDictionary(pr => pr.Id, pr => pr));
        }

        async Task<bool> IGitServiceApi.OpenPullRequest(string title, string targetBranch, string sourceBranch, string body)
        {
            var hasPr = await this.HasOpenPullRequest(targetBranch, sourceBranch);

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

        public async Task<ImmutableList<PullRequest>> GetPullRequests(PullRequestState? state = PullRequestState.Open, string targetBranch = null, string sourceBranch = null, bool includeReviews = false, PullRequestAuthorMode authorMode = PullRequestAuthorMode.All)
        {
            return (from pr in (await pullRequests.Value).Values
                    where pr.State == (state ?? pr.State)
                    where pr.TargetBranch == (targetBranch ?? pr.TargetBranch)
                    where pr.SourceBranch == (sourceBranch ?? pr.SourceBranch)
                    where (authorMode == PullRequestAuthorMode.SystemOnly && pr.Author == username)
                        || (authorMode == PullRequestAuthorMode.NonSystemOnly && pr.Author != username)
                        || authorMode == PullRequestAuthorMode.All
                    orderby pr.Created descending
                    select pr).ToImmutableList();
        }

        private ImmutableList<PullRequestReview> ToPullRequestReviews(JArray data)
        {
            return (from entry in data
                    let user = entry["author"]["login"].ToString()
                    let state = GitHubConverters.ToApprovalState(entry["state"].ToString())
                    where state.HasValue
                    group new
                    {
                        state = state.Value,
                        createdAt = entry["createdAt"].ToString(),
                        resourcePath = entry["resourcePath"].ToString()
                    } by user into states
                    let target = states.OrderByDescending(t => t.createdAt).Last()
                    select new PullRequestReview
                    {
                        State = target.state,
                        SubmittedDate = target.createdAt,
                        Url = ResourcePathToUrl(target.resourcePath.ToString()),
                        Author = states.Key,
                    }).ToImmutableList();
        }

        public Task<string> GetBranchUrl(string name)
        {
            return Task.FromResult<string>(ResourcePathToUrl($"/{owner}/{repository}/tree/{name}"));
        }

        private string ResourcePathToUrl(string v)
        {
            return "https://www.github.com" + v;
        }

        public async Task MigrateOrClosePullRequests(string fromBranch, string toBranch)
        {
            if (!serviceOptions.MigratePullRequests)
            {
                return;
            }
            var openPRs = await GetPullRequests(state: PullRequestState.Open, targetBranch: fromBranch);
            foreach (var openPR in openPRs)
            {
                using (var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod("PATCH"), $"/repos/{owner}/{repository}/pulls/{openPR.Id}")
                {
                    Content = JsonContent(new
                    {
                        @base = toBranch
                    })
                }))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        var jobject = JObject.Parse(await response.Content.ReadAsStringAsync());
                        try
                        {
                            var alreadyMerged = jobject["errors"][0]["message"].ToString().StartsWith("There are no new commits between base branch");
                            if (alreadyMerged)
                            {
                                await client.SendAsync(new HttpRequestMessage(new HttpMethod("PATCH"), $"/repos/{owner}/{repository}/pulls/{openPR.Id}")
                                {
                                    Content = JsonContent(new
                                    {
                                        state = "closed"
                                    })
                                });
                            }
                        }
                        catch { }

                        await client.PostAsync($"/repos/{owner}/{repository}/issues/{openPR.Id}/comments", JsonContent(new
                        {
                            body = $"Could not automatically migrate to {toBranch}. Either this branch has been merged or there was some other error.\n" +
                            "\n" +
                            $"    {string.Join("\n    ", (jobject.ToString(Formatting.Indented)).Split('\n'))}",
                        }));
                    }
                }
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
                Convert.ToBase64String(Encoding.ASCII.GetBytes($"{this.username}:{this.serviceOptions.Password}"))
            );
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "mdekrey-GitAutomation");
            return client;
        }

        private GraphQLClient BuildGraphqlClient(Func<HttpClient> clientFactory)
        {
            var client = clientFactory();
            client.BaseAddress = new Uri("https://api.github.com/graphql");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes($"{this.username}:{this.serviceOptions.Password}"))
            );
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "mdekrey-GitAutomation");
            return new GraphQLClient(client);
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

        #region Status

        public async Task<ImmutableDictionary<string, ImmutableList<CommitStatus>>> GetCommitStatuses(ImmutableList<string> commitShas)
        {
            if (!serviceOptions.CheckStatus)
            {
                return commitShas.Distinct().ToImmutableDictionary(sha => sha, _ => ImmutableList<CommitStatus>.Empty);
            }

            var needed = commitShas.Where(sha => !commitStatus.ContainsKey(sha)).ToHashSet();

            if (needed.Count > 0)
            {
                var requests = string.Join("\n    ",
                    from sha in needed.Distinct()
                    select $"_{sha}: object(oid: \"{sha}\") {{ ...commitStatus }}"
                );

                var data = await graphqlClient.Query(@"
query($owner: String!, $repository: String!) {
  repository(owner: $owner, name: $repository) {
    " + requests + @"
  }
  ...rateLimit
}

fragment commitStatus on Commit {
  status {
    contexts {
      context
      targetUrl
      description
      state
    }
    state
  }
}
" + rateLimitFragment, new
                {
                    owner,
                    repository
                }).ConfigureAwait(false);

                var result = (from sha in commitShas.Distinct()
                              select new
                              {
                                  sha,
                                  result = data["repository"]["_" + sha] != null 
                                            && data["repository"]["_" + sha].Type != JTokenType.Null
                                            && data["repository"]["_" + sha]["status"].Type != JTokenType.Null
                                            ? from entry in data["repository"]["_" + sha]["status"]["contexts"] as JArray
                                              select new CommitStatus
                                              {
                                                  Key = entry["context"].ToString(),
                                                  Url = entry["targetUrl"].ToString(),
                                                  Description = entry["description"].ToString(),
                                                  State = GitHubConverters.ToCommitState(entry["state"].ToString())
                                              }
                                            : Enumerable.Empty<CommitStatus>()
                              }).ToImmutableDictionary(v => v.sha, v => v.result.ToImmutableList());

                foreach (var entry in needed)
                {
                    commitStatus.TryAdd(entry, result[entry]);
                }
            }
            return commitShas.Distinct().ToImmutableDictionary(sha => sha, sha => commitStatus[sha]);
        }

        public void ReceiveCommitStatus(string commitSha, CommitStatus status)
        {
            if (commitStatus.ContainsKey(commitSha))
            {
                commitStatus.AddOrUpdate(commitSha, 
                    ImmutableList<CommitStatus>.Empty, // We already checked the key; should not hit this "add".
                    (_, oldValue) => oldValue.Where(s => s.Key != status.Key).Append(status).ToImmutableList());
            }
        }

        #endregion

        #region PRs

        public async void ReceivePullRequestUpdate(PullRequest pullRequest)
        {
            var target = await pullRequests.Value;

            pullRequest.IsSystem = pullRequest.Author == username;
            pullRequest.Reviews = ImmutableList<PullRequestReview>.Empty;
            target.AddOrUpdate(pullRequest.Id, pullRequest, (id, original) =>
            {
                pullRequest.Reviews = original.Reviews;
                return pullRequest;
            });
        }
        public async void ReceivePullRequestReview(string id, PullRequestReview review, bool remove)
        {
            var target = await pullRequests.Value;

            if (target.ContainsKey(id))
            {
                target.AddOrUpdate(id, addValue: null, updateValueFactory: (_, oldValue) =>
                {
                    oldValue.Reviews = oldValue.Reviews
                        .RemoveAll(oldReview => oldReview.Author == review.Author && oldReview.SubmittedDate.CompareTo(review.SubmittedDate) <= 0)
                        .AddRange(remove ? new PullRequestReview[0] : new[] { review });
                    return oldValue;
                });
            }
        }

        #endregion
    }
}
