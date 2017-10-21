using DataLoader;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.GraphQL
{
    interface IDataLoaderContextAccessor
    {
        DataLoaderContext LoadContext { get; }
    }

    public class DataLoaderContextStore : IDataLoaderContextAccessor
    {
        public DataLoaderContext LoadContext { get; set; }
    }
}
