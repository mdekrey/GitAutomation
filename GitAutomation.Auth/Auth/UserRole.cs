using System;
using System.Collections.Generic;

namespace GitAutomation.Auth
{
    public partial class UserRole
    {
        public string ClaimName { get; set; }
        public string Permission { get; set; }

        public User User { get; set; }
    }
}
