using GitAutomation.DomainModels.Git;
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
        public static CredentialsHandler ToCredentialsProvider(this RepositoryConfiguration options)
        {
            return (url, usernameFromUrl, credentialTypes) =>
                options.Url == url
                    ? new UsernamePasswordCredentials { Password = options.Password, Username = usernameFromUrl }
                    : null;
        }
    }
}
