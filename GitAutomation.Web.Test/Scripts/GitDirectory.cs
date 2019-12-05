using LibGit2Sharp;
using System;
using System.IO;
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
                LibGit2Sharp.Repository.Init(TemporaryDirectory.Path);
            }
        }

        public TemporaryDirectory TemporaryDirectory { get; }
        public string Path => TemporaryDirectory.Path;

        public GitDirectory CreateCopy(CloneOptions? options = null)
        {
            var result = new TemporaryDirectory();

            // TODO - is there a way to include the `shared` flag?
            LibGit2Sharp.Repository.Clone(TemporaryDirectory.Path, result.Path, options ?? new CloneOptions());

            return new GitDirectory(result);
        }

        public void Dispose()
        {
            TemporaryDirectory?.Dispose();
        }
    }
}