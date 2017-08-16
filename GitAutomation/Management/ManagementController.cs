using GitAutomation.Repository;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using GitAutomation.BranchSettings;
using System.Collections.Immutable;

namespace GitAutomation.Management
{
    [Route("api/[controller]")]
    public class ManagementController : Controller
    {
        private readonly IRepositoryState repositoryState;
        private readonly IBranchSettings branchSettings;

        public ManagementController(IRepositoryState repositoryState, IBranchSettings branchSettings)
        {
            this.repositoryState = repositoryState;
            this.branchSettings = branchSettings;
        }

        [HttpGet("remote-branches")]
        public async Task<string[]> RemoteBranches()
        {
            return await repositoryState.RemoteBranches().FirstAsync();
        }


        [HttpGet("log")]
        public async Task<ImmutableList<Processes.OutputMessage>> Log()
        {
            return await repositoryState.ProcessActionsLog.FirstAsync();
        }


        [HttpGet("downstream-branches/{branchName}")]
        public async Task<string[]> DownstreamBranches(string branchName)
        {
            return await branchSettings.GetDownstreamBranches(branchName).FirstAsync();
        }

        [HttpGet("upstream-branches/{branchName}")]
        public async Task<string[]> UpstreamBranches(string branchName)
        {
            return await branchSettings.GetUpstreamBranches(branchName).FirstAsync();
        }

        [HttpGet("all-branches")]
        public async Task<string[]> AllBranches()
        {
            return await branchSettings.GetConfiguredBranches().FirstAsync();
        }
    }
}
