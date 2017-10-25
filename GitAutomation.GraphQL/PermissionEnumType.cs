using GraphQL.Types;

namespace GitAutomation.GraphQL
{
    internal class PermissionEnumType : EnumerationGraphType<Auth.Permission>
    {
        public PermissionEnumType()
        {
            Name = "Permission";
            Description = "Permission types";

            foreach (var value in this.Values)
            {
                value.Name = ((Auth.Permission)value.Value).ToString("g");
            }
        }
    }
}