// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NuGetPackageManager.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.NuGet
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Serialization;

    using global::NuGet;
    using global::NuGet.PackageManagement;
    using global::NuGet.Packaging.Core;
    using global::NuGet.ProjectManagement;
    using global::NuGet.Protocol.Core.Types;
    using global::NuGet.Resolver;
    using global::NuGet.Versioning;

    /// <summary>
    /// Wrapper object around NuGet to make usage straightforward and document/hide the nonsense needed to construct a manager.
    /// </summary>
    public class NuGetPackageManager
    {
        private readonly int privateRepositoryProtocolVersion;

        private readonly string privateRepositorySourceName;

        private readonly string privateRepositoryUrl;

        private readonly string privateRepositoryUsername;

        private readonly string privateRepositoryPassword;

        private readonly IReadOnlyCollection<SourceRepository> sourceRepositories;

        private readonly bool includePrivateRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="NuGetPackageManager"/> class.
        /// </summary>
        public NuGetPackageManager() : this(0, null, null, null, null, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NuGetPackageManager"/> class.
        /// </summary>
        /// <param name="privateRepositoryProtocolVersion">Protocol version of a private repository.</param>
        /// <param name="privateRepositorySourceName">Source name of a private repository.</param>
        /// <param name="privateRepositoryUrl">Source URL of a private repository.</param>
        /// <param name="privateRepositoryUsername">Username of a private repository.</param>
        /// <param name="privateRepositoryPassword">Password of a private repository.</param>
        public NuGetPackageManager(
            int privateRepositoryProtocolVersion,
            string privateRepositorySourceName,
            string privateRepositoryUrl,
            string privateRepositoryUsername,
            string privateRepositoryPassword) : this(privateRepositoryProtocolVersion, privateRepositorySourceName, privateRepositoryUrl, privateRepositoryUsername, privateRepositoryPassword, true)
        {
        }

        private NuGetPackageManager(
            int privateRepositoryProtocolVersion, 
            string privateRepositorySourceName, 
            string privateRepositoryUrl, 
            string privateRepositoryUsername, 
            string privateRepositoryPassword,
            bool includePrivateRepository)
        {
            this.includePrivateRepository = includePrivateRepository;
            this.privateRepositoryProtocolVersion = privateRepositoryProtocolVersion;
            this.privateRepositorySourceName = privateRepositorySourceName;
            this.privateRepositoryUrl = privateRepositoryUrl;
            this.privateRepositoryUsername = privateRepositoryUsername;
            this.privateRepositoryPassword = privateRepositoryPassword;

            if (includePrivateRepository)
            {
                var privatePackageSource = this.ConstructPrivatePackageSource();
                var settingsManager = Settings.LoadDefaultSettings(null, null, null);
                var packageSourceProvider = new PackageSourceProvider(settingsManager, new[] { privatePackageSource });
                var customCredentialProvider = new CustomCredentialProvider(
                    this.privateRepositoryUrl,
                    this.privateRepositoryUsername,
                    this.privateRepositoryPassword);
                var credentialProvider = new SettingsCredentialProvider(customCredentialProvider, packageSourceProvider);

                HttpClient.DefaultCredentialProvider = credentialProvider;
            }

            this.sourceRepositories = this.ConstructSourceRepositories();
        }

        /// <summary>
        /// Gets the latest version of the package specified.
        /// </summary>
        /// <param name="packageId">Package ID.</param>
        /// <param name="includeUnlisted">Include unlisted packages when resolving ID.</param>
        /// <param name="includePreRelease">Include pre release packages when resolving ID.</param>
        /// <returns>String representation of latest version.</returns>
        public async Task<string> GetLatestVersionAsync(
            string packageId,
            bool includeUnlisted,
            bool includePreRelease)
        {
            var folderProject = new FolderNuGetProject(Path.GetTempPath());
            var resolutionContext = new ResolutionContext(
                DependencyBehavior.Ignore,
                // ReSharper disable once RedundantArgumentNameForLiteralExpression - Want to see what's going in here...
                includePrelease: includePreRelease,
                // ReSharper disable once RedundantArgumentNameForLiteralExpression - Want to see what's going in here...
                includeUnlisted: includeUnlisted,
                // ReSharper disable once RedundantArgumentName - Want to see what's going in here...
                versionConstraints: VersionConstraints.None);

            var version = await global::NuGet.PackageManagement.NuGetPackageManager.GetLatestVersionAsync(
                packageId,
                folderProject,
                resolutionContext,
                this.sourceRepositories,
                CancellationToken.None);

            return version.ToString();
        }

        /// <summary>
        /// Install a package (and optionally dependencies) into the specified path.
        /// </summary>
        /// <param name="packageId">Package ID.</param>
        /// <param name="packageVersion">Package version.</param>
        /// <param name="installPath">Path to download packages into.</param>
        /// <param name="includeDependencies">Include dependencies when downloading.</param>
        /// <param name="includeUnlisted">Include unlisted packages when resolving ID.</param>
        /// <param name="includePreRelease">Include pre release packages when resolving ID.</param>
        /// <returns>Task to make async.</returns>
        public async Task DownloadPackageToPathAsync(
            string packageId, 
            string packageVersion,
            string installPath,
            bool includeDependencies,
            bool includeUnlisted,
            bool includePreRelease)
        {
            var folderProject = new FolderNuGetProject(installPath);

            var dependencyBehavior = includeDependencies ? DependencyBehavior.Lowest : DependencyBehavior.Ignore;
            var resolutionContext = new ResolutionContext(
                dependencyBehavior,
                // ReSharper disable once RedundantArgumentNameForLiteralExpression - Want to see what's going in here...
                // ReSharper disable once RedundantArgumentName - Reads better IMO
                includePrelease: includePreRelease,
                // ReSharper disable once RedundantArgumentNameForLiteralExpression - Want to see what's going in here...
                // ReSharper disable once RedundantArgumentName - Reads better IMO
                includeUnlisted: includeUnlisted,
                // ReSharper disable once RedundantArgumentName - Want to see what's going in here...
                versionConstraints: VersionConstraints.None);

            var settings = global::NuGet.Configuration.Settings.LoadDefaultSettings(null, null, null);
            var repoProvider = new SourceRepositoryProvider(settings, new List<Lazy<INuGetResourceProvider>>());
            var packageManager = new global::NuGet.PackageManagement.NuGetPackageManager(repoProvider, settings, installPath, false);
            var projectContext = new EmptyNuGetProjectContext();
            await
                packageManager.InstallPackageAsync(
                    folderProject,
                    new PackageIdentity(packageId, NuGetVersion.Parse(packageVersion)),
                    resolutionContext,
                    projectContext,
                    this.sourceRepositories,
                    null,
                    CancellationToken.None);
        }

        private IReadOnlyCollection<SourceRepository> ConstructSourceRepositories()
        {
            var resourceProvidersV2 = global::NuGet.Protocol.Core.v2.FactoryExtensionsV2.GetCoreV2(null).ToList();
            var resourceProvidersV3 = global::NuGet.Protocol.Core.v3.FactoryExtensionsV2.GetCoreV3(null).ToList();

            var publicPackageSourceV2 = new global::NuGet.Configuration.PackageSource(
                NuGetConfigFile.NuGetPublicGalleryUrlV2,
                NuGetConfigFile.NuGetPublicGalleryNameV2);

            var publicPackageSourceV3 = new global::NuGet.Configuration.PackageSource(
                NuGetConfigFile.NuGetPublicGalleryUrlV3,
                NuGetConfigFile.NuGetPublicGalleryNameV3);

            var ret = new List<SourceRepository>();

            if (this.includePrivateRepository)
            {
                var privatePackageSource = this.ConstructPrivateConfigurationPackageSource();

                List<Lazy<INuGetResourceProvider>> resourceProviders;
                switch (privatePackageSource.ProtocolVersion)
                {
                    case 2:
                        resourceProviders = resourceProvidersV2;
                        break;
                    case 3:
                        resourceProviders = resourceProvidersV3;
                        break;
                    default:
                        throw new NotSupportedException(
                            "Version: " + privatePackageSource.ProtocolVersion + " is not currently supported.");
                }

                ret.Add(new SourceRepository(privatePackageSource,  resourceProviders));
            }

            ret.Add(new SourceRepository(publicPackageSourceV2, resourceProvidersV2));
            ret.Add(new SourceRepository(publicPackageSourceV3, resourceProvidersV3));

            return ret;
        }

        private global::NuGet.Configuration.PackageSource ConstructPrivateConfigurationPackageSource()
        {
            // WARNING - Changes here must be reflected in ConstructPrivatePackageSource (yes this is WRONG but necessary because the different namespaces are used in different places - file a bug with NuGet...)
            var privatePackageSource = !this.includePrivateRepository
                                           ? null
                                           : new global::NuGet.Configuration.PackageSource(
                                                 this.privateRepositoryUrl,
                                                 this.privateRepositorySourceName)
                                                 {
                                                     UserName = this.privateRepositoryUsername,
                                                     Password = this.privateRepositoryPassword,
                                                     IsPasswordClearText = true,
                                                     ProtocolVersion = this.privateRepositoryProtocolVersion,
                                                 };
            return privatePackageSource;
        }

        private PackageSource ConstructPrivatePackageSource()
        {
            // WARNING - Changes here must be reflected in ConstructPrivateConfigurationPackageSource (yes this is WRONG but necessary because the different namespaces are used in different places - file a bug with NuGet...)
            var privatePackageSource = !this.includePrivateRepository
                                           ? null
                                           : new PackageSource(
                                                 this.privateRepositoryUrl,
                                                 this.privateRepositorySourceName)
                                           {
                                               UserName = this.privateRepositoryUsername,
                                               Password = this.privateRepositoryPassword,
                                               IsPasswordClearText = true,
                                               ProtocolVersion = this.privateRepositoryProtocolVersion,
                                           };
            return privatePackageSource;
        }
    }

    /// <summary>
    /// Custom credential provider in an attempt to provide supplied credentials directly to NuGet.Core.
    /// </summary>
    public class CustomCredentialProvider : ICredentialProvider
    {
        private readonly string sourceUrl;

        private readonly string username;

        private readonly string password;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomCredentialProvider"/> class.
        /// </summary>
        /// <param name="sourceUrl">Source URL of a private repository.</param>
        /// <param name="username">Username of a private repository.</param>
        /// <param name="password">Password of a private repository.</param>
        public CustomCredentialProvider(string sourceUrl, string username, string password)
        {
            this.sourceUrl = sourceUrl;
            this.username = username;
            this.password = password;
        }

        /// <inheritdoc />
        public ICredentials GetCredentials(Uri uri, IWebProxy proxy, CredentialType credentialType, bool retrying)
        {
            if (this.sourceUrl == uri.OriginalString.TrimEnd('/'))
            {
                var networkCredential = new NetworkCredential(this.username, this.password);
                return networkCredential;
            }

            throw new NotSupportedException("Uri not supported: " + uri);
        }
    }

    /// <summary>
    /// Ephemeral class used to XML serialize a config file for NuGet.
    /// </summary>
    [Serializable]
    [XmlRoot("configuration")]
    public class NuGetConfigFile
    {
        /// <summary>
        /// Source name for the public NuGet gallery V2.
        /// </summary>
        public const string NuGetPublicGalleryNameV2 = "nuget.org v2";

        /// <summary>
        /// Source URL for the public NuGet gallery V2.
        /// </summary>
        public const string NuGetPublicGalleryUrlV2 = "https://www.nuget.org/api/v2/";

        /// <summary>
        /// Source name for the public NuGet gallery V3.
        /// </summary>
        public const string NuGetPublicGalleryNameV3 = "nuget.org v3";

        /// <summary>
        /// Source URL for the public NuGet gallery V3.
        /// </summary>
        public const string NuGetPublicGalleryUrlV3 = "https://api.nuget.org/v3/index.json";

        /// <summary>
        /// Serializes a supplied config into XML.
        /// </summary>
        /// <param name="config">Config to serialize.</param>
        /// <returns>XML representation of the supplied config.</returns>
        public static string Serialize(NuGetConfigFile config)
        {
            var serializer = new XmlSerializer(typeof(NuGetConfigFile));
            var stringBuilder = new StringBuilder();
            var writer = new StringWriter(stringBuilder);
            var ns = new XmlSerializerNamespaces();
            ns.Add(string.Empty, string.Empty);
            serializer.Serialize(writer, config, ns);
            var ret = stringBuilder.ToString();
            ret = ret.Replace(
                "packageSourceCredentialKeys",
                config.ActivePackageSource.Single(_ => _.Key != NuGetPublicGalleryNameV2).Key).Replace("utf-16", "utf-8");
            return ret;
        }

        /// <summary>
        /// Builds a NuGetConfigFile object from the repository config (to be serialized to disk).
        /// </summary>
        /// <param name="privateRepositorySourceName">Source name of a private repository.</param>
        /// <param name="privateRepositoryUrl">Source URL of a private repository.</param>
        /// <param name="privateRepositoryUsername">Username of a private repository.</param>
        /// <param name="privateRepositoryPassword">Password of a private repository.</param>
        /// <returns>NuGetConfigFile object(to be serialized to disk)</returns>
        public static NuGetConfigFile BuildConfigFileFromRepositoryConfiguration(
            string privateRepositorySourceName,
            string privateRepositoryUrl,
            string privateRepositoryUsername,
            string privateRepositoryPassword)
        {
            var packageSources = new[]
                                                    {
                                                        new AddKeyValue
                                                            {
                                                                Key = NuGetPublicGalleryNameV2,
                                                                Value = NuGetPublicGalleryUrlV2
                                                            },
                                                        new AddKeyValue
                                                            {
                                                                Key = NuGetPublicGalleryNameV3,
                                                                Value = NuGetPublicGalleryUrlV3
                                                            },
                                                        new AddKeyValue
                                                            {
                                                                Key = privateRepositorySourceName,
                                                                Value = privateRepositoryUrl
                                                            }
                                                    };
            var ret = new NuGetConfigFile
            {
                PackageSources = packageSources,
                ActivePackageSource = packageSources,
                PackageSourceCredentialContainer =
                    new PackageSourceCredentialContainer
                    {
                        PackageSourceCredentialKeys =
                            new[]
                                                  {
                                                      new AddKeyValue
                                                          {
                                                              Key = "Username",
                                                              Value = privateRepositoryUsername
                                                          },
                                                      new AddKeyValue
                                                          {
                                                              Key = "ClearTextPassword",
                                                              Value = privateRepositoryPassword
                                                          },
                                                      new AddKeyValue
                                                          {
                                                              Key = "Password",
                                                              Value = string.Empty
                                                          }
                                                  }
                    }
            };
            return ret;
        }

        /// <summary>
        /// Gets or sets the list of active packages sources.
        /// </summary>
        [XmlArray("activePackageSource")]
        [XmlArrayItem("add")]
        public AddKeyValue[] ActivePackageSource { get; set; }

        /// <summary>
        /// Gets or sets the list of all packages sources.
        /// </summary>
        [XmlArray("packageSources")]
        [XmlArrayItem("add")]
        public AddKeyValue[] PackageSources { get; set; }

        /// <summary>
        /// Gets or sets the credentials.
        /// </summary>
        [XmlElement("packageSourceCredentials")]
        public PackageSourceCredentialContainer PackageSourceCredentialContainer { get; set; }
    }

    /// <summary>
    /// Ephemeral class used to XML serialize a config file for NuGet.
    /// </summary>
    [Serializable]
    [XmlRoot("add")]
    public class AddKeyValue
    {
        /// <summary>
        /// Gets or sets the key value.
        /// </summary>
        [XmlAttribute("key")]
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the value value.
        /// </summary>
        [XmlAttribute("value")]
        public string Value { get; set; }
    }

    /// <summary>
    /// Ephemeral class used to XML serialize a config file for NuGet.
    /// </summary>
    [Serializable]
    [XmlRoot("packageSourceCredentials")]
    public class PackageSourceCredentialContainer
    {
        /// <summary>
        /// Gets or sets the credentials.
        /// </summary>
        [XmlArray("packageSourceCredentialKeys")]
        [XmlArrayItem("add")]
        public AddKeyValue[] PackageSourceCredentialKeys { get; set; }
    }
}
