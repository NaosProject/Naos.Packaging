// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageRetrieverTest.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.NuGet.Test
{
    using System;

    using FluentAssertions;

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
            var pm = new PackageRetriever(defaultWorkingDirectory, repoConfig);
            var package = pm.GetPackage(new PackageDescription { Id = "ThisIsPackage" });
            Assert.NotNull(package.PackageFileBytes);
        }

        [Fact(Skip = "Meant for local debugging and to show usage.")]
        public void DownloadPublic()
        {
            var defaultWorkingDirectory = @"D:\Temp\NewNuGet";
            var pm = new PackageRetriever(defaultWorkingDirectory);
            var package = pm.GetPackage(new PackageDescription { Id = "Newtonsoft.Json" });
            Assert.NotNull(package.PackageFileBytes);
        }

        [Fact]
        public static void DistinctPackageIdsMatchExactly_SameCollection_ReturnsTrue()
        {
            var a = new[] { new PackageDescription() { Id = "monkey", Version = null } };
            var b = new[] { new PackageDescription() { Id = "monkey", Version = null } };
            var actual = PackageDescriptionIdOnlyEqualityComparer.DistinctPackageIdsMatchExactly(a, b);
            var expected = true;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void DistinctPackageIdsMatchExactly_SameDuplicatesInFirst_ReturnsTrue()
        {
            var a = new[] { new PackageDescription() { Id = "monkey", Version = "1.0.0" }, new PackageDescription() { Id = "monkey", Version = "1.1.0" } };
            var b = new[] { new PackageDescription() { Id = "monkey", Version = null } };
            var actual = PackageDescriptionIdOnlyEqualityComparer.DistinctPackageIdsMatchExactly(a, b);
            var expected = true;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void DistinctPackageIdsMatchExactly_SameDuplicatesInSecond_ReturnsTrue()
        {
            var a = new[] { new PackageDescription() { Id = "monkey", Version = null } };
            var b = new[] { new PackageDescription() { Id = "monkey", Version = "1.1.0" }, new PackageDescription() { Id = "monkey", Version = "1.0.0" } };
            var actual = PackageDescriptionIdOnlyEqualityComparer.DistinctPackageIdsMatchExactly(a, b);
            var expected = true;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void DistinctPackageIdsMatchExactly_SameDuplicatesInBoth_ReturnsTrue()
        {
            var a = new[] { new PackageDescription() { Id = "monkey", Version = "1.0.0" }, new PackageDescription() { Id = "monkey", Version = "1.1.0" } };
            var b = new[] { new PackageDescription() { Id = "monkey", Version = "1.1.0" }, new PackageDescription() { Id = "monkey", Version = "1.0.0" } };
            var actual = PackageDescriptionIdOnlyEqualityComparer.DistinctPackageIdsMatchExactly(a, b);
            var expected = true;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void DistinctPackageIdsMatchExactly_Different_ReturnsFalse()
        {
            var a = new[] { new PackageDescription() { Id = "monkey", Version = null }, new PackageDescription() { Id = "ape", Version = null } };
            var b = new[] { new PackageDescription() { Id = "monkey", Version = null } };
            var actual = PackageDescriptionIdOnlyEqualityComparer.DistinctPackageIdsMatchExactly(a, b);
            var expected = false;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void GetVersionFromNuSpecFile_NullContents_ReturnsNull()
        {
            var packageManager = new PackageRetriever(null, null);
            var version = packageManager.GetVersionFromNuSpecFile(null);
            Assert.Null(version);
        }

        [Fact]
        public static void GetVersionFromNuSpecFile_EmptyContents_ReturnsNull()
        {
            var packageManager = new PackageRetriever(null, null);
            var version = packageManager.GetVersionFromNuSpecFile(string.Empty);
            Assert.Null(version);
        }

        [Fact]
        public static void GetVersionFromNuSpecFile_InvalidContents_Throws()
        {
            var packageManager = new PackageRetriever(null, null);
            var ex = Assert.Throws<ArgumentException>(() => packageManager.GetVersionFromNuSpecFile("NOT XML..."));
            Assert.Equal("NuSpec contents is not valid to be parsed.", ex.Message);
        }

        [Fact]
        public static void GetVersionFromNuSpecFile_ValidContentsMultipleMetadata_Throws()
        {
            var nuSpecFileContents = @"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd"">
  <metadata>
    <id>Naos.Something</id>
    <version>1.0.300</version>
    <authors>APPVYR-WIN20122\appveyor</authors>
    <owners>APPVYR-WIN20122\appveyor</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Created on 2015-08-18 22:10</description>
    <copyright>Copyright 2015</copyright>
    <dependencies>
      <dependency id=""AWSSDK"" version=""2.3.50.1"" />
    </dependencies>
  </metadata>
  <metadata>
    <id>Naos.Something</id>
    <version>1.0.300</version>
    <authors>APPVYR-WIN20122\appveyor</authors>
    <owners>APPVYR-WIN20122\appveyor</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Created on 2015-08-18 22:10</description>
    <copyright>Copyright 2015</copyright>
    <dependencies>
      <dependency id=""AWSSDK"" version=""2.3.50.1"" />
    </dependencies>
  </metadata>
</package>";

            var packageManager = new PackageRetriever(null, null);
            var ex = Assert.Throws<ArgumentException>(() => packageManager.GetVersionFromNuSpecFile(nuSpecFileContents));
            Assert.Equal("Found multiple metadata nodes in the provided NuSpec.", ex.Message);
        }

        [Fact]
        public static void GetVersionFromNuSpecFile_SucceedsWithXmlHeaderWithEncoding()
        {
            // DO NOT MESS WITH THIS STRING!! - it's got a BOM hiding at the beginning to test
            var nuSpecFileContents = @"﻿<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"">
  <metadata>
    <id>Naos.MessageBus.Hangfire.Database</id>
    <version>1.0.165-addseparatejobtracki</version>
    <authors>Naos Project</authors>
    <owners>Naos Project</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <licenseUrl>https://opensource.org/licenses/MIT</licenseUrl>
    <projectUrl>http://naosproject.com/</projectUrl>
    <iconUrl>http://getthinktank.wpengine.com/wp-content/uploads/2016/05/NAOS-NuGet-Icon.png</iconUrl>
    <description>Created on 2016-05-26 00:39</description>
    <copyright>Copyright (c) 2016 Naos LLC</copyright>
    <dependencies>
      <dependency id=""FluentMigrator"" version=""1.6.1"" />
    </dependencies>
  </metadata>
</package>";

            var packageManager = new PackageRetriever(null, null);
            var version = packageManager.GetVersionFromNuSpecFile(nuSpecFileContents);
            version.Should().Be("1.0.165-addseparatejobtracki");
        }

        [Fact]
        public static void GetVersionFromNuSpecFile_ValidContentsMissingMetadata_Throws()
        {
            var nuSpecFileContents = @"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd"">
</package>";

            var packageManager = new PackageRetriever(null, null);
            var ex = Assert.Throws<ArgumentException>(() => packageManager.GetVersionFromNuSpecFile(nuSpecFileContents));
            Assert.Equal("Could not find metadata in the provided NuSpec.", ex.Message);
        }

        [Fact]
        public static void GetVersionFromNuSpecFile_ValidContentsMultipleVersions_Throws()
        {
            var nuSpecFileContents = @"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd"">
  <metadata>
    <id>Naos.Something</id>
    <version>1.0.299</version>
    <version>1.0.300</version>
    <authors>APPVYR-WIN20122\appveyor</authors>
    <owners>APPVYR-WIN20122\appveyor</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Created on 2015-08-18 22:10</description>
    <copyright>Copyright 2015</copyright>
    <dependencies>
      <dependency id=""AWSSDK"" version=""2.3.50.1"" />
    </dependencies>
  </metadata>
</package>";

            var packageManager = new PackageRetriever(null, null);
            var ex = Assert.Throws<ArgumentException>(() => packageManager.GetVersionFromNuSpecFile(nuSpecFileContents));
            Assert.Equal("Found multiple version nodes in the provided NuSpec.", ex.Message);
        }

        [Fact]
        public static void GetVersionFromNuSpecFile_ValidContentsMissingVersion_Throws()
        {
            var nuSpecFileContents = @"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd"">
  <metadata>
    <id>Naos.Something</id>
    <authors>APPVYR-WIN20122\appveyor</authors>
    <owners>APPVYR-WIN20122\appveyor</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Created on 2015-08-18 22:10</description>
    <copyright>Copyright 2015</copyright>
    <dependencies>
      <dependency id=""AWSSDK"" version=""2.3.50.1"" />
    </dependencies>
  </metadata>
</package>";

            var packageManager = new PackageRetriever(null, null);
            var ex = Assert.Throws<ArgumentException>(() => packageManager.GetVersionFromNuSpecFile(nuSpecFileContents));
            Assert.Equal("Could not find the version in the provided NuSpec.", ex.Message);
        }

        [Fact]
        public static void GetVersionFromNuSpecFile_ValidContents_ValidResult()
        {
            var nuSpecFileContents = @"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd"">
  <metadata>
    <id>Naos.Something</id>
    <version>1.0.299</version>
    <authors>APPVYR-WIN20122\appveyor</authors>
    <owners>APPVYR-WIN20122\appveyor</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Created on 2015-08-18 22:10</description>
    <copyright>Copyright 2015</copyright>
    <dependencies>
      <dependency id=""AWSSDK"" version=""2.3.50.1"" />
    </dependencies>
  </metadata>
</package>";

            var packageManager = new PackageRetriever(null, null);
            var version = packageManager.GetVersionFromNuSpecFile(nuSpecFileContents);
            Assert.Equal("1.0.299", version);
        }
    }
}
