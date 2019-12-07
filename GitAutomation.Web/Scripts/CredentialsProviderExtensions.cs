using GitAutomation.Web;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Scripts
{
    public static class CredentialsProviderExtensions
    {
        public static CredentialsHandler ToCredentialsProvider(this ConfigRepositoryOptions options)
        {
            return (url, usernameFromUrl, credentialTypes) =>
                options.Repository == url
                    ? new UsernamePasswordCredentials { Password = options.Password, Username = usernameFromUrl }
                    : null;
        }
    }
}
