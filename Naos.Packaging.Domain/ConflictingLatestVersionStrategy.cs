// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConflictingLatestVersionStrategy.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.Domain
{
    /// <summary>
    /// Determines what to do when the configured package repositories report
    /// different versions as the latest version of a package.
    /// </summary>
    public enum ConflictingLatestVersionStrategy
    {
        /// <summary>
        /// Use the highest version number found across package repositories.
        /// </summary>
        UseHighestVersion,

        /// <summary>
        /// Throw an exception
        /// </summary>
        ThrowException,
    }
}
