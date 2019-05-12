using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GitAutomation.Serialization
{
    public class Meta
    {
#nullable disable
        public string Version { get; set; }
        public string Configuration { get; set; }
        public string Structure { get; set; }
#nullable restore

        internal void ToAbsolute(string path)
        {
            Configuration = MakeAbsolute(Configuration, path);
            Structure = MakeAbsolute(Structure, path);
        }

        internal void ToRelative(string path)
        {
            Configuration = MakeRelative(Configuration, path);
            Structure = MakeRelative(Structure, path);
        }

        private string MakeAbsolute(string configuration, string path)
        {
            return Path.GetFullPath(Path.Combine(path, configuration));
        }

        private static string MakeRelative(string original, string path)
        {
            return new Uri(path.TrimEnd('/') + "/").MakeRelativeUri(new Uri(Path.GetFullPath(Path.Combine(path, original)))).OriginalString.Replace('\\', '/');
        }
    }
}
