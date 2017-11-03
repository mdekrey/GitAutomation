using GraphQL.Types;

namespace GitAutomation.GraphQL
{
    internal class OrchestrationActionInterface : ObjectGraphType<Orchestration.IRepositoryAction>
    {
        public OrchestrationActionInterface()
        {
            Name = "OrchestrationAction";
            Field(a => a.ActionType);
            Field(nameof(Orchestration.IRepositoryAction.Parameters), a => a.Parameters.ToString(Newtonsoft.Json.Formatting.None));
        }
    }
}