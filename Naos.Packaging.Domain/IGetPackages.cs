// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IGetPackages.cs" company="Naos">
//   Copyright 2015 Naos
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
        /// <returns>
        /// A description of the latest version of the package id specified.
        /// </returns>
        PackageDescription GetLatestVersion(
            string packageId,
            bool includePrerelease = true,
            bool includeDelisted = false,
            string packageRepositorySourceName = null);

        /// <summary>
        /// Downloads the specified packages.
        /// </summary>
        /// <param name="packageDescriptions">Description of packages to download.</param>
        /// <param name="workingDirectory">Directory to download and decompress package to.</param>
        /// <param name="includeDependencies">Include dependencies when downloading (default is FALSE).</param>
        /// <returns>Full paths to the files that were downloaded.</returns>
        ICollection<string> DownloadPackages(
            ICollection<PackageDescription> packageDescriptions,
            string workingDirectory,
            bool includeDependencies = false);

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
        string GetVersionFromNuSpecFile(
            string nuSpecFileContents);
    }
}
