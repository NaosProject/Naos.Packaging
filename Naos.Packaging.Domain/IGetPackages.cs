// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IGetPackages.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.Domain
{
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Methods to interface into the package repository.
    /// </summary>
    public interface IGetPackages
    {
        /// <summary>
        /// Gets the latest version of a package id.
        /// </summary>
        /// <param name="packageId">Package id to get version of.</param>
        /// <param name="includePrerelease">Optional.  Include pre-release packages?  Default is true.</param>
        /// <param name="includeDelisted">Optional.  Include delisted packages?  Default is false.</param>
        /// <param name="packageRepositorySourceName">Optional.  The source name of the package repository to query.  Default is null - all configured reposistories will be queried.</param>
        /// <param name="conflictingLatestVersionStrategy">Optional. Determines what to do when the configured package repositories report different versions as the latest version of a package.   Default is to use the highest number found across package repositories.</param>
        /// <returns>
        /// A description of the latest version of the package id specified.
        /// </returns>
        PackageDescription GetLatestVersion(
            string packageId,
            bool includePrerelease = true,
            bool includeDelisted = false,
            string packageRepositorySourceName = null,
            ConflictingLatestVersionStrategy conflictingLatestVersionStrategy = ConflictingLatestVersionStrategy.UseHighestVersion);

        /// <summary>
        /// Gets all versions of the package id.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="includePrerelease">Optional.  Include pre-release packages?  Default is true.</param>
        /// <param name="includeDelisted">Optional.  Include delisted packages?  Default is false.</param>
        /// <param name="packageRepositorySourceName">Optional.  The source name of the package repository to query.  Default is null - all configured reposistories will be queried.</param>
        /// <returns>
        /// Descriptions of all versions of the package id specified.
        /// </returns>
        IReadOnlyCollection<PackageDescription> GetAllVersions(
            string packageId,
            bool includePrerelease = true,
            bool includeDelisted = false,
            string packageRepositorySourceName = null);

        /// <summary>
        /// Downloads the specified packages.
        /// </summary>
        /// <param name="packageDescriptions">Description of packages to download.</param>
        /// <param name="workingDirectory">Directory to download and decompress package to.</param>
        /// <param name="includeDependencies">Optional.  Include dependencies when downloading.  Default is false.</param>
        /// <param name="includePrerelease">Optional.  Include pre-release packages?  Default is true.</param>
        /// <param name="includeDelisted">Optional.  Include delisted packages?  Default is false.</param>
        /// <param name="packageRepositorySourceName">Optional.  The source name of the package repository to query.  Default is null - all configured reposistories will be queried.</param>
        /// <param name="conflictingLatestVersionStrategy">Optional. Determines what to do when the configured package repositories report different versions as the latest version of a package.   Default is to use the highest number found across package repositories.</param>
        /// <returns>Full paths to the files that were downloaded.</returns>
        ICollection<string> DownloadPackages(
            ICollection<PackageDescription> packageDescriptions,
            string workingDirectory,
            bool includeDependencies = false,
            bool includePrerelease = true,
            bool includeDelisted = false,
            string packageRepositorySourceName = null,
            ConflictingLatestVersionStrategy conflictingLatestVersionStrategy = ConflictingLatestVersionStrategy.UseHighestVersion);

        /// <summary>
        /// Gets package file for a package description.
        /// </summary>
        /// <param name="packageDescription">Package description to get file for.</param>
        /// <returns>Package (description and file).</returns>
        Package GetPackage(
            PackageDescription packageDescription);

        /// <summary>
        /// Gets the bytes of the package file for a package description (as of time of execution).
        /// </summary>
        /// <param name="packageDescription">Package description to get file for.</param>
        /// <returns>Bytes of package file.</returns>
        byte[] GetPackageFile(
            PackageDescription packageDescription);

        /// <summary>
        /// Gets the contents of a file (as a string) matching the search pattern for the package in question (will decompress and search through the contents of the package).
        /// </summary>
        /// <param name="package">Package to find the file(s) in.</param>
        /// <param name="searchPattern">Infix pattern to use for searching for files.</param>
        /// <param name="encoding">Optional encoding to use (UTF-8 [no BOM] is default).</param>
        /// <returns>Dictionary of file name and contents of the file found as a string.</returns>
        IDictionary<string, string> GetMultipleFileContentsFromPackageAsStrings(
            Package package,
            string searchPattern,
            Encoding encoding = null);

        /// <summary>
        /// Gets the contents of a file (as a string) matching the search pattern for the package in question (will decompress and search through the contents of the package).
        /// </summary>
        /// <param name="package">Package to find the file(s) in.</param>
        /// <param name="searchPattern">Infix pattern to use for searching for files.</param>
        /// <returns>Dictionary of file name and contents of the file found as a byte array.</returns>
        IDictionary<string, byte[]> GetMultipleFileContentsFromPackageAsBytes(
            Package package,
            string searchPattern);

        /// <summary>
        /// Gets the version of a package by reading it's NuSpec file.
        /// </summary>
        /// <param name="nuSpecFileContents">Contents of the NuSpec file to read from.</param>
        /// <returns>Version of the package as declared in the NuSpec file.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Nu", Justification = "Spelling/name is correct.")]
#pragma warning disable SA1305 // Field names should not use Hungarian notation - NuSpec is the domain name.
        string GetVersionFromNuSpecFile(string nuSpecFileContents);
#pragma warning restore SA1305 // Field names should not use Hungarian notation
    }
}
