using DataLoader;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.GraphQL.Utilities
{
    public interface IDataLoaderContextAccessor
    {
        DataLoaderContext LoadContext { get; }
    }

    class DataLoaderContextStore : IDataLoaderContextAccessor
    {
        public DataLoaderContext LoadContext { get; set; }
    }
}
