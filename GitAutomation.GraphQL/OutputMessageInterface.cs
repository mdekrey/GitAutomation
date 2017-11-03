using GraphQL.Types;

namespace GitAutomation.GraphQL
{
    internal class OutputMessageInterface : ObjectGraphType<Processes.OutputMessage>
    {
        public OutputMessageInterface()
        {
            Name = "OutputMessage";
            Field<StringGraphType>()
                .Name(nameof(Processes.OutputMessage.Message))
                .Resolve(a => a.Source.Message);
            Field(a => a.ExitCode);
            Field<NonNullGraphType<OutputChannelEnumType>>()
                .Name(nameof(Processes.OutputMessage.Channel))
                .Resolve(a => a.Source.Channel);
        }
    }
}