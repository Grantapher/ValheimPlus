using System;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using NUnit.Framework;
using ValheimPlus;
using ValheimPlus.Configurations;

namespace ValheimPlus.Tests
{
    [TestFixture]
    public class ConfigurationExtraTests
    {
        private string _originalConfigPath;

        [SetUp]
        public void SetUp()
        {
            // Ensure resolver is initialized for Unity/Valheim deps
            _ = typeof(AssemblyResolver);

            _originalConfigPath = ConfigurationExtra.ConfigIniPath;
            ConfigurationExtra.DownloadDefaultIniOverride = null;
            ConfigurationExtra.EmbeddedDefaultIniOverride = null;
        }

        [TearDown]
        public void TearDown()
        {
            ConfigurationExtra.ConfigIniPath = _originalConfigPath;
            ConfigurationExtra.DownloadDefaultIniOverride = null;
            ConfigurationExtra.EmbeddedDefaultIniOverride = null;
        }

        [Test]
        public void DownloadDefaultIniOverride_WhenSet_IsAccessible()
        {
            // Verify override hook is exposed and can be set
            var testContent = "[Server]\nenabled=true";
            ConfigurationExtra.DownloadDefaultIniOverride = () => testContent;

            Assert.IsNotNull(ConfigurationExtra.DownloadDefaultIniOverride);
            Assert.AreEqual(testContent, ConfigurationExtra.DownloadDefaultIniOverride());
        }

        [Test]
        public void EmbeddedDefaultIniOverride_WhenSet_IsAccessible()
        {
            // Verify override hook is exposed and can be set
            var testContent = "[Server]\nenabled=false";
            ConfigurationExtra.EmbeddedDefaultIniOverride = () => testContent;

            Assert.IsNotNull(ConfigurationExtra.EmbeddedDefaultIniOverride);
            Assert.AreEqual(testContent, ConfigurationExtra.EmbeddedDefaultIniOverride());
        }

        [Test]
        public void ConfigIniPath_CanBeSetAndRetrieved()
        {
            var testPath = Path.Combine(Path.GetTempPath(), "test_config.cfg");
            ConfigurationExtra.ConfigIniPath = testPath;

            Assert.AreEqual(testPath, ConfigurationExtra.ConfigIniPath);
        }
    }
}
