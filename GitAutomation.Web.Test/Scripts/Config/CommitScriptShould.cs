using GitAutomation.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Xunit;

namespace GitAutomation.Scripts.Config
{
    [Collection("GitConfiguration collection")]
    public class CommitScriptShould
    {
        private readonly ConfigGitDirectory workingGitDirectory;

        public CommitScriptShould(ConfigGitDirectory workingGitDirectory)
        {
            this.workingGitDirectory = workingGitDirectory;
        }

        [Fact]
        public void CommitWithoutPushing()
        { 
            using (var directory = workingGitDirectory.CreateCopy())
            using (var tempDir = directory.CreateCopy("--branch git-config"))
            {
                File.WriteAllText(Path.Combine(tempDir.Path, "test.txt"), "This is just a test commit, no need to update yaml.");

                var timestamp = DateTimeOffset.Now;
                Invoke(ps => StandardParameters(ps, tempDir.TemporaryDirectory, timestamp));

                var actual = Invoke(ps =>
                {
                    ps.AddScript(@$"cd ""{directory.Path}""
git log --format=oneline --no-decorate
echo -
cd ""{tempDir.Path}""
git log --format=oneline --no-decorate");
                });
                var original = string.Join('\n', actual.TakeWhile(l => l != "-"));
                var next = string.Join('\n', actual.SkipWhile(l => l != "-").Skip(1));
                Assert.EndsWith(original, next);
                Assert.NotEqual(original, next);
            }
        }

        private static void StandardParameters(PowerShell ps, TemporaryDirectory checkout, DateTimeOffset timestamp)
        {
            ps.AddUnrestrictedCommand("./Scripts/Config/commit.ps1");
            ps.AddParameter("password", "");
            ps.AddParameter("userEmail", "author@example.com");
            ps.AddParameter("userName", "A U Thor");
            ps.AddParameter("checkoutPath", checkout.Path);
            ps.AddParameter("branchName", "git-config");
            ps.AddParameter("startTimestamp", timestamp);
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
