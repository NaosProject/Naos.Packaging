// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageRepositoryConfiguration.cs" company="Naos">
//    Copyright (c) Naos 2017. All rights reserved.
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Nuget", Justification = "Spelling/name is correct.")]
        public static PackageRepositoryConfiguration NugetOrgV2 => new PackageRepositoryConfiguration
        {
            ProtocolVersion = 2,
            SourceName = "nugetv2",
            Source = "https://www.nuget.org/api/v2/"
        };

        /// <summary>
        /// Gets the package repository configuration for the v3 nuget public gallery.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Nuget", Justification = "Spelling/name is correct.")]
        public static PackageRepositoryConfiguration NugetOrgV3 => new PackageRepositoryConfiguration
        {
            ProtocolVersion = 3,
            SourceName = "nugetv3",
            Source = "https://api.nuget.org/v3/index.json"
        };

        /// <summary>
        /// Gets the package repository configurations for the nuget public gallery.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Nuget", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Configs", Justification = "Spelling/name is correct.")]
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
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string ClearTextPassword { get; set; }

        /// <summary>
        /// Gets or sets the version of the protocol for this repository.
        /// </summary>
        public int? ProtocolVersion { get; set; }
    }
}
