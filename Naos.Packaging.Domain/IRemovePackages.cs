// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IRemovePackages.cs" company="Naos">
//   Copyright 2015 Naos
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
        void DeletePackage(
            PackageDescription packageDescription,
            string packageRepositorySourceName,
            string apiKey);

        /// <summary>
        /// Deletes all versions of a package.
        /// </summary>
        /// <param name="id">The identifier of the package to delete for all versions.</param>
        /// <param name="packageRepositorySourceName">The source name of the package repository to delete from.</param>
        /// <param name="apiKey">The API key to use.</param>
        void DeleteAllVersionsOfPackage(
            string id,
            string packageRepositorySourceName,
            string apiKey);
    }
}
