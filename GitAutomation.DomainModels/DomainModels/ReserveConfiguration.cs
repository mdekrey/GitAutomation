using System.Collections.Generic;

namespace GitAutomation.DomainModels
{
    public class ReserveConfiguration
    {
        public string? Description { get; set; }
        public Dictionary<string, string> StateScripts { get; set; } = new Dictionary<string, string>();
    }
}