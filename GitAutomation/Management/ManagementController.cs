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
using System.Reactive.Threading.Tasks;

namespace GitAutomation.Management
{
    [Authorize]
    [Route("api/[controller]")]
    public class ManagementController : Controller
    {
        private readonly IRepositoryMediator repository;
        private readonly IRepositoryState repositoryState;
        private readonly IBranchSettings branchSettings;
        private readonly IUnitOfWorkFactory unitOfWorkFactory;
        private readonly IRepositoryOrchestration orchestration;

        public ManagementController(IRepositoryMediator repository, IRepositoryState repositoryState, IBranchSettings branchSettings, IUnitOfWorkFactory unitOfWorkFactory, IRepositoryOrchestration orchestration)
        {
            this.repository = repository;
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
        public Task<ImmutableList<BranchGroupCompleteData>> AllBranches()
        {
            return repository.AllBranches().FirstAsync().ToTask();
        }

        [Authorize(Auth.PolicyNames.Read)]
        [HttpGet("all-branches/hierarchy")]
        public Task<ImmutableList<BranchGroupCompleteData>> AllBranchesHierarchy()
        {
            return repository.AllBranchesHierarchy().FirstAsync().ToTask();
        }

        [Authorize(Auth.PolicyNames.Delete)]
        [HttpDelete("branch/{*branchName}")]
        public void DeleteBranch(string branchName)
        {
            repositoryState.DeleteBranch(branchName);
        }

        [Authorize(Auth.PolicyNames.Read)]
        [HttpGet("details/{*branchName}")]
        public Task<BranchGroupCompleteData> GetDetails(string branchName)
        {
            return repository.GetBranchDetails(branchName).FirstAsync().ToTask();
        }

        [Authorize(Auth.PolicyNames.Create)]
        [HttpPut("branch/create/{*branchName}")]
        public async Task<IActionResult> CreateBranch(string branchName, [FromBody] CreateBranchRequestBody requestBody)
        {
            var branch = await branchSettings.GetBranchBasicDetails(branchName).FirstOrDefaultAsync();
            if (branch != null)
            {
                return StatusCode(409, new { code = "branch-exists", message = "Branch already exists." });
            }

            using (var unitOfWork = unitOfWorkFactory.CreateUnitOfWork())
            {
                foreach (var addedUpstream in requestBody.AddUpstream)
                {
                    branchSettings.AddBranchPropagation(addedUpstream, branchName, unitOfWork);
                }
                branchSettings.UpdateBranchSetting(branchName, requestBody.RecreateFromUpstream, requestBody.BranchType, unitOfWork);

                await unitOfWork.CommitAsync();
            }
            return Ok();
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

        [Authorize(Auth.PolicyNames.Update)]
        [HttpPut("branch/check-upstream/{*branchName}")]
        public void CheckDownstreamMerges(string branchName, [FromServices] IOrchestrationActions orchestrationActions)
        {
            orchestrationActions.CheckDownstreamMerges(branchName);
        }

        [Authorize(Auth.PolicyNames.Approve)]
        [HttpPut("branch/promote")]
        public void PromoteServiceLine([FromBody] PromoteServiceLineBody requestBody, [FromServices] IOrchestrationActions orchestrationActions)
        {
            orchestrationActions.ReleaseToServiceLine(requestBody.ReleaseCandidate, requestBody.ServiceLine, requestBody.TagName, requestBody.AutoConsolidate);
        }

        [Authorize(Auth.PolicyNames.Approve)]
        [HttpPut("branch/consolidate/{*branchName}")]
        public void ConsolidateMerged([FromBody] IEnumerable<string> originalBranches, string branchName, [FromServices] IOrchestrationActions orchestrationActions)
        {
            orchestrationActions.ConsolidateMerged(originalBranches, branchName);
        }

        [Authorize(Auth.PolicyNames.Read)]
        [HttpGet("detect-upstream/{*branchName}")]
        public Task<ImmutableList<string>> DetectUpstream(string branchName, [FromQuery] bool asGroup)
        {
            return repository.DetectShallowUpstream(branchName, asGroup).FirstAsync().ToTask();
        }

        [Authorize(Auth.PolicyNames.Read)]
        [HttpGet("check-prs/{*branchName}")]
        public Task<ImmutableList<GitService.PullRequestWithReviews>> GetUpstreamPullRequests(string branchName)
        {
            return repository.GetUpstreamPullRequests(branchName).FirstAsync().ToTask();
        }

        [Authorize(Auth.PolicyNames.Read)]
        [HttpGet("recommend-groups")]
        public Task<ImmutableList<string>> RecommendGroups()
        {
            return repository.RecommendNewGroups().FirstAsync().ToTask();
        }

        public class CreateBranchRequestBody
        {
            public bool RecreateFromUpstream { get; set; }
            public BranchGroupType BranchType { get; set; }
            public string[] AddUpstream { get; set; }
        }

        public class UpdateBranchRequestBody
        {
            public bool RecreateFromUpstream { get; set; }
            public BranchGroupType BranchType { get; set; }
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
            public bool AutoConsolidate { get; set; }
        }

    }
}
