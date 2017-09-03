using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace GitAutomation.Auth
{
    public class RolesFromConfigurationOptions
    {

        public Dictionary<string, ClaimRule[]> Roles { get; set; } = new Dictionary<string, ClaimRule[]>();
    }

    public class ClaimRule
    {
        public string Type { get; set; }
        public string Value { get; set; }

        public bool IsMatch(Claim claim)
        {
            return (claim.Type == (Type ?? claim.Type))
                && (claim.Value == (Value ?? claim.Value));
        }
    }
}
