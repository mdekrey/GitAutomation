using DataLoader;
using GitAutomation.BranchSettings;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitAutomation.Repository;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using GitAutomation.GitService;
using GitAutomation.GraphQL.Utilities;
using GitAutomation.Auth;
using Microsoft.Extensions.Logging;

namespace GitAutomation.GraphQL
{
    public class Loaders
    {
        private readonly ILogger<Loaders> logger;
        private readonly DataLoaderContext loadContext;
        private readonly IBranchSettingsAccessor branchSettings;
        private readonly IUserPermissionAccessor permissionAccessor;
        private readonly IRemoteRepositoryState repositoryState;
        private readonly IBranchIterationNamingConvention branchIteration;
        private readonly IGitServiceApi gitService;

        public Loaders(IDataLoaderContextAccessor loadContextAccessor, IBranchSettingsAccessor branchSettings, IUserPermissionAccessor permissionAccessor, IRemoteRepositoryState repositoryState, IBranchIterationNamingConvention branchIteration, IGitServiceApi gitService, ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<Loaders>();
            this.loadContext = loadContextAccessor.LoadContext;
            this.branchSettings = branchSettings;
            this.permissionAccessor = permissionAccessor;
            this.repositoryState = repositoryState;
            this.branchIteration = branchIteration;
            this.gitService = gitService;
        }

        public Task<BranchGroup> LoadBranchGroup(string name)
        {
            logger.LogInformation("Enqueue load branch group {0}", name);
            return loadContext.Factory.GetOrCreateLoader<string, BranchGroup>("GetBranchGroup", async keys =>
            {
                logger.LogInformation("Loading {0} branch groups", keys.Distinct().Count());
                var result = await branchSettings.GetBranchGroups(keys.Distinct().ToArray());
                return result.ToDictionary(e => e.Key, e => e.Value);
            }).LoadAsync(name);
        }

        internal async Task<string> GetMergeBaseOfCommitAndRemoteBranch(string commit, string branch)
        {
            logger.LogInformation("Get merge base between {0} and {1}", commit, branch);
            var refs = await LoadAllGitRefs().ConfigureAwait(false);

            var result = refs.FirstOrDefault(r => r.Name == branch);
            return result.Name == branch
                ? await GetMergeBaseOfCommits(commit, result.Commit).ConfigureAwait(false)
                : null;
        }

        internal Task<string> GetMergeBaseOfCommitAndGroup(string commit, string group)
        {
            logger.LogInformation("Get merge base between {0} and {1}", commit, group);
            return LoadLatestBranch(group).ContinueWith(t => GetMergeBaseOfCommits(commit, t.Result?.Commit)).Unwrap();
        }

        internal Task<string> GetMergeBaseOfCommits(string commit1, string commit2)
        {
            logger.LogInformation("Get merge base between {0} and {1}", commit1, commit2);
            return repositoryState.MergeBaseBetweenCommits(commit1, commit2);
        }

        public Task<ImmutableList<string>> LoadBranchGroups()
        {
            logger.LogInformation("Enqueue load all branch groups");
            return loadContext.Factory.GetOrCreateLoader("GetBranchGroups", async () =>
            {
                logger.LogInformation("Loading all branch groups");
                var result = await branchSettings.GetAllBranchGroups();
                return result.Select(group => group.GroupName).ToImmutableList();
            }).LoadAsync();
        }

        internal Task<ImmutableList<PullRequest>> LoadPullRequests(string source, string target, bool includeReviews, PullRequestAuthorMode authorMode)
        {
            // TODO - better loaders
            return gitService.GetPullRequests(state: null, targetBranch: target, sourceBranch: source, includeReviews: includeReviews, authorMode: authorMode);
        }

        internal Task<ImmutableList<string>> LoadDownstreamBranches(string name)
        {
            logger.LogInformation("Enqueue load downstream branches of {0}", name);
            return loadContext.Factory.GetOrCreateLoader<string, ImmutableList<string>>("GetDownstreamBranchGroups", async keys =>
            {
                logger.LogInformation("Loading {0} branch group downstream", keys.Distinct().Count());
                var result = await branchSettings.GetDownstreamBranchGroups(keys.Distinct().ToArray());
                return keys.Distinct().ToDictionary(k => k, key => result.ContainsKey(key) ? result[key] : ImmutableList<string>.Empty);
            }).LoadAsync(name);
        }

