using System;
using System.IO;
using System.Management.Automation;
using Xunit;

namespace GitAutomation.Scripts
{
    public class GitDirectory : IDisposable
    {
        public GitDirectory()
        {
            using (var ps = PowerShell.Create())
            {
                ps.AddScript($"cd \"{TemporaryDirectory.Path}\"");
                ps.AddScript("git init");
                ps.Invoke();
            }
        }

        public TemporaryDirectory TemporaryDirectory { get; } = new TemporaryDirectory();

        public TemporaryDirectory CreateCopy()
        {
            var result = new TemporaryDirectory();

            using (var ps = PowerShell.Create())
            {
                ps.AddScript($"cd \"{result.Path}\"");
                ps.AddScript($"git clone --shared \"{TemporaryDirectory.Path}\" .");
                ps.Invoke();
            }

            return result;
        }

        public void Dispose()
        {
            TemporaryDirectory?.Dispose();
        }
    }
}