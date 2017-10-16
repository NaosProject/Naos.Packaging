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
        /// Unlists a package.
        /// </summary>
        /// <param name="packageDescription">Description of package to unlist.</param>
        void UnlistPackage(PackageDescription packageDescription);

        /// <summary>
        /// Unlists all versions of a package.
        /// </summary>
        /// <param name="id">The identifier of the package to unlist for all versions.</param>
        void UnlistAllVersionsOfPackage(string id);

        /// <summary>
        /// Deletes a package.
        /// </summary>
        /// <param name="packageDescription">Description of package to delete.</param>
        void DeletePackage(PackageDescription packageDescription);

        /// <summary>
        /// Deletes all versions of a package.
        /// </summary>
        /// <param name="id">The identifier of the package to delete for all versions.</param>
        void DeleteAllVersionsOfPackage(string id);
    }
}
