// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageRepositoryConfiguration.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.Domain
{
    /// <summary>
    /// Model object to provide details on the repository.
    /// </summary>
    public class PackageRepositoryConfiguration
    {
        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the name of the package source.
        /// </summary>
        public string SourceName { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string ClearTextPassword { get; set; }

        /// <summary>
        /// Gets or sets the version of the protocol for this repository.
        /// </summary>
        public int ProtocolVersion { get; set; }
    }
}
