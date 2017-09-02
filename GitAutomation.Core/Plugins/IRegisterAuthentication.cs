using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;

namespace GitAutomation.Plugins
{
    public interface IRegisterAuthentication
    {
        void RegisterAuthentication(IServiceCollection services, AuthenticationBuilder authBuilder, IConfiguration configuration);
    }
}
