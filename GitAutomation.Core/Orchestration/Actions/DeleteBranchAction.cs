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
    class DeleteBranchAction : ComplexAction<DeleteBranchAction.Internal>
    {
        private readonly string deletingBranch;
        private readonly DeleteBranchMode mode;

        public override string ActionType => "DeleteBranch";

        public DeleteBranchAction(string deletingBranch, DeleteBranchMode mode)
        {
            this.deletingBranch = deletingBranch;
            this.mode = mode;
        }

        public override JToken Parameters => JToken.FromObject(new Dictionary<string, string>
            {
                { "deletingBranch", deletingBranch },
                { "mode", mode.ToString("g") },
            }.ToImmutableDictionary());

        internal override object[] GetExtraParameters()
        {
            return new object[] {
                deletingBranch,
                mode
            };
        }

        public class Internal : ComplexActionInternal
        {
            private readonly IGitCli cli;
            private readonly IBranchSettings settings;
            private readonly IRepositoryMediator repository;
            private readonly IUnitOfWorkFactory unitOfWorkFactory;
            private readonly string deletingBranch;
            private readonly DeleteBranchMode mode;

            public Internal(IGitCli cli, IBranchSettings settings, IRepositoryMediator repository, IUnitOfWorkFactory unitOfWorkFactory, string deletingBranch, DeleteBranchMode mode)
            {
                this.cli = cli;
                this.settings = settings;
                this.repository = repository;
                this.unitOfWorkFactory = unitOfWorkFactory;
                this.deletingBranch = deletingBranch;
                this.mode = mode;
            }

            protected override async Task RunProcess()
            {
                if (mode == DeleteBranchMode.ActualBranchOnly)
                {
                    await AppendProcess(cli.DeleteRemote(deletingBranch)).WaitUntilComplete();
                }
                else
                {
                    var details = await repository.GetBranchDetails(deletingBranch).FirstAsync();

                    using (var unitOfWork = unitOfWorkFactory.CreateUnitOfWork())
                    {
                        settings.DeleteBranchSettings(deletingBranch, unitOfWork);

                        await unitOfWork.CommitAsync();
                    }

                    if (mode != DeleteBranchMode.GroupOnly)
                    {
                        foreach (var branch in details.Branches)
                        {
                            await AppendProcess(cli.DeleteRemote(branch.Name)).WaitUntilComplete();
                            repository.NotifyPushedRemoteBranch(branch.Name);
                        }
                    }
                }
            }
        }
    }
}
