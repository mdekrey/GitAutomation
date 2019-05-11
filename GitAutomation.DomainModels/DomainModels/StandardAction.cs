using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace GitAutomation.DomainModels
{
    public struct StandardAction
    {
        public StandardAction(string action, Dictionary<string, object> payload)
        {
            Action = action;
            Payload = new Dictionary<string, object>(payload);
        }

        public string Action { get; set; }
        public Dictionary<string, object> Payload { get; set; }
    }

}
