using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.DomainModels
{
    public class RepositoryConfiguration
    {
        public Dictionary<string, ReserveConfiguration> ReserveTypes { get; set; } = new Dictionary<string, ReserveConfiguration>();
    }
}
