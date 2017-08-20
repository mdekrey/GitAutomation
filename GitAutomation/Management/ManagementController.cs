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
        
        [HttpGet("all-branches")]
        public async Task<ImmutableList<string>> AllBranches()
        {
            return await (
                branchSettings.GetConfiguredBranches()
                .WithLatestFrom(
                    repositoryState.RemoteBranches(), 
                    (first, second) => first.Concat(second).Distinct().OrderBy(a => a).ToImmutableList()
                )
            ).FirstAsync();
        }

        [HttpGet("details/{*branchName}")]
        public async Task<BranchDetails> GetDetails(string branchName)
        {
            return await branchSettings.GetBranchDetails(branchName).FirstAsync();
        }

        [HttpPut("branch/propagation/{*branchName}")]
        public async Task UpdateBranch(string branchName, [FromBody] UpdateBranchRequestBody requestBody)
        {
            using (var unitOfWork = unitOfWorkFactory.CreateUnitOfWork())
            {
                foreach (var addedUpstream in requestBody.AddUpstream)
                {
                    branchSettings.AddBranchPropagation(addedUpstream, branchName, unitOfWork);
                }
                foreach (var addedDownstream in requestBody.AddDownstream)
                {
                    branchSettings.AddBranchPropagation(branchName, addedDownstream, unitOfWork);
                }
                foreach (var removeUpstream in requestBody.RemoveUpstream)
                {
                    branchSettings.RemoveBranchPropagation(removeUpstream, branchName, unitOfWork);
                }
                foreach (var removeDownstream in requestBody.RemoveDownstream)
                {
                    branchSettings.RemoveBranchPropagation(branchName, removeDownstream, unitOfWork);
                }
                branchSettings.UpdateBranchSetting(branchName, requestBody.RecreateFromUpstream, unitOfWork);

                await unitOfWork.CommitAsync();
            }
        }

        public class UpdateBranchRequestBody
        {
            public bool RecreateFromUpstream { get; set; }
            public string[] AddUpstream { get; set; }
            public string[] AddDownstream { get; set; }
            public string[] RemoveUpstream { get; set; }
            public string[] RemoveDownstream { get; set; }
        }
    }
}
