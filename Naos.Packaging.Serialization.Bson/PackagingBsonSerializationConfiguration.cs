// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackagingBsonSerializationConfiguration.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.Serialization.Bson
{
    using System;
    using System.Collections.Generic;
    using Naos.Packaging.Domain;
    using OBeautifulCode.Serialization.Bson;

    /// <summary>
    /// Implementation for the <see cref="Naos.Packaging" /> domain.
    /// </summary>
    public class PackagingBsonSerializationConfiguration : BsonSerializationConfigurationBase
    {
        /// <inheritdoc />
        protected override IReadOnlyCollection<string> TypeToRegisterNamespacePrefixFilters => new[]
                                                                                               {
                                                                                                   typeof(Package).Namespace,
                                                                                               };

        /// <inheritdoc />
        protected override IReadOnlyCollection<TypeToRegisterForBson> TypesToRegisterForBson => new[]
                                                                                                {
                                                                                                    typeof(Package).ToTypeToRegisterForBson(),
                                                                                                    typeof(PackageDescription).ToTypeToRegisterForBson(),
                                                                                                    typeof(PackageRepositoryConfiguration).ToTypeToRegisterForBson(),
                                                                                                };
    }
}
