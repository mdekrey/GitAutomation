using System;
using System.Collections.Generic;

namespace GitAutomation.EFCore.SecurityModel
{
    public partial class UserRole
    {
        public string ClaimName { get; set; }
        public string Permission { get; set; }

        public User User { get; set; }
    }
}
