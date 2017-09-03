using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Auth
{
    public interface IRegisterPrincipalValidation
    {
        void RegisterPrincipalValidation(IServiceCollection services, IConfiguration configuration);
    }
}
