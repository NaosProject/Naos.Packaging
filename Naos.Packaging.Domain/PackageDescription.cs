// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageDescription.cs" company="Naos">
//    Copyright (c) Naos 2017. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.Domain
{
    using System;

    using OBeautifulCode.Math.Recipes;

    using static System.FormattableString;

    /// <summary>
    /// Model object of a packaged piece of software.
    /// </summary>
    public class PackageDescription : IEquatable<PackageDescription>
    {
        /// <summary>
        /// The package id for a null package that will run through without any interaction.
        /// </summary>
        public const string NullPackageId = "NullPackage";

        /// <summary>
        /// Gets or sets the ID of the package.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the version of the package.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets the package description as a string in form: [ID].[Version].
        /// </summary>
        /// <returns>String version of package description in form: [ID].[Version].</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Want a method.")]
        public string GetIdDotVersionString()
        {
            var ret = Invariant($"{this.Id}.{(string.IsNullOrEmpty(this.Version) ? "[UnspecifiedVersion]" : this.Version)}");
            return ret;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var result = this.GetIdDotVersionString();
            return result;
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="first">First parameter.</param>
        /// <param name="second">Second parameter.</param>
        /// <returns>A value indicating whether or not the two items are equal.</returns>
        public static bool operator ==(PackageDescription first, PackageDescription second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }

            if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
            {
                return false;
            }

            return string.Equals(first.Id, second.Id, StringComparison.OrdinalIgnoreCase) && string.Equals(first.Version, second.Version, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        /// <param name="first">First parameter.</param>
        /// <param name="second">Second parameter.</param>
        /// <returns>A value indicating whether or not the two items are inequal.</returns>
        public static bool operator !=(PackageDescription first, PackageDescription second) => !(first == second);

        /// <inheritdoc />
        public bool Equals(PackageDescription other) => this == other;

        /// <inheritdoc />
        public override bool Equals(object obj) => this == (obj as PackageDescription);

        /// <inheritdoc />
        public override int GetHashCode() => HashCodeHelper.Initialize().Hash(this.Id.ToUpperInvariant()).Hash(this.Version).Value;
    }
}
