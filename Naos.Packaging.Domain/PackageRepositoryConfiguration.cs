// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageRepositoryConfiguration.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.Domain
{
    using System;
    using System.Collections.Generic;
    using OBeautifulCode.Math.Recipes;

    /// <summary>
    /// Model object to provide details on the repository.
    /// </summary>
    public class PackageRepositoryConfiguration : IEquatable<PackageRepositoryConfiguration>
    {
        /// <summary>
        /// Gets the package repository configuration for the v2 nuget public gallery.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Nuget", Justification = "Spelling/name is correct.")]
        public static PackageRepositoryConfiguration NugetOrgV2 => new PackageRepositoryConfiguration
        {
            ProtocolVersion = 2,
            SourceName = "nugetv2",
            Source = "https://www.nuget.org/api/v2/",
        };

        /// <summary>
        /// Gets the package repository configuration for the v3 nuget public gallery.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Nuget", Justification = "Spelling/name is correct.")]
        public static PackageRepositoryConfiguration NugetOrgV3 => new PackageRepositoryConfiguration
        {
            ProtocolVersion = 3,
            SourceName = "nugetv3",
            Source = "https://api.nuget.org/v3/index.json",
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

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="first">First parameter.</param>
        /// <param name="second">Second parameter.</param>
        /// <returns>A value indicating whether or not the two items are equal.</returns>
        public static bool operator ==(PackageRepositoryConfiguration first, PackageRepositoryConfiguration second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }

            if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
            {
                return false;
            }

            return string.Equals(first.Source, second.Source, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(first.SourceName, second.SourceName, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(first.UserName, second.UserName, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(first.ClearTextPassword, second.ClearTextPassword, StringComparison.OrdinalIgnoreCase) &&
                   first.ProtocolVersion == second.ProtocolVersion;
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        /// <param name="first">First parameter.</param>
        /// <param name="second">Second parameter.</param>
        /// <returns>A value indicating whether or not the two items are inequal.</returns>
        public static bool operator !=(PackageRepositoryConfiguration first, PackageRepositoryConfiguration second) => !(first == second);

        /// <inheritdoc />
        public bool Equals(PackageRepositoryConfiguration other) => this == other;

        /// <inheritdoc />
        public override bool Equals(object obj) => this == (obj as PackageRepositoryConfiguration);

        /// <inheritdoc />
        public override int GetHashCode() => HashCodeHelper.Initialize().Hash(this.Source?.ToUpperInvariant()).Hash(this.SourceName?.ToUpperInvariant()).Hash(this.UserName?.ToUpperInvariant()).Hash(this.ClearTextPassword).Hash(this.ProtocolVersion).Value;
    }
}
