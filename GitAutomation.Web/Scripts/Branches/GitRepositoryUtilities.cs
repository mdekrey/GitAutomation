using GitAutomation.Web;
using LibGit2Sharp;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Scripts.Branches
{
    public static class GitRepositoryUtilities
    {
        public static Repository CloneAsLocal(string checkoutPath, string workingPath, AutomationOptions automationOptions)
        {
            Repository.Init(workingPath, isBare: false);
            var repo = new Repository(workingPath);
            repo.Network.Remotes.Add(automationOptions.WorkingRemote, checkoutPath);
            Commands.Fetch(repo, automationOptions.WorkingRemote, new[] { "refs/heads/*:refs/heads/*" }, new FetchOptions { }, "");
            return repo;
        }


        public static void ResetAndClean(Repository repo)
        {
            repo.Reset(ResetMode.Hard);
            foreach (var entry in repo.RetrieveStatus())
            {
                if (entry.State != FileStatus.Unaltered)
                {
                    System.IO.File.Delete(System.IO.Path.Combine(repo.Info.Path, entry.FilePath));
                }
            }
        }

    }
}
