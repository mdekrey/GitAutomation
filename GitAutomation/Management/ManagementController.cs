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
        private readonly IRemoteRepositoryState repositoryState;
        private readonly IBranchSettings branchSettings;
        private readonly IUnitOfWorkFactory unitOfWorkFactory;

        public ManagementController(IRepositoryMediator repository, IRemoteRepositoryState repositoryState, IBranchSettings branchSettings, IUnitOfWorkFactory unitOfWorkFactory)
        {
            this.repository = repository;
            this.repositoryState = repositoryState;
            this.branchSettings = branchSettings;
            this.unitOfWorkFactory = unitOfWorkFactory;
        }
        
        [Authorize(Auth.PolicyNames.Delete)]
        [HttpDelete("branch/{*branchName}")]
        public void DeleteBranch(string branchName, [FromServices] IOrchestrationActions orchestrationActions, [FromQuery] DeleteBranchMode mode = DeleteBranchMode.BranchAndGroup)
        {
            orchestrationActions.DeleteBranch(branchName, mode);
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
        public async Task UpdateBranch(string branchName, [FromBody] UpdateBranchRequestBody requestBody, [FromServices] IOrchestrationActions orchestrationActions)
        {
            var branchesToCheck = new HashSet<string>();
            branchesToCheck.Add(branchName);
            using (var unitOfWork = unitOfWorkFactory.CreateUnitOfWork())
            {
                foreach (var addedUpstream in requestBody.AddUpstream)
                {
                    branchSettings.AddBranchPropagation(addedUpstream, branchName, unitOfWork);
                    branchesToCheck.Add(addedUpstream);
                }
                foreach (var addedDownstream in requestBody.AddDownstream)
                {
                    branchSettings.AddBranchPropagation(branchName, addedDownstream, unitOfWork);
                    branchesToCheck.Add(addedDownstream);
                }
                foreach (var removeUpstream in requestBody.RemoveUpstream)
                {
                    branchSettings.RemoveBranchPropagation(removeUpstream, branchName, unitOfWork);
                    branchesToCheck.Add(removeUpstream);
                }
                foreach (var removeDownstream in requestBody.RemoveDownstream)
                {
                    branchSettings.RemoveBranchPropagation(branchName, removeDownstream, unitOfWork);
                    branchesToCheck.Add(removeDownstream);
                }
                branchSettings.UpdateBranchSetting(branchName, requestBody.RecreateFromUpstream, requestBody.BranchType, unitOfWork);

                await unitOfWork.CommitAsync();
            }
            foreach (var branch in branchesToCheck)
            {
#pragma warning disable CS4014
                orchestrationActions.CheckDownstreamMerges(branch);
#pragma warning restore CS4014
            }
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
            return repository.DetectShallowUpstream(branchName, asGroup);
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
