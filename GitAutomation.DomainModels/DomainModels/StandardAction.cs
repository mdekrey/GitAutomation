using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace GitAutomation.DomainModels
{
    public struct StandardAction
    {
        public StandardAction(string action, object payload) : this(action, JToken.FromObject(payload))
        {
        }
        public StandardAction(string action, JToken payload)
        {
            Action = action;
            Payload = payload;
        }

        public string Action { get; set; }
        public JToken Payload { get; set; }
    }

}
