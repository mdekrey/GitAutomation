using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace GitAutomation.Scripts.Branches
{
    [CollectionDefinition("GitBranch collection")]
    public class BranchesGitDirectoryDefinition : ICollectionFixture<BranchGitDirectory>
    { }

    public class BranchGitDirectory : GitDirectory
    {

    }
}
