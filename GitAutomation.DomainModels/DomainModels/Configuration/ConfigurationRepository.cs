using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.DomainModels.Configuration
{
    public class ConfigurationRepository
    {
        public Dictionary<string, ReserveConfiguration> ReserveTypes { get; set; } = new Dictionary<string, ReserveConfiguration>();
    }
}
