using System;
using System.IO;

namespace GitAutomation.Scripts
{
    public class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "git-test-scripts", System.IO.Path.GetRandomFileName());
            if (Directory.Exists(directory) )
            {
                throw new InvalidOperationException($"Directory '{directory}' already exists!");
            }
            Directory.CreateDirectory(directory);
            Path = directory;
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, true);
            }
            catch
            {
                // some files were still open. Perhaps the user browsed to the folder so TortoiseGit attached?
            }
        }
    }
}