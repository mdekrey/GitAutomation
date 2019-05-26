using System;
using System.IO;
using System.Management.Automation;
using Xunit;

namespace GitAutomation.Scripts
{
    public class GitDirectory : IDisposable
    {
        public GitDirectory() : this(new TemporaryDirectory())
        {
        }

        public GitDirectory(TemporaryDirectory temporaryDirectory)
        {
            TemporaryDirectory = temporaryDirectory;
            if (!Directory.Exists(System.IO.Path.Combine(temporaryDirectory.Path, ".git")))
            {
                using (var ps = PowerShell.Create())
                {
                    ps.AddScript($"cd \"{TemporaryDirectory.Path}\"");
                    ps.AddScript("git init");
                    ps.Invoke();
                }
            }
        }

        public TemporaryDirectory TemporaryDirectory { get; }
        public string Path => TemporaryDirectory.Path;

        public GitDirectory CreateCopy(string args = "")
        {
            var result = new TemporaryDirectory();

            using (var ps = PowerShell.Create())
            {
                ps.AddScript($"cd \"{result.Path}\"");
                ps.AddScript($"git clone --shared {args} \"{TemporaryDirectory.Path}\" .");
                ps.Invoke();
            }

            return new GitDirectory(result);
        }

        public void Dispose()
        {
            TemporaryDirectory?.Dispose();
        }
    }
}