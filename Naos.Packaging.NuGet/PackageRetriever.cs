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
    public class PackageRetriever : IGetPackages, IDisposable
    {
        private const string DirectoryDateTimeToStringFormat = "yyyy-MM-dd--HH-mm-ss--ffff";

        private const string NugetExeFileName = "nuget.exe";

        private readonly string defaultWorkingDirectory;

        private readonly string nugetExeFilePath;

        private readonly string nugetConfigFilePath;

        private readonly string tempDirectory;

        private readonly Action<string> consoleOutputCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageRetriever"/> class.
        /// </summary>
        /// <param name="defaultWorkingDirectory">Working directory to download temporary files to.</param>
        /// <param name="repoConfig">Optional.  Package repository configuration. If null then only the public gallery will be configured.</param>
        /// <param name="nugetExeFilePath">Optional.  Path to nuget.exe. If null then an embedded copy of nuget.exe will be used.</param>
        /// <param name="nugetConfigFilePath">
        /// Optional.  Path a nuget config xml file.  If null then a config
        /// file will be written/used.  This file will enable the public gallery
        /// as well as the gallery specified in <paramref name="repoConfig"/>.
        /// </param>
        /// <param name="consoleOutputCallback">Optional.  If specified, then console output will be written to this action.</param>
        public PackageRetriever(
            string defaultWorkingDirectory,
            PackageRepositoryConfiguration repoConfig = null,
            string nugetExeFilePath = null,
            string nugetConfigFilePath = null,
            Action<string> consoleOutputCallback = null)
        {
            // check parameters
            if (!Directory.Exists(defaultWorkingDirectory))
            {
                throw new ArgumentException("The specified default working directory does not exist.");
            }

            if ((repoConfig != null) && (nugetConfigFilePath != null))
            {
                throw new ArgumentException("A repo config was specified along with a config XML file path.  If you would like to include a package repository, either include it in the config XML or don't specify a config XML.");
            }

            // setup temp directory
            this.defaultWorkingDirectory = defaultWorkingDirectory;
            this.tempDirectory = Path.Combine(this.defaultWorkingDirectory, Path.GetRandomFileName());
            Directory.CreateDirectory(this.tempDirectory);

            // write nuget.exe to disk if needed
            if (nugetExeFilePath == null)
            {
                nugetExeFilePath = Path.Combine(this.tempDirectory, NugetExeFileName);
                using (var nugetExeStream = AssemblyHelper.OpenEmbeddedResourceStream(NugetExeFileName))
                {
                    using (var fileStream = new FileStream(nugetExeFilePath, FileMode.Create, FileAccess.Write))
                    {
                        nugetExeStream.CopyTo(fileStream);
                    }
                }
            }

            this.nugetExeFilePath = nugetExeFilePath;

            // write NuGet.config to disk if needed
            if (nugetConfigFilePath == null)
            {
                nugetConfigFilePath = Path.Combine(this.tempDirectory, "NuGet.config");
                string packageSource = string.Empty;
                string packageSourceCredentials = string.Empty;
                if (repoConfig != null)
                {
                    packageSource = $@"<add key=""{repoConfig.SourceName}"" value=""{repoConfig.Source}"" />";
                    packageSourceCredentials = $@"<packageSourceCredentials>
                                                    <{repoConfig.SourceName}>
                                                      <add key=""Username"" value=""{repoConfig.Username}"" />
                                                      <add key=""ClearTextPassword"" value=""{repoConfig.ClearTextPassword}"" />
                                                    </{repoConfig.SourceName}>
                                                  </packageSourceCredentials>";
                }

                var configXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                   <configuration>
                                     <packageSources>
                                       {packageSource}
                                       <add key=""nugetv2"" value=""https://www.nuget.org/api/v2/"" />
                                       <add key=""nugetv3"" value=""https://api.nuget.org/v3/index.json"" />
                                     </packageSources>
                                     {packageSourceCredentials}
                                   </configuration>";

                File.WriteAllText(nugetConfigFilePath, configXml, Encoding.UTF8);
            }

            this.nugetConfigFilePath = nugetConfigFilePath;
            this.consoleOutputCallback = consoleOutputCallback;
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
            string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = this.nugetExeFilePath,
                    Arguments = $"{arguments} -configfile \"{this.nugetConfigFilePath}\"",
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

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException("nuget.exe reported an error: " + error);
                }

                return output;
            }
        }
    }
}
