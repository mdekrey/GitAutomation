using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace GitAutomation.GraphQL
{
    class RepositoryActionEntryInterface : UnionGraphType, IAbstractGraphType
    {
        private readonly static StaticRepositoryActionEntryInterface staticEntry = new StaticRepositoryActionEntryInterface();
        private readonly static RepositoryActionReactiveProcessEntryInterface processEntry = new RepositoryActionReactiveProcessEntryInterface();

        public RepositoryActionEntryInterface()
        {
            this.AddPossibleType(staticEntry);
            this.AddPossibleType(processEntry);

            ResolveType = action =>
            {
                switch (action)
                {
                    case Orchestration.StaticRepositoryActionEntry entry:
                        return staticEntry;
                    case Orchestration.RepositoryActionReactiveProcessEntry entry:
                        return processEntry;
                    default:
                        throw new NotSupportedException();
                }
            };
        }
        
        public override string CollectTypes(TypeCollectionContext context)
        {
            context.AddType(staticEntry.Name, staticEntry, context);
            context.AddType(processEntry.Name, processEntry, context);
            return base.CollectTypes(context);
        }
    }

    class StaticRepositoryActionEntryInterface : ObjectGraphType<Orchestration.StaticRepositoryActionEntry>
    {
        public StaticRepositoryActionEntryInterface()
        {
            Name = "StaticRepositoryActionEntry";
            Field(v => v.IsError);
            Field(v => v.Message);
        }
    }

    class RepositoryActionReactiveProcessEntryInterface : ObjectGraphType<Orchestration.RepositoryActionReactiveProcessEntry>
    {
        public RepositoryActionReactiveProcessEntryInterface()
        {
            Name = "ProcessRepositoryActionEntry";
            Field<NonNullGraphType<StringGraphType>>("StartInfo", resolve: v => v.Source.Process.StartInfo);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<OutputMessageInterface>>>>("Output", resolve: v => v.Source.Process.Output);
            Field<ProcessStateTypeEnum>("State", resolve: v => v.Source.Process.State);
            Field<IntGraphType>("ExitCode", resolve: v => v.Source.Process.State == Processes.ProcessState.Exited ? v.Source.Process.ExitCode : (int?)null);
        }
    }
}
