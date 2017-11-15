using GraphQL.Types;

namespace GitAutomation.GraphQL
{
    internal class AppOptionsInterface : ObjectGraphType<AppOptions>
    {
        public AppOptionsInterface()
        {
            Name = "Application";
            Field(a => a.Title);
        }
    }
}