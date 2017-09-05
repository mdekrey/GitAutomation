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

        [Authorize(Auth.PolicyNames.Read)]
        [HttpGet("log")]
        public async Task<ImmutableList<Processes.OutputMessage>> Log()
        {
            return await orchestration.ProcessActionsLog.FirstAsync();
        }

        [Authorize(Auth.PolicyNames.Read)]
        [HttpGet("queue")]
        public async Task<IEnumerable<Object>> Queue()
        {
            return (await orchestration.ActionQueue.FirstAsync()).Select(action => new { ActionType = action.ActionType, Parameters = action.Parameters });
        }
        
        [Authorize(Auth.PolicyNames.Read)]
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

        [Authorize(Auth.PolicyNames.Read)]
        [HttpGet("all-branches/hierarchy")]
        public async Task<ImmutableList<BranchHierarchyDetails>> AllBranchesHierarchy()
        {
            return await (
                branchSettings.GetAllDownstreamBranches()
                    .Take(1)
                    .SelectMany(allBranches => 
                        allBranches.ToObservable().SelectMany(async branch => new BranchHierarchyDetails(branch)
                        {
                            DownstreamBranches = (await branchSettings.GetDownstreamBranches(branch.BranchName).FirstOrDefaultAsync()).Select(b => b.BranchName).ToImmutableList()
                        }).ToArray()
                    )
                    .Select(branches => branches.ToImmutableList())
                .WithLatestFrom(
                    repositoryState.RemoteBranches(),
                    (first, second) =>
                        first
                            .Concat(
                                from branchName in second
                                where !first.Any(b => b.BranchName == branchName)
                                select new BranchHierarchyDetails { BranchName = branchName }
                            )
                            .OrderBy(a => a.BranchName)
                            .ToImmutableList()
                )
            ).FirstAsync();
        }

        [Authorize(Auth.PolicyNames.Delete)]
        [HttpDelete("branch/{*branchName}")]
        public void DeleteBranch(string branchName)
        {
            repositoryState.DeleteBranch(branchName);
        }

        [Authorize(Auth.PolicyNames.Read)]
        [HttpGet("details/{*branchName}")]
        public async Task<BranchDetails> GetDetails(string branchName)
        {
            return await branchSettings.GetBranchDetails(branchName).FirstAsync();
        }

        [Authorize(Auth.PolicyNames.Update)]
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

        [Authorize(Auth.PolicyNames.Approve)]
        [HttpPut("branch/promote")]
        public void PromoteServiceLine([FromBody] PromoteServiceLineBody requestBody, [FromServices] IOrchestrationActions orchestrationActions)
        {
            orchestrationActions.ConsolidateServiceLine(requestBody.ReleaseCandidate, requestBody.ServiceLine, requestBody.TagName);
        }

        [Authorize(Auth.PolicyNames.Read)]
        [HttpGet("detect-upstream/{*branchName}")]
        public async Task<IEnumerable<string>> DetectUpstream(string branchName)
        {
            var allUpstream = await repositoryState.DetectUpstream(branchName);
            var configured = await branchSettings.GetConfiguredBranches().FirstAsync();

            for (var i = 0; i < allUpstream.Count - 1; i++)
            {
                var upstream = allUpstream[i];
                var isConfigured = configured.Any(branch => branch.BranchName == upstream);
                var furtherUpstream = await branchSettings.GetAllUpstreamBranches(upstream).FirstOrDefaultAsync();
                allUpstream = allUpstream.Except(furtherUpstream.Select(b => b.BranchName)).ToImmutableList();
            }

            // TODO - this could be smarter
            for (var i = 0; i < allUpstream.Count - 1; i++)
            {
                var upstream = allUpstream[i];
                var furtherUpstream = await repositoryState.DetectUpstream(upstream);
                allUpstream = allUpstream.Except(furtherUpstream).ToImmutableList();
            }

            return allUpstream;
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

        public class BranchHierarchyDetails : BranchBasicDetails
        {
            public BranchHierarchyDetails()
            {
            }
            public BranchHierarchyDetails(BranchDepthDetails original)
            {
                this.BranchName = original.BranchName;
                this.BranchType = original.BranchType;
                this.RecreateFromUpstream = original.RecreateFromUpstream;
                this.HierarchyDepth = original.Ordinal;
            }

            public ImmutableList<string> DownstreamBranches { get; set; } = ImmutableList<string>.Empty;
            public int HierarchyDepth { get; set; }
        }
    }
}
