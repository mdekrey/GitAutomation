using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.GraphQL.Resolvers
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public abstract class ResolverAttribute : Attribute
    {
    }

    public class SourceAttribute : ResolverAttribute
    {
    }

    public class FromArgumentAttribute : ResolverAttribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class FromServicesAttribute : ResolverAttribute
    {
    }
}
