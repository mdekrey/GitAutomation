using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Serialization.Defaults
{
    public static class DefaultsWriter
    {
        private static readonly char[] EmbeddedResourceSeparators = ".".ToCharArray();

        public static async Task WriteDefaultsToDirectory(string path)
        {
            var assembly = typeof(DefaultsWriter).Assembly;
            var startsWith = typeof(DefaultsWriter).Namespace;
            await Task.WhenAll(assembly.GetManifestResourceNames().Where(n => n.StartsWith(startsWith))
                .Select(resourceName => CopyEmbeddedResource(assembly, path, startsWith, resourceName)));
        }

        private static async Task CopyEmbeddedResource(Assembly assembly, string path, string startsWith, string resourceName)
        {
            var remainingName = resourceName.Substring(startsWith.Length + 1);
            var pathPartCount = Math.Max(1, remainingName.Count(c => c == '.') - 1);
            var outPath = Path.Combine(path, string.Join("/", remainingName.Split(EmbeddedResourceSeparators, pathPartCount)));
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var file = File.Create(outPath);
            await stream.CopyToAsync(file);
        }
    }
}
