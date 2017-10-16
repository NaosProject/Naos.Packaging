// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageRetriever.cs" company="Naos">
//   Copyright 2015 Naos
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
    using System.Xml;

    using Naos.Packaging.Domain;

    using OBeautifulCode.Reflection;

    using Spritely.Redo;

    /// <summary>
    /// NuGet specific implementation of <see cref="IGetPackages"/>.
    /// </summary>
    public class PackageRetriever : IGetPackages, IRemovePackages, IDisposable
    {
        private const string DirectoryDateTimeToStringFormat = "yyyy-MM-dd--HH-mm-ss--ffff";

        private const string NugetExeFileName = "nuget.exe";

        private readonly string defaultWorkingDirectory;

        private readonly string tempDirectory;

        private readonly string nugetConfigFilePath;

        private readonly string nugetExeFilePath;

        private readonly Action<string> consoleOutputCallback;

        private readonly IReadOnlyCollection<PackageRepositoryConfiguration> packageRepositoryConfigurations;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageRetriever"/> class.
        /// </summary>
        /// <param name="defaultWorkingDirectory">Working directory to download temporary files to.</param>
        /// <param name="repoConfigs">Package repository configurations.</param>
        /// <param name="nugetExeFilePath">Optional.  Path to nuget.exe. If null then an embedded copy of nuget.exe will be used.</param>
        /// <param name="consoleOutputCallback">Optional.  If specified, then console output will be written to this action.</param>
        public PackageRetriever(
            string defaultWorkingDirectory,
            IReadOnlyCollection<PackageRepositoryConfiguration> repoConfigs,
            string nugetExeFilePath = null,
            Action<string> consoleOutputCallback = null)
        {
            this.defaultWorkingDirectory = defaultWorkingDirectory;

            this.tempDirectory = this.SetupTempWorkingDirectory(defaultWorkingDirectory);

            if (repoConfigs == null)
            {
                throw new ArgumentNullException(nameof(repoConfigs));
            }

            if (repoConfigs.Contains(null))
            {
                throw new ArgumentException($"{nameof(repoConfigs)} contains null element");
            }

            if (!repoConfigs.Any())
            {
                throw new ArgumentException($"{nameof(repoConfigs)} is empty");
            }

            this.nugetExeFilePath = SetupNugetExe(nugetExeFilePath);

            this.consoleOutputCallback = consoleOutputCallback;

            var configFilePath = Path.Combine(this.tempDirectory, "NuGet.config");
            var packageSource = string.Empty;
            var packageSourceCredentials = string.Empty;

            foreach (var repoConfig in repoConfigs)
            {
                packageSource = $@"{packageSource}{Environment.NewLine}<add key=""{repoConfig.SourceName}"" value=""{repoConfig.Source}"" />";
                if (!string.IsNullOrWhiteSpace(repoConfig.Username) ||
                    (!string.IsNullOrWhiteSpace(repoConfig.ClearTextPassword)))
                {
                    packageSourceCredentials = $@"{packageSourceCredentials}
                                                  <packageSourceCredentials>
                                                    <{repoConfig.SourceName}>
                                                      <add key=""Username"" value=""{repoConfig.Username}"" />
                                                      <add key=""ClearTextPassword"" value=""{repoConfig.ClearTextPassword}"" />
                                                    </{repoConfig.SourceName}>
                                                  </packageSourceCredentials>";
                }
            }

            var configXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                   <configuration>
                                     <packageSources>
                                       {packageSource}
                                     </packageSources>
                                     {packageSourceCredentials}
                                   </configuration>";

            File.WriteAllText(configFilePath, configXml, Encoding.UTF8);
            this.nugetConfigFilePath = configFilePath;
            this.packageRepositoryConfigurations = repoConfigs;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageRetriever"/> class.
        /// </summary>
        /// <param name="defaultWorkingDirectory">Working directory to download temporary files to.</param>
        /// <param name="nugetConfigFilePath">Path a nuget config xml file.</param>
        /// <param name="nugetExeFilePath">Optional.  Path to nuget.exe. If null then an embedded copy of nuget.exe will be used.</param>
        /// <param name="consoleOutputCallback">Optional.  If specified, then console output will be written to this action.</param>
        public PackageRetriever(
            string defaultWorkingDirectory,
            string nugetConfigFilePath,
            string nugetExeFilePath = null,
            Action<string> consoleOutputCallback = null)
        {
            this.defaultWorkingDirectory = defaultWorkingDirectory;

            this.tempDirectory = this.SetupTempWorkingDirectory(defaultWorkingDirectory);

            if (!File.Exists(nugetConfigFilePath))
            {
                throw new ArgumentException($"{nameof(nugetConfigFilePath)} does not exist on disk.");
            }

            this.nugetConfigFilePath = nugetConfigFilePath;

            this.nugetExeFilePath = SetupNugetExe(nugetExeFilePath);

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
                    throw new ArgumentException($"{nameof(nugetConfigFilePath)} has no packageSources");
                }

                foreach (XmlNode node in nodes)
                {
                    if (node == null)
                    {
                        throw new ArgumentException($"Could not parse {nameof(nugetConfigFilePath)} - found null node");
                    }

                    var sourceName = node.Attributes["key"]?.InnerText;
                    var source = node.Attributes["value"]?.InnerText;
                    var protocolVersionString = node.Attributes["protocolVersion"]?.InnerText;
                    int? protocolVersion = null;
                    if (!string.IsNullOrWhiteSpace(protocolVersionString))
                    {
                        try
                        {
                            protocolVersion = int.Parse(protocolVersionString);
                        }
                        catch (Exception)
                        {
                            throw new ArgumentException($"In {nameof(nugetConfigFilePath)} source '{sourceName}:{source}' has an invalid protocolVersion");
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
                throw new ArgumentException($"Could not parse {nameof(nugetConfigFilePath)}", ex);
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
            if (string.Equals(packageDescription.Id, PackageDescription.NullPackageId, StringComparison.CurrentCultureIgnoreCase))
            {
                return null;
            }

            var workingDirectory = Path.Combine(
                this.defaultWorkingDirectory,
                "Down-" + DateTime.Now.ToString(DirectoryDateTimeToStringFormat));

            var packageFilePath =
                Using
                    .LinearBackOff(TimeSpan.FromSeconds(5))
                    .Run(() => this.DownloadPackages(new[] { packageDescription }, workingDirectory).Single())
                    .Now();

            var ret = File.ReadAllBytes(packageFilePath);

            // clean up temp files
            Directory.Delete(workingDirectory, true);

            return ret;
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
                var packageVersion = packageDescription.Version;
                if (string.IsNullOrWhiteSpace(packageVersion))
                {
                    packageVersion = this.GetLatestVersion(packageDescription.Id);
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
                    var packagesConfigXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                           <packages>
                                             <package id=""{packageDescription.Id}"" version=""{packageVersion}"" />
                                           </packages>";
                    string packagesConfigXmlDirectory = Path.Combine(this.tempDirectory, Path.GetRandomFileName());
                    Directory.CreateDirectory(packagesConfigXmlDirectory);
                    string packagesConfigXmlFilePath = Path.Combine(packagesConfigXmlDirectory, "packages.config");
                    File.WriteAllText(packagesConfigXmlFilePath, packagesConfigXml, Encoding.UTF8);
                    toInstall = $"\"{packagesConfigXmlFilePath}\"";
                }

                // there needs to be a space at the end of the output directory path
                // it doesn't matter whether the output directory has a trailing backslash before the space is added
                // https://stackoverflow.com/questions/17322147/illegal-characters-in-path-for-nuget-pack
                var arguments = $"install {toInstall} -outputdirectory \"{workingDirectory} \" -version {packageVersion} -prerelease -noninteractive";
                consoleOutputCallback?.Invoke($"{DateTime.UtcNow}: Run nuget.exe ({this.nugetExeFilePath}) to download '{packageDescription.Id}-{packageVersion}', using the following arguments{Environment.NewLine}{arguments}{Environment.NewLine}");
                var output = this.RunNugetCommandLine(arguments);
                consoleOutputCallback?.Invoke($"{output}{Environment.NewLine}{DateTime.UtcNow}: Run nuget.exe completed{Environment.NewLine}");
            }

            var workingDirectorySnapshotAfter = Directory.GetFiles(workingDirectory, "*", SearchOption.AllDirectories);

            var ret =
                workingDirectorySnapshotAfter.Except(workingDirectorySnapshotBefore)
                    .Where(_ => _.EndsWith(".nupkg"))
                    .ToList();

            return ret;
        }

        /// <inheritdoc />
        public string GetLatestVersion(string packageId)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentException("packageId cannot be null nor whitespace.");
            }

            // run nuget
            var arguments = $"list {packageId} -prerelease -noninteractive";
            consoleOutputCallback?.Invoke($"{DateTime.UtcNow}: Run nuget.exe ({this.nugetExeFilePath}) to list packages for packageId '{packageId}', using the following arguments{Environment.NewLine}{arguments}{Environment.NewLine}");
            var output = this.RunNugetCommandLine(arguments);
            consoleOutputCallback?.Invoke($"{output}{Environment.NewLine}{DateTime.UtcNow}: Run nuget.exe completed{Environment.NewLine}");
            var outputLines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            /* parse output.  output should look like this (the first line may or may not appear):
            Using credentials from config.UserName: user@domain.com
            AcklenAvenue.Queueing.Serializers.JsonNet 1.0.1.39
            CacheManager.Serialization.Json 0.8.0
            com.egis.hue.sdk 1.0.0.2
            Common.Serializer.NewtonsoftJson 0.2.0-pre
            */

            string version = null;
            foreach (var outputLine in outputLines)
            {
                if (outputLine.StartsWith("Using credentials from config"))
                {
                    continue;
                }

                var tokens = outputLine.Split(' ');
                if (tokens[0].Equals(packageId, StringComparison.OrdinalIgnoreCase))
                {
                    if (version != null)
                    {
                        throw new InvalidOperationException($"Package {packageId} is contained within multiple galleries.");
                    }

                    version = tokens[1].Trim();
                }
            }

            return version;
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

            if (string.IsNullOrWhiteSpace(packageDescription.Version))
            {
                throw new ArgumentException($"{nameof(packageDescription)} {nameof(PackageDescription.Version)} is required");
            }



        }

        /// <inheritdoc />
        public void DeleteAllVersionsOfPackage(
            string id,
            string packageRepositorySourceName,
            string apiKey)
        {
            throw new NotImplementedException();
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

        private string RunNugetCommandLine(
            string arguments,
            bool appendConfigFileArgument = true)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = this.nugetExeFilePath,
                    Arguments = !appendConfigFileArgument ? arguments :  $"{arguments} -configfile \"{this.nugetConfigFilePath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    ErrorDialog = false
                }
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

        private string SetupTempWorkingDirectory(
            string defaultWorkingDirectory)
        {
            if (!Directory.Exists(defaultWorkingDirectory))
            {
                throw new ArgumentException($"{nameof(defaultWorkingDirectory)} does not exist on disk.");
            }

            var result = Path.Combine(this.defaultWorkingDirectory, Path.GetRandomFileName());
            Directory.CreateDirectory(result);
            return result;
        }

        private string SetupNugetExe(
            string nugetExeFilePath)
        {
            if (nugetExeFilePath == null)
            {
                // write embedded nuget.exe to disk
                nugetExeFilePath = Path.Combine(this.tempDirectory, NugetExeFileName);
                using (var nugetExeStream = AssemblyHelper.OpenEmbeddedResourceStream(NugetExeFileName))
                {
                    using (var fileStream = new FileStream(nugetExeFilePath, FileMode.Create, FileAccess.Write))
                    {
                        nugetExeStream.CopyTo(fileStream);
                    }
                }
            }
            else
            {
                if (!File.Exists(nugetExeFilePath))
                {
                    throw new ArgumentException($"{nameof(nugetExeFilePath)} does not exist on disk.");
                }
            }

            return nugetExeFilePath;
        }
    }
}
