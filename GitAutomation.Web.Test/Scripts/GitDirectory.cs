using LibGit2Sharp;
using System;
using System.IO;
using Xunit;

namespace GitAutomation.Scripts
{
    public class GitDirectory : IDisposable
    {
        public GitDirectory() : this(new TemporaryDirectory(), false)
        {
        }

        public GitDirectory(bool isBare) : this(new TemporaryDirectory(), isBare)
        {
        }

        public GitDirectory(TemporaryDirectory temporaryDirectory, bool isBare)
        {
            TemporaryDirectory = temporaryDirectory;
            
            if (!LibGit2Sharp.Repository.IsValid(temporaryDirectory.Path))
            {
                LibGit2Sharp.Repository.Init(TemporaryDirectory.Path, isBare);
            }
        }

        public TemporaryDirectory TemporaryDirectory { get; }
        public string Path => TemporaryDirectory.Path;

        public GitDirectory CreateCopy(CloneOptions? options = null)
        {
            options ??= new CloneOptions() { };
            var result = new TemporaryDirectory();

            // TODO - is there a way to include the `shared` flag?
            Repository.Clone(TemporaryDirectory.Path, result.Path, options);
            using var repo = new Repository(result.Path);
            Commands.Fetch(repo, "origin", new[] { "refs/heads/*:refs/heads/*" }, new FetchOptions { }, "");
            repo.Network.Remotes.Remove("origin");

            return new GitDirectory(result, options.IsBare);
        }

        public GitDirectory CreateClone(CloneOptions? options = null)
        {
            options ??= new CloneOptions() { };
            var result = new TemporaryDirectory();

            // TODO - is there a way to include the `shared` flag?
            Repository.Clone(TemporaryDirectory.Path, result.Path, options);

            return new GitDirectory(result, options.IsBare);
        }

        public void Dispose()
        {
            TemporaryDirectory?.Dispose();
        }
    }
}