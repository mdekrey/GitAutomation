using GitAutomation.GraphQL.Utilities.Resolvers;
using GitAutomation.Repository;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.GraphQL
{
    public struct MergeBaseInfo
    {
        public GitRef Source;
        public string TargetCommit;
    }

    // Base type is the merge base itself
    public class MergeBaseGraphType : ObjectGraphType<MergeBaseInfo>
    {
        public MergeBaseGraphType()
        {
            Name = "CompareRefs";

            Field<StringGraphType>()
                .Name("mergeBase")
                .Resolve(this, nameof(MergeBase));

            Field<IntGraphType>()
                .Name("commitsAhead")
                .Resolve(this, nameof(CommitsAhead));

            Field<IntGraphType>()
                .Name("commitsBehind")
                .Resolve(this, nameof(CommitsBehind));
        }

        private Task<string> MergeBase([Source] MergeBaseInfo source, [FromServices] Loaders loaders)
        {
            if (source.TargetCommit == null)
            {
                return null;
            }
            return loaders.GetMergeBaseOfCommits(source.Source.Commit, source.TargetCommit);
        }


        private async Task<int?> CommitsAhead([Source] MergeBaseInfo source, [FromServices] IGitCli cli)
        {
            if (source.TargetCommit == null)
            {
                return null;
            }
            return await cli.RevListCount(source.Source.Commit, source.TargetCommit);
        }


        private async Task<int?> CommitsBehind([Source] MergeBaseInfo source, [FromServices] IGitCli cli)
        {
            if (source.TargetCommit == null)
            {
                return null;
            }
            return await cli.RevListCount(source.TargetCommit, source.Source.Commit);
        }
    }
}
