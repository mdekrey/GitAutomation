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
using GitAutomation.Work;

namespace GitAutomation.Management
{
    [Route("api/[controller]")]
    public class ManagementController : Controller
    {
        private readonly IRepositoryState repositoryState;
        private readonly IBranchSettings branchSettings;
        private readonly IUnitOfWorkFactory unitOfWorkFactory;

        public ManagementController(IRepositoryState repositoryState, IBranchSettings branchSettings, IUnitOfWorkFactory unitOfWorkFactory)
        {
            this.repositoryState = repositoryState;
            this.branchSettings = branchSettings;
            this.unitOfWorkFactory = unitOfWorkFactory;
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

        [HttpGet("queue")]
        public async Task<IEnumerable<Object>> Queue()
        {
            return (await repositoryState.ActionQueue.FirstAsync()).Select(action => new { ActionType = action.ActionType, Parameters = action.Parameters });
        }



        [HttpGet("downstream-branches/{*branchName}")]
        public async Task<string[]> DownstreamBranches(string branchName)
        {
            return await branchSettings.GetDownstreamBranches(branchName).FirstAsync();
        }

        [HttpGet("upstream-branches/{*branchName}")]
        public async Task<string[]> UpstreamBranches(string branchName)
        {
            return await branchSettings.GetUpstreamBranches(branchName).FirstAsync();
        }

        [HttpGet("all-branches")]
        public async Task<string[]> AllBranches()
        {
            return await branchSettings.GetConfiguredBranches().FirstAsync();
        }

        [HttpPut("branch/{*branchName}")]
        public async Task UpdateBranch(string branchName, [FromBody] UpdateBranchRequestBody requestBody)
        {
            using (var unitOfWork = unitOfWorkFactory.CreateUnitOfWork())
            {
                foreach (var addedUpstream in requestBody.AddUpstream)
                {
                    branchSettings.AddBranchSetting(addedUpstream, branchName, unitOfWork);
                }
                foreach (var addedDownstream in requestBody.AddDownstream)
                {
                    branchSettings.AddBranchSetting(branchName, addedDownstream, unitOfWork);
                }
                foreach (var removeUpstream in requestBody.RemoveUpstream)
                {
                    branchSettings.RemoveBranchSetting(removeUpstream, branchName, unitOfWork);
                }
                foreach (var removeDownstream in requestBody.RemoveDownstream)
                {
                    branchSettings.RemoveBranchSetting(branchName, removeDownstream, unitOfWork);
                }

                await unitOfWork.CommitAsync();
            }
        }

        public class UpdateBranchRequestBody
        {
            public string[] AddUpstream { get; set; }
            public string[] AddDownstream { get; set; }
            public string[] RemoveUpstream { get; set; }
            public string[] RemoveDownstream { get; set; }
        }
    }
}
