// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackagingBsonConfiguration.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.Serialization.Bson
{
    using System;
    using System.Collections.Generic;
    using Naos.Packaging.Domain;
    using Naos.Serialization.Bson;

    /// <summary>
    /// Implementation for the <see cref="Naos.Packaging" /> domain.
    /// </summary>
    public class PackagingBsonConfiguration : BsonConfigurationBase
    {
        /// <inheritdoc />
        protected override IReadOnlyCollection<Type> ClassTypesToRegister => new[] { typeof(Package), typeof(PackageDescription), typeof(PackageRepositoryConfiguration) };
    }
}
