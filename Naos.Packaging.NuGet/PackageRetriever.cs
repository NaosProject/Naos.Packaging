﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageRetriever.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.NuGet
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using System.Xml;

    using Naos.Packaging.Domain;

    /// <summary>
    /// NuGet specific implementation of <see cref="IGetPackages"/>.
    /// </summary>
    public class PackageRetriever : IGetPackages
    {
        private const string DirectoryDateTimeToStringFormat = "yyyy-MM-dd--HH-mm-ss--ffff";

        private readonly string defaultWorkingDirectory;

        private readonly IManageNuGetPackages nugetManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageRetriever"/> class.
        /// </summary>
        /// <param name="defaultWorkingDirectory">Working directory to download temporary files to.</param>
        public PackageRetriever(string defaultWorkingDirectory) : this(null, defaultWorkingDirectory)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageRetriever"/> class.
        /// </summary>
        /// <param name="repoConfig">Package repository configuration.</param>
        /// <param name="defaultWorkingDirectory">Working directory to download temporary files to.</param>
        public PackageRetriever(PackageRepositoryConfiguration repoConfig, string defaultWorkingDirectory)
        {
            this.defaultWorkingDirectory = defaultWorkingDirectory;

            this.nugetManager = repoConfig == null
                                    ? new NuGetPackageManager()
                                    : new NuGetPackageManager(
                                          repoConfig.ProtocolVersion,
                                          repoConfig.SourceName,
                                          repoConfig.Source,
                                          repoConfig.Username,
                                          repoConfig.ClearTextPassword);
        }

        /// <summary>
        /// Designed to chain with the constructor this will make sure the default working directory is cleaned up (for failed previous runs).
        /// </summary>
        /// <returns>Current object to allow chaining with constructor.</returns>
        public PackageRetriever WithCleanWorkingDirectory()
        {
            if (Directory.Exists(this.defaultWorkingDirectory))
            {
                Directory.Delete(this.defaultWorkingDirectory, true);
            }

            Directory.CreateDirectory(this.defaultWorkingDirectory);
            return this;
        }

        /// <summary>
        /// Compares the distinct package IDs of two sets for equality.
        /// </summary>
        /// <param name="firstSet">First set of packages to compare.</param>
        /// <param name="secondSet">Second set of packages to compare.</param>
        /// <returns>Whether or not the distinct package IDs match exactly.</returns>
        public static bool DistinctPackageIdsMatchExactly(
            ICollection<PackageDescription> firstSet,
            ICollection<PackageDescription> secondSet)
        {
            if (firstSet.Count == 0 && secondSet.Count == 0)
            {
                return true;
            }

            var firstSetDistinctOrderedIds = firstSet.Select(_ => _.Id).ToList().Distinct().OrderBy(_ => _);
            var secondSetDistinctOrderedIds = secondSet.Select(_ => _.Id).ToList().Distinct().OrderBy(_ => _);

            var ret = firstSetDistinctOrderedIds.SequenceEqual(secondSetDistinctOrderedIds);
            return ret;
        }

        /// <inheritdoc />
        public byte[] GetPackageFile(PackageDescription packageDescription, bool bundleAllDependencies = false)
        {
            if (string.Equals(packageDescription.Id, PackageDescription.NullPackageId, StringComparison.CurrentCultureIgnoreCase))
            {
                return null;
            }

            var workingDirectory = Path.Combine(
                this.defaultWorkingDirectory,
                "Down-" + DateTime.Now.ToString(DirectoryDateTimeToStringFormat));
            byte[] ret;
            if (bundleAllDependencies)
            {
                var packageFilePaths = this.DownloadPackages(new[] { packageDescription }, workingDirectory, true);
                var bundleStagePath = Path.Combine(workingDirectory, "Bundle");
                foreach (var packageFilePath in packageFilePaths)
                {
                    var packageName = new FileInfo(packageFilePath).Name.Replace(".nupkg", string.Empty);
                    var targetPath = Path.Combine(bundleStagePath, packageName);
                    ZipFile.ExtractToDirectory(packageFilePath, targetPath);

                    // delete tools dir to avoid unnecessary issues with unrelated assemblies
                    var toolsPath = Path.Combine(targetPath, "tools");
                    if (Directory.Exists(toolsPath))
                    {
                        Directory.Delete(toolsPath, true);
                    }

                    // thin out older frameworks so there is a single copy of the assembly (like if we have net45, net40, net35, windows8, etc. - only keep net45...).
                    var libPath = Path.Combine(targetPath, "lib");
                    var frameworkDirectories = Directory.Exists(libPath)
                                                   ? Directory.GetDirectories(libPath)
                                                   : new string[0];

                    if (frameworkDirectories.Any())
                    {
                        var frameworkFolderToKeep = frameworkDirectories.Length == 1
                                                        ? frameworkDirectories.Single()
                                                        : frameworkDirectories.Where(
                                                            directoryPath =>
                                                                {
                                                                    var directoryName = Path.GetFileName(directoryPath);
                                                                    var includeInWhere = directoryName != null
                                                                                         && directoryName.StartsWith(
                                                                                             "net",
                                                                                             StringComparison.InvariantCultureIgnoreCase);
                                                                    return includeInWhere;
                                                                }).OrderByDescending(_ => _).FirstOrDefault();

                        // ReSharper disable once ConvertIfStatementToNullCoalescingExpression - seems more confusing that way...
                        if (frameworkFolderToKeep == null)
                        {
                            // this will happen with a package that doesn't honor the 'NET' prefix on framework folders...
                            frameworkFolderToKeep = frameworkDirectories.Where(
                                directoryPath =>
                                    {
                                        var directoryName = Path.GetFileName(directoryPath);
                                        var includeInWhere = directoryName != null;
                                        return includeInWhere;
                                    }).OrderByDescending(_ => _).FirstOrDefault();
                        }

                        var unnecessaryFrameworks =
                            frameworkDirectories.Except(new[] { frameworkFolderToKeep }).ToList();
                        foreach (var unnecessaryFramework in unnecessaryFrameworks)
                        {
                            Directory.Delete(unnecessaryFramework, true);
                        }
                    }
                }

                var bundledFilePath = Path.Combine(workingDirectory, packageDescription.Id + "_DependenciesBundled.zip");
                ZipFile.CreateFromDirectory(bundleStagePath, bundledFilePath);
                ret = File.ReadAllBytes(bundledFilePath);
            }
            else
            {
                var packageFilePath = this.DownloadPackages(new[] { packageDescription }, workingDirectory).Single();
                ret = File.ReadAllBytes(packageFilePath);
            }

            // clean up temp files
            Directory.Delete(workingDirectory, true);

            return ret;
        }

        /// <inheritdoc />
        public Package GetPackage(PackageDescription packageDescription, bool bundleAllDependencies)
        {
            var ret = new Package
                          {
                              PackageDescription = packageDescription,
                              PackageFileBytes = this.GetPackageFile(packageDescription, bundleAllDependencies),
                              PackageFileBytesRetrievalDateTimeUtc = DateTime.UtcNow,
                              AreDependenciesBundled = bundleAllDependencies
                          };

            return ret;
        }

        /// <inheritdoc />
        public IDictionary<string, string> GetMultipleFileContentsFromPackageAsStrings(
            Package package,
            string searchPattern,
            Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            var dictionaryBytes = this.GetMultipleFileContentsFromPackageAsBytes(package, searchPattern);
            var dictionaryStrings = dictionaryBytes.ToDictionary(_ => _.Key, _ => encoding.GetString(_.Value));

            return dictionaryStrings;
        }

        /// <inheritdoc />
        public IDictionary<string, byte[]> GetMultipleFileContentsFromPackageAsBytes(
            Package package,
            string searchPattern)
        {
            if (string.Equals(package.PackageDescription.Id, PackageDescription.NullPackageId, StringComparison.CurrentCultureIgnoreCase))
            {
                return new Dictionary<string, byte[]>();
            }

            // download package (decompressed)
            var workingDirectory = Path.Combine(
                this.defaultWorkingDirectory,
                "PackageFileContentsSearch-" + DateTime.Now.ToString(DirectoryDateTimeToStringFormat));
            var packageFilePath = Path.Combine(workingDirectory, "Package.zip");
            Directory.CreateDirectory(workingDirectory);
            File.WriteAllBytes(packageFilePath, package.PackageFileBytes);
            ZipFile.ExtractToDirectory(packageFilePath, Directory.GetParent(packageFilePath).FullName);

            // get list of files as fullpath strings
            var files = Directory.GetFiles(workingDirectory, "*", SearchOption.AllDirectories);

            // normalize slashes in searchPattern AND in file list
            var normalizedSlashesSearchPattern = searchPattern.Replace(@"\", "/");
            var normalizedSlashesFiles = files.Select(_ => _.Replace(@"\", "/"));
            var filesToGetContentsFor =
                normalizedSlashesFiles.Where(
                    _ =>
                    CultureInfo.CurrentCulture.CompareInfo.IndexOf(
                        _,
                        normalizedSlashesSearchPattern,
                        CompareOptions.IgnoreCase) >= 0);

            var ret = filesToGetContentsFor.ToDictionary(_ => _, File.ReadAllBytes);

            // clean up temp files
            Directory.Delete(workingDirectory, true);

            return ret;
        }

        /// <inheritdoc />
        public string GetVersionFromNuSpecFile(string nuSpecFileContents)
        {
            if (string.IsNullOrEmpty(nuSpecFileContents))
            {
                return null;
            }

            var missingMetaDataMessage = "Could not find metadata in the provided NuSpec.";
            var multipleMetaDataMessage = "Found multiple metadata nodes in the provided NuSpec.";
            var missingVersionMessage = "Could not find the version in the provided NuSpec.";
            var multipleVersionMessage = "Found multiple version nodes in the provided NuSpec.";

            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(nuSpecFileContents);

                var xpath = "/*[local-name()='package']/*[local-name()='metadata']";
                var nodes = xmlDoc.SelectNodes(xpath);

                if (nodes == null || nodes.Count == 0)
                {
                    throw new ArgumentException(missingMetaDataMessage);
                }

                if (nodes.Count > 1)
                {
                    throw new ArgumentException(multipleMetaDataMessage);
                }

                // only one node now
                var childNodes = nodes[0];

                var versionNodes = new List<XmlNode>();
                foreach (XmlNode childNode in childNodes)
                {
                    if (childNode.Name == "version")
                    {
                        versionNodes.Add(childNode);
                    }
                }

                if (versionNodes.Count == 0)
                {
                    throw new ArgumentException(missingVersionMessage);
                }

                if (versionNodes.Count > 1)
                {
                    throw new ArgumentException(multipleVersionMessage);
                }

                var ret = versionNodes.Single().InnerText;

                return ret;
            }
            catch (Exception ex)
            {
                if (ex.Message == missingMetaDataMessage || ex.Message == multipleMetaDataMessage
                    || ex.Message == missingVersionMessage || ex.Message == multipleVersionMessage)
                {
                    throw;
                }

                throw new ArgumentException("NuSpec contents is not valid to be parsed.", ex);
            }
        }

        /// <inheritdoc />
        public string GetLatestVersion(string packageId)
        {
            var latestVersionTask = this.nugetManager.GetLatestVersionAsync(packageId, true, true);
            latestVersionTask.Wait();
            return latestVersionTask.Result;
        }

        /// <inheritdoc />
        public ICollection<string> DownloadPackages(
            ICollection<PackageDescription> packageDescriptions,
            string workingDirectory,
            bool includeDependencies = false)
        {
            if (!Directory.Exists(workingDirectory))
            {
                Directory.CreateDirectory(workingDirectory);
            }

            var workingDirectorySnapshotBefore = Directory.GetFiles(workingDirectory, "*", SearchOption.AllDirectories);

            foreach (var packageDescription in packageDescriptions)
            {
                var packageVersion = !string.IsNullOrEmpty(packageDescription.Version)
                                  ? packageDescription.Version
                                  : this.GetLatestVersion(packageDescription.Id);

                this.nugetManager.DownloadPackageToPathAsync(
                    packageDescription.Id,
                    packageVersion,
                    workingDirectory,
                    includeDependencies,
                    true,
                    true).Wait();
            }

            var workingDirectorySnapshotAfter = Directory.GetFiles(workingDirectory, "*", SearchOption.AllDirectories);

            var ret =
                workingDirectorySnapshotAfter.Except(workingDirectorySnapshotBefore)
                    .Where(_ => _.EndsWith(".nupkg"))
                    .ToList();

            return ret;
        }
    }
}