        internal Task<ImmutableList<string>> LoadUpstreamBranches(string name)
        {
            logger.LogInformation("Enqueue load upstream branches of {0}", name);
            return loadContext.Factory.GetOrCreateLoader<string, ImmutableList<string>>("GetUpstreamBranchGroups", async keys =>
            {
                logger.LogInformation("Loading {0} branch group upstream", keys.Distinct().Count());
                var result = await branchSettings.GetUpstreamBranchGroups(keys.Distinct().ToArray());
                return keys.Distinct().ToDictionary(k => k, key => result.ContainsKey(key) ? result[key] : ImmutableList<string>.Empty);
            }).LoadAsync(name);
        }

        internal Task<ImmutableList<CommitStatus>> LoadBranchStatus(string commitSha)
        {
            logger.LogInformation("Load commit status of {0}", commitSha);
            return loadContext.Factory.GetOrCreateLoader<string, ImmutableList<CommitStatus>>("GetUpstreamBranchGroups", async keys =>
            {
                logger.LogInformation("Loading {0} branch group upstream", keys.Distinct().Count());
                var result = await gitService.GetCommitStatuses(keys.Distinct().ToImmutableList());
                return keys.Distinct().ToDictionary(k => k, key => result.ContainsKey(key) ? result[key] : ImmutableList<CommitStatus>.Empty);
            }).LoadAsync(commitSha);
        }

        internal Task<ImmutableList<GitRef>> LoadAllGitRefs()
        {
            logger.LogInformation("Enqueue load all git refs");
            return loadContext.Factory.GetOrCreateLoader<ImmutableList<GitRef>>("GetAllGitRefs", async () =>
            {
                logger.LogInformation("Loading all git refs");
                return await repositoryState.RemoteBranches().FirstOrDefaultAsync();
            }).LoadAsync();
        }

        async internal Task<ImmutableList<GitRef>> LoadActualBranches(string name)
        {
            logger.LogInformation("Load actual branches of {0}", name);
            var refs = await LoadAllGitRefs().ConfigureAwait(false);
            return (from branch in refs
                    where branchIteration.IsBranchIteration(name, branch.Name)
                    select branch).ToImmutableList();
        }

        async internal Task<GitRef?> LoadLatestBranch(string name)
        {
            logger.LogInformation("Load latest branch of {0}", name);
            var refs = await LoadActualBranches(name).ConfigureAwait(false);
            var latestName = branchIteration.GetLatestBranchNameIteration(name, refs.Select(n => n.Name));
            return latestName == null ? (GitRef?)null : refs.SingleOrDefault(r => r.Name == latestName);
        }

        internal Task<ImmutableList<string>> GetUsers()
        {
            return loadContext.Factory.GetOrCreateLoader("GetAllUsers", () => permissionAccessor.GetUsers()).LoadAsync();
        }

        internal Task<ImmutableList<string>> LoadUsers(string role)
        {
            return loadContext.Factory.GetOrCreateLoader<string, ImmutableList<string>>("GetUsersByRole", async keys =>
            {
                var result = await permissionAccessor.GetUsersByRole(keys.Distinct().ToArray());
                return keys.Distinct().ToDictionary(e => e, e => result.ContainsKey(e) ? result[e] : ImmutableList<string>.Empty);
            }).LoadAsync(role);
        }

        internal Task<ImmutableList<string>> GetRoles()
        {
            return loadContext.Factory.GetOrCreateLoader("GetAllRoles", () => permissionAccessor.GetRoles()).LoadAsync();
        }

        internal Task<ImmutableList<string>> LoadRoles(string username)
        {
            return loadContext.Factory.GetOrCreateLoader<string, ImmutableList<string>>("GetRolesByUser", async keys =>
            {
                var result = await permissionAccessor.GetRolesByUser(keys.Distinct().ToArray());
                return keys.Distinct().ToDictionary(k => k, key => result.ContainsKey(key) ? result[key] : ImmutableList<string>.Empty);
            }).LoadAsync(username);
        }

    }
}
