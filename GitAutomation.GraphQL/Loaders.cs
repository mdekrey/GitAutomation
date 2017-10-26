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

namespace GitAutomation.GraphQL
{
    public class Loaders
    {
        private readonly DataLoaderContext loadContext;
        private readonly IBranchSettingsAccessor branchSettings;
        private readonly IUserPermissionAccessor permissionAccessor;
        private readonly IRepositoryState repositoryState;
        private readonly IBranchIterationNamingConvention branchIteration;
        private readonly IGitServiceApi gitService;

        public Loaders(IDataLoaderContextAccessor loadContextAccessor, IBranchSettingsAccessor branchSettings, IUserPermissionAccessor permissionAccessor, IRepositoryState repositoryState, IBranchIterationNamingConvention branchIteration, IGitServiceApi gitService)
        {
            this.loadContext = loadContextAccessor.LoadContext;
            this.branchSettings = branchSettings;
            this.permissionAccessor = permissionAccessor;
            this.repositoryState = repositoryState;
            this.branchIteration = branchIteration;
            this.gitService = gitService;
        }

        public Task<BranchGroup> LoadBranchGroup(string name)
        {
            return loadContext.Factory.GetOrCreateLoader<string, BranchGroup>("GetBranchGroup", async keys => {
                var result = await branchSettings.GetBranchGroups(keys.ToArray());
                return result.ToDictionary(e => e.Key, e => e.Value);
            }).LoadAsync(name);
        }

        internal async Task<string> GetMergeBaseOfCommitAndRemoteBranch(string commit, string branch)
        {
            var refs = await LoadAllGitRefs().ConfigureAwait(false);

            var result = refs.FirstOrDefault(r => r.Name == branch);
            return result.Name == branch
                ? await GetMergeBaseOfCommits(commit, result.Commit).ConfigureAwait(false)
                : null;
        }

        internal Task<string> GetMergeBaseOfCommitAndGroup(string commit, string group)
        {
            return LoadLatestBranch(group).ContinueWith(t => GetMergeBaseOfCommits(commit, t.Result.Commit)).Unwrap();
        }

        internal Task<string> GetMergeBaseOfCommits(string commit1, string commit2)
        {
            return repositoryState.MergeBaseBetweenCommits(commit1, commit2);
        }

        public Task<ImmutableList<string>> LoadBranchGroups()
        {
            return loadContext.Factory.GetOrCreateLoader("GetBranchGroups", async () => {
                var result = await branchSettings.GetAllBranchGroups();
                return result.Select(group => group.GroupName).ToImmutableList();
            }).LoadAsync();
        }

        internal Task<ImmutableList<PullRequest>> LoadPullRequests(string source, string target)
        {
            // TODO - GraphQL to GitHub and pass through?
            return gitService.GetPullRequests(targetBranch: target, sourceBranch: source);
        }

        internal Task<ImmutableList<string>> LoadDownstreamBranches(string name)
        {
            return loadContext.Factory.GetOrCreateLoader<string, ImmutableList<string>>("GetDownstreamBranchGroups", async keys => {
                var result = await branchSettings.GetDownstreamBranchGroups(keys.ToArray());
                return result.ToDictionary(e => e.Key, e => e.Value);
            }).LoadAsync(name);
        }

        internal Task<ImmutableList<string>> LoadUpstreamBranches(string name)
        {
            return loadContext.Factory.GetOrCreateLoader<string, ImmutableList<string>>("GetUpstreamBranchGroups", async keys => {
                var result = await branchSettings.GetUpstreamBranchGroups(keys.ToArray());
                return result.ToDictionary(e => e.Key, e => e.Value);
            }).LoadAsync(name);
        }

        internal Task<ImmutableList<CommitStatus>> LoadBranchStatus(string commitSha)
        {
            // TODO - GraphQL to GitHub and pass through?
            return gitService.GetCommitStatus(commitSha);
        }
        
        internal Task<ImmutableList<GitRef>> LoadAllGitRefs()
        {
            return loadContext.Factory.GetOrCreateLoader<ImmutableList<GitRef>>("GetAllGitRefs", async () => {
                return await repositoryState.RemoteBranches().FirstOrDefaultAsync();
            }).LoadAsync();
        }

        async internal Task<ImmutableList<GitRef>> LoadActualBranches(string name)
        {
            var refs = await LoadAllGitRefs().ConfigureAwait(false);
            return (from branch in refs
                    where branchIteration.IsBranchIteration(name, branch.Name)
                    select branch).ToImmutableList();
        }

        async internal Task<GitRef> LoadLatestBranch(string name)
        {
            var refs = await LoadActualBranches(name).ConfigureAwait(false);
            var latestName = branchIteration.GetLatestBranchNameIteration(name, refs.Select(n => n.Name));
            return refs.SingleOrDefault(r => r.Name == latestName);
        }

        internal Task<ImmutableList<string>> GetUsers()
        {
            return loadContext.Factory.GetOrCreateLoader("GetAllUsers", () => permissionAccessor.GetUsers()).LoadAsync();
        }

        internal Task<ImmutableList<string>> LoadUsers(string role)
        {
            return loadContext.Factory.GetOrCreateLoader<string, ImmutableList<string>>("GetUsersByRole", async keys => {
                var result = await permissionAccessor.GetUsersByRole(keys.ToArray());
                return keys.ToDictionary(e => e, e => result.ContainsKey(e) ? result[e] : ImmutableList<string>.Empty);
            }).LoadAsync(role);
        }

        internal Task<ImmutableList<string>> GetRoles()
        {
            return loadContext.Factory.GetOrCreateLoader("GetAllRoles", () => permissionAccessor.GetRoles()).LoadAsync();
        }

        internal Task<ImmutableList<string>> LoadRoles(string username)
        {
            return loadContext.Factory.GetOrCreateLoader<string, ImmutableList<string>>("GetRolesByUser", async keys => {
                var result = await permissionAccessor.GetRolesByUser(keys.ToArray());
                return result.ToDictionary(e => e.Key, e => e.Value);
            }).LoadAsync(username);
        }

    }
}
