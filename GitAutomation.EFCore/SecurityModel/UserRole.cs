using System;
using System.Collections.Generic;

namespace GitAutomation.EFCore.SecurityModel
{
    public partial class UserRole
    {
        public string ClaimName { get; set; }
        public string Role { get; set; }

        public User ClaimNameNavigation { get; set; }
    }
}
