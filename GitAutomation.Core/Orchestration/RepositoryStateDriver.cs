using GitAutomation.BranchSettings;
using GitAutomation.Orchestration;
using GitAutomation.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;

namespace GitAutomation.Orchestration
{
    class RepositoryStateDriver : IRepositoryStateDriver
    {
        private readonly IRemoteRepositoryState repositoryState;
        private readonly IRepositoryOrchestration orchestration;
        private readonly IOrchestrationActions orchestrationActions;
        private readonly IBranchSettings branchSettings;
        private CompositeDisposable disposable = null;

        public RepositoryStateDriver(IRemoteRepositoryState repositoryState, IRepositoryOrchestration orchestration, IOrchestrationActions orchestrationActions, IBranchSettings branchSettings)
        {
            this.repositoryState = repositoryState;
            this.orchestration = orchestration;
            this.orchestrationActions = orchestrationActions;
            this.branchSettings = branchSettings;
        }

        public void Dispose()
        {
            disposable?.Dispose();
        }

        public void Start()
        {
            disposable = new CompositeDisposable(
                orchestration.ProcessActions().Subscribe(
                    onNext: _ =>
                    {
                    },
                    onCompleted: () =>
                    {
                        Console.WriteLine("COMPLETED - This shouldn't happen!");
                    },
                    onError: _ =>
                    {
                        Console.WriteLine("ERROR'D - This shouldn't happen!");
                        Console.WriteLine(_);
                    }
                ),
                repositoryState
                    .RemoteBranches()
                    .Where(branches => branches.Count > 0)
                    .Subscribe(allBranches =>
                        // TODO - only push to branches that changed
                        branchSettings.GetAllDownstreamBranches()
                            .SelectMany(all => all.Select(each => each.GroupName).ToObservable())
                            .SelectMany(downstreamBranch => orchestrationActions.CheckDownstreamMerges(downstreamBranch))
                            .Subscribe(
                                onNext: _ => { }, 
                                onError: (ex) =>
                                {
                                    Console.WriteLine(ex);
                                }
                            )
                    )
            );
        }

    }
}
