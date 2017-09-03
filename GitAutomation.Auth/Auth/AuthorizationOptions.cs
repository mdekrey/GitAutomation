using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Auth
{
    public class AuthorizationOptions
    {
        /// <summary>
        /// An array of <see cref="IRegisterPrincipalValidation"/> type names
        /// </summary>
        public string[] Types { get; set; }
    }
}
