// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageRetriever.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.NuGet
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;

    using Naos.Packaging.Domain;
    using OBeautifulCode.Assertion.Recipes;
    using OBeautifulCode.Reflection.Recipes;
    using Spritely.Redo;

    using static System.FormattableString;

    /// <summary>
    /// NuGet specific implementation of <see cref="IGetPackages"/>.
    /// </summary>
    public class PackageRetriever : IGetPackages, IRemovePackages, IDisposable
    {
        private const string DirectoryDateTimeToStringFormat = "yyyy-MM-dd--HH-mm-ss--ffff";

        private const string NugetExeFileName = "nuget.exe";

        private readonly string defaultWorkingDirectory;

        private readonly string tempDirectory;

        private readonly string nugetExeFilePath;

        private readonly Action<string> consoleOutputCallback;

        private readonly IReadOnlyCollection<PackageRepositoryConfiguration> packageRepositoryConfigurations;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageRetriever"/> class.
        /// </summary>
        /// <param name="defaultWorkingDirectory">Working directory to download temporary files to.</param>
        /// <param name="repoConfigs">Package repository configurations.</param>
        /// <param name="nugetExeFilePathOverride">Optional.  Path to nuget.exe. If null then an embedded copy of nuget.exe will be used.</param>
        /// <param name="consoleOutputCallback">Optional.  If specified, then console output will be written to this action.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "nuget", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Configs", Justification = "Spelling/name is correct.")]
        public PackageRetriever(
            string defaultWorkingDirectory,
            IReadOnlyCollection<PackageRepositoryConfiguration> repoConfigs,
            string nugetExeFilePathOverride = null,
            Action<string> consoleOutputCallback = null)
        {
            this.defaultWorkingDirectory = defaultWorkingDirectory;

            this.tempDirectory = SetupTempWorkingDirectory(defaultWorkingDirectory);

            if (repoConfigs == null)
            {
                throw new ArgumentNullException(nameof(repoConfigs));
            }

            if (repoConfigs.Contains(null))
            {
                throw new ArgumentException(Invariant($"{nameof(repoConfigs)} contains null element"));
            }

            if (!repoConfigs.Any())
            {
                throw new ArgumentException(Invariant($"{nameof(repoConfigs)} is empty"));
            }

            this.nugetExeFilePath = this.SetupNugetExe(nugetExeFilePathOverride);

            this.consoleOutputCallback = consoleOutputCallback;

            this.packageRepositoryConfigurations = repoConfigs;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageRetriever"/> class.
        /// </summary>
        /// <param name="defaultWorkingDirectory">Working directory to download temporary files to.</param>
        /// <param name="nugetConfigFilePath">Path a nuget config xml file.</param>
        /// <param name="nugetExeFilePath">Optional.  Path to nuget.exe. If null then an embedded copy of nuget.exe will be used.</param>
        /// <param name="consoleOutputCallback">Optional.  If specified, then console output will be written to this action.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "nuget", Justification = "Spelling/name is correct.")]
        public PackageRetriever(
            string defaultWorkingDirectory,
            string nugetConfigFilePath,
            string nugetExeFilePath = null,
            Action<string> consoleOutputCallback = null)
        {
            this.defaultWorkingDirectory = defaultWorkingDirectory;

            this.tempDirectory = SetupTempWorkingDirectory(defaultWorkingDirectory);

            if (!File.Exists(nugetConfigFilePath))
            {
                throw new ArgumentException(Invariant($"{nameof(nugetConfigFilePath)} does not exist on disk."));
            }

            this.nugetExeFilePath = this.SetupNugetExe(nugetExeFilePath);

            this.consoleOutputCallback = consoleOutputCallback;

            var repoConfigs = new List<PackageRepositoryConfiguration>();
            try
            {
                var nugetConfigFileContents = File.ReadAllText(this.nugetConfigFilePath);

                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(nugetConfigFileContents);

                var xpath = "/*[local-name()='configuration']/*[local-name()='packageSources']/*[local-name()='add']";
                var nodes = xmlDoc.SelectNodes(xpath);

                if (nodes == null || nodes.Count == 0)
                {
                    throw new ArgumentException(Invariant($"{nameof(nugetConfigFilePath)} has no packageSources"));
                }

                foreach (XmlNode node in nodes)
                {
                    if (node == null)
                    {
                        throw new ArgumentException(Invariant($"Could not parse {nameof(nugetConfigFilePath)} - found null node"));
                    }

                    var sourceName = node.Attributes["key"]?.InnerText;
                    var source = node.Attributes["value"]?.InnerText;
                    var protocolVersionString = node.Attributes["protocolVersion"]?.InnerText;
                    int? protocolVersion = null;
                    if (!string.IsNullOrWhiteSpace(protocolVersionString))
                    {
                        try
                        {
                            protocolVersion = int.Parse(protocolVersionString, CultureInfo.InvariantCulture);
                        }
                        catch (Exception)
                        {
                            throw new ArgumentException(Invariant($"In {nameof(nugetConfigFilePath)} source '{sourceName}:{source}' has an invalid protocolVersion"));
                        }
                    }

                    var repoConfig = new PackageRepositoryConfiguration()
                    {
                        Source = source,
                        SourceName = sourceName,
                        ProtocolVersion = protocolVersion,
                    };

                    repoConfigs.Add(repoConfig);
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException(Invariant($"Could not parse {nameof(nugetConfigFilePath)}"), ex);
            }

            this.packageRepositoryConfigurations = repoConfigs;
        }

        /// <inheritdoc />
        public Package GetPackage(
            PackageDescription packageDescription)
        {
            var ret = new Package
                          {
                              PackageDescription = packageDescription,
                              PackageFileBytes = this.GetPackageFile(packageDescription),
                              PackageFileBytesRetrievalDateTimeUtc = DateTime.UtcNow,
                          };

            return ret;
        }

        /// <inheritdoc />
        public byte[] GetPackageFile(
            PackageDescription packageDescription)
        {
            new { packageDescription }.Must().NotBeNull();
            var ret =
                Using
                    .LinearBackOff(TimeSpan.FromSeconds(5))
                    .Run(() =>
                    {
                        var workingDirectory = Path.Combine(
                            this.defaultWorkingDirectory,
                            "Down-" + DateTime.Now.ToString(
                                DirectoryDateTimeToStringFormat,
                                CultureInfo.InvariantCulture));

                        var packageFilePath = this.DownloadPackages(new[] { packageDescription }, workingDirectory).Single();
                        var packageFileBytes = File.ReadAllBytes(packageFilePath);

                        // clean up temp files
                        Directory.Delete(workingDirectory, true);
                        return packageFileBytes;
                    })
                    .Now();

            return ret;
        }

        /// <inheritdoc />
        public ICollection<string> DownloadPackages(
            ICollection<PackageDescription> packageDescriptions,
            string workingDirectory,
            bool includeDependencies = false,
            bool includePrerelease = true,
            bool includeDelisted = false,
            string packageRepositorySourceName = null,
            ConflictingLatestVersionStrategy conflictingLatestVersionStrategy = ConflictingLatestVersionStrategy.UseHighestVersion)
        {
            if (!Directory.Exists(workingDirectory))
            {
                Directory.CreateDirectory(workingDirectory);
            }

            var workingDirectorySnapshotBefore = Directory.GetFiles(workingDirectory, "*", SearchOption.AllDirectories);

            foreach (var packageDescription in packageDescriptions ?? new PackageDescription[0])
            {
                var packageVersion = packageDescription.Version;
                if (string.IsNullOrWhiteSpace(packageVersion))
                {
                    packageVersion = this.GetLatestVersion(packageDescription.Id, includePrerelease, includeDelisted, packageRepositorySourceName, conflictingLatestVersionStrategy)?.Version;
                    if (packageVersion == null)
                    {
                        throw new ArgumentException(
                            "Could not find a version for the package (package ID may be incorrect or containing source may be offline); ID: "
                            + packageDescription.Id);
                    }
                }

                string toInstall;
                if (includeDependencies)
                {
                    toInstall = packageDescription.Id;
                }
                else
                {
                    // the only way to install without dependencies is to create a packages.config xml file
                    // to install with dependencies we can simply point nuget.exe at the package id
                    // the file must be called packages.config
                    var packagesConfigXml = Invariant($@"<?xml version=""1.0"" encoding=""utf-8""?>
                                           <packages>
                                             <package id=""{packageDescription.Id}"" version=""{packageVersion}"" />
                                           </packages>");
                    var packagesConfigXmlDirectory = Path.Combine(this.tempDirectory, Path.GetRandomFileName());
                    Directory.CreateDirectory(packagesConfigXmlDirectory);
                    var packagesConfigXmlFilePath = Path.Combine(packagesConfigXmlDirectory, "packages.config");
                    File.WriteAllText(packagesConfigXmlFilePath, packagesConfigXml, Encoding.UTF8);
                    toInstall = Invariant($"\"{packagesConfigXmlFilePath}\"");
                }

                // there needs to be a space at the end of the output directory path
                // it doesn't matter whether the output directory has a trailing backslash before the space is added
                // https://stackoverflow.com/questions/17322147/illegal-characters-in-path-for-nuget-pack
                var arguments = Invariant($"install {toInstall} -outputdirectory \"{workingDirectory} \" -version {packageVersion} -prerelease");
                this.consoleOutputCallback?.Invoke(Invariant($"{DateTime.UtcNow}: Run nuget.exe ({this.nugetExeFilePath}) to download '{packageDescription.Id}-{packageVersion}', using the following arguments{Environment.NewLine}{arguments}{Environment.NewLine}"));
                var output = this.RunNugetCommandLine(arguments);
                this.consoleOutputCallback?.Invoke(Invariant($"{output}{Environment.NewLine}{DateTime.UtcNow}: Run nuget.exe completed{Environment.NewLine}"));
            }

            var workingDirectorySnapshotAfter = Directory.GetFiles(workingDirectory, "*", SearchOption.AllDirectories);

            var ret =
                workingDirectorySnapshotAfter.Except(workingDirectorySnapshotBefore)
                    .Where(_ => _.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
                    .ToList();

            return ret;
        }

        /// <inheritdoc />
        public PackageDescription GetLatestVersion(
            string packageId,
            bool includePrerelease = true,
            bool includeDelisted = false,
            string packageRepositorySourceName = null,
            ConflictingLatestVersionStrategy conflictingLatestVersionStrategy = ConflictingLatestVersionStrategy.UseHighestVersion)
        {
            var packageDescriptionUsingListApi = this.GetLatestVersionUsingListApi(packageId, includePrerelease, includeDelisted, packageRepositorySourceName, conflictingLatestVersionStrategy);

            var packageDescriptionUsingSearchApi = this.GetLatestVersionUsingSearchApi(packageId, includePrerelease, includeDelisted, packageRepositorySourceName, conflictingLatestVersionStrategy);

            PackageDescription result;

            if ((packageDescriptionUsingListApi != null) && (packageDescriptionUsingSearchApi != null))
            {
                var listVersion = GetVersion(packageDescriptionUsingListApi.Version);
                var searchVersion = GetVersion(packageDescriptionUsingSearchApi.Version);

                if (conflictingLatestVersionStrategy == ConflictingLatestVersionStrategy.ThrowException)
                {
                    if (listVersion != searchVersion)
                    {
                        throw new InvalidOperationException(
                            Invariant($"The latest version of package {packageId} is different in multiple galleries and {nameof(ConflictingLatestVersionStrategy)} is {nameof(ConflictingLatestVersionStrategy.ThrowException)}.  Versions found: {listVersion} and {searchVersion}"));
                    }

                    result = packageDescriptionUsingListApi;
                }
                else if (conflictingLatestVersionStrategy == ConflictingLatestVersionStrategy.UseHighestVersion)
                {
                    result = listVersion > searchVersion
                        ? packageDescriptionUsingListApi
                        : packageDescriptionUsingSearchApi;
                }
                else
                {
                    throw new NotSupportedException(
                        Invariant($"This {nameof(ConflictingLatestVersionStrategy)} is not supported: {conflictingLatestVersionStrategy}"));
                }
            }
            else if (packageDescriptionUsingListApi != null)
            {
                result = packageDescriptionUsingListApi;
            }
            else if (packageDescriptionUsingSearchApi != null)
            {
                result = packageDescriptionUsingSearchApi;
            }
            else
            {
                result = null;
            }

            return result;
        }

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "nuget", Justification = "Spelling/name is correct.")]
        public IReadOnlyCollection<PackageDescription> GetAllVersions(
            string packageId,
            bool includePrerelease = true,
            bool includeDelisted = false,
            string packageRepositorySourceName = null)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentException(Invariant($"{nameof(packageId)} must be specified."));
            }

            var arguments = Invariant($"list {packageId} -allversions");
            if (includePrerelease)
            {
                arguments = Invariant($"{arguments} -prerelease");
            }

            if (includeDelisted)
            {
                arguments = Invariant($"{arguments} -includedelisted");
            }

            var sourceArgument = this.BuildSourceUrlArgumentFromSourceName(packageRepositorySourceName);
            arguments = Invariant($"{arguments} {sourceArgument}");

            this.consoleOutputCallback?.Invoke(Invariant($"{DateTime.UtcNow}: Run nuget.exe ({this.nugetExeFilePath}) to list all packages for packageId '{packageId}', using the following arguments{Environment.NewLine}{arguments}{Environment.NewLine}"));
            var output = this.RunNugetCommandLine(arguments);
            this.consoleOutputCallback?.Invoke(Invariant($"{output}{Environment.NewLine}{DateTime.UtcNow}: Run nuget.exe completed{Environment.NewLine}"));

            throw new NotImplementedException("nuget.exe has a bug whereby only the latest version is returned");
        }

        /// <inheritdoc />
        public void DeletePackage(
            PackageDescription packageDescription,
            string packageRepositorySourceName,
            string apiKey)
        {
            if (packageDescription == null)
            {
                throw new ArgumentNullException(nameof(packageDescription));
            }

            if (string.IsNullOrWhiteSpace(packageDescription.Id))
            {
                throw new ArgumentException(Invariant($"{nameof(packageDescription)} {nameof(PackageDescription.Id)} is required"));
            }

            if (string.IsNullOrWhiteSpace(packageDescription.Version))
            {
                throw new ArgumentException(Invariant($"{nameof(packageDescription)} {nameof(PackageDescription.Version)} is required"));
            }

            if (string.IsNullOrWhiteSpace(packageRepositorySourceName))
            {
                throw new ArgumentException(Invariant($"{nameof(packageRepositorySourceName)} is required"));
            }

            var sourceArgument = this.BuildSourceUrlArgumentFromSourceName(packageRepositorySourceName);
            var arguments = Invariant($"delete {packageDescription.Id} {packageDescription.Version} {sourceArgument}");
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                arguments = Invariant($"{arguments} -ApiKey {apiKey}");
            }

            this.consoleOutputCallback?.Invoke(Invariant($"{DateTime.UtcNow}: Run nuget.exe ({this.nugetExeFilePath}) to delete package id '{packageDescription.Id}' version '{packageDescription.Version}', using the following arguments{Environment.NewLine}{arguments}{Environment.NewLine}"));

            // tested on the Nuget.org public gallery:
            // if the version of the package has been unlisted, then no error occurs
            // if the version of the package has never existed, then we get a 404 error
            // if the package name has never existed, then we get a 404 error
            var output = this.RunNugetCommandLine(arguments, appendConfigFileArgument: false);
            this.consoleOutputCallback?.Invoke(Invariant($"{output}{Environment.NewLine}{DateTime.UtcNow}: Run nuget.exe completed{Environment.NewLine}"));
        }

        /// <inheritdoc />
        public void DeleteAllVersionsOfPackage(
            string packageId,
            string packageRepositorySourceName,
            string apiKey)
        {
            var packageDescriptions = this.GetAllVersions(packageId, includePrerelease: true, includeDelisted: true, packageRepositorySourceName: packageRepositorySourceName);
            foreach (var packageDescription in packageDescriptions)
            {
                this.DeletePackage(packageDescription, packageRepositorySourceName, apiKey);
            }
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
            new { package }.Must().NotBeNull();
            new { searchPattern }.Must().NotBeNull();

            // download package (decompressed)
            var workingDirectory = Path.Combine(
                this.defaultWorkingDirectory,
                "PackageFileContentsSearch-" + DateTime.Now.ToString(DirectoryDateTimeToStringFormat, CultureInfo.InvariantCulture));
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
        public string GetVersionFromNuSpecFile(
            string nuSpecFileContents)
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
                var sanitizedNuSpecFileContents = nuSpecFileContents.Replace("\ufeff", string.Empty); // strip the BOM as it makes XML.Load bomb...;
                xmlDoc.LoadXml(sanitizedNuSpecFileContents);

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
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the class.
        /// </summary>
        /// <param name="disposing">Determines if managed resources should be disposed.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Not much we can do if it doesn't work.")]
        protected virtual void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                try
                {
                    Directory.Delete(this.tempDirectory, true);
                }
                catch (Exception)
                {
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "It is managed.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "nuget", Justification = "Spelling/name is correct.")]
        private string RunNugetCommandLine(
            string arguments,
            bool appendConfigFileArgument = false)
        {
            arguments = Invariant($"{arguments} -noninteractive");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = this.nugetExeFilePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    ErrorDialog = false,
                },
            };

            using (process)
            {
                if (!process.Start())
                {
                    throw new InvalidOperationException("nuget.exe could not be started.");
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException("nuget.exe reported an error: " + error);
                }

                return output;
            }
        }

        private static string SetupTempWorkingDirectory(string workingDirectory)
        {
            if (!Directory.Exists(workingDirectory))
            {
                throw new ArgumentException(Invariant($"{nameof(workingDirectory)} does not exist on disk."));
            }

            var result = Path.Combine(workingDirectory, Path.GetRandomFileName());
            Directory.CreateDirectory(result);
            return result;
        }

        private static Version GetVersion(
            string version)
        {
            var result = new Version(version.Split(new[] { "-" }, StringSplitOptions.RemoveEmptyEntries).First());

            return result;
        }

        private string SetupNugetExe(string nugetExeFilePathOverride)
        {
            var result = nugetExeFilePathOverride;
            if (result == null)
            {
                // write embedded nuget.exe to disk
                result = Path.Combine(this.tempDirectory, NugetExeFileName);
                using (var nugetExeStream = AssemblyHelper.OpenEmbeddedResourceStream(NugetExeFileName))
                {
                    using (var fileStream = new FileStream(result, FileMode.Create, FileAccess.Write))
                    {
                        nugetExeStream.CopyTo(fileStream);
                    }
                }
            }
            else
            {
                if (!File.Exists(result))
                {
                    throw new ArgumentException(Invariant($"{nameof(result)} does not exist on disk."));
                }
            }

            return result;
        }

        private string BuildSourceUrlArgumentFromSourceName(
            string packageRepositorySourceName)
        {
            var result = string.Empty;
            if (!string.IsNullOrWhiteSpace(packageRepositorySourceName))
            {
                var packageRepository = this.packageRepositoryConfigurations.SingleOrDefault(_ => _.SourceName.Equals(packageRepositorySourceName, StringComparison.OrdinalIgnoreCase));
                if (packageRepository == null)
                {
                    throw new ArgumentException(Invariant($"{nameof(packageRepositorySourceName)} is not a valid source in the nuget config"));
                }

                result = Invariant($"-source \"{packageRepository.Source}\"");
            }

            return result;
        }

        private IReadOnlyCollection<string> ExecutePackageSearch(
            string packageId,
            string api,
            bool includePrerelease,
            bool includeDelisted,
            string packageRepositorySourceName)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentException("packageId cannot be null nor whitespace.");
            }

            var arguments = Invariant($"{api} {packageId}");
            if (includePrerelease)
            {
                arguments = Invariant($"{arguments} -prerelease");
            }

            if (includeDelisted)
            {
                arguments = Invariant($"{arguments} -includedelisted");
            }

            if (!string.IsNullOrWhiteSpace(packageRepositorySourceName))
            {
                var sourceArgument = this.BuildSourceUrlArgumentFromSourceName(packageRepositorySourceName);

                arguments = Invariant($"{arguments} {sourceArgument}");
            }

            this.consoleOutputCallback?.Invoke(Invariant($"{DateTime.UtcNow}: Run nuget.exe ({this.nugetExeFilePath}) to list latest package for packageId '{packageId}', using the following arguments{Environment.NewLine}{arguments}{Environment.NewLine}"));
            var output = this.RunNugetCommandLine(arguments);

            this.consoleOutputCallback?.Invoke(Invariant($"{output}{Environment.NewLine}{DateTime.UtcNow}: Run nuget.exe completed{Environment.NewLine}"));
            var result = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            return result;
        }

        private PackageDescription GetLatestVersionUsingListApi(
            string packageId,
            bool includePrerelease,
            bool includeDelisted,
            string packageRepositorySourceName,
            ConflictingLatestVersionStrategy conflictingLatestVersionStrategy)
        {
            var outputLines = this.ExecutePackageSearch(packageId, "list", includePrerelease, includeDelisted, packageRepositorySourceName);

            /* parse output.  output should look like this (the first line may or may not appear):
            Using credentials from config.UserName: user@domain.com
            AcklenAvenue.Queueing.Serializers.JsonNet 1.0.1.39
            CacheManager.Serialization.Json 0.8.0
            com.egis.hue.sdk 1.0.0.2
            Common.Serializer.NewtonsoftJson 0.2.0-pre
            */

            PackageDescription result = null;
            foreach (var outputLine in outputLines)
            {
                if (outputLine.StartsWith("Using credentials from config", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // some nuget gallery servers don't support the list API
                if (outputLine.StartsWith("WARNING: This version of nuget.exe does not support listing packages", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var tokens = outputLine.Split(' ');
                var foundPackageId = tokens[0];

                if (foundPackageId.Equals(packageId, StringComparison.OrdinalIgnoreCase))
                {
                    var foundVersion = tokens[1].Trim();

                    if (result == null)
                    {
                        result = new PackageDescription { Id = packageId, Version = foundVersion };
                    }
                    else
                    {
                        if (conflictingLatestVersionStrategy == ConflictingLatestVersionStrategy.ThrowException)
                        {
                            throw new InvalidOperationException(
                                Invariant($"The latest version of package {packageId} is different in multiple galleries and {nameof(ConflictingLatestVersionStrategy)} is {nameof(ConflictingLatestVersionStrategy.ThrowException)}.  Versions found: {result.Version} and {foundVersion}"));
                        }
                        else if (conflictingLatestVersionStrategy == ConflictingLatestVersionStrategy.UseHighestVersion)
                        {
                            var version1 = GetVersion(result.Version);
                            var version2 = GetVersion(foundVersion);

                            if (version1 == version2)
                            {
                                throw new NotSupportedException(
                                    Invariant($"The latest version of package {packageId} is different in multiple galleries and {nameof(ConflictingLatestVersionStrategy)} is {nameof(ConflictingLatestVersionStrategy.ThrowException)}.  Versions found: {result.Version} and {foundVersion}.  These two versions have the same Major.Minor.Patch version (e.g. 1.2.3).  Comparing [-Suffix] (e.g. 1.2.3-beta1, 1.2.3-beta2) is not supported."));
                            }
                            else if (version2 > version1)
                            {
                                result = new PackageDescription { Id = packageId, Version = foundVersion };
                            }
                        }
                        else
                        {
                            throw new NotSupportedException(
                                Invariant($"This {nameof(ConflictingLatestVersionStrategy)} is not supported: {conflictingLatestVersionStrategy}"));
                        }
                    }
                }
            }

            return result;
        }

        private PackageDescription GetLatestVersionUsingSearchApi(
            string packageId,
            bool includePrerelease,
            bool includeDelisted,
            string packageRepositorySourceName,
            ConflictingLatestVersionStrategy conflictingLatestVersionStrategy)
        {
            var outputLines = this.ExecutePackageSearch(packageId, "search", includePrerelease, includeDelisted, packageRepositorySourceName);

            var regex = new Regex("^> (.*?) \\| (.*?) \\| Downloads.*?$", RegexOptions.Compiled);

            PackageDescription result = null;
            foreach (var outputLine in outputLines)
            {
                var match = regex.Match(outputLine);

                if ((match.Groups.Count == 3) && (!string.IsNullOrWhiteSpace(match.Groups[1].Value)) && (!string.IsNullOrWhiteSpace(match.Groups[2].Value)))
                {
                    var foundPackageId = match.Groups[1].Value;
                    var foundVersion = match.Groups[2].Value;

                    if (foundPackageId.Equals(packageId, StringComparison.OrdinalIgnoreCase))
                    {
                        var packageDescription = new PackageDescription { Id = packageId, Version = foundVersion };

                        if (result == null)
                        {
                            result = packageDescription;

                            continue;
                        }

                        var version1 = GetVersion(result.Version);

                        var version2 = GetVersion(foundVersion);

                        if (conflictingLatestVersionStrategy == ConflictingLatestVersionStrategy.ThrowException)
                        {
                            if (version1 != version2)
                            {
                                throw new InvalidOperationException(
                                    Invariant($"The latest version of package {packageId} is different in multiple galleries and {nameof(ConflictingLatestVersionStrategy)} is {nameof(ConflictingLatestVersionStrategy.ThrowException)}.  Versions found: {result.Version} and {foundVersion}"));
                            }
                        }
                        else if (conflictingLatestVersionStrategy == ConflictingLatestVersionStrategy.UseHighestVersion)
                        {
                            if (version2 > version1)
                            {
                                result = packageDescription;
                            }
                        }
                        else
                        {
                            throw new NotSupportedException(
                                Invariant($"This {nameof(ConflictingLatestVersionStrategy)} is not supported: {conflictingLatestVersionStrategy}"));
                        }
                    }
                }
            }

            return result;
        }
    }
}
