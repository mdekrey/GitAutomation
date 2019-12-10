using GitAutomation.DomainModels.Git;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Scripts
{
    public static class GitIdentityExtensions
    {
        public static Identity ToGitIdentity(this GitIdentity identity)
        {
            return new Identity(identity.UserName, identity.UserEmail);
        }
    }
}
