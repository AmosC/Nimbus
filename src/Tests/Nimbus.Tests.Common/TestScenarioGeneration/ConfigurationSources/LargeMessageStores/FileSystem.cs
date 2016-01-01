using System;
using System.IO;
using Nimbus.Configuration.LargeMessages;
using Nimbus.LargeMessages.FileSystem.Configuration;

namespace Nimbus.Tests.Common.TestScenarioGeneration.ConfigurationSources.LargeMessageStores
{
    internal class FileSystem : IConfigurationScenario<LargeMessageStorageConfiguration>
    {
        public string Name { get; } = "FileSystem";
        public string[] Categories { get; } = {"FileSystem"};

        public ScenarioInstance<LargeMessageStorageConfiguration> CreateInstance()
        {
            var largeMessageBodyTempPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                                        "Nimbus Integration Test Suite",
                                                        Guid.NewGuid().ToString());

            var configuration = new FileSystemStorageConfiguration()
                .WithStorageDirectory(largeMessageBodyTempPath);

            var instance = new ScenarioInstance<LargeMessageStorageConfiguration>(configuration);
            instance.Disposing += (s, e) => Directory.Delete(largeMessageBodyTempPath, true);

            return instance;
        }
    }
}