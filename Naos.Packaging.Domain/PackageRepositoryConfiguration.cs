// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageRepositoryConfiguration.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.Domain
{
    using System.Collections.Generic;

    /// <summary>
    /// Model object to provide details on the repository.
    /// </summary>
    public class PackageRepositoryConfiguration
    {
        /// <summary>
        /// Gets the package repository configuration for the v2 nuget public gallery.
        /// </summary>
        public static PackageRepositoryConfiguration NugetOrgV2 => new PackageRepositoryConfiguration
        {
            ProtocolVersion = 2,
            SourceName = "nugetv2",
            Source = "https://www.nuget.org/api/v2/"
        };

        /// <summary>
        /// Gets the package repository configuration for the v3 nuget public gallery.
        /// </summary>
        public static PackageRepositoryConfiguration NugetOrgV3 => new PackageRepositoryConfiguration
        {
            ProtocolVersion = 3,
            SourceName = "nugetv3",
            Source = "https://api.nuget.org/v3/index.json"
        };

        /// <summary>
        /// Gets the package repository configurations for the nuget public gallery.
        /// </summary>
        public static IReadOnlyCollection<PackageRepositoryConfiguration> AllNugetOrgConfigs => new[] { NugetOrgV2, NugetOrgV3 };

        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the name of the package source.
        /// </summary>
        public string SourceName { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string ClearTextPassword { get; set; }

        /// <summary>
        /// Gets or sets the version of the protocol for this repository.
        /// </summary>
        public int ProtocolVersion { get; set; }
    }
}
