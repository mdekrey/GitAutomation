using System;
using System.Collections.Generic;

namespace GitAutomation.EFCore.SecurityModel
{
    public partial class User
    {
        public User()
        {
            UserRole = new HashSet<UserRole>();
        }

        public string ClaimName { get; set; }

        public ICollection<UserRole> UserRole { get; set; }
    }
}
