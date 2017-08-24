using GitAutomation.Orchestration;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;

namespace GitAutomation.Repository
{
    class RepositoryStateDriver : IRepositoryStateDriver
    {
        private readonly IRepositoryOrchestration orchestration;
        private readonly IRepositoryState repositoryState;
        private CompositeDisposable disposable = null;

        public RepositoryStateDriver(IRepositoryOrchestration orchestration, IRepositoryState repositoryState)
        {
            this.orchestration = orchestration;
            this.repositoryState = repositoryState;
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
                        Console.WriteLine(_);
                    },
                    onCompleted: () =>
                    {
                        Console.WriteLine("COMPLETED - This shouldn't happen!");
                    },
                    onError: _ =>
                    {
                        Console.WriteLine(_);
                    }
                ),
                repositoryState
                    .RemoteBranches()
                    .Where(branches => branches.Length > 0)
                    .Subscribe(branches =>
                        repositoryState
                            .CheckAllDownstreamMerges()
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
