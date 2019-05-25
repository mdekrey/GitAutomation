using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;
using Xunit;
using GitAutomation.Extensions;
using Newtonsoft.Json;
using System.Linq;
using GitAutomation.DomainModels;

namespace GitAutomation.Scripts.Config
{
    [Collection("GitConfiguration collection")]
    public class CloneScriptShould
    {
        private readonly ConfigGitDirectory workingGitDirectory;

        public CloneScriptShould(ConfigGitDirectory workingGitDirectory)
        {
            this.workingGitDirectory = workingGitDirectory;
        }

        [Fact]
        public void HandleAlreadyClonedDirectories()
        {
            using (var directory = workingGitDirectory.CreateCopy())
            using (var tempDir = new TemporaryDirectory())
            {
                // Arrange it to already have a clone
                using (var ps = PowerShell.Create())
                {
                    ps.AddScript($"git clone --shared --branch git-config \"{directory.Path}\" \"{tempDir.Path}\"");
                    ps.Invoke();
                }

                // Act to receive the expected FSA's
                var timestamp = DateTimeOffset.Now;
                var result = Invoke(ps =>
                {
                    ps.AddUnrestrictedCommand("./Scripts/Config/clone.ps1");
                    StandardParameters(ps, directory, tempDir);
                    ps.AddParameter("startTimestamp", timestamp);
                });

                // Assert that we're correct
                Assert.Equal(1, result.Count);
                var standardAction = JsonConvert.DeserializeObject<StandardAction>(result.Single());
                Assert.Equal("ConfigurationRepository:ReadyToLoad", standardAction.Action);
                Assert.Equal(timestamp.ToString(), standardAction.Payload["startTimestamp"]);
            }
        }

        private static void StandardParameters(PowerShell ps, TemporaryDirectory directory, TemporaryDirectory tempDir)
        {
            ps.AddParameter("repository", directory.Path);
            ps.AddParameter("password", "");
            ps.AddParameter("userEmail", "author@example.com");
            ps.AddParameter("userName", "A U Thor");
            ps.AddParameter("checkoutPath", tempDir.Path);
            ps.AddParameter("branchName", "git-config");
        }

        private ICollection<string> Invoke(Action<PowerShell> addParameters)
        {
            using (var ps = PowerShell.Create())
            {
                ps.AddUnrestrictedCommand("./Scripts/Globals.ps1");
                addParameters(ps);
                return ps.Invoke<string>();
            }
        }
    }
}
