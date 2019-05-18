using GitAutomation.DomainModels;
using GitAutomation.DomainModels.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GitAutomation.Serialization
{
    public static class SerializationUtils
    {
        private static readonly ReaderWriterLockSlim lockSlim = new ReaderWriterLockSlim();

        public static ISerializer Serializer { get; } 
            = new SerializerBuilder()
                .DisableAliases()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();
        public static IDeserializer Deserializer { get; } 
            = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

        public static bool MetaExists(string path)
        {
            var metaPath = Path.Combine(path, "meta.yaml");
            return File.Exists(metaPath);
        }

        public static async Task<Meta> LoadMetaAsync(string path)
        {
            var metaPath = Path.Combine(path, "meta.yaml");
            var meta = await ReadYamlFileAsync<Meta>(metaPath);
            meta.ToAbsolute(path);
            return meta;
        }

        public static async Task<ConfigurationRepository> LoadConfigurationAsync(Meta meta)
        {
            return await ReadYamlFileAsync<ConfigurationRepository>(meta.Configuration);
        }

        public static async Task<RepositoryStructure> LoadStructureAsync(Meta meta)
        {
            var builder = await ReadYamlFileAsync<RepositoryStructure.Builder>(meta.Structure);
            return builder.Build();
        }

        private static async Task<T> ReadYamlFileAsync<T>(string path)
        {
            using (var file = File.OpenRead(path))
            using (var textReader = new StreamReader(file))
            {
                var text = await textReader.ReadToEndAsync();
                return Deserializer.Deserialize<T>(text);
            }
        }

    }
}
