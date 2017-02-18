﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageDescription.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.Domain
{
    /// <summary>
    /// Model object of a packaged piece of software.
    /// </summary>
    public class PackageDescription
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
        public string GetIdDotVersionString()
        {
            var ret = string.Format(
                "{0}.{1}",
                this.Id,
                string.IsNullOrEmpty(this.Version) ? "[UnspecifiedVersion]" : this.Version);
            return ret;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var result = this.GetIdDotVersionString();
            return result;
        }
    }
}
