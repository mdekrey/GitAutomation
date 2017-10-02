using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.GitHub
{
    public class GithubServiceApiOptions
    {
        public string Password { get; set; }

        public bool CheckStatus { get; set; } = true;

        public bool CheckPullRequestReviews { get; set; } = true;
    }
}
