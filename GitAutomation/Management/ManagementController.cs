﻿using GitAutomation.Repository;
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
using GitAutomation.Orchestration;
using Microsoft.AspNetCore.Authorization;

namespace GitAutomation.Management
{
    [Authorize]
    [Route("api/[controller]")]
    public class ManagementController : Controller
    {
        private readonly IRepositoryState repositoryState;
        private readonly IBranchSettings branchSettings;
        private readonly IUnitOfWorkFactory unitOfWorkFactory;
        private readonly IRepositoryOrchestration orchestration;

        public ManagementController(IRepositoryState repositoryState, IBranchSettings branchSettings, IUnitOfWorkFactory unitOfWorkFactory, IRepositoryOrchestration orchestration)
        {
            this.repositoryState = repositoryState;
            this.branchSettings = branchSettings;
            this.unitOfWorkFactory = unitOfWorkFactory;
            this.orchestration = orchestration;
        }

        [HttpGet("log")]
        public async Task<ImmutableList<Processes.OutputMessage>> Log()
        {
            return await orchestration.ProcessActionsLog.FirstAsync();
        }

        [HttpGet("queue")]
        public async Task<IEnumerable<Object>> Queue()
        {
            return (await orchestration.ActionQueue.FirstAsync()).Select(action => new { ActionType = action.ActionType, Parameters = action.Parameters });
        }
        
        [HttpGet("all-branches")]
        public async Task<ImmutableList<BranchBasicDetails>> AllBranches()
        {
            return await (
                branchSettings.GetConfiguredBranches()
                .WithLatestFrom(
                    repositoryState.RemoteBranches(), 
                    (first, second) => 
                        first
                            .Concat(
                                from branchName in second
                                where !first.Any(b => b.BranchName == branchName)
                                select new BranchBasicDetails { BranchName = branchName }
                            )
                            .OrderBy(a => a.BranchName)
                            .ToImmutableList()
                )
            ).FirstAsync();
        }

        [HttpDelete("branch/{*branchName}")]
        public void DeleteBranch(string branchName)
        {
            repositoryState.DeleteBranch(branchName);
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
                branchSettings.UpdateBranchSetting(branchName, requestBody.RecreateFromUpstream, requestBody.BranchType, unitOfWork);

                await unitOfWork.CommitAsync();
            }
        }

        [HttpPut("branch/promote")]
        public void PromoteServiceLine([FromBody] PromoteServiceLineBody requestBody, [FromServices] IOrchestrationActions orchestrationActions)
        {
            orchestrationActions.ConsolidateServiceLine(requestBody.ReleaseCandidate, requestBody.ServiceLine, requestBody.TagName);
        }

        [HttpGet("detect-upstream/{*branchName}")]
        public async Task<IEnumerable<string>> DetectUpstream(string branchName)
        {
            return await repositoryState.DetectUpstream(branchName);
        }

        public class UpdateBranchRequestBody
        {
            public bool RecreateFromUpstream { get; set; }
            public BranchType BranchType { get; set; }
            public string[] AddUpstream { get; set; }
            public string[] AddDownstream { get; set; }
            public string[] RemoveUpstream { get; set; }
            public string[] RemoveDownstream { get; set; }
        }

        public class PromoteServiceLineBody
        {
            public string ServiceLine { get; set; }
            public string ReleaseCandidate { get; set; }
            public string TagName { get; set; }
        }
    }
}
