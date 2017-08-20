using GitAutomation.BranchSettings;
using GitAutomation.Repository;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Hooks
{
    [Route("api/[controller]")]
    public class GitWebHookController : Controller
    {
        [HttpPost]
        public void Post([FromServices] IRepositoryState repositoryState, [FromServices] IBranchSettings branchSettings)
        {
            repositoryState.CheckForUpdates()
                .Concat(
                    repositoryState.RemoteBranches().Take(1).SelectMany(allBranches => allBranches.ToObservable())
                        .SelectMany(upstream => branchSettings.GetDownstreamBranches(upstream).Take(1).SelectMany(branches => branches.ToObservable().Select(downstream => new { upstream, downstream })))
                        .ToList()
                        .SelectMany(all => all.Select(each => each.downstream).Distinct().ToObservable())
                        .SelectMany(upstreamBranch => CheckDownstreamMerges(repositoryState, upstreamBranch))
                )
                .Select(_ => Unit.Default)
                .Subscribe(onNext: _ => { }, onError: (ex) =>
                {
                    Console.WriteLine(ex);
                });
        }

        private static IObservable<Processes.OutputMessage> CheckDownstreamMerges(IRepositoryState repositoryState, string downstreamBranch)
        {
            return repositoryState.CheckDownstreamMerges(downstreamBranch);
        }
    }
}
