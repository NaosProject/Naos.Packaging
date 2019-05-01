// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IRemovePackages.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.Domain
{
    /// <summary>
    /// Methods to delete or unlist packages.
    /// </summary>
    public interface IRemovePackages
    {
        /// <summary>
        /// Deletes a package.
        /// </summary>
        /// <param name="packageDescription">Description of package to delete.</param>
        /// <param name="packageRepositorySourceName">The source name of the package repository to delete from.</param>
        /// <param name="apiKey">The API key to use.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "api", Justification = "Spelling/name is correct.")]
        void DeletePackage(
            PackageDescription packageDescription,
            string packageRepositorySourceName,
            string apiKey);

        /// <summary>
        /// Deletes all versions of a package.
        /// </summary>
        /// <param name="packageId">The identifier of the package to delete for all versions.</param>
        /// <param name="packageRepositorySourceName">The source name of the package repository to delete from.</param>
        /// <param name="apiKey">The API key to use.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "api", Justification = "Spelling/name is correct.")]
        void DeleteAllVersionsOfPackage(
            string packageId,
            string packageRepositorySourceName,
            string apiKey);
    }
}
