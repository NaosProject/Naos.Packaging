// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Package.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.Domain
{
    using System;
    using System.Linq;
    using OBeautifulCode.Math.Recipes;

    /// <summary>
    /// A full package, description and the file bytes as of a date and time.
    /// </summary>
    public class Package : IEquatable<Package>
    {
        /// <summary>
        /// Gets or sets the description of the package.
        /// </summary>
        public PackageDescription PackageDescription { get; set; }

        /// <summary>
        /// Gets or sets the bytes of the package file at specified date and time.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Want it this way.")]
        public byte[] PackageFileBytes { get; set; }

        /// <summary>
        /// Gets or sets the date and time UTC that the package file bytes were retrieved.
        /// </summary>
        public DateTime PackageFileBytesRetrievalDateTimeUtc { get; set; }

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="first">First parameter.</param>
        /// <param name="second">Second parameter.</param>
        /// <returns>A value indicating whether or not the two items are equal.</returns>
        public static bool operator ==(Package first, Package second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }

            if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
            {
                return false;
            }

            return first.PackageDescription == second.PackageDescription &&
                   (bool)(first.PackageFileBytes?.SequenceEqual(second.PackageFileBytes) ?? (second.PackageFileBytes == null ? (bool?)true : false)) && first.PackageFileBytesRetrievalDateTimeUtc ==
                   second.PackageFileBytesRetrievalDateTimeUtc;
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        /// <param name="first">First parameter.</param>
        /// <param name="second">Second parameter.</param>
        /// <returns>A value indicating whether or not the two items are inequal.</returns>
        public static bool operator !=(Package first, Package second) => !(first == second);

        /// <inheritdoc />
        public bool Equals(Package other) => this == other;

        /// <inheritdoc />
        public override bool Equals(object obj) => this == (obj as Package);

        /// <inheritdoc />
        public override int GetHashCode() => HashCodeHelper.Initialize().Hash(this.PackageDescription).Hash(this.PackageFileBytes).Hash(this.PackageFileBytesRetrievalDateTimeUtc).Value;
    }
}
