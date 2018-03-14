using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace GitAutomation.EFCore.SecurityModel
{
    public class EfPermissionManagementOptions
    {
        public string ClaimType { get; set; } = ClaimTypes.Name;
    }
}
