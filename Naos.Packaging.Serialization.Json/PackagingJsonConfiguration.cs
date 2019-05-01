// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackagingJsonConfiguration.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.Serialization.Json
{
    using System;
    using System.Collections.Generic;
    using Naos.Packaging.Domain;
    using Naos.Serialization.Json;

    /// <summary>
    /// Implementation for the <see cref="Naos.Packaging" /> domain.
    /// </summary>
    public class PackagingJsonConfiguration : JsonConfigurationBase
    {
        /// <inheritdoc />
        protected override IReadOnlyCollection<Type> TypesToAutoRegister => new[] { typeof(Package), typeof(PackageDescription), typeof(PackageRepositoryConfiguration) };
    }
}
