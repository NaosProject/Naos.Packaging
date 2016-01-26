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
            Action action = () => new NuGetPackageManager(5, "SourceName", "RepoUrl", "UserName", "Password");

            var ex = Assert.Throws<NotSupportedException>(action);
            Assert.Equal("Version: 5 is not currently supported.", ex.Message);
        }

        [Fact(Skip = "Meant for local debugging and to show usage.")]
        public void DownloadPrivate()
        {
            var defaultWorkingDirectory = @"D:\Temp\NewNuGet";
            var downloadDirectory = Path.Combine(defaultWorkingDirectory, Guid.NewGuid() + ".tmp");

            var pm = new NuGetPackageManager(2, "ThisIsGalleryName", "https://ci.appveyor.com/nuget/XXX", "ThisIsUser", "ThisIsPassword");

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
    }
}
