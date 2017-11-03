using GraphQL.Types;

namespace GitAutomation.GraphQL
{
    internal class OutputChannelEnumType : EnumerationGraphType<Processes.OutputChannel>
    {
        public OutputChannelEnumType()
        {
            Name = "OutputChannel";
            Description = "Channel for the output message";

            foreach (var value in this.Values)
            {
                value.Name = ((Processes.OutputChannel)value.Value).ToString("g");
            }
        }
    }
}