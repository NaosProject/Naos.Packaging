// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Package.cs" company="Naos">
//    Copyright (c) Naos 2017. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.Domain
{
    using System;

    /// <summary>
    /// A full package, description and the file bytes as of a date and time.
    /// </summary>
    public class Package
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
    }
}
