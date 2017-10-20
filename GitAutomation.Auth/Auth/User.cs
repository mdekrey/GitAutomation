using System;
using System.Collections.Generic;

namespace GitAutomation.Auth
{
    public partial class User
    {
        public User()
        {
            Roles = new HashSet<UserRole>();
        }

        public string ClaimName { get; set; }

        public ICollection<UserRole> Roles { get; set; }
    }
}
