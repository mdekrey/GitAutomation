using GitAutomation.DomainModels;
using GitAutomation.Web;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Scripts.Branches.Common.Steps
{
    public static class OutputBranchPreparer
    {
        public static (bool push, bool newOutputBranch, string outputBranchName)? PrepareOutputBranch(ReserveScriptParameters parameters, ILogger logger, Repository repo, IBranchNaming branchNaming)
        {
            var outputBranches = parameters.Reserve.GetBranchesByRole("Output");
            var (newOutputBranch, outputBranchName) =
                outputBranches.Length switch
                {
                    0 => (true, branchNaming.GetDefaultOutputBranchName(parameters.Name)),
                    1 => (false, outputBranches[0]),
                    _ => (false, null),
                };

            var push = false;
            if (outputBranchName == null)
            {
                logger.LogError($"Had multiple output branches: {string.Join(", ", outputBranches)}", outputBranches);
                return null;
            }

            if (newOutputBranch)
            {
                if (repo.Branches[outputBranchName] != null)
                {
                    logger.LogError($"Default output branch '{outputBranchName}' already exists but was not allocated to reserve.", outputBranchName);
                    return null;
                }
                push = true;
                Commands.Checkout(repo, parameters.UpstreamReserves.First().Value.OutputCommit);
            }
            else
            {
                Commands.Checkout(repo, outputBranchName);
            }

            return (push, newOutputBranch, outputBranchName);
        }

    }
}
