using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.GitLab
{
    class GitLabServiceApiOptions
    {
        public int ProjectId { get; set; }
        public string Username { get; set; }
        public string PersonalAccessToken { get; set; }
    }
}
