using GitAutomation.BranchSettings;
using GitAutomation.Processes;
using GitAutomation.Repository;
using GitAutomation.Work;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace GitAutomation.Orchestration.Actions
{
    class ReleaseToServiceLineAction : ComplexAction<ReleaseToServiceLineAction.Internal>
    {
        private readonly string releaseCandidateBranch;
        private readonly string serviceLineBranch;
        private readonly string tagName;
        private readonly bool autoConsolidate;

        public override string ActionType => "ReleaseToServiceLine";

        public ReleaseToServiceLineAction(string releaseCandidateBranch, string serviceLineBranch, string tagName, bool autoConsolidate)
        {
            this.releaseCandidateBranch = releaseCandidateBranch;
            this.serviceLineBranch = serviceLineBranch;
            this.tagName = tagName;
            this.autoConsolidate = autoConsolidate;
        }

        public override JToken Parameters => JToken.FromObject(new Dictionary<string, string>
            {
                { "releaseCandidateBranch", releaseCandidateBranch },
                { "serviceLineBranch", serviceLineBranch },
            }.ToImmutableDictionary());

        internal override object[] GetExtraParameters()
        {
            return new object[]
            {
                releaseCandidateBranch,
                serviceLineBranch,
                tagName,
                autoConsolidate,
            };
        }

        public class Internal : ComplexActionInternal
        {
            private readonly IGitCli cli;
            private readonly IRepositoryMediator repository;
            private readonly IBranchSettings settings;
            private readonly IBranchIterationMediator branchIteration;
            private readonly IUnitOfWorkFactory unitOfWorkFactory;
            private readonly IRepositoryOrchestration orchestration;
            private readonly bool isReadOnly;
            private readonly string releaseCandidateBranch;
            private readonly string serviceLineBranch;
            private readonly string tagName;
            private readonly bool autoConsolidate;

            public Internal(IGitCli cli, IRepositoryMediator repository, IBranchSettings settings, IBranchIterationMediator branchIteration, IUnitOfWorkFactory unitOfWorkFactory, IRepositoryOrchestration orchestration, IOptions<GitRepositoryOptions> options, string releaseCandidateBranch, string serviceLineBranch, string tagName, bool autoConsolidate)
            {
                this.cli = cli;
                this.repository = repository;
                this.settings = settings;
                this.branchIteration = branchIteration;
                this.unitOfWorkFactory = unitOfWorkFactory;
                this.orchestration = orchestration;
                this.isReadOnly = options.Value.ReadOnly;
                this.releaseCandidateBranch = releaseCandidateBranch;
                this.serviceLineBranch = serviceLineBranch;
                this.tagName = tagName;
                this.autoConsolidate = autoConsolidate;
            }

            protected override async Task RunProcess()
            {
                if (isReadOnly)
                {
                    return;
                }

                // either:
                // 1. create new service line from release candidate
                // 2. merge --ff-only from release candidate to service line

                // if it passes:
                //   collect upstream branches
                //   push service line
                
                var upstreamLines = await repository.DetectShallowUpstreamServiceLines(releaseCandidateBranch).FirstOrDefaultAsync();
                var disposable = new CompositeDisposable();

                var readyToFinalize = await CreateOrFastForwardServiceLine(releaseCandidateBranch, repository, cli);

                if (!readyToFinalize)
                {
                    await AppendMessage($"{serviceLineBranch} unable to be fast-forwarded from {releaseCandidateBranch}; aborting", isError: true);
                }
                else
                {
                    if (!string.IsNullOrEmpty(tagName))
                    {
                        await AppendProcess(cli.AnnotatedTag(tagName, $"Automated release to service line {serviceLineBranch} from {releaseCandidateBranch}")).WaitUntilComplete();
                    }

                    var serviceLine = await settings.GetBranchBasicDetails(serviceLineBranch).FirstOrDefaultAsync();
                    // possible TODO for the future: give option to add missing upstream lines always
                    if (serviceLine == null)
                    {
                        // We need to set it up as a service line
                        using (var work = unitOfWorkFactory.CreateUnitOfWork())
                        {
                            settings.UpdateBranchSetting(serviceLineBranch, UpstreamMergePolicy.None, BranchGroupType.ServiceLine, work);
                            foreach (var upstreamServiceLine in upstreamLines)
                            {
                                settings.AddBranchPropagation(upstreamServiceLine, serviceLineBranch, work);
                            }

                            await work.CommitAsync();
                        }
                    }

                    if (autoConsolidate)
                    {
                        var consolidating = (await repository.GetBranchDetails(releaseCandidateBranch).FirstOrDefaultAsync()).UpstreamBranchGroups;
                        foreach (var upstreamServiceLine in upstreamLines)
                        {
                            var upstreamDetails = await repository.GetBranchDetails(upstreamServiceLine).FirstOrDefaultAsync();
                            consolidating = consolidating.Except(upstreamDetails.UpstreamBranchGroups).Except(new[] { upstreamServiceLine }).ToImmutableList();
                        }
                        var releasedCandidate = await settings.GetConfiguredBranches().Select(branches => branches.Find(branch => branchIteration.IsBranchIteration(branch.GroupName, releaseCandidateBranch)).GroupName).FirstOrDefaultAsync();
#pragma warning disable CS4014
                        orchestration.EnqueueAction(new ConsolidateMergedAction(consolidating.Concat(new[] { releasedCandidate }), serviceLineBranch));
#pragma warning restore
                    }

                    if (!string.IsNullOrEmpty(tagName))
                    {
                        await AppendProcess(cli.Push(tagName)).WaitUntilComplete();
                    }

                    await AppendProcess(cli.Push(serviceLineBranch)).WaitUntilComplete();
                }
            }

            private async Task<bool> CreateOrFastForwardServiceLine(string latestBranchName, IRepositoryMediator repository, IGitCli cli)
            {
                var showRefResult = await repository.GetBranchRef(serviceLineBranch).Take(1);
                if (showRefResult == null)
                {
                    // create service line
                    await AppendProcess(cli.CheckoutRemote(latestBranchName)).WaitUntilComplete();

                    await AppendProcess(cli.CheckoutNew(serviceLineBranch)).WaitUntilComplete();

                    return true;
                }
                else
                {
                    // fast-forward
                    await AppendProcess(cli.CheckoutRemote(serviceLineBranch)).WaitUntilComplete();

                    var fastForward = cli.MergeFastForward(latestBranchName);
                    await AppendProcess(fastForward).WaitUntilComplete();
                    var fastForwardResult = fastForward.ExitCode;

                    return fastForwardResult == 0;
                }
            }
        }

    }
}
