using System.Collections.Generic;

namespace GitAutomation.DomainModels.Configuration
{
    public class ReserveConfiguration
    {
        public string? Title { get; set; }
        public string? HelpLink { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public Dictionary<string, string> StateScripts { get; set; } = new Dictionary<string, string>();
    }
}