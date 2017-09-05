﻿namespace GitAutomation.Repository
{
    public class GitRepositoryOptions
    {
        public string CheckoutPath { get; set; } = "/working";

        public string Repository { get; set; }

        public string Password { get; set; }

        public string ApiType { get; set; }

        public string UserEmail { get; set; }

        public string UserName { get; set; }

        public string IntegrationNamingConventionType { get; set; }
    }
}