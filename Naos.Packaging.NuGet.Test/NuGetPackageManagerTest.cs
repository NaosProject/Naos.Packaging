// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NuGetPackageManagerTest.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.NuGet.Test
{
    using System;
    using System.IO;

    using Xunit;

    public class NuGetPackageManagerTest
    {
        [Fact]
        public void UnSupportedProtocolVersionThrows()
        {
            Action action = () => new NuGetPackageManager(5, "SourceName", "RepoUrl", "UserName", "Password", str => { });

            var ex = Assert.Throws<NotSupportedException>(action);
            Assert.Equal("Version: 5 is not currently supported.", ex.Message);
        }

        [Fact(Skip = "Meant for local debugging and to show usage.")]
        public void DownloadPrivate()
        {
            var defaultWorkingDirectory = @"D:\Temp\NewNuGet";
            var downloadDirectory = Path.Combine(defaultWorkingDirectory, Guid.NewGuid() + ".tmp");

            var pm = new NuGetPackageManager(2, "ThisIsGalleryName", "https://ci.appveyor.com/nuget/XXX", "ThisIsUser", "ThisIsPassword", str => { });

            var includeUnlisted = true;
            var includePreRelease = true;
            var latestVersionTask = pm.GetLatestVersionAsync("ThisIsPackageId", includeUnlisted, includePreRelease);
            latestVersionTask.Wait();
            var version = latestVersionTask.Result;

            var includeDependencies = true;

            pm.DownloadPackageToPathAsync(
                "ThisIsPackageid",
                version,
                downloadDirectory,
                includeDependencies,
                includeUnlisted,
                includePreRelease).Wait();
        }

        [Fact(Skip = "Meant for local debugging and to show usage.")]
        public void DownloadPublic()
        {
            var defaultWorkingDirectory = @"D:\Temp\NewNuGet";
            var downloadDirectory = Path.Combine(defaultWorkingDirectory, Guid.NewGuid() + ".tmp");

            var pm = new NuGetPackageManager();

            var includeUnlisted = true;
            var includePreRelease = true;
            var latestVersionTask = pm.GetLatestVersionAsync("Newtonsoft.Json", includeUnlisted, includePreRelease);
            latestVersionTask.Wait();
            var version = latestVersionTask.Result;

            var includeDependencies = true;

            pm.DownloadPackageToPathAsync(
                "Newtonsoft.Json",
                version,
                downloadDirectory,
                includeDependencies,
                includeUnlisted,
                includePreRelease).Wait();
        }

        [Fact]
        public static void BuildConfigFileFromRepositoryConfigurationThenSerializeNuGetConfig_ValidObject_ValidXml()
        {
            var sourceName = "ThisIsSource";
            var source = "https://this-is-url";
            var username = "ThisIsUser";
            var password = "ThisIsPassword";

            var config = NuGetConfigFile.BuildConfigFileFromRepositoryConfiguration(
                sourceName,
                source,
                username,
                password);

            var actualXml = NuGetConfigFile.Serialize(config);
            var expectedXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine +
"<configuration>" + Environment.NewLine +
"  <activePackageSource>" + Environment.NewLine +
"    <add key=\"nuget.org v2\" value=\"https://www.nuget.org/api/v2/\" />" + Environment.NewLine +
"    <add key=\"nuget.org v3\" value=\"https://api.nuget.org/v3/index.json\" />" + Environment.NewLine +
"    <add key=\"" + sourceName + "\" value=\"" + source + "\" />" + Environment.NewLine +
"  </activePackageSource>" + Environment.NewLine +
"  <packageSources>" + Environment.NewLine +
"    <add key=\"nuget.org v2\" value=\"https://www.nuget.org/api/v2/\" />" + Environment.NewLine +
"    <add key=\"nuget.org v3\" value=\"https://api.nuget.org/v3/index.json\" />" + Environment.NewLine +
"    <add key=\"" + sourceName + "\" value=\"" + source + "\" />" + Environment.NewLine +
"  </packageSources>" + Environment.NewLine +
"  <packageSourceCredentials>" + Environment.NewLine +
"    <" + sourceName + ">" + Environment.NewLine +
"      <add key=\"Username\" value=\"" + username + "\" />" + Environment.NewLine +
"      <add key=\"ClearTextPassword\" value=\"" + password + "\" />" + Environment.NewLine +
"      <add key=\"Password\" value=\"\" />" + Environment.NewLine +
"    </" + sourceName + ">" + Environment.NewLine +
"  </packageSourceCredentials>" + Environment.NewLine +
"</configuration>";
            Assert.Equal(expectedXml, actualXml);
        }
    }
}
