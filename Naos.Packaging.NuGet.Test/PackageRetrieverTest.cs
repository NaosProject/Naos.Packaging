// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageRetrieverTest.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.NuGet.Test
{
    using Naos.Packaging.Domain;

    using Xunit;

    public class PackageRetrieverTest
    {
        [Fact(Skip = "Meant for local debugging and to show usage.")]
        public void DownloadPrivate()
        {
                var repoConfig = new PackageRepositoryConfiguration
                                 {
                                     Source = "https://ci.appveyor.com/nuget/XXX",
                                     ClearTextPassword = "ThisIsPassword",
                                     Username = "ThisIsUser",
                                     SourceName = "ThisIsGalleryName",
                                     ProtocolVersion = 2,
                                 };

            var defaultWorkingDirectory = @"D:\Temp\NewNuGet";
            var pm = new PackageRetriever(repoConfig, defaultWorkingDirectory);
            var bundleAllDependencies = false;
            var package = pm.GetPackage(new PackageDescription { Id = "ThisIsPackage" }, bundleAllDependencies);
            Assert.NotNull(package.PackageFileBytes);
        }

        [Fact(Skip = "Meant for local debugging and to show usage.")]
        public void DownloadPublic()
        {
            var defaultWorkingDirectory = @"D:\Temp\NewNuGet";
            var pm = new PackageRetriever(defaultWorkingDirectory);
            var bundleAllDependencies = false;
            var package = pm.GetPackage(new PackageDescription { Id = "Newtonsoft.Json" }, bundleAllDependencies);
            Assert.NotNull(package.PackageFileBytes);
        }
    }
}
