using GitAutomation.Repository;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.GraphQL
{
    public class GitRefInterface : ObjectGraphType<GitRef>
    {
        public GitRefInterface()
        {
            Name = "GitRef";

            Field(r => r.Name);
            Field(r => r.Commit);
        }
    }
}
