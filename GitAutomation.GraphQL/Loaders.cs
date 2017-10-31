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
        private readonly IBranchSettingsAccessor branchSettings;
        private readonly IUserPermissionAccessor permissionAccessor;
        private readonly IRepositoryState repositoryState;
        private readonly IBranchIterationNamingConvention branchIteration;
        private readonly IGitServiceApi gitService;

        public Loaders(IBranchSettingsAccessor branchSettings, IUserPermissionAccessor permissionAccessor, IRepositoryState repositoryState, IBranchIterationNamingConvention branchIteration, IGitServiceApi gitService, ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<Loaders>();
            this.branchSettings = branchSettings;
            this.permissionAccessor = permissionAccessor;
            this.repositoryState = repositoryState;
            this.branchIteration = branchIteration;
            this.gitService = gitService;
        }

        public Task<BranchGroup> LoadBranchGroup(string name)
        {
            logger.LogInformation("Enqueue load branch group {0}", name);
            return branchSettings.GetBranchGroups(name).ContinueWith(t => t.Result[name]);
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

        public async Task<ImmutableList<string>> LoadBranchGroups()
        {
            logger.LogInformation("Enqueue load all branch groups");
            logger.LogInformation("Loading all branch groups");
            var result = await branchSettings.GetAllBranchGroups();
            return result.Select(group => group.GroupName).ToImmutableList();
        }

        internal Task<ImmutableList<PullRequest>> LoadPullRequests(string source, string target)
        {
            // TODO - GraphQL to GitHub and pass through?
            return gitService.GetPullRequests(targetBranch: target, sourceBranch: source);
        }

        internal Task<ImmutableList<string>> LoadDownstreamBranches(string name)
        {
            logger.LogInformation("Enqueue load downstream branches of {0}", name);
            return branchSettings.GetDownstreamBranchGroups(name).ContinueWith(t => t.Result.ContainsKey(name) ? t.Result[name] : ImmutableList<string>.Empty);
        }

        internal Task<ImmutableList<string>> LoadUpstreamBranches(string name)
        {
            logger.LogInformation("Enqueue load upstream branches of {0}", name);
            return branchSettings.GetUpstreamBranchGroups(name).ContinueWith(t => t.Result.ContainsKey(name) ? t.Result[name] : ImmutableList<string>.Empty);
        }

        internal Task<ImmutableList<CommitStatus>> LoadBranchStatus(string commitSha)
        {
            logger.LogInformation("Load commit status of {0}", commitSha);
            // TODO - GraphQL to GitHub and pass through?
            return gitService.GetCommitStatus(commitSha);
        }

        internal Task<ImmutableList<GitRef>> LoadAllGitRefs()
        {
            logger.LogInformation("Enqueue load all git refs");
            return repositoryState.RemoteBranches().FirstOrDefaultAsync().ToTask();
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
            return permissionAccessor.GetUsers();
        }

        internal async Task<ImmutableList<string>> LoadUsers(string role)
        {
            var result = await permissionAccessor.GetUsersByRole(new[] { role });
            return result.ContainsKey(role) ? result[role] : ImmutableList<string>.Empty;
        }

        internal Task<ImmutableList<string>> GetRoles()
        {
            return permissionAccessor.GetRoles();
        }

        internal async Task<ImmutableList<string>> LoadRoles(string username)
        {
            var result = await permissionAccessor.GetRolesByUser(new[] { username });
            return result.ContainsKey(username) ? result[username] : ImmutableList<string>.Empty;
        }

    }
}
