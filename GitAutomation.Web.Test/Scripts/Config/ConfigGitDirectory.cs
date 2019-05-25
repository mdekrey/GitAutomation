using GitAutomation.Serialization.Defaults;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;
using Xunit;

namespace GitAutomation.Scripts.Config
{
    public class ConfigGitDirectory : GitDirectory
    {
        public ConfigGitDirectory()
        {
            DefaultsWriter.WriteDefaultsToDirectory(TemporaryDirectory.Path).Wait();


            using (var ps = PowerShell.Create())
            {
                ps.AddScript($"cd \"{TemporaryDirectory.Path}\"");
                ps.AddScript("git checkout -B git-config");
                ps.AddScript("git add .");
                ps.AddScript("git commit --author=\"A U Thor <author@example.com>\" -m \"Initial commit\"");
                ps.Invoke();
            }
        }
    }


    [CollectionDefinition("GitConfiguration collection")]
    public class ConfigGitDirectoryDefinition : ICollectionFixture<ConfigGitDirectory>
    { }
}
