using GitAutomation.DomainModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GitAutomation.Serialization
{
    public static class Serialization
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

    }
}
